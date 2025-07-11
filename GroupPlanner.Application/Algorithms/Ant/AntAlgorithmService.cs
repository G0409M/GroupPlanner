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

        public async Task<AlgorithmRunResultDto> RunAsync(List<TaskDto> tasks,List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities,AntAlgorithmParameters parameters,
                    Func<int, double, System.Threading.Tasks.Task>? reportProgress = null)
        {
            var pheromone = InitializePheromones(subtasks, availabilities);
            double bestScore = double.MinValue;
            List<ScheduleEntryDto> bestSchedule = null!;
            var scoreHistory = new List<double>();

            for (int iteration = 0; iteration < parameters.Iterations; iteration++)
            {
                var antSchedules = new List<List<ScheduleEntryDto>>();

                for (int ant = 0; ant < parameters.AntCount; ant++)
                {
                    var schedule = ConstructSolution(subtasks, availabilities, tasks, pheromone, parameters.Alpha, parameters.Beta);
                    antSchedules.Add(schedule);
                }

                var evaluated = antSchedules
                    .Select(s => new EvaluatedSchedule
                    {
                        Schedule = s,
                        Score = ScheduleEvaluator.Evaluate(s, subtasks, tasks, availabilities)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                var bestInIteration = evaluated.First();
                scoreHistory.Add(Math.Max(bestScore, scoreHistory.LastOrDefault()));


                if (bestInIteration.Score > bestScore)
                {
                    bestScore = bestInIteration.Score;
                    bestSchedule = bestInIteration.Schedule.Select(CloneScheduleEntry).ToList();
                }

                EvaporatePheromones(pheromone, parameters.EvaporationRate);
                UpdatePheromones(pheromone, evaluated, parameters.Q);

                if (reportProgress != null)
                    await reportProgress(iteration + 1, bestInIteration.Score);
            }

            return new AlgorithmRunResultDto
            {

                BestScore = bestScore,
                Schedule = bestSchedule!,
                ScoreHistoryJson = JsonConvert.SerializeObject(scoreHistory),
                ParametersJson = JsonConvert.SerializeObject(parameters)
            };
        }

        private List<ScheduleEntryDto> ConstructSolution(List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities,
            List<TaskDto> tasks, Dictionary<(int subtaskId, DateTime date), double> pheromone, double alpha, double beta)
        {
            var schedule = new List<ScheduleEntryDto>();
            var random = _random;

            var availableDays = availabilities
                .OrderBy(a => a.Date)
                .Select(a => new AvailableDay
                {
                    Date = a.Date,
                    HoursLeft = a.AvailableHours
                })
                .ToList();

            var taskMap = tasks.ToDictionary(t => t.EncodedName!);

            var subtasksByOrder = subtasks
                .GroupBy(s => s.Order)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(_ => random.Next()).ToList()) 
                .ToList();

            foreach (var level in subtasksByOrder)
            {
                foreach (var subtask in level)
                {
                    if (subtask.TaskEncodedName == null)
                        continue;

                    var task = taskMap[subtask.TaskEncodedName];
                    var deadline = task.Deadline ?? DateTime.MaxValue;
                    int remaining = subtask.EstimatedTime;

                    while (remaining > 0)
                    {
                        var candidateDays = availableDays
                            .Where(d => d.Date <= deadline && d.HoursLeft > 0)
                            .ToList();

                        if (!candidateDays.Any())
                            break;

                        var probabilities = new List<(AvailableDay day, double probability)>();
                        foreach (var day in candidateDays)
                        {
                            double tau = pheromone.GetValueOrDefault((subtask.Id, day.Date), 1.0);
                            double eta = (double)day.HoursLeft / subtask.EstimatedTime + 0.01; 

                            double value = Math.Pow(tau, alpha) * Math.Pow(eta, beta);
                            probabilities.Add((day, value));
                        }

                        double total = probabilities.Sum(p => p.probability);
                        double r = random.NextDouble() * total;

                        double cumulative = 0;
                        AvailableDay? chosenDay = null;
                        foreach (var (day, prob) in probabilities)
                        {
                            cumulative += prob;
                            if (r <= cumulative)
                            {
                                chosenDay = day;
                                break;
                            }
                        }

                        if (chosenDay == null)
                            chosenDay = probabilities.First().day;

                        int assignable = Math.Min(remaining, chosenDay.HoursLeft);
                        int assigned = random.Next(1, assignable + 1);

                        schedule.Add(new ScheduleEntryDto
                        {
                            Date = chosenDay.Date,
                            Hours = assigned,
                            Subtask = subtask
                        });

                        chosenDay.HoursLeft -= assigned;
                        remaining -= assigned;
                    }

                    if (remaining > 0)
                    {
                        var fallback = availableDays.Where(d => d.HoursLeft > 0).OrderBy(_ => random.Next()).ToList();
                        foreach (var day in fallback)
                        {
                            if (remaining <= 0) break;
                            int assigned = Math.Min(remaining, 4);
                            schedule.Add(new ScheduleEntryDto
                            {
                                Date = day.Date,
                                Hours = assigned,
                                Subtask = subtask
                            });
                            day.HoursLeft -= assigned;
                            remaining -= assigned;
                        }
                    }
                }
            }

            return schedule
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Subtask?.TaskEncodedName)
                .ThenBy(e => e.Subtask?.Order)
                .ToList();
        }

        private Dictionary<(int subtaskId, DateTime date), double> InitializePheromones(List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities)
        {
            var pheromones = new Dictionary<(int, DateTime), double>();

            foreach (var subtask in subtasks)
            {
                foreach (var day in availabilities)
                {
                    pheromones[(subtask.Id, day.Date)] = 1.0;
                }
            }

            return pheromones;
        }

        private void EvaporatePheromones(Dictionary<(int subtaskId, DateTime date), double> pheromones, double evaporationRate)
        {
            var keys = pheromones.Keys.ToList();

            foreach (var key in keys)
            {
                pheromones[key] *= (1.0 - evaporationRate);

                
                if (pheromones[key] < 0.0001)
                {
                    pheromones[key] = 0.0001;
                }
            }
        }

        private void UpdatePheromones(Dictionary<(int subtaskId, DateTime date), double> pheromones,List<EvaluatedSchedule> evaluatedSchedules, double Q)
        {
            var topSchedules = evaluatedSchedules.Take(3);

            foreach (var eval in topSchedules)
            {
                var schedule = (List<ScheduleEntryDto>)eval.Schedule;
                double score = (double)eval.Score;

                foreach (var entry in schedule.Where(e => e.Subtask != null))
                {
                    var key = (entry.Subtask!.Id, entry.Date);

                    double delta = Q / Math.Max(score, 1.0); 
                    pheromones[key] += delta;
                }
            }
        }

        private DateTime SelectDayByRoulette(List<DateTime> days,List<double> pheromones,List<double> heuristics,double alpha,double beta, double randomChance = 0.1)
        {
            if (days.Count != pheromones.Count || days.Count != heuristics.Count)
                throw new ArgumentException("All lists must be of equal length");

            if (_random.NextDouble() < randomChance)
                return days[_random.Next(days.Count)];

            var weights = new List<double>();
            for (int i = 0; i < days.Count; i++)
            {
                double pheromone = pheromones[i];
                double heuristic = heuristics[i];
                double weight = Math.Pow(pheromone, alpha) * Math.Pow(heuristic, beta);
                weights.Add(weight);
            }

            double total = weights.Sum();
            if (total == 0)
                return days[_random.Next(days.Count)];

            double r = _random.NextDouble();
            double cumulative = 0.0;

            for (int i = 0; i < days.Count; i++)
            {
                cumulative += weights[i] / total;
                if (r <= cumulative)
                    return days[i];
            }

            return days.Last(); 
        }

        private ScheduleEntryDto CloneScheduleEntry(ScheduleEntryDto entry)
        {
            return new ScheduleEntryDto
            {
                Date = entry.Date,
                Hours = entry.Hours,
                Subtask = entry.Subtask
            };
        }
        private class EvaluatedSchedule
        {
            public List<ScheduleEntryDto> Schedule { get; set; } = new();
            public double Score { get; set; }
        }
    }
    

}
