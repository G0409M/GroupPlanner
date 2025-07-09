using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Infrastructure.Repositories
{
    public class SubtaskRepository:ISubtaskRepository
    {
        private readonly GroupPlannerDbContext _dbContext;

        public SubtaskRepository(GroupPlannerDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async System.Threading.Tasks.Task Create(Subtask subtask)
        {
            _dbContext.Subtasks.Add(subtask);

            // od razu pobierz powiązany task
            var task = await _dbContext.Tasks
                .Include(t => t.Subtasks)
                .FirstOrDefaultAsync(t => t.Id == subtask.TaskId);

            if (task != null)
            {
                if (task.ProgressStatus == ProgressStatus.Completed)
                {
                    task.ProgressStatus = ProgressStatus.InProgress;
                }
            }

            await _dbContext.SaveChangesAsync();
        }


        public async Task<IEnumerable<Subtask>> GetAllByEncodedName(string encodedName)
        => await _dbContext.Subtasks
            .Where(s => s.Task.EncodedName == encodedName)
            .ToListAsync();

        public async System.Threading.Tasks.Task Delete(Subtask subtask)
        {
            // znajdź task przed usunięciem
            var task = await _dbContext.Tasks
                .Include(t => t.Subtasks)
                .FirstOrDefaultAsync(t => t.Id == subtask.TaskId);

            if (task == null)
                throw new InvalidOperationException("Task not found");

            _dbContext.Subtasks.Remove(subtask);
            await _dbContext.SaveChangesAsync();

            // PO usunięciu przelicz status zadania
            if (!task.Subtasks.Any(st => st.Id != subtask.Id))
            {
                // nie ma żadnych subtasks
                task.ProgressStatus = ProgressStatus.NotStarted;
            }
            else if (task.Subtasks.All(st => st.Id == subtask.Id || st.ProgressStatus == ProgressStatus.Completed))
            {
                // wszystkie pozostałe były Completed
                task.ProgressStatus = ProgressStatus.Completed;
            }
            else if (task.Subtasks.Any(st => st.Id != subtask.Id && st.ProgressStatus == ProgressStatus.InProgress))
            {
                // coś zostało w InProgress
                task.ProgressStatus = ProgressStatus.InProgress;
            }
            else
            {
                task.ProgressStatus = ProgressStatus.NotStarted;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<Subtask?> GetByIdAsync(int id)
        {
            return await _dbContext.Subtasks
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        public async Task<IEnumerable<Subtask>> GetAllByUserId(string userId)
        {
            return await _dbContext.Subtasks
                .Include(s => s.Task)
                .ThenInclude(t => t.Details)
                .Where(s => s.Task.CreatedById == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subtask>> GetAllByTaskId(int taskId)
        {
            return await _dbContext.Subtasks
                .Where(s => s.TaskId == taskId)
                .ToListAsync();
        }
        public async System.Threading.Tasks.Task UpdateWorkedHours(int subtaskId, int workedHours)
        {
            // pobierz od razu task + subtasks
            var task = await _dbContext.Tasks
        .Include(t => t.Subtasks)
        .FirstOrDefaultAsync(t => t.Subtasks.Any(s => s.Id == subtaskId));

            if (task == null)
                throw new InvalidOperationException("Task with given subtask not found");

            var subtask = task.Subtasks.First(s => s.Id == subtaskId);

            subtask.WorkedHours = Math.Clamp(workedHours, 0, subtask.EstimatedTime);

            // aktualizacja statusu podzadania
            if (subtask.WorkedHours == 0)
                subtask.ProgressStatus = ProgressStatus.NotStarted;
            else if (subtask.WorkedHours >= subtask.EstimatedTime)
                subtask.ProgressStatus = ProgressStatus.Completed;
            else
                subtask.ProgressStatus = ProgressStatus.InProgress;

            // logika statusu całego zadania:
            if (task.Subtasks.All(st => st.ProgressStatus == ProgressStatus.Completed))
            {
                task.ProgressStatus = ProgressStatus.Completed;
            }
            else if (task.Subtasks.All(st => st.ProgressStatus == ProgressStatus.NotStarted))
            {
                task.ProgressStatus = ProgressStatus.NotStarted;
            }
            else
            {
                task.ProgressStatus = ProgressStatus.InProgress;
            }

            await _dbContext.SaveChangesAsync();
            await _dbContext.Entry(task).ReloadAsync();
        }
        public async Task<List<Subtask>> GetByIds(List<int> ids)
        {
            return await _dbContext.Subtasks
                .Where(s => ids.Contains(s.Id))
                .ToListAsync();
        }




    }
}
