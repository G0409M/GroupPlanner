using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask.Commands
{
    public class CreateSubtaskCommandHandler : IRequestHandler<CreateSubtaskCommand>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ISubtaskRepository _subtaskRepository;
        public readonly IUserContext _userContext;

        public CreateSubtaskCommandHandler(IUserContext userContext, ITaskRepository taskRepository, ISubtaskRepository subtaskRepository)
        {
            _userContext = userContext;
            _taskRepository = taskRepository;
            _subtaskRepository = subtaskRepository;
        }

        public async Task<Unit> Handle(CreateSubtaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByEncodedName(request.TaskEncodedName!);
            var user = _userContext.GetCurrentUser();
            var isEditable = user != null && (task.CreatedById == user.Id || user.IsInRole("Moderator"));
            if (!isEditable)
            {
                return Unit.Value;
            }
            var subtask = new Domain.Entities.Subtask()
            {
                Deadline = request.Deadline,
                Description = request.Description,
                TaskId = task.Id,
            };
            await _subtaskRepository.Create(subtask);
            return Unit.Value;
        }
    }
}
