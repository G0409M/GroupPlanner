using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace GroupPlanner.Infrastructure.Repositories
{
    public class DailyAvailabilityRepository : IDailyAvailabilityRepository
    {
        private readonly GroupPlannerDbContext _dbContext;

        public DailyAvailabilityRepository(GroupPlannerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async System.Threading.Tasks.Task Create(DailyAvailability dailyAvailability)
        {
            _dbContext.Add(dailyAvailability);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<DailyAvailability>> GetAllByUserId(string CreatedById)
        {
            return await _dbContext.DailyAvailabilities
                .Where(d => d.CreatedById == CreatedById)
                .ToListAsync();
        }

        public async Task<DailyAvailability?> GetByDateAndUserId(DateTime date, string CreatedById)
        {
            return await _dbContext.DailyAvailabilities
                .FirstOrDefaultAsync(d => d.Date == date && d.CreatedById == CreatedById);
        }

        public System.Threading.Tasks.Task Commit()
        {
            return _dbContext.SaveChangesAsync();
        }

        public async System.Threading.Tasks.Task Update(DailyAvailability dailyAvailability)
        {
            _dbContext.Update(dailyAvailability);
            await _dbContext.SaveChangesAsync();
        }

        public async System.Threading.Tasks.Task Delete(int id)
        {
            var availability = await _dbContext.DailyAvailabilities.FindAsync(id);
            if (availability != null)
            {
                _dbContext.DailyAvailabilities.Remove(availability);
                await _dbContext.SaveChangesAsync();
            }
        }
        public async Task<DailyAvailability?> GetById(int id)
        {
            return await _dbContext.DailyAvailabilities.FindAsync(id);
        }

    }
}
