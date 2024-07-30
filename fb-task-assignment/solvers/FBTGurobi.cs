//using data_manipulation_visualisation;
//using Google.OrTools.Sat;
//using Gurobi;
//using Microsoft.Z3;

//namespace solvers
//{
//    public class FBTGurobi
//    {
//        private GRBEnv env;
//        private GRBModel problem;

//        private DataModel dataModel;
//        private SolverConfiguration solverConfiguration;

//        private Dictionary<(int, int), GRBVar> taskAssign;
//        private Dictionary<(int, int), GRBVar> instructorDayStatus;
//        private Dictionary<(int, int, int), GRBVar> instructorTimeStatus;
//        private Dictionary<(int, int), GRBVar> instructorSubjectStatus;
//        private Dictionary<(int, int, int), GRBVar> instructorSegmentStatus;
//        private Dictionary<(int, int, int), GRBVar> instructorPatternStatus;
//        private Dictionary<(int, int), GRBLinExpr> assignProduct;

//        private List<List<(int, int)>>? solutions;

//        public FBTGurobi(
//            DataModel dataModel,
//            SolverConfiguration solverConfiguration
//        )
//        {
//            env = new GRBEnv(false);
//            problem = new GRBModel(env);
//            this.solverConfiguration = solverConfiguration;
//            this.dataModel = dataModel;
//            taskAssign = new Dictionary<(int, int), GRBVar>();
//            instructorDayStatus = new Dictionary<(int, int), GRBVar>();
//            instructorTimeStatus = new Dictionary<(int, int, int), GRBVar>();
//            instructorSubjectStatus = new Dictionary<(int, int), GRBVar>();
//            instructorSegmentStatus = new Dictionary<(int, int, int), GRBVar>();
//            instructorPatternStatus = new Dictionary<(int, int, int), GRBVar>();
//            assignProduct = new Dictionary<(int, int), GRBLinExpr>();
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
//        public List<List<(int, int)>>? IsSatisfied()
//        {
//            dataModel.SetRange();
//            CreateModel();

//            var sumExpr = new GRBLinExpr();
//            foreach (int t in dataModel.AllTasks)
//            {
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    sumExpr.AddTerm(1, taskAssign[(t, i)]);
//                }
//            }

//            problem.SetObjective(
//                Helper.GurobiHelper.CreateDelta(
//                    problem,
//                    dataModel.NumTasks,
//                    sumExpr,
//                    dataModel.NumTasks
//                ),
//                GRB.MINIMIZE
//            );

//            problem.Optimize();

//            if (problem.Status == GRB.Status.OPTIMAL)
//            {
//                return Helper.GurobiHelper.GetResults(
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
//            var totalDeltas = new GRBLinExpr();

//            // MINIMIZE DAY (numInstructors * numDays)
//            if (dataModel.ObjectiveOptions[0] > 0)
//            {
//                var literals = new GRBLinExpr();
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
//                                literals.Add(taskAssign[(t, i)]);
//                                count++;
//                            }
//                        }

//                        if (count > 0)
//                        {
//                            instructorDayStatus[(i, d)] = problem.AddVar(0, 1, 0.0, GRB.BINARY, $"IDS[{i}][{d}]");

//                            problem.AddGenConstrIndicator(
//                                instructorDayStatus[(i, d)],
//                                1,
//                                literals >= 1,
//                                "auxilliaryConstraint"
//                            );

//                            problem.AddGenConstrIndicator(
//                                instructorDayStatus[(i, d)],
//                                0,
//                                literals == 0,
//                                "auxilliaryConstraint"
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
//                            Helper.GurobiHelper.WrapperExpr(
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
//                            Helper.GurobiHelper.WrapperExpr(
//                                problem,
//                                Helper.GurobiHelper.CreateDelta(
//                                    problem,
//                                    dataModel.NumDays * dataModel.NumInstructors,
//                                    AddTeachingDayObjective(),
//                                    0
//                                ),
//                                0,
//                                int.MaxValue
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.AddTerm(
//                            dataModel.ObjectiveWeight[0],
//                            Helper.GurobiHelper.CreateSquare(
//                                problem,
//                                Helper.GurobiHelper.WrapperExpr(
//                                    problem,
//                                    AddTeachingDayObjective(),
//                                    0,
//                                    int.MaxValue
//                                ),
//                                0
//                            )
//                        );
//                        break;
//                }
//            }

//            // MINIMIZE TIME (numInstructors * numDays * numTimes)
//            if (dataModel.ObjectiveOptions[1] > 0)
//            {
//                var literals = new List<ILiteral>();
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
//                                    literals.Add(taskAssign[(t, i)]);
//                                }
//                            }

//                            if (literals.Count > 0)
//                            {
//                                instructorTimeStatus[(i, d, ti)] = problem.NewBoolVar($"ITS[{i}][{d}][{ti}]");
//                                problem.Add(LinearExpr.Sum(literals) > 0).OnlyEnforceIf(instructorTimeStatus[(i, d, ti)]);
//                                problem.Add(LinearExpr.Sum(literals) == 0).OnlyEnforceIf(instructorTimeStatus[(i, d, ti)].Not());
//                            }
//                            literals.Clear();
//                        }
//                    }

//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.Add(dataModel.ObjectiveWeight[1] * AddTeachingTimeObjective());
//                        break;
//                    case 2:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[1]
//                            * Helper.OrtoolsHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTimes
//                                    * dataModel.NumDays
//                                    * dataModel.NumInstructors,
//                                AddTeachingTimeObjective(),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[1]
//                            * Helper.OrtoolsHelper.CreateSquare(
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
//                var firstLiterals = new List<ILiteral>();
//                var secondLiterals = new List<ILiteral>();
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
//                                    firstLiterals.Add(taskAssign[(t, i)]);
//                                }
//                            }

//                            instructorSegmentStatus[(i, d, sm)] = problem.NewBoolVar($"ISS[{i}][{d}][{sm}]");
//                            if (firstLiterals.Count == 0)
//                            {
//                                //problem.AddHint(instructorSegmentStatus[(i, d, sm)], 0);
//                            }
//                            problem.Add(LinearExpr.Sum(firstLiterals) > 0).OnlyEnforceIf(instructorSegmentStatus[(i, d, sm)]);
//                            problem.Add(LinearExpr.Sum(firstLiterals) == 0).OnlyEnforceIf(instructorSegmentStatus[(i, d, sm)].Not());
//                            firstLiterals.Clear();
//                        }

//                        for (int p = 0; p < (1 << dataModel.NumSegments); p++)
//                        {
//                            foreach (int sm in dataModel.AllSegments)
//                            {
//                                if ((p & (1 << (dataModel.NumSegments - sm - 1))) > 0)
//                                {
//                                    secondLiterals.Add(Helper.OrtoolsHelper.BoolState(instructorSegmentStatus[(i, d, sm)], true));
//                                }
//                                else
//                                {
//                                    secondLiterals.Add(Helper.OrtoolsHelper.BoolState(instructorSegmentStatus[(i, d, sm)], false));
//                                }
//                            }

//                            instructorPatternStatus[(i, d, p)] = problem.NewBoolVar($"IPS[{i}][{d}[{p}]");
//                            problem.Add(LinearExpr.Sum(secondLiterals) == dataModel.NumSegments).OnlyEnforceIf(instructorPatternStatus[(i, d, p)]);
//                            problem.Add(LinearExpr.Sum(firstLiterals) != dataModel.NumSegments).OnlyEnforceIf(instructorPatternStatus[(i, d, p)].Not());
//                            secondLiterals.Clear();
//                        }

//                    }
//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.Add(dataModel.ObjectiveWeight[2] * AddPatternCostObjective());
//                        break;
//                    case 2:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[2]
//                            * Helper.OrtoolsHelper.CreateDelta(
//                                problem,
//                                (1 << dataModel.NumSegments)
//                                * dataModel.NumDays
//                                * dataModel.NumInstructors
//                                * dataModel.NumSegments,
//                                AddPatternCostObjective(),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[2]
//                            * Helper.OrtoolsHelper.CreateSquare(
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
//                var literals = new List<ILiteral>();
//                foreach (int i in dataModel.AllInstructors)
//                {
//                    foreach (int s in dataModel.AllSubjects)
//                    {
//                        foreach (int t in dataModel.AllTasks)
//                        {
//                            if (dataModel.TaskSubjectMapping[t] == s)
//                            {
//                                literals.Add(taskAssign[(t, i)]);
//                            }
//                        }

//                        instructorSubjectStatus[(i, s)] = problem.NewBoolVar($"ISUS[{i}][{s}]");
//                        if (literals.Count == 0)
//                        {
//                            //problem.AddHint(instructorSubjectStatus[(i, s)], 0);
//                        }
//                        problem.Add(LinearExpr.Sum(literals) > 0).OnlyEnforceIf(instructorSubjectStatus[(i, s)]);
//                        problem.Add(LinearExpr.Sum(literals) == 0).OnlyEnforceIf(instructorSubjectStatus[(i, s)].Not());
//                        literals.Clear();
//                    }
//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.Add(dataModel.ObjectiveWeight[3] * AddSubjectDiversityObjective());
//                        break;
//                    case 2:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[3]
//                            * Helper.OrtoolsHelper.CreateDelta(
//                                problem,
//                                dataModel.NumSubjects,
//                                AddSubjectDiversityObjective(),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[3]
//                            * Helper.OrtoolsHelper.CreateSquare(
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
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[4]
//                            * AddQuotaReachedObjective()
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[4]
//                            * Helper.OrtoolsHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTasks,
//                                AddQuotaReachedObjective(),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[4]
//                            * Helper.OrtoolsHelper.CreateSquare(
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
//                var sumList = new List<BoolVar>();
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

//                            var product = problem.NewBoolVar("auxiliaryVariable");

//                            problem.AddMinEquality(
//                                product,
//                                new[] {
//                                    taskAssign[(n1, i)],
//                                    taskAssign[(n2, i)]
//                                }
//                            );

//                            sumList.Add(product);
//                        }
//                        assignProduct[(n1, n2)] = LinearExpr.Sum(sumList);
//                        sumList.Clear();
//                    }
//                }

//                switch (dataModel.Strategy)
//                {
//                    case 1:
//                        totalDeltas.Add(dataModel.ObjectiveWeight[5] * AddWalkingDistanceObjective());
//                        break;
//                    case 2:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[5]
//                            * Helper.OrtoolsHelper.CreateDelta(
//                                problem,
//                                Int32.MaxValue,
//                                AddWalkingDistanceObjective(),
//                                0
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[5]
//                            * Helper.OrtoolsHelper.CreateSquare(
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
//                        totalDeltas.Add(
//                            -1
//                            * dataModel.ObjectiveWeight[6]
//                            * AddSubjectPreferenceObjective()
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[6]
//                            * Helper.OrtoolsHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTasks * 5,
//                                AddSubjectPreferenceObjective(),
//                                dataModel.NumTasks * 5
//                                )
//                            );
//                        break;
//                    case 3:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[6]
//                            * Helper.OrtoolsHelper.CreateSquare(
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
//                        totalDeltas.Add(
//                            -1
//                            * dataModel.ObjectiveWeight[7]
//                            * AddSlotPreferenceObjective()
//                        );
//                        break;
//                    case 2:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[7]
//                            * Helper.OrtoolsHelper.CreateDelta(
//                                problem,
//                                dataModel.NumTasks * 5,
//                                AddSlotPreferenceObjective(),
//                                dataModel.NumTasks * 5
//                            )
//                        );
//                        break;
//                    case 3:
//                        totalDeltas.Add(
//                            dataModel.ObjectiveWeight[7]
//                            * Helper.OrtoolsHelper.CreateSquare(
//                                problem,
//                                AddSlotPreferenceObjective(),
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
//                    problem.SetObjective(totalDeltas, GRB.MINIMIZE);
//                    break;
//                case 2:
//                    problem.SetObjective(totalDeltas, GRB.MINIMIZE);
//                    break;
//                case 3:
//                    problem.SetObjective(totalDeltas, GRB.MINIMIZE);
//                    break;
//            }

//            problem.Optimize(); //

//            if (
//                problem.Status == GRB.Status.OPTIMAL
//            )
//            {
//                return Helper.GurobiHelper.GetResults(
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
//                    taskAssign[(t, i)] = problem.AddVar(0, 1, 0.0, GRB.BINARY, $"A[{t}][{i}]");
//                }
//            }
//        }

//        // Constraints
//        private void AddTaskInstructorConstraint()
//        {
//            var literals = new GRBLinExpr();
//            foreach (var t in dataModel.AllTasks)
//            {
//                foreach (var i in dataModel.AllInstructorsWithBackup)
//                {
//                    literals.AddTerm(1, taskAssign[(t, i)]);

//                }
//                problem.AddConstr(literals == 1, $"OneTaskOneInstructor[{t}]");
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

//            var taskAssignedThatSlot = new GRBLinExpr();
//            var taskAssignedConflictWithThatSlot = new GRBLinExpr();

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

//                    var indicatorVar = problem.AddVar(0, 1, 0.0, GRB.BINARY, $"auxilliaryVariable");

//                    problem.AddGenConstrIndicator(indicatorVar, 1, taskAssignedThatSlot >= 1, $"IndicatorLeft[{i}][{s}]");
//                    problem.AddGenConstrIndicator(indicatorVar, 0, taskAssignedThatSlot <= 0, $"IndicatorRight[{i}][{s}]");
//                    problem.AddGenConstrIndicator(indicatorVar, 1, taskAssignedConflictWithThatSlot <= dataModel.SlotConflict[s, s], $"IndicatorRight[{i}][{s}]");

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
//                    problem.AddConstr(taskAssign[(data.Item2, data.Item1)] == 1, $"preAssign[{data.Item2}][{data.Item1}]");
//                }
//                if (data.Item3 == -1)
//                {
//                    problem.AddConstr(taskAssign[(data.Item2, data.Item1)] == 0, $"preAssign[{data.Item2}][{data.Item1}]");
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
//                        problem.AddConstr(taskAssign[(t, i)] == 0, $"InstructorAbility[{t}][{i}]");
//                    }
//                }
//            }
//        }

//        private void AddQuotaConstraint()
//        {
//            var taskAssigned = new GRBLinExpr();
//            foreach (var i in dataModel.AllInstructorsWithBackup)
//            {
//                foreach (var t in dataModel.AllTasks)
//                {
//                    taskAssigned.AddTerm(1, taskAssign[(t, i)]);
//                }
//                problem.AddRange(
//                    taskAssigned,
//                    dataModel.InstructorMinQuota[i],
//                    dataModel.InstructorQuota[i],
//                    $"InstructorQuota[{i}]"
//                );
//                taskAssigned.Clear();
//            }
//        }
//        #endregion

//        #region Objectives
//        // MINIMIZE DAY
//        private GRBLinExpr AddTeachingDayObjective()
//        {
//            var teachingDay = new GRBLinExpr();
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
//        private GRBLinExpr AddTeachingTimeObjective()
//        {
//            var teachingTime = new GRBLinExpr();
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
//        private GRBLinExpr AddPatternCostObjective()
//        {
//            var allPatternCost = new GRBLinExpr();
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
//        private GRBVar AddSubjectDiversityObjective()
//        {
//            var literals = new GRBLinExpr();
//            var subjectDiversity = new GRBLinExpr();
//            var result = problem.AddVar(
//                0,
//                dataModel.NumSubjects,
//                0.0,
//                GRB.INTEGER,
//                "subjectDiversity"
//            );
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int s in dataModel.AllSubjects)
//                {
//                    literals.AddTerm(1, instructorSubjectStatus[(i, s)]);
//                }
//                subjectDiversity.Add(literals);
//                problem.AddConstr(result >= subjectDiversity, "auxilliaryConstraint");
//                literals.Clear();
//                subjectDiversity.Clear();
//            }
//            return result;
//        }

//        // MINIMIZE QUOTA DIFF
//        private GRBLinExpr AddQuotaReachedObjective()
//        {
//            var quotaDifference = new GRBLinExpr();
//            var sumExpr = new GRBLinExpr();
//            var result = problem.AddVar(
//                0,
//                dataModel.NumTasks,
//                0.0,
//                GRB.INTEGER,
//                "maxQuotaDifference"
//            );
//            foreach (int i in dataModel.AllInstructors)
//            {
//                foreach (int t in dataModel.AllTasks)
//                {
//                    sumExpr.AddTerm(1, taskAssign[(t, i)]);
//                }

//                quotaDifference.Add(
//                    dataModel.InstructorQuota[i]
//                    - sumExpr
//                );

//                problem.AddConstr(result >= quotaDifference, "auxilliaryConstraint");

//                sumExpr.Clear();
//                quotaDifference.Clear();
//            }

//            return result;
//        }

//        // MINIMIZE WALKING DISTANCE
//        private GRBLinExpr AddWalkingDistanceObjective()
//        {
//            var walkingDistance = new GRBLinExpr();
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

//                    var auxilliaryVariable = problem.AddVar(0, int.MaxValue, 0.0, GRB.INTEGER, "auxilliaryVariable");

//                    problem.AddConstr(
//                        assignProduct[(n1, n2)]
//                        ==
//                        auxilliaryVariable,
//                        "auxilliaryConstraint"
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
//                        auxilliaryVariable
//                    );
//                }
//            return walkingDistance;
//        }

//        // MAXIMIZE SUBJECT PREFERENCE
//        private GRBLinExpr AddSubjectPreferenceObjective()
//        {
//            var sumExpr = new GRBLinExpr();
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
//        private GRBLinExpr AddSlotPreferenceObjective()
//        {
//            var sumExpr = new GRBLinExpr();
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
