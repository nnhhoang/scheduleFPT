using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solvers
{
    public class SolverConfiguration
    {
        public int TimeLimit { get; set; } = 120;
        public int Threads { get; set; } = 2;
        public SolveMode Mode { get; set; } = SolveMode.Optimize;
        public int SolutionLimit { get; set; } = 10;
        public bool LogSearchProgress { get; set; } = true;

        public enum SolveMode
        {
            ConstraintOnly = 0,
            Optimize = 1
        }
    }
}
