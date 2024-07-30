//using data_manipulation_visualisation;
//using Google.OrTools.Sat;
//using ILOG.Concert;
//using ILOG.CP;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace solvers
//{
//    public class FBTCplexCp
//    {
//        private CP problem;

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

//        public FBTCplexCp(
//            DataModel dataModel,
//            SolverConfiguration solverConfiguration
//        )
//        {
//            problem = new CP();
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
//                Helper.CplexCpHelper.CreateDelta(
//                    problem,
//                    dataModel.NumTasks,
//                    sumExpr,
//                    dataModel.NumTasks
//                )
//            );

//            var status = problem.Solve();

//            if (status)
//            {
//                return (int) problem.ObjValue;
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
//                Helper.CplexCpHelper.CreateDelta(
//                    problem,
//                    dataModel.NumTasks,
//                    sumExpr,
//                    dataModel.NumTasks
//                )
//            );

//            var status = problem.Solve();
//            if (status)
//            {
//                return Helper.CplexCpHelper.GetResults(
//                    problem,
//                    dataModel,
//                    taskAssign
//                );
//            }
//            return null;
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
//            var literals = new List<IIntVar>();
//            foreach (var t in dataModel.AllTasks)
//            {
//                foreach (var i in dataModel.AllInstructorsWithBackup)
//                {
//                    literals.Add(taskAssign[(t, i)]);

//                }
//                problem.AddEq(problem.Sum(literals.ToArray()), 1);
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

//            var taskAssignedThatSlot = new List<IIntVar>();
//            var taskAssignedConflictWithThatSlot = new List<IIntVar>();

//            foreach (var i in dataModel.AllInstructors)
//            {
//                foreach (var s in dataModel.AllSlots)
//                {
//                    foreach (var t in taskInThisSlot[s])
//                    {
//                        taskAssignedThatSlot.Add(taskAssign[(t, i)]);
//                    }
//                    foreach (var t in taskConflitWithThisSlot[s])
//                    {
//                        taskAssignedConflictWithThatSlot.Add(taskAssign[(t, i)]);
//                    }

//                    problem.Add(
//                        problem.IfThen(
//                            problem.Ge(problem.Sum(taskAssignedThatSlot.ToArray()),
//                            1
//                        ),
//                            problem.Le(
//                                problem.Sum(taskAssignedConflictWithThatSlot.ToArray()),
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
//            var taskAssigned = new List<IIntVar>();
//            foreach (var i in dataModel.AllInstructorsWithBackup)
//            {
//                foreach (var t in dataModel.AllTasks)
//                {
//                    taskAssigned.Add(taskAssign[(t, i)]);
//                }
//                problem.AddRange(
//                    dataModel.InstructorMinQuota[i],
//                    problem.Sum(taskAssigned.ToArray()),
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
//            var subjectDiversity = problem.IntExprArray(dataModel.AllInstructors.Length);
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int s in dataModel.AllSubjects)
//                {
//                    literals.AddTerm(1, instructorSubjectStatus[(i, s)]);
//                }
//                subjectDiversity[i] = literals;
//                literals.Clear();
//            }
//            return problem.Max(subjectDiversity);
//        }

//        // MINIMIZE QUOTA DIFF
//        private IIntExpr AddQuotaReachedObjective()
//        {
//            var quotaDifference = problem.IntExprArray(dataModel.AllInstructors.Length);
//            var sumExpr = problem.LinearIntExpr();
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int t in dataModel.AllTasks)
//                {
//                    sumExpr.AddTerm(1, taskAssign[(t, i)]);
//                }
//                quotaDifference[i] = problem.Sum(dataModel.InstructorQuota[i], problem.Negative(sumExpr));
//                sumExpr.Clear();
//            }
//            return problem.Max(quotaDifference);
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
