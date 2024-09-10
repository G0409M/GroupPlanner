using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask
{
    public class SubtaskDto
    {
        public string Description { get; set; } = default!;
        public DateTime? Deadline { get; set; }
    }
}
