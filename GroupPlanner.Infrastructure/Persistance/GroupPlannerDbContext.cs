using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Infrastructure.Persistance
{
    public class GroupPlannerDbContext: DbContext
    {
        public GroupPlannerDbContext(DbContextOptions<GroupPlannerDbContext> options): base(options)
        {

        }
        public DbSet<Domain.Entities.Task> Tasks { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Domain.Entities.Task>()
                .OwnsOne(c => c. Details);
        }
    }
}
