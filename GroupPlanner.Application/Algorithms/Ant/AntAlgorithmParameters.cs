using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms.Ant
{
    public class AntAlgorithmParameters
    {
        public int Iterations { get; set; } = 50;
        public int AntCount { get; set; } = 20;
        public double Alpha { get; set; } = 1.0;
        public double Beta { get; set; } = 2.0;
        public double EvaporationRate { get; set; } = 0.5;
        public double Q { get; set; } = 100.0;
    }
}
