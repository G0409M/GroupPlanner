using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms.Ant
{
    public class AntAlgorithmParameters
    {
        public int Iterations { get; set; }
        public int AntCount { get; set; } 
        public double Alpha { get; set; }
        public double Beta { get; set; } 
        public double EvaporationRate { get; set; } 
        public double Q { get; set; } 
    }
}
