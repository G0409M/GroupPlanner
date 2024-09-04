using GroupPlanner.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Commands.EditTask
{
    public class EditTaskCommandHandler : IRequestHandler<EditTaskCommand>
    {
        private readonly ITaskRepository _repository;

        public EditTaskCommandHandler(ITaskRepository repository)
        {
            _repository = repository;
        }
        public async Task<Unit> Handle(EditTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repository.GetByEncodedName(request.EncodedName!);
            task.Name = request.Name;
            task.TaskType = request.TaskType;
            task.Details.Description = request.Description;
            task.Details.Deadline = request.Deadline;
            await _repository.Commit();
            return Unit.Value;


        }
    }
}
