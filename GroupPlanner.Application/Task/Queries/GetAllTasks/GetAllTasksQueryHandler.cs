using AutoMapper;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Domain.Interfaces;
using MediatR;

namespace GroupPlanner.Application.Task.Queries.GetAllTasks
{
    public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, IEnumerable<TaskDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly IUserContext _userContext;

        public GetAllTasksQueryHandler(ITaskRepository taskRepository, IMapper mapper, IUserContext userContext)
        {
            this._taskRepository = taskRepository;
            this._mapper = mapper;
            this._userContext = userContext;
        }

        public async Task<IEnumerable<TaskDto>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
        {
            // Pobranie aktualnego użytkownika
            var currentUser = _userContext.GetCurrentUser();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Użytkownik nie jest zalogowany.");
            }

            // Pobranie tylko zadań stworzonych przez aktualnego użytkownika
            var tasks = await _taskRepository.GetAllByUserId(currentUser.Id);

            var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);
            return dtos;
        }
    }
}
