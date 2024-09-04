using AutoMapper;
using GroupPlanner.Domain.Interfaces;
using MediatR;


namespace GroupPlanner.Application.Task.Queries.GetTaskByEncodedName
{
    public class GetTaskByEncodedNameQueryHandler : IRequestHandler<GetTaskByEncodedNameQuery, TaskDto>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public GetTaskByEncodedNameQueryHandler(ITaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }
        public async Task<TaskDto> Handle(GetTaskByEncodedNameQuery request, CancellationToken cancellationToken)
        {
           var task = await  _taskRepository.GetByEncodedName(request.EncodedName);
            var Dto = _mapper.Map<TaskDto>(task);
            return Dto;
        }
    }
}
