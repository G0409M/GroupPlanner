using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task
{
    public class TaskDto
    {

        public string Name { get; set; } = default!;
        public TaskType TaskType { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime? Deadline { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Ważne;


        public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.Nierozpoczete;
        public string? EncodedName { get; set; }
        public bool IsEditable { get; set; }
    }
}
