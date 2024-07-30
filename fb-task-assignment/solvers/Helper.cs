using data_manipulation_visualisation;
using Google.OrTools.Sat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solvers
{
    public class Helper
    {
        public static string CalculateExecutionTime(Action function, string functionName)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            function.Invoke();
            stopwatch.Stop();

            TimeSpan timeSpan = stopwatch.Elapsed;
            string formattedTime = $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}:{timeSpan.Milliseconds:000}";

            Console.WriteLine($"[{functionName}'] took {formattedTime} to execute.");

            return formattedTime;
        }

        //public static class CplexCpHelper
        //{
        //    public static IIntVar CreateDelta(CP problem, int maxDelta, ILinearIntExpr actualValue, int targetValue)
        //    {
        //        var delta = problem.IntVar(0, maxDelta, "auxilaryVariable");
        //        problem.AddLe(actualValue, problem.Sum(targetValue, delta));
        //        problem.AddGe(actualValue, problem.Sum(targetValue, problem.Negative(delta)));
        //        return delta;
        //    }

        //    public static List<List<(int, int)>> GetResults(CP problem, DataModel dataModel, Dictionary<(int, int), IIntVar> taskAssign)
        //    {
        //        var result = new List<(int, int)>();

        //        foreach (int t in dataModel.AllTasks)
        //        {
        //            bool isAssigned = false;
        //            foreach (int i in dataModel.AllInstructors)
        //            {
        //                if (problem.GetValue(taskAssign[(t, i)]) == 1)
        //                {
        //                    isAssigned = true;
        //                    result.Add((t, i));
        //                }
        //            }
        //            if (!isAssigned)
        //            {
        //                result.Add((t, -1));
        //            }
        //        }

        //        List<List<(int, int)>> results = new List<List<(int, int)>> { result };
        //        return results;
        //    }
        //}

        //public static class CplexHelper
        //{
        //    public static IIntVar CreateSquare(Cplex problem, IIntExpr actualValue, int targetValue)
        //    {
        //        var result = problem.IntVar(0, int.MaxValue);
        //        var temp = problem.IntVar(-int.MaxValue, int.MaxValue);
        //        problem.AddEq(temp, problem.Sum(actualValue, -targetValue));
        //        problem.AddEq(result, problem.Prod(temp, temp));
        //        return result;
        //    }
        //    public static IIntVar CreateDelta(Cplex problem, int maxDelta, IIntVar actualValue, int targetValue)
        //    {
        //        var delta = problem.IntVar(0, maxDelta, "auxilaryVariable");
        //        problem.AddLe(actualValue, problem.Sum(targetValue, delta));
        //        problem.AddGe(actualValue, problem.Sum(targetValue, problem.Negative(delta)));
        //        return delta;
        //    }

        //    public static IIntVar WrapperExpr(Cplex problem, IIntExpr expr, int lowerBound, int upperBound)
        //    {
        //        var wrapperVariable = problem.IntVar(lowerBound, upperBound);
        //        problem.AddEq(wrapperVariable, expr);
        //        return wrapperVariable;
        //    }

        //    public static IIntExpr BoolState(Cplex problem, IIntVar variable, bool state)
        //    {
        //        if (state) return variable;
        //        else return problem.Negative(variable);
        //    }

        //    public static List<List<(int, int)>> GetResults(Cplex problem, DataModel dataModel, Dictionary<(int, int), IIntVar> taskAssign)
        //    {
        //        var result = new List<(int, int)>();

        //        foreach (int t in dataModel.AllTasks)
        //        {
        //            bool isAssigned = false;
        //            foreach (int i in dataModel.AllInstructors)
        //            {
        //                if (problem.GetValue(taskAssign[(t, i)]) == 1)
        //                {
        //                    isAssigned = true;
        //                    result.Add((t, i));
        //                }
        //            }
        //            if (!isAssigned)
        //            {
        //                result.Add((t, -1));
        //            }
        //        }

        //        List<List<(int, int)>> results = new List<List<(int, int)>> { result };
        //        return results;
        //    }
        //}

        //public static class GurobiHelper
        //{
        //    public static GRBVar WrapperExpr(GRBModel problem, GRBLinExpr expr, int lowerBound, int upperBound)
        //    {
        //        var wrapperVariable = problem.AddVar(lowerBound, upperBound, 0.0, GRB.INTEGER, "auxilliaryVariable");
        //        problem.AddConstr(wrapperVariable == expr, "auxiliaryConstraint");
        //        return wrapperVariable;
        //    }
        //    public static GRBLinExpr CreateDelta(GRBModel problem, int maxDelta, GRBLinExpr actualValue, int targetValue)
        //    {
        //        var delta = problem.AddVar(0, maxDelta, 0.0, GRB.INTEGER, "auxilaryVariable");
        //        problem.AddConstr(actualValue <= targetValue + delta, "auxiliaryConstraint");
        //        problem.AddConstr(actualValue >= targetValue - delta, "auxiliaryConstraint");
        //        return delta * 1;
        //    }

        //    public static GRBVar CreateSquare(GRBModel problem, GRBVar actualValue, int targetValue)
        //    {
        //        var result = problem.AddVar(0, int.MaxValue, 0.0, GRB.BINARY, "auxilaryVariable");
        //        var temp = problem.AddVar(-int.MaxValue, int.MaxValue, 0.0, GRB.BINARY, "auxilaryVariable");
        //        problem.AddConstr(temp == actualValue - targetValue, "auxiliaryConstraint");
        //        problem.AddQConstr(result == temp * temp, "auxiliaryConstraint");
        //        return result;
        //    }

        //    public static List<List<(int, int)>> GetResults(GRBModel problem, DataModel dataModel, Dictionary<(int, int), GRBVar> taskAssign)
        //    {
        //        var result = new List<(int, int)>();

        //        foreach (int t in dataModel.AllTasks)
        //        {
        //            bool isAssigned = false;
        //            foreach (int i in dataModel.AllInstructors)
        //            {
        //                if (problem.GetVarByName($"A[{t}][{i}]").X == 1)
        //                {
        //                    isAssigned = true;
        //                    result.Add((t, i));
        //                }
        //            }
        //            if (!isAssigned)
        //            {
        //                result.Add((t, -1));
        //            }
        //        }

        //        List<List<(int, int)>> results = new List<List<(int, int)>> { result };
        //        return results;
        //    }
        //}

        public static class OrtoolsHelper
        {
            #region Ortools
            public static LinearExpr CreateDelta(CpModel problem, int maxDelta, LinearExpr actualValue, int targetValue)
            {
                var delta = problem.NewIntVar(0, maxDelta, "auxilaryVariable");
                problem.Add(actualValue <= targetValue + delta);
                problem.Add(actualValue >= targetValue - delta);
                return delta;
            }
            public static LinearExpr CreateSquare(CpModel problem, LinearExpr actualValue, int targetValue)
            {
                var result = problem.NewIntVar(0, int.MaxValue, "auxilaryVariable");
                var temp = problem.NewIntVar(-int.MaxValue, int.MaxValue, "auxilaryVariable");
                problem.Add(temp == actualValue - targetValue);
                problem.AddMultiplicationEquality(result, new[] { temp, temp });
                return result;
            }
            public static ILiteral BoolState(ILiteral variable, bool state)
            {
                if (state) return variable;
                else return variable.Not();
            }

            public static List<List<(int, int)>> GetResults(CpSolver solver, DataModel dataModel, Dictionary<(int, int), BoolVar> taskAssign)
            {
                var result = new List<(int, int)>();

                foreach (int t in dataModel.AllTasks)
                {
                    bool isAssigned = false;
                    foreach (int i in dataModel.AllInstructors)
                    {
                        if (solver.Value(taskAssign[(t, i)]) == 1)
                        {
                            isAssigned = true;
                            result.Add((t, i));
                        }
                    }
                    if (!isAssigned)
                    {
                        result.Add((t, -1));
                    }
                }

                List<List<(int, int)>> results = new List<List<(int, int)>> { result };
                return results;
            }
            #endregion
        }
    }
}
