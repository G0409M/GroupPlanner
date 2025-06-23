using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Entities
{
    public enum AlgorithmType
    {
        Genetic,
        Ant
    }
    public class AlgorithmResult
    {
        public int Id { get; set; }

        public AlgorithmType Algorithm { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public double ResultValue { get; set; } 
        public string ResultData { get; set; } = default!;
        public TimeSpan Duration { get; set; }

        public string? CreatedById { get; set; }
        public IdentityUser? CreatedBy { get; set; }
    }

}
