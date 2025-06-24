using AutoMapper;
using FluentValidation;
using GroupPlanner.Application.Mapping;
using GroupPlanner.Application.ApplicationUser;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation.AspNetCore;
using GroupPlanner.Application.Task;
using GroupPlanner.Application.Algorithms.Genetic;

namespace GroupPlanner.Application
{
    public static class ServiceCollectionExtension
    {
        public static void AddApplication(this IServiceCollection services)
        {
            // Rejestracja UserContext
            services.AddScoped<IUserContext, UserContext>();

            // Rejestracja AutoMappera z profilem korzystającym z IUserContext
            services.AddScoped<IMapper>(provider =>
            {
                var userContext = provider.GetRequiredService<IUserContext>();
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile(new TaskMappingProfile(userContext));
                });

                return config.CreateMapper();
            });

            // Rejestracja FluentValidation z automatyczną walidacją
            services.AddValidatorsFromAssemblyContaining<TaskDto>();
            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationClientsideAdapters();

            services.AddScoped<IGeneticAlgorithmService, GeneticAlgorithmService>();
        }
    }
}
