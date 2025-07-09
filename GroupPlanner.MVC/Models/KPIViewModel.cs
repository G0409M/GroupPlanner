namespace GroupPlanner.MVC.Models
{
    public class KPIViewModel
    {
        // Completed
        public int CompletedCount { get; set; }
        public int CompletedPercent { get; set; }

        // In Progress
        public int InProgressCount { get; set; }
        public int InProgressPercent { get; set; }

        // Not Started
        public int NotStartedCount { get; set; }
        public int NotStartedPercent { get; set; }

        // Hours (use double if you track partial hours, e.g. 1.5h)
        public double TotalEstimated { get; set; }
        public double TotalWorked { get; set; }

        // Optional: Total subtasks for convenience/debugging
        public int TotalSubtasks => CompletedCount + InProgressCount + NotStartedCount;
    }
}
