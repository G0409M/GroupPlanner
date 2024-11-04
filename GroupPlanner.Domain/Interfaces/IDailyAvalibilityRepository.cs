using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Interfaces
{
    public interface IDailyAvailabilityRepository
    {
        System.Threading.Tasks.Task Create(DailyAvailability dailyAvailability);
        Task<IEnumerable<DailyAvailability>> GetAllByUserId(string CreatedById);
        Task<DailyAvailability?> GetByDateAndUserId(DateTime date, string CreatedById);
        Task<DailyAvailability?> GetById(int id);
        System.Threading.Tasks.Task Commit();
        System.Threading.Tasks.Task Update(DailyAvailability dailyAvailability);
        System.Threading.Tasks.Task Delete(int id);
    }
}
