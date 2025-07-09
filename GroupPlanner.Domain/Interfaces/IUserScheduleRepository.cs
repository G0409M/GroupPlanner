using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Interfaces
{
    public interface IUserScheduleRepository
    {
        Task<IEnumerable<UserSchedule>> GetAllByUserId(string userId);
        Task<UserSchedule?> GetById(int id);
        System.Threading.Tasks.Task Create(UserSchedule schedule);
        System.Threading.Tasks.Task Delete(int id);
        System.Threading.Tasks.Task Commit();

        Task<UserSchedule?> GetLatestByUserId(string userId);
    }
}
