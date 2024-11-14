using FluentValidation;
using GroupPlanner.Domain.Interfaces;
namespace GroupPlanner.Application.Task.Commands.CreateTask
{
    public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator(ITaskRepository repository)
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50);
                

            RuleFor(c => c.Description)
                .NotEmpty();
            RuleFor(c => c.ProgressStatus)
                .IsInEnum().WithMessage("Invalid progress status.");
        }
    }
}
