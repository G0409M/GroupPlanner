using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask
{
    public class SubtaskDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }
        public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.NotStarted;
        [Range(1, 1000)]
        public int EstimatedTime { get; set; } = 1;
        [Required]
        public int WorkedHours { get; set; } = 0;

        public string? TaskEncodedName { get; set; }
        public DateTime? TaskDeadline { get; set; }
        public int Order { get; set; }
    }
}
