using FluentValidation;
using GroupPlanner.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Dto.Task
{
    public class TaskValidator:AbstractValidator<TaskDto>
    {
        public TaskValidator(ITaskRepository repository)
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50)
                .Custom((value,context)=>
                {
                    var existingTask = repository.GetByName(value);
                });

            RuleFor(c => c.Description)
                .NotEmpty();
        }
    }
}
