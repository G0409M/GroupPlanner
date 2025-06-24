using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GroupPlanner.Infrastructure.Persistance.Repositories
{
    public class AlgorithmResultRepository : IAlgorithmResultRepository
    {
        private readonly GroupPlannerDbContext _context;

        public AlgorithmResultRepository(GroupPlannerDbContext context)
        {
            _context = context;
        }

        public async Task<List<AlgorithmResult>> GetAllByUserId(string userId)
        {
            return await _context.AlgorithmResults
                .Where(r => r.CreatedById == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<AlgorithmResult?> GetByIdAsync(int id)
        {
            return await _context.AlgorithmResults
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async System.Threading.Tasks.Task Create(AlgorithmResult result)
        {
            await _context.AlgorithmResults.AddAsync(result);
            await Commit();
        }

        public async System.Threading.Tasks.Task Delete(int id)
        {
            var result = await _context.AlgorithmResults.FindAsync(id);
            if (result != null)
            {
                _context.AlgorithmResults.Remove(result);
            }
        }

        public async System.Threading.Tasks.Task Commit()
        {
            await _context.SaveChangesAsync();
        }
    }
}
