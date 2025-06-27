using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms
{
    public class EvaluatedScheduleDto
    {
        public List<ScheduleEntryDto> Schedule { get; set; } = new();
        public double Score { get; set; }
    }
    public static class ScheduleEvaluator
    {
        public static double Evaluate(
    List<ScheduleEntryDto> schedule,
    List<SubtaskDto> subtasks,
    List<TaskDto> tasks,
    List<DailyAvailabilityDto> availability)
        {
            double score = 0.0;

            var availabilityByDate = availability
            .GroupBy(a => a.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.AvailableHours)
            );


            var scheduleByDate = schedule
                .GroupBy(e => e.Date.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var task in tasks)
            {
                var taskSubtasks = subtasks
                    .Where(s => s.TaskEncodedName == task.EncodedName)
                    .OrderBy(s => s.Order)
                    .ToList();

                if (!taskSubtasks.Any())
                    continue;

                // Sprawdzenie deadline całego zadania
                var latestDate = schedule
                    .Where(e => taskSubtasks.Any(st => st.Id == e.Subtask.Id))
                    .Select(e => e.Date.Date)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();

                if (task.Deadline != null && latestDate > task.Deadline.Value.Date)
                {
                    var daysLate = (latestDate - task.Deadline.Value.Date).TotalDays;
                    score -= 15 * daysLate;
                }

                // Sprawdzenie kolejności podzadań
                for (int i = 0; i < taskSubtasks.Count - 1; i++)
                {
                    var first = taskSubtasks[i];
                    var second = taskSubtasks[i + 1];

                    var firstMaxDate = schedule
                        .Where(e => e.Subtask.Id == first.Id)
                        .Select(e => e.Date.Date)
                        .DefaultIfEmpty(DateTime.MinValue)
                        .Max();

                    var secondMinDate = schedule
                        .Where(e => e.Subtask.Id == second.Id)
                        .Select(e => e.Date.Date)
                        .DefaultIfEmpty(DateTime.MaxValue)
                        .Min();

                    if (firstMaxDate > secondMinDate)
                    {
                        // złamana kolejność
                        score -= 50;
                    }
                }
            }

            // Ocena przypisań subtasków
            foreach (var subtask in subtasks)
            {
                var assignedTime = schedule
                    .Where(e => e.Subtask.Id == subtask.Id)
                    .Sum(e => e.Hours);

                if (assignedTime < subtask.EstimatedTime)
                {
                    score -= 40 * (subtask.EstimatedTime - assignedTime);
                }
                else if (assignedTime > subtask.EstimatedTime)
                {
                    score -= 60 * (assignedTime - subtask.EstimatedTime);
                }
                else
                {
                    var parentTask = tasks.FirstOrDefault(t => t.EncodedName == subtask.TaskEncodedName);
                    var priority = parentTask?.Priority ?? TaskPriority.Ważne;
                    score += 20 * (int)priority;
                }

                foreach (var entry in schedule.Where(e => e.Subtask.Id == subtask.Id))
                {
                    var date = entry.Date.Date;
                    if (!availabilityByDate.TryGetValue(date, out var availableHours))
                    {
                        score -= 50;
                        continue;
                    }

                    var totalAssignedForThisDay = scheduleByDate[date].Sum(e => e.Hours);
                    if (totalAssignedForThisDay > availableHours)
                    {
                        score -= 30 * (totalAssignedForThisDay - availableHours);
                    }

                    if (entry.Hours > availableHours)
                    {
                        score -= 25;
                    }
                }
            }

            return score;
        }


    }




}
