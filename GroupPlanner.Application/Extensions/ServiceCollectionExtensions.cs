using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.Mapping;
using GroupPlanner.Application.Task.Commands.CreateTask;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GroupPlanner.Application.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserContext, UserContext>();
            services.AddMediatR(typeof(CreateTaskCommand));

            services.AddScoped(provider => new MapperConfiguration(cfg =>
            {
                var scope = provider.CreateScope();
                var userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
                cfg.AddProfile(new TaskMappingProfile(userContext));

            }).CreateMapper()
            );

            services.AddAutoMapper(typeof(TaskMappingProfile));

            services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>()
                .AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
        }
    }
}
