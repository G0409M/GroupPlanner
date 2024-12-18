﻿using FluentValidation;
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
               .MaximumLength(50);
               

            RuleFor(c => c.Description)
                .NotEmpty();
            RuleFor(c => c.ProgressStatus)
                .IsInEnum().WithMessage("Invalid progress status.");
        }
    }
}
