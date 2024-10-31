using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Interfaces
{
    public interface ISubtaskRepository
    {
        System.Threading.Tasks.Task Create(Subtask subtask);
        System.Threading.Tasks.Task<IEnumerable<Subtask>> GetAllByEncodedName(string encodedName);
        System.Threading.Tasks.Task Delete(Subtask subtask);
        System.Threading.Tasks.Task<Subtask?> GetByIdAsync(int id);
    }
}
