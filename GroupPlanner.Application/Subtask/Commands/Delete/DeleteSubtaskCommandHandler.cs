using GroupPlanner.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask.Commands.Delete
{
    public class DeleteSubtaskCommandHandler : IRequestHandler<DeleteSubtaskCommand>
    {
        private readonly ISubtaskRepository _subtaskRepository;

        public DeleteSubtaskCommandHandler(ISubtaskRepository subtaskRepository)
        {
            _subtaskRepository = subtaskRepository;
        }

        public async Task<Unit> Handle(DeleteSubtaskCommand request, CancellationToken cancellationToken)
        {
            var subtask = await _subtaskRepository.GetByIdAsync(request.Id);

            if (subtask == null)
            {
                throw new KeyNotFoundException($"Subtask with ID {request.Id} not found.");
            }

            await _subtaskRepository.Delete(subtask);
            return Unit.Value;
        }
    }

}
