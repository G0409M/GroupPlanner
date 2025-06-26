using GroupPlanner.Application.Subtask;

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
