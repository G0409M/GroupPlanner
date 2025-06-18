using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Entities
{
    public enum TaskType
    {
        Projekt,
        Zadanie,
        ZadanieDodatkowe,
        Spotkanie,
        Inne
    }
    public enum ProgressStatus
    {
        Nierozpoczete,  
        WTrakcie,  
        Ukonczone    
    }
    public enum TaskPriority
    {
        Nieważne = 1,
        MałoWażne = 2,
        Ważne = 3,
        BardzoWażne = 4
    }

    public class Task
    {
        public  int Id { get; set; }
        public string Name { get; set; } = default!;
        public TaskDetails Details { get; set; } = new TaskDetails();
        public TaskType TaskType { get; set; } = default!;

        public TaskPriority Priority { get; set; } = TaskPriority.Ważne;

        public string EncodedName { get; private set; } = default!;

        public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.Nierozpoczete;
        public string? CreatedById { get; set; }
        public IdentityUser? CreatedBy { get; set; }
        public List<Subtask> Subtasks { get; set; } = new (); 

        public void EncodeName() => EncodedName = Name.ToLower().Replace(" ", "-");
    }
}
