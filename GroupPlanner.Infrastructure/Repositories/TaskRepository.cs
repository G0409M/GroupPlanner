using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Infrastructure.Repositories
{
    internal class TaskRepository : ITaskRepository
    {
        private GroupPlannerDbContext _dbContext;
        public TaskRepository(GroupPlannerDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Create(Domain.Entities.Task task)
        {
            _dbContext.Add(task);
            await _dbContext.SaveChangesAsync();
        }
        public Task<Domain.Entities.Task> GetByName(string name)
            => _dbContext.Tasks.FirstOrDefaultAsync(cw=> cw.Name.ToLower()==name.ToLower());
    }
}
