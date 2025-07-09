using GroupPlanner.Application.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.UserSchedule
{
    public class UserScheduleDto
    {
        public string Name { get; set; } = default!;
        public List<ScheduleEntryDto> Entries { get; set; } = new();
    }
}
