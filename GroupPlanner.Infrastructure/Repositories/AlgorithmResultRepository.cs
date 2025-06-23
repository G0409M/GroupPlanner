using GroupPlanner.Domain.Entities;
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
    public class AlgorithmResultRepository : IAlgorithmResultRepository
    {
        private readonly GroupPlannerDbContext _dbContext;

        public AlgorithmResultRepository(GroupPlannerDbContext context)
        {
            _dbContext = context;
        }

        public async System.Threading.Tasks.Task SaveAsync(AlgorithmResult result)
        {
            _dbContext.AlgorithmResults.Add(result);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<AlgorithmResult>> GetAllAsync()
        {
            return await _dbContext.AlgorithmResults.ToListAsync();
        }
    }

}
