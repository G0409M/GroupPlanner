using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask.Commands
{
    public class CreateSubtaskCommandValidator:AbstractValidator<CreateSubtaskCommand>
    {
        public CreateSubtaskCommandValidator()
        {
            RuleFor(s => s.TaskEncodedName).NotEmpty().NotNull();
            RuleFor(s=>s.Description).NotEmpty().NotNull();
            RuleFor(s => s.Deadline).NotEmpty().NotNull();
            RuleFor(s => s.ProgressStatus)
               .IsInEnum().WithMessage("Invalid progress status.");
            RuleFor(s => s.EstimatedTime)
                .GreaterThan(0).WithMessage("Estimated time must be greater than 0.");
        }
    }
}
