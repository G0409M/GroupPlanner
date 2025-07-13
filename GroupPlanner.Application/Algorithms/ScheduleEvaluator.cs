using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;


namespace GroupPlanner.Application.Algorithms
    {
    public static class ScheduleEvaluator
    {
        private const int BonusSubtaskExact = 40;
        private const int PenaltySubtaskMismatch = 25;

        private const int BonusOrderOk = 20;
        private const int PenaltyOrderWrong = 80;

        private const int BonusDeadlineOk = 30;
        private const int PenaltyDeadlineMissed = 60;

        private const int BonusAvailabilityFit = 10;
        private const int PenaltyOverbooked = 50;

        private const int BonusFewSplitDays = 20;
        private const int PenaltyManySplitDays = 20;

        private const int PenaltyImbalanceFactor = 10;

        private const int PenaltyUnassignedTask = 100;

        public static double Evaluate(List<ScheduleEntryDto> schedule, List<SubtaskDto> subtasks, List<TaskDto> tasks,
            List<DailyAvailabilityDto> availability)
        {
            double score = 0;

            var taskDict = tasks.ToDictionary(t => t.EncodedName!);

            var groupedBySubtaskId = schedule
                .Where(h => h.Subtask != null)
                .GroupBy(h => h.Subtask!.Id)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Date).ToList());

            foreach (var sub in subtasks)
            {
                var priority = taskDict.TryGetValue(sub.TaskEncodedName ?? "", out var task)
                    ? task.Priority
                    : TaskPriority.Medium;

                double weight = GetPriorityWeight(priority);

                var entries = groupedBySubtaskId.GetValueOrDefault(sub.Id) ?? new List<ScheduleEntryDto>();
                int total = entries.Sum(e => e.Hours);

                if (total == sub.EstimatedTime)
                    score += BonusSubtaskExact * weight;
                else
                    score -= PenaltySubtaskMismatch * weight;
            }

            var groupedByTask = subtasks.GroupBy(s => s.TaskEncodedName);
            foreach (var taskGroup in groupedByTask)
            {
                var ordered = taskGroup.OrderBy(s => s.Order).ToList();
                DateTime? lastEnd = null;

                foreach (var sub in ordered)
                {
                    var entries = groupedBySubtaskId.GetValueOrDefault(sub.Id);
                    if (entries == null || entries.Count == 0)
                        continue;

                    var priority = taskDict.TryGetValue(sub.TaskEncodedName ?? "", out var task)
                        ? task.Priority
                        : TaskPriority.Medium;

                    double weight = GetPriorityWeight(priority);

                    var earliest = entries.First().Date;
                    var latest = entries.Last().Date;

                    if (lastEnd.HasValue && earliest < lastEnd.Value)
                        score -= PenaltyOrderWrong * weight;
                    else
                        score += BonusOrderOk * weight;

                    lastEnd = latest;
                }
            }

            foreach (var task in tasks)
            {
                double weight = GetPriorityWeight(task.Priority);
                var taskEntries = schedule
                    .Where(e => e.Subtask != null && e.Subtask.TaskEncodedName == task.EncodedName)
                    .ToList();

                if (!taskEntries.Any())
                {
                    score -= PenaltyUnassignedTask * weight;
                    continue;
                }

                var maxDate = taskEntries.Max(e => e.Date);
                if (task.Deadline.HasValue && maxDate > task.Deadline.Value)
                    score -= PenaltyDeadlineMissed * weight;
                else
                    score += BonusDeadlineOk * weight;
            }

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
                    score -= PenaltyOverbooked;
                else
                    score += BonusAvailabilityFit;
            }

            foreach (var sub in subtasks)
            {
                var entries = groupedBySubtaskId.GetValueOrDefault(sub.Id) ?? new List<ScheduleEntryDto>();
                var uniqueDays = entries.Select(e => e.Date).Distinct().Count();

                if (uniqueDays <= 2)
                    score += BonusFewSplitDays;
                else if (uniqueDays <= 4)
                    score += BonusFewSplitDays / 2;
                else
                    score -= PenaltyManySplitDays;
            }

            if (dayStats.Any())
            {
                var avgLoad = dayStats.Average(x => x.Available > 0 ? (double)x.Used / x.Available : 0);
                var deviation = dayStats.Sum(x =>
                {
                    double ratio = x.Available > 0 ? (double)x.Used / x.Available : 0;
                    return Math.Abs(ratio - avgLoad);
                });

                score -= deviation * PenaltyImbalanceFactor;
            }


            return score;
        }

        private static double GetPriorityWeight(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Critical => 2.0,
                TaskPriority.High => 1.5,
                TaskPriority.Medium => 1.0,
                TaskPriority.Low => 0.5,
                _ => 1.0
            };
        }
    }

}


