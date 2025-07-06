using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Entities
{
    public enum TaskType
    {
        [Display(Name = "Project")]
        Project,

        [Display(Name = "Task")]
        Task,

        [Display(Name = "Additional Task")]
        AdditionalTask,

        [Display(Name = "Other")]
        Other
    }

    public enum ProgressStatus
    {
        [Display(Name = "Not Started")]
        NotStarted,

        [Display(Name = "In Progress")]
        InProgress,

        [Display(Name = "Completed")]
        Completed
    }

    public enum TaskPriority
    {
        [Display(Name = "Low")]
        Low = 1,

        [Display(Name = "Medium")]
        Medium = 2,

        [Display(Name = "High")]
        High = 3,

        [Display(Name = "Critical")]
        Critical = 4
    }


    public class Task
    {
        public  int Id { get; set; }
        public string Name { get; set; } = default!;
        public TaskDetails Details { get; set; } = new TaskDetails();
        public TaskType TaskType { get; set; } = default!;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public string EncodedName { get; private set; } = default!;

        public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.NotStarted;
        public string? CreatedById { get; set; }
        public IdentityUser? CreatedBy { get; set; }
        public List<Subtask> Subtasks { get; set; } = new (); 

        public void EncodeName() => EncodedName = Name.ToLower().Replace(" ", "-");
    }
}
