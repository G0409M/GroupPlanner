using GroupPlanner.Application.Dto.Task;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Queries.GetAllTasks
{
    public class GetAllTasksQuery: IRequest<IEnumerable<TaskDto>>
    {
    }
}
