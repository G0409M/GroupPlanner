namespace GroupPlanner.MVC.Models
{
    public class UpcomingDayDto
    {
        public DateTime Date { get; set; }
        public List<UpcomingEntryDto> Entries { get; set; } = new();
    }

    public class UpcomingEntryDto
    {
        public DateTime Date { get; set; }
        public string TaskName { get; set; }
        public string SubtaskDescription { get; set; }
        public int Hours { get; set; }
        public int SubtaskId { get; set; }
        public int WorkedHours { get; set; }

    }
}
