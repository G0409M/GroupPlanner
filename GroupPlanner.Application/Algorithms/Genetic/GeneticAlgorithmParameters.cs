using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms.Genetic
{
    public class GeneticAlgorithmParameters
    {
        public int PopulationSize { get; set; } 
        public int Generations { get; set; } 
        public double CrossoverProbability { get; set; } 
        public double MutationProbability { get; set; }
        public int TournamentSize { get; set; } 
        public int ElitismCount { get; set; } 
    }
}
