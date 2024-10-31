using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask
{
    public class SubtaskDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = default!;
        public DateTime? Deadline { get; set; }
        public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.Nierozpoczete;
        public double EstimatedTime { get; set; } = 1.0;
    }
}
