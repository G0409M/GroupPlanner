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
    
    public static class ScheduleEvaluator
    {
        public static double Evaluate(List<ScheduleEntryDto> schedule,List<SubtaskDto> subtasks,List<TaskDto> tasks,List<DailyAvailabilityDto> availability)
        {
            double penalty = 0;

            // grupujemy harmonogram po subtask
            var groupedBySubtask = schedule
                .Where(h => h.Subtask != null)
                .GroupBy(h => h.Subtask)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Date).ToList());

            var allSubtasks = subtasks;

            // kara za niezakończone zadania lub po terminie
            foreach (var task in tasks)
            {
                var taskEntries = schedule
                    .Where(h => h.Subtask != null && h.Subtask.TaskEncodedName == task.EncodedName)
                    .ToList();

                if (taskEntries.Any())
                {
                    var maxDate = taskEntries.Max(h => h.Date);
                    if (task.Deadline.HasValue && maxDate > task.Deadline.Value)
                    {
                        penalty += (int)task.Priority * 500;
                    }
                }
                else
                {
                    penalty += (int)task.Priority * 1000;
                }
            }

            // kara za niepełne przypisanie godzin dla podzadań
            foreach (var sub in allSubtasks)
            {
                var entries = groupedBySubtask.ContainsKey(sub)
                    ? groupedBySubtask[sub]
                    : new List<ScheduleEntryDto>();

                var total = entries.Sum(w => w.Hours);
                var diff = Math.Abs(sub.EstimatedTime - total);

                if (diff > 0)
                {
                    penalty += diff * (int)sub.ProgressStatus * (int)TaskPriority.Ważne * 200;
                }
            }

            // kara za złamanie kolejności podzadań w ramach zadania
            var taskGroups = allSubtasks.GroupBy(p => p.TaskEncodedName);
            foreach (var group in taskGroups)
            {
                var orderedSubs = group.OrderBy(p => p.Order).ToList();
                DateTime? lastEnd = null;

                foreach (var sub in orderedSubs)
                {
                    if (!groupedBySubtask.ContainsKey(sub))
                        continue;

                    var subSchedule = groupedBySubtask[sub];
                    var earliest = subSchedule.First().Date;

                    if (lastEnd.HasValue && earliest < lastEnd.Value)
                    {
                        penalty += (int)sub.ProgressStatus * 300;
                    }

                    lastEnd = subSchedule.Last().Date;
                }
            }

            // kara za przekroczenie dostępnych godzin w danym dniu
            var dayStats = schedule
                .GroupBy(h => h.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Used = g.Sum(x => x.Hours),
                    Available = availability.FirstOrDefault(a => a.Date == g.Key)?.AvailableHours ?? 0
                })
                .Where(x => x.Available > 0)
                .ToList();

            foreach (var day in dayStats)
            {
                if (day.Used > day.Available)
                {
                    var over = day.Used - day.Available;
                    penalty += over * 500;
                }
            }

            // kara za nierównomierne rozłożenie godzin
            if (dayStats.Any())
            {
                var avg = dayStats.Average(x => (double)x.Used / x.Available);
                var sumDev = dayStats.Sum(x => Math.Abs((double)x.Used / x.Available - avg));
                penalty += sumDev * 20;
            }
            const double maxPenalty = 100000;
            double normalized = 1.0 - Math.Min(penalty / maxPenalty, 1.0);
            return normalized;
        }




    }




}
