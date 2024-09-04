using AutoMapper;
using GroupPlanner.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Queries.GetAllTasks
{
    public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, IEnumerable<TaskDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public GetAllTasksQueryHandler(ITaskRepository taskRepository, IMapper mapper)
        {
            this._taskRepository = taskRepository;
            this._mapper = mapper;
        }
        public async Task<IEnumerable<TaskDto>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
        {
            var tasks = await _taskRepository.GetAll();
            var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);
            return dtos;
        }
    }
}
