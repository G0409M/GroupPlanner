using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Domain.Interfaces
{
    public interface ITaskRepository
    {
        Task Create(Domain.Entities.Task task);

        Task<Domain.Entities.Task?> GetByName(string name);

        Task<IEnumerable<Domain.Entities.Task?>> GetAll();

        Task<Domain.Entities.Task?> GetByEncodedName(string encodedName);
        Task Commit();

    }
}
