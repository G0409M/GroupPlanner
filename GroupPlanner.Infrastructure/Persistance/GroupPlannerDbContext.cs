using GroupPlanner.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Infrastructure.Persistance
{
    public class GroupPlannerDbContext: IdentityDbContext<IdentityUser>

    {
        public GroupPlannerDbContext(DbContextOptions<GroupPlannerDbContext> options): base(options)
        {

        }
        public DbSet<Domain.Entities.Task> Tasks { get; set; }

        public DbSet<Domain.Entities.Subtask> Subtasks { get; set; }
        public DbSet<DailyAvailability> DailyAvailabilities { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Domain.Entities.Task>()
                .OwnsOne(c => c.Details);
            modelBuilder.Entity<Domain.Entities.Task>()
               .HasMany(c => c.Subtasks)
               .WithOne(s => s.Task)
               .HasForeignKey(s => s.TaskId);


            modelBuilder.Entity<DailyAvailability>()
                .HasOne(d => d.CreatedBy)
                .WithMany()
                .HasForeignKey(d => d.CreatedById);

            modelBuilder.HasDefaultSchema("dbo");
            modelBuilder.Entity<IdentityUser>(
                entity =>
                {
                    entity.ToTable(name: "User");
                }
                );
            modelBuilder.Entity<IdentityRole>(
                entity =>
                {
                    entity.ToTable(name: "Role");
                }
                );
            modelBuilder.Entity<IdentityUserRole<string>>(
                entity =>
                {
                    entity.ToTable(name: "UserRole");
                }
                );
            modelBuilder.Entity<IdentityUserClaim<string>>(
                entity =>
                {
                    entity.ToTable(name: "UserClaim");
                }
                );
            modelBuilder.Entity<IdentityUserLogin<string>>(
                entity =>
                {
                    entity.ToTable(name: "UserLogin");
                }
                );
            modelBuilder.Entity<IdentityRoleClaim<string>>(
                entity =>
                {
                    entity.ToTable(name: "RoleClaim");
                }
                );
            modelBuilder.Entity<IdentityUserToken<string>>(
                entity =>
                {
                    entity.ToTable(name: "UserToken");
                }
                );
        }
    }
}
