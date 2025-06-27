using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using Newtonsoft.Json;

namespace GroupPlanner.Application.Algorithms.Ant
{
    public class AntAlgorithmService: IAntAlgorithmService
    {
        private readonly Random _random = new();

        public async Task<AlgorithmRunResultDto> RunAsync(
            List<TaskDto> tasks,
            List<SubtaskDto> subtasks,
            List<DailyAvailabilityDto> availabilities,
            AntAlgorithmParameters parameters,
            Func<int, double, System.Threading.Tasks.Task>? reportProgress = null)
        {
            var pheromones = InitializePheromones(subtasks, availabilities);

            var bestScoreHistory = new List<double>();
            List<ScheduleEntryDto> bestSchedule = null!;
            double bestScore = double.MinValue;

            for (int iteration = 0; iteration < parameters.Iterations; iteration++)
            {
                var allSolutions = new List<List<ScheduleEntryDto>>();
                var solutionScores = new List<double>();

                for (int ant = 0; ant < parameters.AntCount; ant++)
                {
                    var solution = ConstructSolution(subtasks, tasks, availabilities, pheromones, parameters);

                    double score = ScheduleEvaluator.Evaluate(solution, subtasks, tasks, availabilities);

                    allSolutions.Add(solution);
                    solutionScores.Add(score);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSchedule = solution;
                    }
                }

                UpdatePheromones(pheromones, allSolutions, solutionScores, parameters);

                bestScoreHistory.Add(bestScore);

                if (reportProgress != null)
                {
                    await reportProgress(iteration, bestScore);
                }
            }

            return new AlgorithmRunResultDto
            {
                Schedule = bestSchedule,
                BestScore = bestScore,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ScoreHistoryJson = JsonConvert.SerializeObject(bestScoreHistory)
            };
        }

        private Dictionary<(int subtaskId, DateTime date), double> InitializePheromones(
            List<SubtaskDto> subtasks,
            List<DailyAvailabilityDto> availability,
            double initialTau = 1.0)
        {
            var pheromones = new Dictionary<(int subtaskId, DateTime date), double>();

            foreach (var subtask in subtasks)
            {
                foreach (var day in availability.Select(a => a.Date.Date).Distinct())
                {
                    pheromones[(subtask.Id, day)] = initialTau;
                }
            }

            return pheromones;
        }

        private double CalculateHeuristic(
            SubtaskDto subtask,
            DateTime date,
            DailyAvailabilityDto availabilityForDay)
        {
            var deadline = subtask.TaskDeadline ?? DateTime.MaxValue.Date;
            var daysToDeadline = Math.Max(0, (deadline.Date - date.Date).Days);

            double heuristic = availabilityForDay.AvailableHours / (1 + daysToDeadline);
            return heuristic;
        }

        private List<ScheduleEntryDto> ConstructSolution(
            List<SubtaskDto> subtasks,
            List<TaskDto> tasks,
            List<DailyAvailabilityDto> availabilities,
            Dictionary<(int subtaskId, DateTime date), double> pheromones,
            AntAlgorithmParameters parameters)
        {
            var solution = new List<ScheduleEntryDto>();

            var availabilityByDate = availabilities
                .GroupBy(a => a.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.AvailableHours));

            var subtasksByTask = subtasks
                .GroupBy(s => s.TaskEncodedName)
                .ToList();

            foreach (var taskGroup in subtasksByTask)
            {
                var orderedSubtasks = taskGroup.OrderBy(s => s.Order).ToList();

                foreach (var subtask in orderedSubtasks)
                {
                    double remaining = subtask.EstimatedTime;

                    var eligibleDates = availabilities
                        .Where(a =>
                            a.Date.Date <= (subtask.TaskDeadline ?? DateTime.MaxValue.Date) &&
                            a.AvailableHours >= 1.0)
                        .Select(a => a.Date.Date)
                        .Distinct()
                        .ToList();

                    if (subtask.Order > 1)
                    {
                        var previousSubtask = orderedSubtasks.FirstOrDefault(x => x.Order == subtask.Order - 1);
                        if (previousSubtask != null)
                        {
                            var previousMaxDate = solution
                                .Where(e => e.Subtask.Id == previousSubtask.Id)
                                .Select(e => e.Date)
                                .DefaultIfEmpty(DateTime.MinValue)
                                .Max();

                            eligibleDates = eligibleDates
                                .Where(d => d >= previousMaxDate.Date)
                                .ToList();
                        }
                    }

                    while (remaining >= 1.0 && eligibleDates.Any())
                    {
                        var probabilities = new List<(DateTime day, double probability)>();
                        double total = 0;

                        foreach (var day in eligibleDates)
                        {
                            pheromones.TryGetValue((subtask.Id, day), out var tau);
                            tau = tau > 0 ? tau : 1.0;

                            var heuristic = CalculateHeuristic(subtask, day,
                                availabilities.First(a => a.Date.Date == day));

                            var p = Math.Pow(tau, parameters.Alpha) * Math.Pow(heuristic, parameters.Beta);
                            probabilities.Add((day, p));
                            total += p;
                        }

                        double randomThreshold = _random.NextDouble() * total;
                        double cumulative = 0;
                        DateTime chosenDate = eligibleDates.First();

                        foreach (var (day, prob) in probabilities)
                        {
                            cumulative += prob;
                            if (cumulative >= randomThreshold)
                            {
                                chosenDate = day;
                                break;
                            }
                        }

                        var availableHours = availabilityByDate[chosenDate];
                        var alreadyAssigned = solution
                            .Where(e => e.Date.Date == chosenDate)
                            .Sum(e => e.Hours);

                        double remainingAvailable = availableHours - alreadyAssigned;
                        if (remainingAvailable < 1.0)
                        {
                            eligibleDates.Remove(chosenDate);
                            continue;
                        }

                        double chunk = Math.Min(remaining, remainingAvailable);

                        solution.Add(new ScheduleEntryDto
                        {
                            Date = chosenDate,
                            Subtask = subtask,
                            Hours = chunk
                        });

                        remaining -= chunk;
                    }

                    if (remaining > 0 && eligibleDates.Any())
                    {
                        var fallbackDay = eligibleDates[_random.Next(eligibleDates.Count)];
                        solution.Add(new ScheduleEntryDto
                        {
                            Date = fallbackDay,
                            Subtask = subtask,
                            Hours = remaining
                        });
                    }
                }
            }

            return solution;
        }

        private void UpdatePheromones(
            Dictionary<(int subtaskId, DateTime date), double> pheromones,
            List<List<ScheduleEntryDto>> allSolutions,
            List<double> solutionScores,
            AntAlgorithmParameters parameters)
        {
            foreach (var key in pheromones.Keys.ToList())
            {
                pheromones[key] *= (1 - parameters.EvaporationRate);
            }

            var bestIndexes = solutionScores
                .Select((score, idx) => new { score, idx })
                .OrderByDescending(x => x.score)
                .Take(10)
                .Select(x => x.idx)
                .ToList();

            foreach (var idx in bestIndexes)
            {
                var solution = allSolutions[idx];
                var score = solutionScores[idx];
                var deltaTau = parameters.Q / Math.Max(1.0, Math.Abs(score));

                foreach (var entry in solution)
                {
                    var key = (entry.Subtask.Id, entry.Date.Date);
                    if (pheromones.ContainsKey(key))
                    {
                        pheromones[key] += deltaTau;
                    }
                }
            }
        }
    }
}
