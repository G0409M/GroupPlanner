using GroupPlanner.Application.Algorithms.Ant;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using GroupPlanner.Infrastructure.Persistance.Repositories;
using GroupPlanner.Infrastructure.Repositories;
using GroupPlanner.Infrastructure.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Infrastructure.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddInfrastructure (this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<GroupPlannerDbContext>(options => options.UseSqlServer(
                configuration.GetConnectionString("GroupPlanner")));

            services.AddDefaultIdentity<IdentityUser>()
                .AddRoles<IdentityRole>()
              .AddEntityFrameworkStores<GroupPlannerDbContext>();

            services.AddScoped<GroupPlannerSeeder>();

            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ISubtaskRepository, SubtaskRepository>();
            services.AddScoped<IDailyAvailabilityRepository, DailyAvailabilityRepository>(); 
            services.AddScoped<IAlgorithmResultRepository, AlgorithmResultRepository>();
            services.AddScoped<IAntAlgorithmService, AntAlgorithmService>();
            services.AddScoped<IUserScheduleRepository, UserScheduleRepository>();




        }
    }
}
