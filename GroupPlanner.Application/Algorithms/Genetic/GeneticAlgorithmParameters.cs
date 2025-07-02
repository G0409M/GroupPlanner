using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms.Genetic
{
    public class GeneticAlgorithmParameters
    {
        public int PopulationSize { get; set; } = 20;
        public int Generations { get; set; } = 50;
        public double CrossoverProbability { get; set; } = 0.7;
        public double MutationProbability { get; set; } = 0.1;
        public int TournamentSize { get; set; } = 3;
        public int ElitismCount { get; set; } = 1;
    }
}
