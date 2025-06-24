using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Interfaces
{
    public interface IAlgorithmResultRepository
    {
        Task<List<AlgorithmResult>> GetAllByUserId(string userId);
        Task<AlgorithmResult?> GetByIdAsync(int id);
        System.Threading.Tasks.Task Create(AlgorithmResult result);
        System.Threading.Tasks.Task Delete(int id);
        System.Threading.Tasks.Task Commit();
    }
}
