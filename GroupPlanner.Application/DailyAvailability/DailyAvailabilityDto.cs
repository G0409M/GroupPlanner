using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.DailyAvailability
{
    public class DailyAvailabilityDto
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public double AvailableHours { get; set; }
    }

}
