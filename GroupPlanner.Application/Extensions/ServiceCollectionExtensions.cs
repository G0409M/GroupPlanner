using FluentValidation;
using FluentValidation.AspNetCore;
using GroupPlanner.Application.Mapping;
using GroupPlanner.Application.Task.Commands.CreateTask;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(typeof(CreateTaskCommand));

            services.AddAutoMapper(typeof(TaskMappingProfile));

            services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>()
                .AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
        }
    }
}
