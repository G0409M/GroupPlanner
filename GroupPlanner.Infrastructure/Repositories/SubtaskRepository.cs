using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Infrastructure.Repositories
{
    public class SubtaskRepository:ISubtaskRepository
    {
        private readonly GroupPlannerDbContext _dbContext;

        public SubtaskRepository(GroupPlannerDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async System.Threading.Tasks.Task Create (Subtask subtask)
        {
            _dbContext.Subtasks.Add(subtask);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<Subtask>> GetAllByEncodedName(string encodedName)
        => await _dbContext.Subtasks
            .Where(s => s.Task.EncodedName == encodedName)
            .ToListAsync();

        public async System.Threading.Tasks.Task Delete(Subtask subtask)
        {
            _dbContext.Subtasks.Remove(subtask);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<Subtask?> GetByIdAsync(int id)
        {
            return await _dbContext.Subtasks
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        public async Task<IEnumerable<Subtask>> GetAllByUserId(string userId)
        {
            return await _dbContext.Subtasks
                .Include(s => s.Task) 
                .Where(s => s.Task.CreatedById == userId)
                .ToListAsync();
        }
    }
}
