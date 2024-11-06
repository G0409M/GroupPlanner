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

            // Sprawdzamy, czy deadline podzadania nie wykracza poza deadline zadania
            if (request.Deadline.HasValue && task.Details.Deadline.HasValue && request.Deadline > task.Details.Deadline)
            {
                throw new InvalidOperationException("Deadline podzadania nie może przekraczać deadline zadania.");
            }

            var subtask = new Domain.Entities.Subtask()
            {
                Deadline = request.Deadline,
                Description = request.Description,
                TaskId = task.Id,
                ProgressStatus = request.ProgressStatus,
                EstimatedTime = request.EstimatedTime
            };
            await _subtaskRepository.Create(subtask);
            return Unit.Value;
        }

    }
}
