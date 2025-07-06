using GroupPlanner.Infrastructure.Persistance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Infrastructure.Seeders
{
    public class GroupPlannerSeeder
    {
        private readonly GroupPlannerDbContext _dbContext;
        public GroupPlannerSeeder(GroupPlannerDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Seed()
        {
            if(await _dbContext.Database.CanConnectAsync())
            {
                if(!_dbContext.Tasks.Any())
                {
                    var T1 = new Domain.Entities.Task()
                    {
                        Name = "Projekt z Ekonometrii",
                        TaskType = Domain.Entities.TaskType.Task,
                        Details = new Domain.Entities.TaskDetails()
                        {
                            Description = "Projekt zaliczeniowy",
                            Deadline = DateTime.UtcNow.AddDays(10)
                        },
                        ProgressStatus = Domain.Entities.ProgressStatus.NotStarted
                    };

                    T1.EncodeName();
                    _dbContext.Tasks.Add(T1);
                    await _dbContext.SaveChangesAsync();


                }
            }
        }
    }
}
