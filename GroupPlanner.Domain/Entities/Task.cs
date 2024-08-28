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
    public class Task
    {
        public  int Id { get; set; }
        public string Name { get; set; } = default!;
        public TaskDetails Details { get; set; } = default!;
        public TaskType TaskType { get; set; } = default!;
        public string EncodedName { get; private set; } = default!;

        public void EncodeName() => EncodedName = Name.ToLower().Replace(" ", "-");
    }
}
