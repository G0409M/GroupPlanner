using GroupPlanner.Application.Algorithms;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Domain.Entities;

namespace GroupPlanner.MVC.Models
{
    public class DashboardViewModel
    {
        public List<SubtaskDto> Subtasks { get; set; } = new();
        public Dictionary<string, string> TaskNameMap { get; set; } = new();
        public UserSchedule? LatestSchedule { get; set; }
        public List<ScheduleEntryDto> PlannedEntries { get; set; } = new();
        public List<ScheduleEntryDto> UpcomingTasks { get; set; } = new();
        public KPIViewModel KPI { get; set; } = new();
    }
}
