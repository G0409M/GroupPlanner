using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.Algorithms.Genetic;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using Newtonsoft.Json;
using System;
namespace GroupPlanner.Application.Algorithms.Ant
{
    public class AntAlgorithmService : IAntAlgorithmService
    {
        private readonly Random _random = new();

        public async Task<AlgorithmRunResultDto> RunAsync(
            List<TaskDto> tasks,
            List<SubtaskDto> subtasks,
            List<DailyAvailabilityDto> availabilities,
            AntAlgorithmParameters parameters,
            Func<int, double, System.Threading.Tasks.Task>? reportProgress = null)
        {
            // inicjalizacja feromonów
            var pheromone = new Dictionary<(string subtaskEncodedName, DateTime date), double>();
            foreach (var sub in subtasks)
                foreach (var day in availabilities)
                    pheromone[(sub.TaskEncodedName ?? "", day.Date)] = 1.0;

            double bestScore = double.MinValue;
            List<ScheduleEntryDto>? bestSchedule = null;
            var scoreHistory = new List<double>();

            for (int iter = 0; iter < parameters.Iterations; iter++)
            {
                var solutions = new List<List<ScheduleEntryDto>>();
                var scores = new List<double>();

                for (int k = 0; k < parameters.AntCount; k++)
                {
                    var schedule = BuildAntSchedule(pheromone, subtasks, availabilities, tasks, parameters.Alpha, parameters.Beta, _random);

                    var score = ScheduleEvaluator.Evaluate(schedule, subtasks, tasks, availabilities);
                    solutions.Add(schedule);
                    scores.Add(score);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSchedule = schedule;
                    }
                }

                UpdatePheromones(pheromone, solutions, scores, parameters.Q, parameters.EvaporationRate);

                scoreHistory.Add(bestScore);

                if (reportProgress != null)
                    await reportProgress(iter + 1, bestScore);
            }

            return new AlgorithmRunResultDto
            {
                BestScore = bestScore,
                Schedule = bestSchedule,
                ScoreHistoryJson = System.Text.Json.JsonSerializer.Serialize(scoreHistory),
                ParametersJson = System.Text.Json.JsonSerializer.Serialize(parameters)
            };
        }

        private List<ScheduleEntryDto> BuildAntSchedule(
    Dictionary<(string subtaskEncodedName, DateTime date), double> pheromone,
    List<SubtaskDto> subtasks,
    List<DailyAvailabilityDto> availabilities,
    List<TaskDto> tasks,
    double alpha,
    double beta,
    Random random)
        {
            var schedule = new List<ScheduleEntryDto>();

            var availableDays = availabilities
                .Select(a => new AvailableDay
                {
                    Date = a.Date,
                    HoursLeft = a.AvailableHours
                })
                .OrderBy(a => a.Date)
                .ToList();

            var subtasksByOrder = subtasks
                .GroupBy(s => s.Order)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var orderGroup in subtasksByOrder)
            {
                foreach (var subtask in orderGroup.OrderBy(_ => random.Next()))
                {
                    if (subtask.TaskEncodedName == null)
                        continue;

                    // hard constraint: nie zaczynaj dopóki poprzednie nie skończone
                    if (subtask.Order > 1)
                    {
                        var previousSubs = subtasks
                            .Where(s => s.TaskEncodedName == subtask.TaskEncodedName && s.Order == subtask.Order - 1)
                            .ToList();

                        bool previousDone = previousSubs.All(prev =>
                        {
                            var total = schedule
                                .Where(x => x.Subtask?.TaskEncodedName == prev.TaskEncodedName &&
                                            x.Subtask?.Order == prev.Order)
                                .Sum(x => x.Hours);
                            return total >= prev.EstimatedTime;
                        });

                        if (!previousDone)
                            continue; // nie można zacząć

                        // dodatkowe ograniczenie: nie przed zakończeniem poprzedniego
                        var prevEndDate = schedule
                            .Where(x => previousSubs.Any(p => p.TaskEncodedName == x.Subtask?.TaskEncodedName && p.Order == x.Subtask?.Order))
                            .OrderByDescending(x => x.Date)
                            .Select(x => x.Date)
                            .FirstOrDefault();

                        availableDays = availableDays
                            .Where(d => d.Date >= prevEndDate)
                            .ToList();
                    }

                    int remaining = subtask.EstimatedTime;

                    while (remaining > 0)
                    {
                        var deadline = tasks.FirstOrDefault(t => t.EncodedName == subtask.TaskEncodedName)?.Deadline
                            ?? DateTime.MaxValue;

                        // candidateDays tylko w terminie
                        var candidateDays = availableDays
                            .Where(d => d.Date <= deadline && d.HoursLeft > 0)
                            .ToList();

                        if (!candidateDays.Any())
                            break; // nie próbujemy na siłę

                        var probabilities = new List<(DateTime day, double probability)>();
                        double denom = 0;

                        foreach (var day in candidateDays)
                        {
                            var key = (subtask.TaskEncodedName, day.Date);
                            double tau = pheromone.GetValueOrDefault(key, 1.0);

                            double heuristic = 1.0 / (1 + Math.Abs((day.Date - deadline).TotalDays));
                            heuristic *= day.HoursLeft;

                            double value = Math.Pow(tau, alpha) * Math.Pow(heuristic, beta);
                            probabilities.Add((day.Date, value));
                            denom += value;
                        }

                        if (denom == 0)
                            break;

                        var normalized = probabilities
                            .Select(p => (day: p.day, prob: p.probability / denom))
                            .ToList();

                        double roll = random.NextDouble();
                        double cumulative = 0;
                        DateTime chosenDate = normalized.Last().day;

                        foreach (var p in normalized)
                        {
                            cumulative += p.prob;
                            if (roll <= cumulative)
                            {
                                chosenDate = p.day;
                                break;
                            }
                        }

                        var chosenDay = availableDays.First(d => d.Date == chosenDate);
                        int assignable = Math.Min(remaining, chosenDay.HoursLeft);
                        int assigned = Math.Min(assignable, random.Next(1, assignable + 1));

                        schedule.Add(new ScheduleEntryDto
                        {
                            Date = chosenDate,
                            Hours = assigned,
                            Subtask = subtask
                        });

                        chosenDay.HoursLeft -= assigned;
                        remaining -= assigned;
                    }
                }
            }

            return schedule
                .OrderBy(s => s.Date)
                .ThenBy(s => s.Subtask?.TaskEncodedName ?? "")
                .ThenBy(s => s.Subtask?.Order ?? int.MaxValue)
                .ToList();
        }







        private void UpdatePheromones(
    Dictionary<(string subtaskEncodedName, DateTime date), double> pheromones,
    List<List<ScheduleEntryDto>> antSchedules,
    List<double> antScores,
    double evaporationRate,
    double Q)
        {
            double minPheromone = 0.1;
            double maxPheromone = 5.0;

            // parowanie
            foreach (var key in pheromones.Keys.ToList())
            {
                pheromones[key] *= (1 - evaporationRate);
                pheromones[key] = Math.Max(minPheromone, pheromones[key]);
            }

            // wzmocnienie
            for (int k = 0; k < antSchedules.Count; k++)
            {
                var solution = antSchedules[k];
                double contribution = Q * antScores[k];

                foreach (var entry in solution.Where(x => x.Subtask != null))
                {
                    var key = (entry.Subtask!.TaskEncodedName ?? "", entry.Date);
                    pheromones[key] += contribution;
                    pheromones[key] = Math.Min(maxPheromone, pheromones[key]);
                }
            }
        }


    }

}
