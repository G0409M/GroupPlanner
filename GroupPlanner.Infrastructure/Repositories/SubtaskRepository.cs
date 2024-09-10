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
    }
}
