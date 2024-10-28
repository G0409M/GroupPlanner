using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Commands.DelateTask
{
    internal class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
    {
        private readonly ITaskRepository _repository;
        private readonly IUserContext _userContext;

        public DeleteTaskCommandHandler(ITaskRepository repository, IUserContext userContext)
        {
            _repository = repository;
            _userContext = userContext;
        }

        public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repository.GetByEncodedName(request.EncodedName);
            if (task == null)
            {
                throw new InvalidOperationException("Task not found.");
            }

            var user = _userContext.GetCurrentUser();
            var isDeletable = user != null && (task.CreatedById == user.Id || user.IsInRole("Moderator"));
            if (!isDeletable)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this task.");
            }

            await _repository.Delete(request.EncodedName);
            return Unit.Value;
        }
    }
}
