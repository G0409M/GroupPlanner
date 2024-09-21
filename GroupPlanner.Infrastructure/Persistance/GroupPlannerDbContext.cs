﻿using Microsoft.AspNetCore.Identity;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Domain.Entities.Task>()
                .OwnsOne(c => c.Details);
            modelBuilder.Entity<Domain.Entities.Task>()
               .HasMany(c => c.Subtasks)
               .WithOne(s => s.Task)
               .HasForeignKey(s => s.TaskId);

        }
    }
}
