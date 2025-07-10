using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Entities
{
    public class UserSchedule
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Name { get; set; } = "Your Schedule"; 

        public string ScheduleDataJson { get; set; } = default!; 
        public string? CreatedById { get; set; }
        public IdentityUser? CreatedBy { get; set; }
    }
}
