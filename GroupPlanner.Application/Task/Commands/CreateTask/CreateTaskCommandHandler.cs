using AutoMapper;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Commands.CreateTask
{
    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly IUserContext _userContext;

        public CreateTaskCommandHandler(ITaskRepository taskRepository, IMapper mapper, IUserContext userContext)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
            _userContext = userContext;
        }
        public async Task<Unit> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            var currentUser = _userContext.GetCurrentUser();
            if (currentUser == null)
            {
                throw new InvalidOperationException("User is not authenticated.");
            }
            var task1 = _mapper.Map<Domain.Entities.Task>(request);
            task1.EncodeName();
            task1.Details.CreatedAt = DateTime.Now;
            task1.CreatedById = currentUser.Id;
            if (task1.ProgressStatus == ProgressStatus.Nierozpoczete)
            {
                task1.ProgressStatus = request.ProgressStatus;
            }

            await _taskRepository.Create(task1);

            return Unit.Value;

        }



    }
}
