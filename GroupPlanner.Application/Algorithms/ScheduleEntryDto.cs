using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Domain.Entities;

namespace GroupPlanner.Application.Algorithms
{
    public class ScheduleEntryDto
    {
        public DateTime Date { get; set; }
        public SubtaskDto? Subtask { get; set; }
        public double Hours { get; set; }

        public string? SubtaskDescription => Subtask?.Description;
        public string? TaskEncodedName => Subtask?.TaskEncodedName;
    }
}
