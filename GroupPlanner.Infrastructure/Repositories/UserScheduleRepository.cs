using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace GroupPlanner.Infrastructure.Repositories
{
    public class UserScheduleRepository : IUserScheduleRepository
    {
        private readonly GroupPlannerDbContext _dbContext;

        public UserScheduleRepository(GroupPlannerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<UserSchedule>> GetAllByUserId(string userId)
        {
            return await _dbContext.UserSchedules
                .Where(s => s.CreatedById == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserSchedule?> GetById(int id)
        {
            return await _dbContext.UserSchedules.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async System.Threading.Tasks.Task Create(UserSchedule schedule)
        {
            _dbContext.UserSchedules.Add(schedule);
            await _dbContext.SaveChangesAsync();
        }

        public async System.Threading.Tasks.Task Delete(int id)
        {
            var schedule = await _dbContext.UserSchedules.FindAsync(id);
            if (schedule != null)
            {
                _dbContext.UserSchedules.Remove(schedule);
            }
        }

        public async System.Threading.Tasks.Task Commit()
        {
            await _dbContext.SaveChangesAsync();
        }
        public async Task<UserSchedule?> GetLatestByUserId(string userId)
        {
            return await _dbContext.UserSchedules
                .Where(s => s.CreatedById == userId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

    }
}
