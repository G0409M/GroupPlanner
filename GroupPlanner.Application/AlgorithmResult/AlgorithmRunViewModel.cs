using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.AlgorithmResult
{
    public class AlgorithmRunViewModel
    {
        public AlgorithmType AlgorithmType { get; set; }

        // Genetic
        public int PopulationSize { get; set; } = 20;
        public int Generations { get; set; } = 50;
        public double CrossoverProbability { get; set; } = 0.7;
        public double MutationProbability { get; set; } = 0.1;
        public int TournamentSize { get; set; } = 3;

        // Ant Colony
        public int AntCount { get; set; } = 10;
        public int Iterations { get; set; } = 50;
        public double Alpha { get; set; } = 1.0;
        public double Beta { get; set; } = 2.0;
        public double EvaporationRate { get; set; } = 0.5;
        public double Q { get; set; } = 100.0;
    }

}