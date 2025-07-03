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
            int score = 0;

            // grupowanie po podzadaniu
            var groupedBySubtask = schedule
                .Where(h => h.Subtask != null)
                .GroupBy(h => h.Subtask)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Date).ToList());

            // 1️⃣ punkty za pełne przypisanie godzin
            foreach (var sub in subtasks)
            {
                var entries = groupedBySubtask.ContainsKey(sub)
                    ? groupedBySubtask[sub]
                    : new List<ScheduleEntryDto>();

                int total = entries.Sum(e => e.Hours);
                if (total == sub.EstimatedTime)
                {
                    score += 50; // premia za komplet
                }
                else
                {
                    score -= 20; // kara za brak godzin
                }
            }

            // 2️⃣ punkty za przestrzeganie kolejności
            var groupedByTask = subtasks.GroupBy(x => x.TaskEncodedName);
            foreach (var taskGroup in groupedByTask)
            {
                var ordered = taskGroup.OrderBy(s => s.Order).ToList();
                DateTime? lastEnd = null;

                foreach (var sub in ordered)
                {
                    if (!groupedBySubtask.ContainsKey(sub))
                        continue;

                    var subEntries = groupedBySubtask[sub];
                    var earliest = subEntries.First().Date;

                    if (lastEnd.HasValue && earliest < lastEnd.Value)
                    {
                        score -= 100; // mocna kara
                    }
                    else
                    {
                        score += 30; // premia za poprawną kolejność
                    }

                    lastEnd = subEntries.Last().Date;
                }
            }

            // 3️⃣ punkty za przestrzeganie deadline
            foreach (var task in tasks)
            {
                var taskEntries = schedule
                    .Where(e => e.Subtask != null && e.Subtask.TaskEncodedName == task.EncodedName)
                    .ToList();

                if (!taskEntries.Any())
                {
                    score -= 100; // kara brak planu
                    continue;
                }

                var maxDate = taskEntries.Max(e => e.Date);
                if (task.Deadline.HasValue && maxDate > task.Deadline.Value)
                {
                    score -= 50;
                }
                else
                {
                    score += 20;
                }
            }

            // 4️⃣ punkty za brak przekroczenia dostępności
            var dayStats = schedule
                .GroupBy(e => e.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Used = g.Sum(e => e.Hours),
                    Available = availability.FirstOrDefault(a => a.Date == g.Key)?.AvailableHours ?? 0
                })
                .ToList();

            foreach (var day in dayStats)
            {
                if (day.Used > day.Available)
                {
                    score -= 50; // kara za nadgodziny
                }
                else
                {
                    score += 10; // premia
                }
            }

            // 5️⃣ punkty za skupienie godzin (jedno podzadanie na dzień)
            foreach (var sub in subtasks)
            {
                var entries = groupedBySubtask.ContainsKey(sub)
                    ? groupedBySubtask[sub]
                    : new List<ScheduleEntryDto>();

                var uniqueDays = entries.Select(e => e.Date).Distinct().Count();

                if (uniqueDays <= 2)
                {
                    score += 30; // super
                }
                else if (uniqueDays <= 4)
                {
                    score += 10; // ok
                }
                else
                {
                    score -= 20; // rozproszone
                }
            }

            // 6️⃣ punktacja za równomierne rozłożenie (jakość planu)
            if (dayStats.Any())
            {
                var avgLoad = dayStats.Average(x => (double)x.Used / x.Available);
                var deviation = dayStats.Sum(x => Math.Abs((double)x.Used / x.Available - avgLoad));
                score -= (int)(deviation * 10);
            }

            return score;
        }



    }




}
