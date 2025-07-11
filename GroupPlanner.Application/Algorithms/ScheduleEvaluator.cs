using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;

namespace GroupPlanner.Application.Algorithms
{
    
    public static class ScheduleEvaluator
    {
        public static double Evaluate(List<ScheduleEntryDto> schedule,List<SubtaskDto> subtasks,List<TaskDto> tasks,List<DailyAvailabilityDto> availability)
        {
            int score = 0;

            var groupedBySubtask = schedule
                .Where(h => h.Subtask != null)
                .GroupBy(h => h.Subtask)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Date).ToList());

            
            foreach (var sub in subtasks)
            {
                var entries = groupedBySubtask.ContainsKey(sub)
                    ? groupedBySubtask[sub]
                    : new List<ScheduleEntryDto>();

                int total = entries.Sum(e => e.Hours);
                if (total == sub.EstimatedTime)
                {
                    score += 50; 
                }
                else
                {
                    score -= 20; 
                }
            }

            
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
                        score -= 100; 
                    }
                    else
                    {
                        score += 30; 
                    }

                    lastEnd = subEntries.Last().Date;
                }
            }

            
            foreach (var task in tasks)
            {
                var taskEntries = schedule
                    .Where(e => e.Subtask != null && e.Subtask.TaskEncodedName == task.EncodedName)
                    .ToList();

                if (!taskEntries.Any())
                {
                    score -= 100; 
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
                    score -= 100; 
                }
                else
                {
                    score += 10; 
                }
            }

            
            foreach (var sub in subtasks)
            {
                var entries = groupedBySubtask.ContainsKey(sub)
                    ? groupedBySubtask[sub]
                    : new List<ScheduleEntryDto>();

                var uniqueDays = entries.Select(e => e.Date).Distinct().Count();

                if (uniqueDays <= 2)
                {
                    score += 30; 
                }
                else if (uniqueDays <= 4)
                {
                    score += 10; 
                }
                else
                {
                    score -= 20;
                }
            }

           
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
