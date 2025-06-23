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
        System.Threading.Tasks.Task SaveAsync(AlgorithmResult result);
        Task<IEnumerable<AlgorithmResult>> GetAllAsync();
    }
}
