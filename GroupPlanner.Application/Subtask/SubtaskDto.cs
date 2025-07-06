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
        public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.NotStarted;
        public int EstimatedTime { get; set; } = 1;
        public string? TaskEncodedName { get; set; }
        public DateTime? TaskDeadline { get; set; }
        public int Order { get; set; }
    }
}
