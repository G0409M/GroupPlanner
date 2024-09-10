using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Commands.EditTask
{
    internal class EditTaskCommandHandler : IRequestHandler<EditTaskCommand>
    {
        private readonly ITaskRepository _repository;
        private readonly IUserContext _userContext;

        public EditTaskCommandHandler(ITaskRepository repository, IUserContext userContext)
        {
            _repository = repository;
            _userContext = userContext;
        }
        public async Task<Unit> Handle(EditTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repository.GetByEncodedName(request.EncodedName!);
            var user = _userContext.GetCurrentUser();
            var isEditable = user != null && (task.CreatedById == user.Id|| user.IsInRole("Moderator"));
            if(!isEditable)
            {
                return Unit.Value;
            }

            if (task == null)
            {
                throw new InvalidOperationException("Task not found.");
            }
            task.TaskType = request.TaskType;
            task.Details.Description = request.Description;
            task.Details.Deadline = request.Deadline;
            await _repository.Commit();
            return Unit.Value;


        }
    }
}
