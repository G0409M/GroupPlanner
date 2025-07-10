using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GroupPlanner.Application.AlgorithmResult
{

    public class AlgorithmRunViewModel
    {
        [Required]
        public AlgorithmType AlgorithmType { get; set; }

        // Genetic
        [Range(1, int.MaxValue, ErrorMessage = "Population size must be greater than zero.")]
        public int PopulationSize { get; set; } = 20;

        [Range(1, int.MaxValue, ErrorMessage = "Number of generations must be greater than zero.")]
        public int Generations { get; set; } = 50;

        [Range(0.0, 1.0, ErrorMessage = "Crossover probability must be between 0 and 1.")]
        public double CrossoverProbability { get; set; } = 0.7;

        [Range(0.0, 1.0, ErrorMessage = "Mutation probability must be between 0 and 1.")]
        public double MutationProbability { get; set; } = 0.1;

        [Range(1, int.MaxValue, ErrorMessage = "Tournament size must be greater than zero.")]
        public int TournamentSize { get; set; } = 3;

        // Ant Colony
        [Range(1, int.MaxValue, ErrorMessage = "Number of ants must be greater than zero.")]
        public int AntCount { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Number of iterations must be greater than zero.")]
        public int Iterations { get; set; } = 50;

        [Range(0.01, double.MaxValue, ErrorMessage = "Alpha must be greater than zero.")]
        public double Alpha { get; set; } = 1.0;

        [Range(0.01, double.MaxValue, ErrorMessage = "Beta must be greater than zero.")]
        public double Beta { get; set; } = 2.0;

        [Range(0.0, 1.0, ErrorMessage = "Evaporation rate must be between 0 and 1.")]
        public double EvaporationRate { get; set; } = 0.5;

        [Range(0.01, double.MaxValue, ErrorMessage = "Q must be greater than zero.")]
        public double Q { get; set; } = 100.0;
    }



}