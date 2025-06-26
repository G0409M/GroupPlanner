using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
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
            double timeFitScore = 1.0;
            double deadlineScore = 1.0;
            double orderScore = 1.0;
            double balanceScore = 1.0;
            double coverageScore = 1.0;
            double allocationScore = 1.0;

            var groupedByDate = schedule
                .GroupBy(e => e.Date)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Hours));

            // 1. timeFitScore: nie przekraczamy dostępności
            int badDays = groupedByDate
                .Count(day =>
                {
                    var dailyLimit = availability.FirstOrDefault(a => a.Date == day.Key)?.AvailableHours ?? 0;
                    return day.Value > dailyLimit;
                });
            timeFitScore = 1.0 - (badDays / (double)(groupedByDate.Count == 0 ? 1 : groupedByDate.Count));

            // 2. coverageScore: ile podzadań w ogóle zaplanowano
            var scheduledSubtaskIds = schedule
                .Select(s => s.Subtask?.Id)
                .Where(id => id != null)
                .Distinct()
                .ToHashSet();
            coverageScore = scheduledSubtaskIds.Count / (double)(subtasks.Count == 0 ? 1 : subtasks.Count);

            // 3. allocationScore: dopasowanie zaplanowanych godzin do EstimatedTime
            allocationScore = subtasks.Sum(st =>
            {
                var scheduled = schedule
                    .Where(s => s.Subtask?.Id == st.Id)
                    .Sum(s => s.Hours);

                var ratio = Math.Min(scheduled / st.EstimatedTime, 1.0); // maks. 1.0
                return ratio;
            }) / (double)(subtasks.Count == 0 ? 1 : subtasks.Count);

            // 4. deadlineScore: czy subtaski zakończono przed terminem
            int missedDeadlines = 0;
            var groupedByTask = subtasks
                .GroupBy(s => s.TaskEncodedName)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in groupedByTask)
            {
                var subtaskIds = kvp.Value.Select(s => s.Id).ToHashSet();
                var deadline = kvp.Value.FirstOrDefault()?.TaskDeadline;

                if (deadline != null)
                {
                    var latest = schedule
                        .Where(s => s.Subtask != null && subtaskIds.Contains(s.Subtask.Id))
                        .OrderByDescending(s => s.Date)
                        .FirstOrDefault();

                    if (latest != null && latest.Date > deadline.Value)
                        missedDeadlines++;
                }
            }
            deadlineScore = 1.0 - (missedDeadlines / (double)(groupedByTask.Count == 0 ? 1 : groupedByTask.Count));

            // 5. orderScore: zachowana kolejność subtasks (wg ID)
            int orderViolations = 0;
            foreach (var kvp in groupedByTask)
            {
                var orderedSubtasks = kvp.Value.OrderBy(s => s.Id).ToList();
                var subtaskDates = new Dictionary<int, DateTime>();

                foreach (var subtask in orderedSubtasks)
                {
                    var entries = schedule.Where(s => s.Subtask?.Id == subtask.Id).ToList();
                    if (entries.Any())
                        subtaskDates[subtask.Id] = entries.Min(e => e.Date);
                }

                for (int i = 1; i < orderedSubtasks.Count; i++)
                {
                    var prev = orderedSubtasks[i - 1];
                    var next = orderedSubtasks[i];

                    if (subtaskDates.TryGetValue(prev.Id, out var datePrev) &&
                        subtaskDates.TryGetValue(next.Id, out var dateNext) &&
                        datePrev > dateNext)
                    {
                        orderViolations++;
                    }
                }
            }
            orderScore = 1.0 - (orderViolations / (double)(subtasks.Count == 0 ? 1 : subtasks.Count));

            // 6. balanceScore: równe rozłożenie godzin
            if (groupedByDate.Count > 1)
            {
                var avg = groupedByDate.Values.Average();
                var variance = groupedByDate.Values.Select(val => Math.Pow(val - avg, 2)).Average();
                balanceScore = 1.0 / (1.0 + variance); // im mniejsza wariancja, tym lepszy wynik
            }

            // Końcowy score
            var score = 0.2 * timeFitScore
                      + 0.2 * deadlineScore
                      + 0.2 * coverageScore
                      + 0.2 * allocationScore
                      + 0.1 * orderScore
                      + 0.1 * balanceScore;

            return Math.Round(score, 6);
        }
    }



}
