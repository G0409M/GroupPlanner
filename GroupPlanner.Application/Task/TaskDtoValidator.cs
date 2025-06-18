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
        RuleFor(x => x.Name).NotEmpty().WithMessage("Task name is required");
        RuleFor(x => x.Deadline).GreaterThan(DateTime.Today).When(x => x.Deadline.HasValue)
            .WithMessage("Deadline must be in the future");
    }
}
