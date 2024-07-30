//using data_manipulation_visualisation;
//using Google.OrTools.Sat;
//using ILOG.Concert;
//using ILOG.CPLEX;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Threading.Tasks;

//namespace solvers
//{
//    public class FBTCplex
//    {
//        private Cplex problem;

//        private DataModel dataModel;
//        private SolverConfiguration solverConfiguration;

//        private Dictionary<(int, int), IIntVar> taskAssign;
//        private Dictionary<(int, int), IIntVar> instructorDayStatus;
//        private Dictionary<(int, int, int), IIntVar> instructorTimeStatus;
//        private Dictionary<(int, int), IIntVar> instructorSubjectStatus;
//        private Dictionary<(int, int, int), IIntVar> instructorSegmentStatus;
//        private Dictionary<(int, int, int), IIntVar> instructorPatternStatus;
//        private Dictionary<(int, int), ILinearIntExpr> assignProduct;

//        private List<List<(int, int)>>? solutions;

//        public FBTCplex(
//            DataModel dataModel,
//            SolverConfiguration solverConfiguration
//        )
//        {
//            problem = new Cplex();
//            this.solverConfiguration = solverConfiguration;
//            this.dataModel = dataModel;
//            taskAssign = new Dictionary<(int, int), IIntVar>();
//            instructorDayStatus = new Dictionary<(int, int), IIntVar>();
//            instructorTimeStatus = new Dictionary<(int, int, int), IIntVar>();
//            instructorSubjectStatus = new Dictionary<(int, int), IIntVar>();
//            instructorSegmentStatus = new Dictionary<(int, int, int), IIntVar>();
//            instructorPatternStatus = new Dictionary<(int, int, int), IIntVar>();
//            assignProduct = new Dictionary<(int, int), ILinearIntExpr>();
//        }

//        public void CreateModel()
//        {
//            Helper.CalculateExecutionTime(AddDecisionVariable, "AddDecisionVariable");
//            Helper.CalculateExecutionTime(AddTaskInstructorConstraint, "AddTaskInstructorConstraint");
//            Helper.CalculateExecutionTime(AddNoSlotConflictConstraint, "AddNoSlotConflictConstraint");
//            Helper.CalculateExecutionTime(AddPreassignConstraint, "AddPreassignConstraint");
//            Helper.CalculateExecutionTime(AddAbilityConstraint, "AddAbilityConstraint");
//            Helper.CalculateExecutionTime(AddQuotaConstraint, "AddQuotaConstraint");
//        }

//        public void SetupSolver()
//        {
//            problem.SetParam(Cplex.IntParam.TimeLimit, 120);
//        }

//        #region Solve
//        public int FindBackupInstructor()
//        {
//            dataModel.NumBackupInstructors = dataModel.NumTasks;
//            dataModel.SetRange();
//            CreateModel();

//            var sumExpr = problem.LinearIntExpr();
//            foreach (int t in dataModel.AllTasks)
//            {
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    sumExpr.AddTerm(1, taskAssign[(t, i)]);
//                }
//            }

//            problem.AddObjective(
//                ObjectiveSense.Minimize,
//                Helper.CplexHelper.CreateDelta(
//                    problem,
//                    dataModel.NumTasks,
//                    Helper.CplexHelper.WrapperExpr(
//                        problem,
//                        sumExpr,
//                        0,
//                        dataModel.NumTasks * dataModel.NumInstructors
//                    ),
//                    dataModel.NumTasks
//                )
//            );

//            var status = problem.Solve();

//            if (status)
//            {
//                return (int)problem.ObjValue;
//            }
//            return dataModel.NumTasks;
//        }
//        public List<List<(int, int)>>? IsSatisfied()
//        {
//            dataModel.SetRange();
//            CreateModel();

//            var sumExpr = problem.LinearIntExpr();
//            foreach (int t in dataModel.AllTasks)
//            {
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    sumExpr.AddTerm(1, taskAssign[(t, i)]);
//                }
//            }

//            problem.Minimize(
//                Helper.CplexHelper.CreateDelta(
//                    problem,
//                    dataModel.NumTasks,
//                    Helper.CplexHelper.WrapperExpr(
//                        problem,
//                        sumExpr,
//                        0,
//                        dataModel.NumTasks * dataModel.NumInstructors
//                    ),
//                    dataModel.NumTasks
//                )
//            );

//            var status = problem.Solve();
//            if (status)
//            {
//                return Helper.CplexHelper.GetResults(
//                    problem,
//                    dataModel,
//                    taskAssign
//                );
//            }
//            return null;
//        }
//        public List<List<(int, int)>>? ObjectiveOptimize()
//        {
//            dataModel.SetRange();
//            CreateModel();

//            #region Add objective to model
//            var totalDeltas = problem.LinearIntExpr();

//            // MINIMIZE DAY (numInstructors * numDays)
//            if (dataModel.ObjectiveOptions[0] > 0)
//            {
//                var literals = problem.LinearIntExpr();
//                var count = 0;
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    foreach (int d in dataModel.AllDays)
//                    {
//                        foreach (int t in dataModel.AllTasks)
//                        {
//                            if (
//                                dataModel.InstructorSlot[
//                                    i,
//                                    dataModel.TaskSlotMapping[t]
//                                ]
//                                == 1
//                                &&
//                                dataModel.SlotDay[
//                                    dataModel.TaskSlotMapping[t],
//                                    d
//                                ]
//                                == 1
//                            )
//                            {
//                                literals.AddTerm(1, taskAssign[(t, i)]);
//                                count++;
//                            }
//                        }

//                        if (count > 0)
//                        {
//                            instructorDayStatus[(i, d)] = problem.BoolVar($"IDS[{i}][{d}]");
//                            problem.Add(
//                                problem.IfThen(
//                                    problem.Eq(instructorDayStatus[(i, d)], 1),
//                                    problem.Ge(literals, 1)
//                                )
//                            );
//                            problem.Add(
//                                problem.IfThen(
//                                    problem.Eq(instructorDayStatus[(i, d)], 0),
//                                    problem.Eq(literals, 0)
//                                )
//                            );
//                        }
//                        literals.Clear();
//                        count = 0;
//                    }
//                }


//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[0],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddTeachingDayObjective(),
//                                0,
//                                int.MaxValue
//                            )
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[0],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                dataModel.NumDays * dataModel.NumInstructors,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddTeachingDayObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[0],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                AddTeachingDayObjective(),
//                                0
//                            )
//                        );
//                        break;
//                }
//            }

//            // MINIMIZE TIME (numInstructors * numDays * numTimes)
//            if (dataModel.ObjectiveOptions[1] > 0)
//            {
//                var literals = problem.LinearIntExpr();
//                var count = 0;
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    foreach (int d in dataModel.AllDays)
//                    {
//                        foreach (int ti in dataModel.AllTimes)
//                        {
//                            foreach (int t in dataModel.AllTasks)
//                            {
//                                if (
//                                    dataModel.InstructorSlot[
//                                        i,
//                                        dataModel.TaskSlotMapping[t]
//                                    ]
//                                    == 1
//                                    &&
//                                    dataModel.SlotDay[
//                                        dataModel.TaskSlotMapping[t],
//                                        d
//                                    ]
//                                    == 1
//                                    &&
//                                    dataModel.SlotTime[
//                                        dataModel.TaskSlotMapping[t],
//                                        ti
//                                    ]
//                                    == 1
//                                )
//                                {
//                                    literals.AddTerm(1, taskAssign[(t, i)]);
//                                    count++;
//                                }
//                            }

//                            if (count > 0)
//                            {
//                                instructorTimeStatus[(i, d, ti)] = problem.BoolVar($"ITS[{i}][{d}][{ti}]");

//                                problem.Add(
//                                    problem.IfThen(
//                                        problem.Eq(instructorTimeStatus[(i, d, ti)], 1),
//                                        problem.Ge(literals, 1)
//                                    )
//                                );

//                                problem.Add(
//                                    problem.IfThen(
//                                        problem.Eq(instructorTimeStatus[(i, d, ti)], 0),
//                                        problem.Eq(literals, 0)
//                                    )
//                                );
//                            }
//                            literals.Clear();
//                            count = 0;
//                        }
//                    }

//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[1],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddTeachingTimeObjective(),
//                                0,
//                                dataModel.NumInstructors
//                                    * dataModel.NumDays
//                                    * dataModel.NumTimes
//                            )
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[1],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTimes
//                                    * dataModel.NumDays
//                                    * dataModel.NumInstructors,
//                                Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddTeachingTimeObjective(),
//                                0,
//                                dataModel.NumInstructors
//                                    * dataModel.NumDays
//                                    * dataModel.NumTimes
//                                ),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[1],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                AddTeachingTimeObjective(),
//                                0
//                            )
//                        );
//                        break;
//                }
//            }

//            // MINIMIZE PATTERN COST (numInstructors * numDays * (numSegments + 2^num Segments)
//            if (dataModel.ObjectiveOptions[2] > 0)
//            {
//                var firstLiterals = problem.LinearIntExpr();
//                var secondLiterals = new List<IIntExpr>();
//                var firstCount = 0;
//                //var sencondCount = 0;
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    foreach (int d in dataModel.AllDays)
//                    {
//                        foreach (int sm in dataModel.AllSegments)
//                        {
//                            foreach (int t in dataModel.AllTasks)
//                            {
//                                if (
//                                    dataModel.InstructorSlot[
//                                        i,
//                                        dataModel.TaskSlotMapping[t]
//                                    ]
//                                    == 1
//                                    &&
//                                    dataModel.SlotSegment[
//                                        dataModel.TaskSlotMapping[t],
//                                        d,
//                                        sm
//                                    ]
//                                    == 1
//                                )
//                                {
//                                    firstLiterals.AddTerm(1, taskAssign[(t, i)]);
//                                    firstCount++;
//                                }
//                            }

//                            instructorSegmentStatus[(i, d, sm)] = problem.BoolVar($"ISS[{i}][{d}][{sm}]");
//                            if (firstCount == 0)
//                            {
//                                //problem.AddHint(instructorSegmentStatus[(i, d, sm)], 0);
//                            }

//                            problem.Add(
//                                problem.IfThen(
//                                    problem.Eq(instructorSegmentStatus[(i, d, sm)], 1),
//                                    problem.Ge(firstLiterals, 1)
//                                )
//                            );

//                            problem.Add(
//                                problem.IfThen(
//                                    problem.Eq(instructorSegmentStatus[(i, d, sm)], 0),
//                                    problem.Eq(firstLiterals, 0)
//                                )
//                            );
//                            firstLiterals.Clear();
//                        }

//                        for (int p = 0; p < (1 << dataModel.NumSegments); p++)
//                        {
//                            foreach (int sm in dataModel.AllSegments)
//                            {
//                                if ((p & (1 << (dataModel.NumSegments - sm - 1))) > 0)
//                                {
//                                    secondLiterals.Add(Helper.CplexHelper.BoolState(problem, instructorSegmentStatus[(i, d, sm)], true));
//                                }
//                                else
//                                {
//                                    secondLiterals.Add(Helper.CplexHelper.BoolState(problem, instructorSegmentStatus[(i, d, sm)], false));
//                                }
//                            }

//                            instructorPatternStatus[(i, d, p)] = problem.BoolVar($"IPS[{i}][{d}[{p}]");

//                            problem.Add(
//                                problem.IfThen(
//                                    problem.Eq(instructorPatternStatus[(i, d, p)], 1),
//                                    problem.Eq(
//                                        problem.Sum(
//                                            secondLiterals.ToArray()
//                                        ), 
//                                        dataModel.NumSegments
//                                    )
//                                )
//                            );

//                            problem.Add(
//                                problem.IfThen(
//                                    problem.Eq(instructorPatternStatus[(i, d, p)], 0),
//                                    problem.Not(
//                                        problem.Eq(
//                                            firstLiterals,
//                                            dataModel.NumSegments
//                                        )
//                                    )
//                                )
//                            );

//                            secondLiterals.Clear();
//                        }

//                    }
//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[2],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddPatternCostObjective(),
//                                0,
//                                int.MaxValue
//                            )
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[2],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                (1 << dataModel.NumSegments)
//                                    * dataModel.NumDays
//                                    * dataModel.NumInstructors
//                                    * dataModel.NumSegments,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddPatternCostObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[2],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                AddPatternCostObjective(),
//                                0
//                            )
//                        );
//                        break;
//                }
//            }

//            // MINIMIZE SUBJECT DIVERSITY (numInstructor * numSubject)
//            if (dataModel.ObjectiveOptions[3] > 0)
//            {
//                var literals = problem.LinearIntExpr();
//                var count = 0;
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    foreach (int s in dataModel.AllSubjects)
//                    {
//                        foreach (int t in dataModel.AllTasks)
//                        {
//                            if (dataModel.TaskSubjectMapping[t] == s)
//                            {
//                                literals.AddTerm(1, taskAssign[(t, i)]);
//                                count++;
//                            }
//                        }

//                        instructorSubjectStatus[(i, s)] = problem.BoolVar($"ISUS[{i}][{s}]");

//                        if (count == 0)
//                        {
//                            //problem.AddHint(instructorSubjectStatus[(i, s)], 0);
//                        }

//                        problem.Add(
//                            problem.IfThen(
//                                problem.Eq(instructorSubjectStatus[(i, s)], 1),
//                                problem.Ge(literals, 1)
//                            )
//                        );

//                        problem.Add(
//                            problem.IfThen(
//                                problem.Eq(instructorSubjectStatus[(i, s)], 0),
//                                problem.Eq(literals, 0)
//                            )
//                        );

//                        literals.Clear();
//                    }
//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[3],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddSubjectDiversityObjective(),
//                                0,
//                                int.MaxValue
//                            )
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[3],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                dataModel.NumSubjects,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddSubjectDiversityObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[3],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                AddSubjectDiversityObjective(),
//                                0
//                            )
//                        );
//                        break;
//                }
//            }

//            // MINIMIZE QUOTA DIFF (0)
//            if (dataModel.ObjectiveOptions[4] > 0)
//            {
//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[4],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddQuotaReachedObjective(),
//                                0,
//                                int.MaxValue
//                            )
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[4],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTasks,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddQuotaReachedObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[4],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                AddQuotaReachedObjective(),
//                                0
//                            )
//                        );
//                        break;
//                }
//            }

//            // MINIMIZE WALKING DISTANCE (numTask^2)
//            if (dataModel.ObjectiveOptions[5] > 0)
//            {
//                var sumExpr = problem.LinearIntExpr();
//                for (int n1 = 0; n1 < dataModel.NumTasks - 1; n1++)
//                {
//                    for (int n2 = n1 + 1; n2 < dataModel.NumTasks; n2++)
//                    {
//                        if (
//                            dataModel.AreaSlotCoefficient[
//                                dataModel.TaskSlotMapping[n1],
//                                dataModel.TaskSlotMapping[n2]
//                            ]
//                            == 0
//                            || dataModel.AreaDistance[
//                                dataModel.TaskAreaMapping[n1],
//                                dataModel.TaskAreaMapping[n2]
//                            ]
//                            == 0
//                        )
//                        {
//                            continue;
//                        }

//                        foreach (int i in dataModel.AllInstructors)
//                        {
//                            if (
//                                dataModel.InstructorSlot[
//                                    i,
//                                    dataModel.TaskSlotMapping[n1]
//                                ]
//                                == 0
//                                ||
//                                dataModel.InstructorSlot[
//                                    i,
//                                    dataModel.TaskSlotMapping[n2]
//                                ]
//                                == 0
//                                ||
//                                dataModel.InstructorSubject[
//                                    i,
//                                    dataModel.TaskSubjectMapping[n1]
//                                ]
//                                == 0
//                                ||
//                                dataModel.InstructorSubject[
//                                    i,
//                                    dataModel.TaskSubjectMapping[n2]
//                                ]
//                                == 0
//                            )
//                            {
//                                continue;
//                            }

//                            var product = problem.BoolVar("auxiliaryVariable");

//                            problem.Add(
//                                problem.IfThen(
//                                    problem.Eq(problem.Sum(taskAssign[(n1, i)], taskAssign[(n2, i)]), 2),
//                                    problem.Eq(product, 1)
//                                )
//                            );

//                            sumExpr.AddTerm(1, product);
//                        }
//                        assignProduct[(n1, n2)] = sumExpr;
//                        sumExpr.Clear();
//                    }
//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[5],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddWalkingDistanceObjective(),
//                                0,
//                                int.MaxValue
//                            )
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[5],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                int.MaxValue,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddWalkingDistanceObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[5],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                AddWalkingDistanceObjective(),
//                                0
//                            )
//                        );
//                        break;
//                }
//            }

//            // O-07 MAXIMIZE SUBJECT PREFERENCE (0)
//            if (dataModel.ObjectiveOptions[6] > 0)
//            {
//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            -1 * dataModel.ObjectiveWeight[6],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddSubjectPreferenceObjective(),
//                                0,
//                                int.MaxValue
//                            ) 
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[6],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTasks * 5,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddSubjectPreferenceObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                dataModel.NumTasks * 5
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[6],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                AddSubjectPreferenceObjective(),
//                                dataModel.NumTasks * 5
//                            )
//                        );
//                        break;
//                }
//            }

//            // MAXIMIZE SLOT PREFERENCE (0)
//            if (dataModel.ObjectiveOptions[7] > 0)
//            {
//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.AddTerm(
//                            -1 * dataModel.ObjectiveWeight[7],
//                            Helper.CplexHelper.WrapperExpr(
//                                problem,
//                                AddSlotPreferenceObjective(),
//                                0,
//                                int.MaxValue
//                            )
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[7],
//                            Helper.CplexHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTasks * 5,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddSlotPreferenceObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                dataModel.NumTasks * 5
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[7],
//                            Helper.CplexHelper.CreateSquare(
//                                problem,
//                                Helper.CplexHelper.WrapperExpr(
//                                    problem,
//                                    AddSlotPreferenceObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                dataModel.NumTasks * 5
//                            )
//                        );
//                        break;
//                }
//            }
//            #endregion

//            switch (dataModel.Strategy)
//            {
//                case 1:
//                    problem.AddObjective(ObjectiveSense.Minimize, totalDeltas);
//                    break;
//                case 2:
//                    problem.AddObjective(ObjectiveSense.Minimize, totalDeltas);
//                    break;
//                case 3:
//                    problem.AddObjective(ObjectiveSense.Minimize, totalDeltas);
//                    break;
//            }

//            var status = problem.Solve();

//            if (!status)
//            {
//                return null;
//            }

//            if (
//                problem.GetStatus() == Cplex.Status.Optimal
//                ||
//                problem.GetStatus() == Cplex.Status.Feasible
//            )
//            {
//                return Helper.CplexHelper.GetResults(
//                    problem,
//                    dataModel,
//                    taskAssign
//                );
//            }
//            else return null;
//        }
//        #endregion

//        #region Decision variables + Constraints
//        // Decision variables
//        private void AddDecisionVariable()
//        {
//            foreach (var t in dataModel.AllTasks)
//            {
//                foreach (var i in dataModel.AllInstructorsWithBackup)
//                {
//                    taskAssign[(t, i)] = problem.BoolVar($"A[{t}][{i}]");
//                }
//            }
//        }

//        // Constraints
//        private void AddTaskInstructorConstraint()
//        {
//            var literals = problem.LinearIntExpr();
//            foreach (var t in dataModel.AllTasks)
//            {
//                foreach (var i in dataModel.AllInstructorsWithBackup)
//                {
//                    literals.AddTerm(1, taskAssign[(t, i)]);

//                }
//                problem.AddEq(literals, 1);
//                literals.Clear();
//            }
//        }

//        private void AddNoSlotConflictConstraint()
//        {
//            var taskInThisSlot = new List<List<int>>();
//            var taskConflitWithThisSlot = new List<List<int>>();

//            foreach (var s in dataModel.AllSlots)
//            {
//                var subTaskInThisSlot = new List<int>();
//                var subTaskConflictWithThisSlot = new List<int>();

//                foreach (var t in dataModel.AllTasks)
//                {
//                    if (
//                        dataModel.TaskSlotMapping[t] == s
//                        &&
//                        dataModel.SlotConflict[s, s] == 1
//                    )
//                    {
//                        subTaskInThisSlot.Add(t);
//                    }

//                    if (dataModel.SlotConflict[dataModel.TaskSlotMapping[t], s] == 1)
//                    {
//                        subTaskConflictWithThisSlot.Add(t);
//                    }
//                }
//                taskInThisSlot.Add(subTaskInThisSlot);
//                taskConflitWithThisSlot.Add(subTaskConflictWithThisSlot);
//            }

//            var taskAssignedThatSlot = problem.LinearIntExpr();
//            var taskAssignedConflictWithThatSlot = problem.LinearIntExpr();

//            foreach (var i in dataModel.AllInstructors)
//            {
//                foreach (var s in dataModel.AllSlots)
//                {
//                    foreach (var t in taskInThisSlot[s])
//                    {
//                        taskAssignedThatSlot.AddTerm(1, taskAssign[(t, i)]);
//                    }
//                    foreach (var t in taskConflitWithThisSlot[s])
//                    {
//                        taskAssignedConflictWithThatSlot.AddTerm(1, taskAssign[(t, i)]);
//                    }

//                    var indicatorVar = problem.BoolVar($"auxilliaryVariable");

//                    problem.Add(
//                        problem.IfThen(
//                            problem.Eq(indicatorVar,
//                            1
//                        ),
//                            problem.Le(
//                                taskAssignedThatSlot,
//                                1
//                            )
//                        )
//                    );

//                    problem.Add(
//                        problem.IfThen(
//                            problem.Eq(indicatorVar,
//                            0
//                        ),
//                            problem.Eq(
//                                taskAssignedThatSlot,
//                                0
//                            )
//                        )
//                    );


//                    problem.Add(
//                        problem.IfThen(
//                            problem.Eq(indicatorVar,
//                            1
//                        ),
//                            problem.Le(
//                                taskAssignedConflictWithThatSlot,
//                                dataModel.SlotConflict[s, s]
//                            )
//                        )
//                    );

//                    taskAssignedThatSlot.Clear();
//                    taskAssignedConflictWithThatSlot.Clear();
//                }
//            }
//        }

//        private void AddPreassignConstraint()
//        {
//            foreach (var data in dataModel.InstructorPreassign)
//            {
//                if (data.Item3 == 1)
//                {
//                    problem.AddEq(taskAssign[(data.Item2, data.Item1)], 1);
//                }
//                if (data.Item3 == -1)
//                {
//                    problem.AddEq(taskAssign[(data.Item2, data.Item1)], 0);
//                }
//            }
//        }

//        private void AddAbilityConstraint()
//        {
//            foreach (var t in dataModel.AllTasks)
//            {
//                foreach (var i in dataModel.AllInstructors)
//                {
//                    if (
//                        dataModel.InstructorSubject[
//                            i,
//                            dataModel.TaskSubjectMapping[t]
//                        ]
//                        == 0
//                        ||
//                        dataModel.InstructorSlot[
//                            i,
//                            dataModel.TaskSlotMapping[t]
//                        ]
//                        == 0
//                    )
//                    {
//                        problem.AddEq(taskAssign[(t, i)], 0);
//                    }
//                }
//            }
//        }

//        private void AddQuotaConstraint()
//        {
//            var taskAssigned = problem.LinearIntExpr();
//            foreach (var i in dataModel.AllInstructorsWithBackup)
//            {
//                foreach (var t in dataModel.AllTasks)
//                {
//                    taskAssigned.AddTerm(1, taskAssign[(t, i)]);
//                }
//                problem.AddRange(
//                    dataModel.InstructorMinQuota[i],
//                    taskAssigned,
//                    dataModel.InstructorQuota[i]
//                );
//                taskAssigned.Clear();
//            }
//        }
//        #endregion

//        #region Objectives
//        // MINIMIZE DAY
//        private IIntExpr AddTeachingDayObjective()
//        {
//            var teachingDay = problem.LinearIntExpr();
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int d in dataModel.AllDays)
//                {
//                    if (instructorDayStatus.TryGetValue((i, d), out var value))
//                    {
//                        teachingDay.AddTerm(1, value);
//                    }
//                }
//            }
//            return teachingDay;
//        }

//        // MINIMIZE TIME
//        private IIntExpr AddTeachingTimeObjective()
//        {
//            var teachingTime = problem.LinearIntExpr();
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int d in dataModel.AllDays)
//                {
//                    foreach (int ti in dataModel.AllTimes)
//                    {
//                        if (instructorTimeStatus.TryGetValue((i, d, ti), out var value))
//                        {
//                            teachingTime.AddTerm(1, value);
//                        }
//                    }
//                }
//            }
//            return teachingTime;
//        }

//        // MINIMIZE SEGMENT COST
//        private IIntExpr AddPatternCostObjective()
//        {
//            var allPatternCost = problem.LinearIntExpr();
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int d in dataModel.AllDays)
//                {
//                    for (int p = 0; p < (1 << dataModel.NumSegments); p++)
//                    {
//                        allPatternCost.AddTerm(
//                            dataModel.PatternCost[p],
//                            instructorPatternStatus[(i, d, p)]
//                        );
//                    }
//                }

//            }
//            return allPatternCost;
//        }

//        // MINIMIZE SUBJECT DIVERSITY
//        private IIntExpr AddSubjectDiversityObjective()
//        {
//            var literals = problem.LinearIntExpr();
//            var subjectDiversity = new List<IIntExpr>();
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int s in dataModel.AllSubjects)
//                {
//                    literals.AddTerm(1, instructorSubjectStatus[(i, s)]);
//                }
//                subjectDiversity.Add(literals);
//                literals.Clear();
//            }
//            return problem.Max(subjectDiversity.ToArray());
//        }

//        // MINIMIZE QUOTA DIFF
//        private IIntExpr AddQuotaReachedObjective()
//        {
//            var quotaDifference = new List<IIntExpr>();
//            var sumExpr = problem.LinearIntExpr();
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int t in dataModel.AllTasks)
//                {
//                    sumExpr.AddTerm(1, taskAssign[(t, i)]);
//                }
//                quotaDifference.Add(problem.Sum(dataModel.InstructorQuota[i], problem.Negative(sumExpr)));
//                sumExpr.Clear();
//            }
//            return problem.Max(quotaDifference.ToArray());
//        }

//        // MINIMIZE WALKING DISTANCE
//        private IIntExpr AddWalkingDistanceObjective()
//        {
//            var walkingDistance = problem.LinearIntExpr();
//            for (int n1 = 0; n1 < dataModel.NumTasks - 1; n1++)
//                for (int n2 = n1 + 1; n2 < dataModel.NumTasks; n2++)
//                {
//                    if (
//                        dataModel.AreaSlotCoefficient[
//                            dataModel.TaskSlotMapping[n1],
//                            dataModel.TaskSlotMapping[n2]
//                        ] == 0
//                        ||
//                        dataModel.AreaDistance[
//                            dataModel.TaskAreaMapping[n1],
//                            dataModel.TaskAreaMapping[n2]
//                        ] == 0
//                    )
//                    {
//                        continue;
//                    }

//                    var auxiliaryVariable = problem.IntVar(int.MinValue, int.MaxValue);
//                    problem.AddEq(
//                        auxiliaryVariable,
//                        assignProduct[(n1, n2)]
//                    );

//                    walkingDistance.AddTerm(
//                        dataModel.AreaSlotCoefficient[
//                            dataModel.TaskSlotMapping[n1],
//                            dataModel.TaskSlotMapping[n2]
//                        ]
//                        * dataModel.AreaDistance[
//                            dataModel.TaskAreaMapping[n1],
//                            dataModel.TaskAreaMapping[n2]
//                        ],
//                        auxiliaryVariable
//                    );
//                }
//            return walkingDistance;
//        }

//        // MAXIMIZE SUBJECT PREFERENCE
//        private IIntExpr AddSubjectPreferenceObjective()
//        {
//            var sumExpr = problem.LinearIntExpr();
//            foreach (int t in dataModel.AllTasks)
//            {
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    sumExpr.AddTerm(
//                        dataModel.InstructorSubjectPreference[
//                            i,
//                            dataModel.TaskSubjectMapping[t]
//                        ],
//                        taskAssign[(t, i)]
//                    );
//                }
//            }
//            return sumExpr;
//        }

//        // MAXIMIZE SLOT PREFERENCE
//        private IIntExpr AddSlotPreferenceObjective()
//        {
//            var sumExpr = problem.LinearIntExpr();
//            foreach (int t in dataModel.AllTasks)
//            {
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    sumExpr.AddTerm(
//                        dataModel.InstructorSlotPreference[
//                            i,
//                            dataModel.TaskSlotMapping[t]
//                        ],
//                        taskAssign[(t, i)]
//                    );
//                }
//            }
//            return sumExpr;
//        }
//        #endregion
//    }
//}
