using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Entities
{
    public class Subtask
    {
        public int Id { get; set; }
        public string Description { get; set; } = default!;
        public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.Nierozpoczete;
        public int EstimatedTime { get; set; }= 1;
        public int Order { get; set; }
        public int TaskId { get; set; } = default!;
        public Task Task { get; set; } = default!;
    }
}