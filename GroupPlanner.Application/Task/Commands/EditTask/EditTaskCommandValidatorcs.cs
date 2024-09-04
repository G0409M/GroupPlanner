using FluentValidation;
using FluentValidation.Validators;
using GroupPlanner.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Commands.EditTask
{
    internal class EditTaskCommandValidatorcs:AbstractValidator<EditTaskCommand>
    {
        public EditTaskCommandValidatorcs(ITaskRepository repository)
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
