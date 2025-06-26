using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.AlgorithmResult
{
    public class AlgorithmResultDto
    {
        public int Id { get; set; }
        public AlgorithmType Algorithm { get; set; } 
        public DateTime CreatedAt { get; set; }
        public string? CreatedById { get; set; }
        public double ResultValue { get; set; }
        public string ResultData { get; set; } = default!;
        public TimeSpan Duration { get; set; }
        public bool IsEditable { get; set; }
        public string? ParametersJson { get; set; }
        public string? ScoreHistoryJson { get; set; }
    }
}
