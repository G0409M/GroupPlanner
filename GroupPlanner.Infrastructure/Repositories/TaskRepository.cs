using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;


namespace GroupPlanner.Infrastructure.Repositories
{
    internal class TaskRepository : ITaskRepository
    {
        private GroupPlannerDbContext _dbContext;
        public TaskRepository(GroupPlannerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task Commit()
        => _dbContext.SaveChangesAsync();
        

        public async Task Create(Domain.Entities.Task task)
        {
            _dbContext.Add(task);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<Domain.Entities.Task?>> GetAll()
            => await _dbContext.Tasks.ToListAsync();

        public async Task<Domain.Entities.Task?> GetByEncodedName(string encodedName)
          => await _dbContext.Tasks.FirstAsync(c=>c.EncodedName==encodedName);

        public Task<Domain.Entities.Task?> GetByName(string name)
            => _dbContext.Tasks.FirstOrDefaultAsync(cw=> cw.Name.ToLower()==name.ToLower());
        public async Task<IEnumerable<Domain.Entities.Task>> GetAllByUserId(string userId)  // Poprawiony typ zwracany
        {
            return await _dbContext.Tasks
                .Where(t => t.CreatedById == userId)
                .ToListAsync();
        }
        public async Task Delete(string encodedName)
        {
            var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.EncodedName == encodedName);

            if (task != null)
            {
                _dbContext.Tasks.Remove(task);
                await _dbContext.SaveChangesAsync();
            }
        }

    }
}
