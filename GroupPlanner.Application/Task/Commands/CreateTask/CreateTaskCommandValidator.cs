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
                .MaximumLength(50)
                .Custom((value, context) =>
                {
                    var existingTask = repository.GetByName(value).Result;
                    if (existingTask != null)
                    {
                        context.AddFailure($"{value} is not unique.");
                    }
                });

            RuleFor(c => c.Description)
                .NotEmpty();
        }
    }
}
