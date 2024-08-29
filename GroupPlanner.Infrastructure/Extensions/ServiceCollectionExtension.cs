using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using GroupPlanner.Infrastructure.Repositories;
using GroupPlanner.Infrastructure.Seeders;
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

            services.AddScoped<GroupPlannerSeeder>();

            services.AddScoped<ITaskRepository, TaskRepository>();

        }
    }
}
