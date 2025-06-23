using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.AlgorithmResult
{
    public class AlgorithmResultDetailsDto
    {
        public List<DailyAvailabilityDto> Availability { get; set; } = new();
        public List<TaskDto> Tasks { get; set; } = new();
        public List<SubtaskDto> Subtasks { get; set; } = new();
    }
}
