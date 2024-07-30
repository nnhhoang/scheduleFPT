using data_manipulation_visualisation;
using Google.OrTools.Sat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solvers
{
    public class FBTCPSAT
    {
        private CpModel problem;
        private CpSolver solver;

        private DataModel dataModel;
        private SolverConfiguration solverConfiguration;

        private Dictionary<(int, int), BoolVar> taskAssign;
        private List<BoolVar> taskAssignFlat;
        private Dictionary<(int, int), BoolVar> instructorDayStatus;
        private Dictionary<(int, int, int), BoolVar> instructorTimeStatus;
        private Dictionary<(int, int), BoolVar> instructorSubjectStatus;
        private Dictionary<(int, int, int), BoolVar> instructorSegmentStatus;
        private Dictionary<(int, int, int), BoolVar> instructorPatternStatus;
        private Dictionary<(int, int), LinearExpr> assignProduct;

        private List<List<(int, int)>>? solutions;

        public FBTCPSAT(
            DataModel dataModel,
            SolverConfiguration solverConfiguration
        )
        {
            problem = new CpModel();
            solver = new CpSolver();
            this.solverConfiguration = solverConfiguration;
            this.dataModel = dataModel;
            taskAssign = new Dictionary<(int, int), BoolVar>();
            instructorDayStatus = new Dictionary<(int, int), BoolVar>();
            instructorTimeStatus = new Dictionary<(int, int, int), BoolVar>();
            instructorSubjectStatus = new Dictionary<(int, int), BoolVar>();
            instructorSegmentStatus = new Dictionary<(int, int, int), BoolVar>();
            instructorPatternStatus = new Dictionary<(int, int, int), BoolVar>();
            assignProduct = new Dictionary<(int, int), LinearExpr>();
            taskAssignFlat = new List<BoolVar>();
        }

        public void CreateModel()
        {
            Helper.CalculateExecutionTime(AddDecisionVariable, "AddDecisionVariable");
            Helper.CalculateExecutionTime(AddTaskInstructorConstraint, "AddTaskInstructorConstraint");
            Helper.CalculateExecutionTime(AddNoSlotConflictConstraint, "AddNoSlotConflictConstraint");
            Helper.CalculateExecutionTime(AddPreassignConstraint, "AddPreassignConstraint");
            Helper.CalculateExecutionTime(AddAbilityConstraint, "AddAbilityConstraint");
            Helper.CalculateExecutionTime(AddQuotaConstraint, "AddQuotaConstraint");
        }

        public void SetupSolver()
        {
            solver.StringParameters
                 += "linearization_level: 0;"
                 + $"subsolvers:\"no_lp\";"
                 + $"max_time_in_seconds: {solverConfiguration.TimeLimit};"
                 //+ $"enumerate_all_solutions: {solverConfiguration.Mode != SolverConfiguration.SolveMode.ConstraintOnly};"
                 + $"log_search_progress: {solverConfiguration.LogSearchProgress};"
                 + $"num_workers: 12";

            //var solverCallback = new FBTSolutionCallback(
            //    taskAssignFlat.ToArray(),
            //    solverConfiguration.SolutionLimit
            //);
        }

        #region Solve
        public int FindBackupInstructor()
        {
            dataModel.NumBackupInstructors = dataModel.NumTasks;
            dataModel.SetRange();
            CreateModel();

            var sumList = new List<ILiteral>();
            foreach (int t in dataModel.AllTasks)
            {
                foreach (int i in dataModel.AllInstructors)
                {
                    sumList.Add(taskAssign[(t, i)]);
                }
            }

            problem.Minimize(
                Helper.OrtoolsHelper.CreateDelta(
                    problem,
                    dataModel.NumTasks,
                    LinearExpr.Sum(sumList),
                    dataModel.NumTasks
                )
            );

            var status = solver.Solve(problem);

            if (
                status == CpSolverStatus.Optimal
                ||
                status == CpSolverStatus.Feasible
            )
            {
                return (int)solver.ObjectiveValue;
            }
            return dataModel.NumTasks;
        }
        public List<List<(int, int)>>? IsSatisfied()
        {
            dataModel.SetRange();
            CreateModel();

            var sumList = new List<ILiteral>();
            foreach (int t in dataModel.AllTasks)
            {
                foreach (int i in dataModel.AllInstructors)
                {
                    sumList.Add(taskAssign[(t, i)]);
                }
            }

            problem.Minimize(
                Helper.OrtoolsHelper.CreateDelta(
                    problem,
                    dataModel.NumTasks,
                    LinearExpr.Sum(sumList),
                    dataModel.NumTasks
                )
            );

            var status = solver.Solve(problem);
            if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
            {
                return Helper.OrtoolsHelper.GetResults(
                    solver,
                    dataModel,
                    taskAssign
                );
            }
            return null;
        }
        public List<List<(int, int)>>? ObjectiveOptimize()
        {
            dataModel.SetRange();
            CreateModel();

            #region Add objective to model
            var totalDeltas = new List<LinearExpr>();

            // MINIMIZE DAY (numInstructors * numDays)
            if (dataModel.ObjectiveOptions[0] > 0)
            {
                var literals = new List<ILiteral>();
                foreach (int i in dataModel.AllInstructors)
                {
                    foreach (int d in dataModel.AllDays)
                    {
                        foreach (int t in dataModel.AllTasks)
                        {
                            if (
                                dataModel.InstructorSlot[
                                    i,
                                    dataModel.TaskSlotMapping[t]
                                ]
                                == 1
                                &&
                                dataModel.SlotDay[
                                    dataModel.TaskSlotMapping[t],
                                    d
                                ]
                                == 1
                            )
                            {
                                literals.Add(taskAssign[(t, i)]);
                            }
                        }

                        if (literals.Count > 0)
                        {
                            instructorDayStatus[(i, d)] = problem.NewBoolVar($"IDS[{i}][{d}]");
                            problem.Add(LinearExpr.Sum(literals) > 0).OnlyEnforceIf(instructorDayStatus[(i, d)]);
                            problem.Add(LinearExpr.Sum(literals) == 0).OnlyEnforceIf(instructorDayStatus[(i, d)].Not());
                        }
                        literals.Clear();
                    }
                }


                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(dataModel.ObjectiveWeight[0] * AddTeachingDayObjective());
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[0]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                dataModel.NumDays * dataModel.NumInstructors,
                                AddTeachingDayObjective(),
                                0
                            )
                        );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[0]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddTeachingDayObjective(),
                                0
                            )
                        );
                        break;
                }
            }

            // MINIMIZE TIME (numInstructors * numDays * numTimes)
            if (dataModel.ObjectiveOptions[1] > 0)
            {
                var literals = new List<ILiteral>();
                foreach (int i in dataModel.AllInstructors)
                {
                    foreach (int d in dataModel.AllDays)
                    {
                        foreach (int ti in dataModel.AllTimes)
                        {
                            foreach (int t in dataModel.AllTasks)
                            {
                                if (
                                    dataModel.InstructorSlot[
                                        i,
                                        dataModel.TaskSlotMapping[t]
                                    ]
                                    == 1
                                    &&
                                    dataModel.SlotDay[
                                        dataModel.TaskSlotMapping[t],
                                        d
                                    ]
                                    == 1
                                    &&
                                    dataModel.SlotTime[
                                        dataModel.TaskSlotMapping[t],
                                        ti
                                    ]
                                    == 1
                                )
                                {
                                    literals.Add(taskAssign[(t, i)]);
                                }
                            }

                            if (literals.Count > 0)
                            {
                                instructorTimeStatus[(i, d, ti)] = problem.NewBoolVar($"ITS[{i}][{d}][{ti}]");
                                problem.Add(LinearExpr.Sum(literals) > 0).OnlyEnforceIf(instructorTimeStatus[(i, d, ti)]);
                                problem.Add(LinearExpr.Sum(literals) == 0).OnlyEnforceIf(instructorTimeStatus[(i, d, ti)].Not());
                            }
                            literals.Clear();
                        }
                    }

                }

                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(dataModel.ObjectiveWeight[1] * AddTeachingTimeObjective());
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[1]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                dataModel.NumTimes
                                    * dataModel.NumDays
                                    * dataModel.NumInstructors,
                                AddTeachingTimeObjective(),
                                0
                            )
                        );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[1]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddTeachingTimeObjective(),
                                0
                            )
                        );
                        break;
                }
            }

            // MINIMIZE PATTERN COST (numInstructors * numDays * (numSegments + 2^num Segments)
            if (dataModel.ObjectiveOptions[2] > 0)
            {
                var firstLiterals = new List<ILiteral>();
                var secondLiterals = new List<ILiteral>();
                foreach (int i in dataModel.AllInstructors)
                {
                    foreach (int d in dataModel.AllDays)
                    {
                        foreach (int sm in dataModel.AllSegments)
                        {
                            foreach (int t in dataModel.AllTasks)
                            {
                                if (
                                    dataModel.InstructorSlot[
                                        i,
                                        dataModel.TaskSlotMapping[t]
                                    ]
                                    == 1
                                    &&
                                    dataModel.SlotSegment[
                                        dataModel.TaskSlotMapping[t],
                                        d,
                                        sm
                                    ]
                                    == 1
                                )
                                {
                                    firstLiterals.Add(taskAssign[(t, i)]);
                                }
                            }

                            instructorSegmentStatus[(i, d, sm)] = problem.NewBoolVar($"ISS[{i}][{d}][{sm}]");
                            if (firstLiterals.Count == 0)
                            {
                                //problem.AddHint(instructorSegmentStatus[(i, d, sm)], 0);
                            }
                            problem.Add(LinearExpr.Sum(firstLiterals) > 0).OnlyEnforceIf(instructorSegmentStatus[(i, d, sm)]);
                            problem.Add(LinearExpr.Sum(firstLiterals) == 0).OnlyEnforceIf(instructorSegmentStatus[(i, d, sm)].Not());
                            firstLiterals.Clear();
                        }

                        for (int p = 0; p < (1 << dataModel.NumSegments); p++)
                        {
                            foreach (int sm in dataModel.AllSegments)
                            {
                                if ((p & (1 << (dataModel.NumSegments - sm - 1))) > 0)
                                {
                                    secondLiterals.Add(Helper.OrtoolsHelper.BoolState(instructorSegmentStatus[(i, d, sm)], true));
                                }
                                else
                                {
                                    secondLiterals.Add(Helper.OrtoolsHelper.BoolState(instructorSegmentStatus[(i, d, sm)], false));
                                }
                            }

                            instructorPatternStatus[(i, d, p)] = problem.NewBoolVar($"IPS[{i}][{d}[{p}]");
                            problem.Add(LinearExpr.Sum(secondLiterals) == dataModel.NumSegments).OnlyEnforceIf(instructorPatternStatus[(i, d, p)]);
                            problem.Add(LinearExpr.Sum(firstLiterals) != dataModel.NumSegments).OnlyEnforceIf(instructorPatternStatus[(i, d, p)].Not());
                            secondLiterals.Clear();
                        }

                    }
                }

                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(dataModel.ObjectiveWeight[2] * AddPatternCostObjective());
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[2]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                (1 << dataModel.NumSegments)
                                * dataModel.NumDays
                                * dataModel.NumInstructors
                                * dataModel.NumSegments,
                                AddPatternCostObjective(),
                                0
                            )
                        );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[2]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddPatternCostObjective(),
                                0
                            )
                        );
                        break;
                }
            }

            // MINIMIZE SUBJECT DIVERSITY (numInstructor * numSubject)
            if (dataModel.ObjectiveOptions[3] > 0)
            {
                var literals = new List<ILiteral>();
                foreach (int i in dataModel.AllInstructors)
                {
                    foreach (int s in dataModel.AllSubjects)
                    {
                        foreach (int t in dataModel.AllTasks)
                        {
                            if (dataModel.TaskSubjectMapping[t] == s)
                            {
                                literals.Add(taskAssign[(t, i)]);
                            }
                        }

                        instructorSubjectStatus[(i, s)] = problem.NewBoolVar($"ISUS[{i}][{s}]");
                        if (literals.Count == 0)
                        {
                            //problem.AddHint(instructorSubjectStatus[(i, s)], 0);
                        }
                        problem.Add(LinearExpr.Sum(literals) > 0).OnlyEnforceIf(instructorSubjectStatus[(i, s)]);
                        problem.Add(LinearExpr.Sum(literals) == 0).OnlyEnforceIf(instructorSubjectStatus[(i, s)].Not());
                        literals.Clear();
                    }
                }

                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(dataModel.ObjectiveWeight[3] * AddSubjectDiversityObjective());
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[3]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                dataModel.NumSubjects,
                                AddSubjectDiversityObjective(),
                                0
                            )
                        );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[3]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddSubjectDiversityObjective(),
                                0
                            )
                        );
                        break;
                }
            }

            // MINIMIZE QUOTA DIFF (0)
            if (dataModel.ObjectiveOptions[4] > 0)
            {
                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[4]
                            * AddQuotaReachedObjective()
                        );
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[4]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                dataModel.NumTasks,
                                AddQuotaReachedObjective(),
                                0
                            )
                        );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[4]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddQuotaReachedObjective(),
                                0
                            )
                        );
                        break;
                }
            }

            // MINIMIZE WALKING DISTANCE (numTask^2)
            if (dataModel.ObjectiveOptions[5] > 0)
            {
                var sumList = new List<BoolVar>();
                for (int n1 = 0; n1 < dataModel.NumTasks - 1; n1++)
                {
                    for (int n2 = n1 + 1; n2 < dataModel.NumTasks; n2++)
                    {
                        if (
                            dataModel.AreaSlotCoefficient[
                                dataModel.TaskSlotMapping[n1],
                                dataModel.TaskSlotMapping[n2]
                            ]
                            == 0
                            || dataModel.AreaDistance[
                                dataModel.TaskAreaMapping[n1],
                                dataModel.TaskAreaMapping[n2]
                            ]
                            == 0
                        )
                        {
                            continue;
                        }

                        foreach (int i in dataModel.AllInstructors)
                        {
                            if (
                                dataModel.InstructorSlot[
                                    i,
                                    dataModel.TaskSlotMapping[n1]
                                ]
                                == 0
                                ||
                                dataModel.InstructorSlot[
                                    i,
                                    dataModel.TaskSlotMapping[n2]
                                ]
                                == 0
                                ||
                                dataModel.InstructorSubject[
                                    i,
                                    dataModel.TaskSubjectMapping[n1]
                                ]
                                == 0
                                ||
                                dataModel.InstructorSubject[
                                    i,
                                    dataModel.TaskSubjectMapping[n2]
                                ]
                                == 0
                            )
                            {
                                continue;
                            }

                            var product = problem.NewBoolVar("auxiliaryVariable");

                            problem.AddMinEquality(
                                product,
                                new[] {
                                    taskAssign[(n1, i)],
                                    taskAssign[(n2, i)]
                                }
                            );

                            sumList.Add(product);
                        }
                        assignProduct[(n1, n2)] = LinearExpr.Sum(sumList);
                        sumList.Clear();
                    }
                }

                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(dataModel.ObjectiveWeight[5] * AddWalkingDistanceObjective());
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[5]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                Int32.MaxValue,
                                AddWalkingDistanceObjective(),
                                0
                            )
                        );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[5]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddWalkingDistanceObjective(),
                                0
                            )
                        );
                        break;
                }
            }

            // O-07 MAXIMIZE SUBJECT PREFERENCE (0)
            if (dataModel.ObjectiveOptions[6] > 0)
            {
                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(
                            -1
                            * dataModel.ObjectiveWeight[6]
                            * AddSubjectPreferenceObjective()
                        );
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[6]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                dataModel.NumTasks * 5,
                                AddSubjectPreferenceObjective(),
                                dataModel.NumTasks * 5
                                )
                            );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[6]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddSubjectPreferenceObjective(),
                                dataModel.NumTasks * 5
                            )
                        );
                        break;
                }
            }

            // MAXIMIZE SLOT PREFERENCE (0)
            if (dataModel.ObjectiveOptions[7] > 0)
            {
                switch (dataModel.Strategy)
                {
                    case 1:
                        totalDeltas.Add(
                            -1
                            * dataModel.ObjectiveWeight[7]
                            * AddSlotPreferenceObjective()
                        );
                        break;
                    case 2:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[7]
                            * Helper.OrtoolsHelper.CreateDelta(
                                problem,
                                dataModel.NumTasks * 5,
                                AddSlotPreferenceObjective(),
                                dataModel.NumTasks * 5
                            )
                        );
                        break;
                    case 3:
                        totalDeltas.Add(
                            dataModel.ObjectiveWeight[7]
                            * Helper.OrtoolsHelper.CreateSquare(
                                problem,
                                AddSlotPreferenceObjective(),
                                dataModel.NumTasks * 5
                            )
                        );
                        break;
                }
            }
            #endregion

            switch (dataModel.Strategy)
            {
                case 1:
                    problem.Minimize(LinearExpr.Sum(totalDeltas));
                    break;
                case 2:
                    problem.Minimize(LinearExpr.Sum(totalDeltas));
                    break;
                case 3:
                    problem.Minimize(LinearExpr.Sum(totalDeltas));
                    break;
            }

            var status = solver.Solve(problem); //

            if (
                status == CpSolverStatus.Optimal
                ||
                status == CpSolverStatus.Feasible
            )
            {
                return Helper.OrtoolsHelper.GetResults(
                    solver, 
                    dataModel, 
                    taskAssign
                );
            }
            else return null;
        }
        #endregion

        #region Decision variables + Constraints
        // Decision variables
        private void AddDecisionVariable()
        {
            foreach (var t in dataModel.AllTasks)
            {
                foreach (var i in dataModel.AllInstructorsWithBackup)
                {
                    var taskAssignVariable = problem.NewBoolVar($"A[{t}][{i}]");

                    taskAssign[(t, i)] = taskAssignVariable;

                    if (solverConfiguration.Mode == SolverConfiguration.SolveMode.Optimize)
                    {
                        taskAssignFlat.Add(taskAssignVariable);
                    }
                }
            }
        }

        // Constraints
        private void AddTaskInstructorConstraint()
        {
            var literals = new List<ILiteral>();
            foreach (var t in dataModel.AllTasks)
            {
                foreach (var i in dataModel.AllInstructorsWithBackup)
                {
                    literals.Add(taskAssign[(t, i)]);

                }
                problem.AddExactlyOne(literals);
                literals.Clear();
            }
        }

        private void AddNoSlotConflictConstraint()
        {
            var taskInThisSlot = new List<List<int>>();
            var taskConflitWithThisSlot = new List<List<int>>();

            foreach (var s in dataModel.AllSlots)
            {
                var subTaskInThisSlot = new List<int>();
                var subTaskConflictWithThisSlot = new List<int>();

                foreach (var t in dataModel.AllTasks)
                {
                    if (
                        dataModel.TaskSlotMapping[t] == s
                        &&
                        dataModel.SlotConflict[s, s] == 1
                    )
                    {
                        subTaskInThisSlot.Add(t);
                    }

                    if (dataModel.SlotConflict[dataModel.TaskSlotMapping[t], s] == 1)
                    {
                        subTaskConflictWithThisSlot.Add(t);
                    }
                }
                taskInThisSlot.Add(subTaskInThisSlot);
                taskConflitWithThisSlot.Add(subTaskConflictWithThisSlot);
            }

            var taskAssignedThatSlot = new List<LinearExpr>();
            var taskAssignedConflictWithThatSlot = new List<LinearExpr>();

            foreach (var i in dataModel.AllInstructors)
            {
                foreach (var s in dataModel.AllSlots)
                {
                    foreach (var t in taskInThisSlot[s])
                    {
                        taskAssignedThatSlot.Add(taskAssign[(t, i)]);
                    }
                    foreach (var t in taskConflitWithThisSlot[s])
                    {
                        taskAssignedConflictWithThatSlot.Add(taskAssign[(t, i)]);
                    }

                    var indicatorVar = problem.NewBoolVar($"auxiliaryVariable");

                    problem.Add(LinearExpr.Sum(taskAssignedThatSlot) >= 1).OnlyEnforceIf(indicatorVar);
                    problem.Add(LinearExpr.Sum(taskAssignedThatSlot) == 0).OnlyEnforceIf(indicatorVar.Not());
                    problem.Add(LinearExpr.Sum(taskAssignedConflictWithThatSlot) <= dataModel.SlotConflict[s, s]).OnlyEnforceIf(indicatorVar);

                    taskAssignedThatSlot.Clear();
                    taskAssignedConflictWithThatSlot.Clear();
                }
            }
        }

        private void AddPreassignConstraint()
        {
            foreach (var data in dataModel.InstructorPreassign)
            {
                if (data.Item3 == 1)
                {
                    problem.Add(taskAssign[(data.Item2, data.Item1)] == 1);
                }
                if (data.Item3 == -1)
                {
                    problem.Add(taskAssign[(data.Item2, data.Item1)] == 0);
                }
            }
        }

        private void AddAbilityConstraint()
        {
            foreach (var t in dataModel.AllTasks)
            {
                foreach (var i in dataModel.AllInstructors)
                {
                    if (
                        dataModel.InstructorSubject[
                            i,
                            dataModel.TaskSubjectMapping[t]
                        ]
                        == 0
                        ||
                        dataModel.InstructorSlot[
                            i,
                            dataModel.TaskSlotMapping[t]
                        ]
                        == 0
                    )
                    {
                        problem.Add(taskAssign[(t, i)] == 0);
                    }
                }
            }
        }

        private void AddQuotaConstraint()
        {
            var taskAssigned = new List<IntVar>();
            foreach (var i in dataModel.AllInstructorsWithBackup)
            {
                foreach (var t in dataModel.AllTasks)
                {
                    taskAssigned.Add(taskAssign[(t, i)]);
                }
                problem.AddLinearConstraint(
                    LinearExpr.Sum(taskAssigned),
                    dataModel.InstructorMinQuota[i],
                    dataModel.InstructorQuota[i]
                );
                taskAssigned.Clear();
            }
        }
        #endregion

        #region Objectives
        // MINIMIZE DAY
        private LinearExpr AddTeachingDayObjective()
        {
            var teachingDay = new List<ILiteral>();
            foreach (int i in dataModel.AllInstructors)
            {
                foreach (int d in dataModel.AllDays)
                {
                    if (instructorDayStatus.TryGetValue((i, d), out var value))
                    {
                        teachingDay.Add(value);
                    }
                }
            }
            return LinearExpr.Sum(teachingDay);
        }

        // MINIMIZE TIME
        private LinearExpr AddTeachingTimeObjective()
        {
            var teachingTime = new List<ILiteral>();
            foreach (int i in dataModel.AllInstructors)
            {
                foreach (int d in dataModel.AllDays)
                {
                    foreach (int ti in dataModel.AllTimes)
                    {
                        if (instructorTimeStatus.TryGetValue((i, d, ti), out var value))
                        {
                            teachingTime.Add(value);
                        }
                    }
                }
            }
            return LinearExpr.Sum(teachingTime);
        }

        // MINIMIZE SEGMENT COST
        private LinearExpr AddPatternCostObjective()
        {
            var allPatternCost = new List<LinearExpr>();
            foreach (int i in dataModel.AllInstructors)
            {
                foreach (int d in dataModel.AllDays)
                {
                    for (int p = 0; p < (1 << dataModel.NumSegments); p++)
                    {
                        allPatternCost.Add(
                            dataModel.PatternCost[p]
                            * instructorPatternStatus[(i, d, p)]
                        );
                    }
                }

            }
            return LinearExpr.Sum(allPatternCost);
        }

        // MINIMIZE SUBJECT DIVERSITY
        private LinearExpr AddSubjectDiversityObjective()
        {
            var literals = new List<ILiteral>();
            var subjectDiversity = new List<LinearExpr>();
            foreach (int i in dataModel.AllInstructors)
            {
                foreach (int s in dataModel.AllSubjects)
                {
                    literals.Add(instructorSubjectStatus[(i, s)]);
                }
                subjectDiversity.Add(LinearExpr.Sum(literals));
                literals.Clear();
            }
            var result = problem.NewIntVar(
                0,
                dataModel.NumSubjects,
                "subjectDiversity"
            );
            problem.AddMaxEquality(result, subjectDiversity);
            return result;
        }

        // MINIMIZE QUOTA DIFF
        private LinearExpr AddQuotaReachedObjective()
        {
            var quotaDifference = new List<LinearExpr>();
            var sumList = new List<IntVar>();
            foreach (int i in dataModel.AllInstructors)
            {
                foreach (int t in dataModel.AllTasks)
                {
                    sumList.Add(taskAssign[(t, i)]);
                }
                quotaDifference.Add(
                    dataModel.InstructorQuota[i]
                    - LinearExpr.Sum(sumList));
                sumList.Clear();
            }
            var result = problem.NewIntVar(
                0,
                dataModel.NumTasks,
                "maxQuotaDifference"
            );
            problem.AddMaxEquality(result, quotaDifference);
            return result;
        }

        // MINIMIZE WALKING DISTANCE
        private LinearExpr AddWalkingDistanceObjective()
        {
            var walkingDistance = new List<LinearExpr>();
            for (int n1 = 0; n1 < dataModel.NumTasks - 1; n1++)
                for (int n2 = n1 + 1; n2 < dataModel.NumTasks; n2++)
                {
                    if (
                        dataModel.AreaSlotCoefficient[
                            dataModel.TaskSlotMapping[n1],
                            dataModel.TaskSlotMapping[n2]
                        ] == 0
                        ||
                        dataModel.AreaDistance[
                            dataModel.TaskAreaMapping[n1],
                            dataModel.TaskAreaMapping[n2]
                        ] == 0
                    )
                    {
                        continue;
                    }

                    walkingDistance.Add(
                        assignProduct[(n1, n2)]
                        * dataModel.AreaSlotCoefficient[
                            dataModel.TaskSlotMapping[n1],
                            dataModel.TaskSlotMapping[n2]
                        ]
                        * dataModel.AreaDistance[
                            dataModel.TaskAreaMapping[n1],
                            dataModel.TaskAreaMapping[n2]
                        ]
                    );
                }
            return LinearExpr.Sum(walkingDistance);
        }

        // MAXIMIZE SUBJECT PREFERENCE
        private LinearExpr AddSubjectPreferenceObjective()
        {
            var sumList = new List<LinearExpr>();
            foreach (int t in dataModel.AllTasks)
            {
                foreach (int i in dataModel.AllInstructors)
                {
                    sumList.Add(
                        taskAssign[(t, i)]
                        * dataModel.InstructorSubjectPreference[
                            i,
                            dataModel.TaskSubjectMapping[t]
                        ]
                    );
                }
            }
            return LinearExpr.Sum(sumList);
        }

        // MAXIMIZE SLOT PREFERENCE
        private LinearExpr AddSlotPreferenceObjective()
        {
            var sumList = new List<LinearExpr>();
            foreach (int t in dataModel.AllTasks)
            {
                foreach (int i in dataModel.AllInstructors)
                {
                    sumList.Add(
                        taskAssign[(t, i)]
                        * dataModel.InstructorSlotPreference[
                            i,
                            dataModel.TaskSlotMapping[t]
                        ]
                    );
                }
            }
            return LinearExpr.Sum(sumList);
        }
        #endregion

        #region Solution callback
        public class FBTSolutionCallback : CpSolverSolutionCallback
        {
            public FBTSolutionCallback(IntVar[] variables, int solutionLimit)
            {
                this.variables = variables;
                this.solutionLimit = solutionLimit;
            }

            public override void OnSolutionCallback()
            {
                {
                    Console.WriteLine(String.Format("Solution #{0}: time = {1:F2} s", solutionCount, WallTime()));
                    var solutionString = new StringBuilder();
                    foreach (IntVar v in variables)
                    {
                        solutionString.Append(string.Format("{0} = {1}\n", v.ToString(), Value(v)));
                    }
                    Console.WriteLine(solutionString);
                    solutionCount++;
                    if (solutionCount >= solutionLimit)
                    {
                        Console.WriteLine(String.Format("Stopping search after {0} solutions", solutionLimit));
                        StopSearch();
                    }
                }
            }

            public int SolutionCount()
            {
                return solutionCount;
            }

            private int solutionCount;
            private IntVar[] variables;
            private readonly int solutionLimit;
        }
        #endregion
    }
}
