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
        public static double Evaluate(List<ScheduleEntryDto> schedule, List<SubtaskDto> subtasks, List<TaskDto> tasks, List<DailyAvailabilityDto> availability)
        {
            double penalty = 0.0;

            // 1. Kara za przekroczenie dostępności
            var groupedByDate = schedule
                .GroupBy(e => e.Date)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Hours));

            foreach (var day in groupedByDate)
            {
                var dailyLimit = availability.FirstOrDefault(a => a.Date == day.Key)?.AvailableHours ?? 0;
                if (day.Value > dailyLimit)
                {
                    penalty += (day.Value - dailyLimit) * 10; // duża kara za przekroczenie
                }
            }

            // 2. Kara za nieprzydzielone podzadania
            var scheduledSubtaskIds = schedule
                .Select(s => s.Subtask?.Id)
                .Where(id => id != null)
                .Distinct()
                .ToHashSet();

            foreach (var subtask in subtasks)
            {
                if (!scheduledSubtaskIds.Contains(subtask.Id))
                {
                    penalty += 20; // kara za brak przypisania podzadania
                }
            }

            // 3. Kara za przeciążenie podzadania (więcej niż EstimatedTime)
            foreach (var group in schedule.GroupBy(s => s.Subtask?.Id))
            {
                var subtask = subtasks.FirstOrDefault(st => st.Id == group.Key);
                if (subtask != null)
                {
                    var totalHours = group.Sum(g => g.Hours);
                    if (totalHours > subtask.EstimatedTime)
                    {
                        penalty += (totalHours - subtask.EstimatedTime) * 5;
                    }
                }
            }

            return penalty;
        }
    }

}
