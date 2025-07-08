using FluentValidation;
using GroupPlanner.Application.Task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TaskDtoValidator : AbstractValidator<TaskDto>
{
    public TaskDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Task name is required")
            .MaximumLength(100)
            .WithMessage("Task name cannot exceed 100 characters");

        RuleFor(x => x.Deadline)
            .NotNull()
            .WithMessage("Deadline is required")
            .GreaterThan(DateTime.Today)
            .WithMessage("Deadline must be in the future");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters");
    }
}
