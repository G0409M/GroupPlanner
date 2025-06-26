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
        [Range(1, int.MaxValue, ErrorMessage = "Rozmiar populacji musi być większy od zera.")]
        public int PopulationSize { get; set; } = 20;

        [Range(1, int.MaxValue, ErrorMessage = "Liczba generacji musi być większa od zera.")]
        public int Generations { get; set; } = 50;

        [Range(0.0, 1.0, ErrorMessage = "Prawdopodobieństwo krzyżowania musi być między 0 a 1.")]
        public double CrossoverProbability { get; set; } = 0.7;

        [Range(0.0, 1.0, ErrorMessage = "Prawdopodobieństwo mutacji musi być między 0 a 1.")]
        public double MutationProbability { get; set; } = 0.1;

        [Range(1, int.MaxValue, ErrorMessage = "Rozmiar turnieju musi być większy od zera.")]
        public int TournamentSize { get; set; } = 3;

        // Ant Colony
        [Range(1, int.MaxValue, ErrorMessage = "Liczba mrówek musi być większa od zera.")]
        public int AntCount { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Liczba iteracji musi być większa od zera.")]
        public int Iterations { get; set; } = 50;

        [Range(0.01, double.MaxValue, ErrorMessage = "Alpha musi być większa od zera.")]
        public double Alpha { get; set; } = 1.0;

        [Range(0.01, double.MaxValue, ErrorMessage = "Beta musi być większa od zera.")]
        public double Beta { get; set; } = 2.0;

        [Range(0.0, 1.0, ErrorMessage = "Współczynnik parowania musi być między 0 a 1.")]
        public double EvaporationRate { get; set; } = 0.5;

        [Range(0.01, double.MaxValue, ErrorMessage = "Q musi być większe od zera.")]
        public double Q { get; set; } = 100.0;
    }


}