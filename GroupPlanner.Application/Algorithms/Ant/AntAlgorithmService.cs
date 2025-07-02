using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.Algorithms.Genetic;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using Newtonsoft.Json;
namespace GroupPlanner.Application.Algorithms.Ant
{
    public class AntAlgorithmService: IAntAlgorithmService
    {
        private readonly Random _random = new();

        public async Task<AlgorithmRunResultDto> RunAsync(List<TaskDto> tasks,List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities,
                AntAlgorithmParameters parameters,Func<int, double, System.Threading.Tasks.Task>? reportProgress = null)
        {
            var pheromone = new Dictionary<(string subtaskEncodedName, DateTime date), double>();
            var random = new Random();

            foreach (var subtask in subtasks)
            {
                foreach (var day in availabilities.Select(a => a.Date))
                {
                    // startowa wartość z rozrzutem
                    pheromone[(subtask.TaskEncodedName ?? "", day)] = 0.1 + random.NextDouble() * 0.5;
                }
            }

            double bestScore = double.MinValue;
            List<ScheduleEntryDto>? bestSchedule = null;
            var scoreHistory = new List<double>();

            for (int iter = 0; iter < parameters.Iterations; iter++)
            {
                var solutions = new List<List<ScheduleEntryDto>>();
                var scores = new List<double>();

                for (int k = 0; k < parameters.AntCount; k++)
                {
                    var solution = BuildAntSchedule(pheromone, subtasks, availabilities, tasks, parameters.Alpha, parameters.Beta, random);
                    var score = ScheduleEvaluator.Evaluate(solution, subtasks, tasks, availabilities);

                    solutions.Add(solution);
                    scores.Add(score);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSchedule = solution;
                    }
                }

                UpdatePheromones(pheromone, solutions, scores, parameters.EvaporationRate, parameters.Q);

                scoreHistory.Add(bestScore);

                // log
                Console.WriteLine($"Iteration {iter + 1}, bestScore: {bestScore:F7}");

                if (reportProgress != null)
                {
                    await reportProgress(iter + 1, bestScore);
                }
            }

            return new AlgorithmRunResultDto
            {
                BestScore = bestScore,
                Schedule = bestSchedule?.Where(x => x.Subtask != null).ToList(),
                ScoreHistoryJson = System.Text.Json.JsonSerializer.Serialize(scoreHistory),
                ParametersJson = System.Text.Json.JsonSerializer.Serialize(parameters)
            };
        }


        private List<ScheduleEntryDto> BuildAntSchedule(Dictionary<(string subtaskEncodedName, DateTime date), double> pheromone,
        List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities,List<TaskDto> tasks,double alpha,double beta,Random random)
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

            var taskMap = tasks
                .Where(t => t.EncodedName != null)
                .ToDictionary(t => t.EncodedName!);

            var shuffledSubtasks = subtasks.OrderBy(_ => random.Next()).ToList();

            foreach (var subtask in shuffledSubtasks)
            {
                var task = taskMap.GetValueOrDefault(subtask.TaskEncodedName ?? "");
                var deadline = task?.Deadline ?? DateTime.MaxValue;
                int remaining = subtask.EstimatedTime;

                while (remaining > 0)
                {
                    var candidateDays = availableDays
                        .Where(d => d.Date <= deadline && d.HoursLeft > 0)
                        .ToList();

                    if (!candidateDays.Any())
                        break;

                    var probabilities = new List<(DateTime day, double probability)>();
                    double denom = 0.0;

                    foreach (var day in candidateDays)
                    {
                        var key = (subtask.TaskEncodedName ?? "", day.Date);
                        double tau = pheromone.GetValueOrDefault(key, 1.0);

                        double daysToDeadline = (deadline - day.Date).TotalDays;
                        double heuristic = 1.0 / (1.0 + Math.Exp(-daysToDeadline));  // sigmoid

                        heuristic *= day.HoursLeft;

                        double value = Math.Pow(tau, alpha) * Math.Pow(heuristic, beta);

                        denom += value;
                        probabilities.Add((day.Date, value));
                    }

                    var probNormalized = probabilities
                        .Select(p => (day: p.day, probability: p.probability / denom))
                        .ToList();

                    double roll = random.NextDouble();
                    double cumulative = 0.0;
                    DateTime chosenDate = probNormalized.Last().day;

                    foreach (var p in probNormalized)
                    {
                        cumulative += p.probability;
                        if (roll <= cumulative)
                        {
                            chosenDate = p.day;
                            break;
                        }
                    }

                    var chosenDay = availableDays.First(d => d.Date == chosenDate);
                    int assignable = Math.Min(remaining, chosenDay.HoursLeft);
                    int assigned = random.Next(1, assignable + 1);

                    schedule.Add(new ScheduleEntryDto
                    {
                        Date = chosenDate,
                        Hours = assigned,
                        Subtask = subtask
                    });

                    chosenDay.HoursLeft -= assigned;
                    remaining -= assigned;
                }

                if (remaining > 0)
                {
                    var fallbackDays = availableDays.OrderBy(_ => random.Next()).ToList();
                    foreach (var day in fallbackDays)
                    {
                        if (remaining <= 0) break;
                        int assigned = Math.Min(remaining, 4);
                        schedule.Add(new ScheduleEntryDto
                        {
                            Date = day.Date,
                            Hours = assigned,
                            Subtask = subtask
                        });
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

        private void UpdatePheromones(Dictionary<(string subtaskEncodedName, DateTime date), double> pheromones,List<List<ScheduleEntryDto>> antSchedules,List<double> antScores,
     double evaporationRate,double Q)
        {
            foreach (var key in pheromones.Keys.ToList())
            {
                pheromones[key] *= (1 - evaporationRate);
            }

            for (int k = 0; k < antSchedules.Count; k++)
            {
                var solution = antSchedules[k];
                double contribution = Q * antScores[k];

                foreach (var entry in solution.Where(x => x.Subtask != null))
                {
                    var key = (entry.Subtask!.TaskEncodedName ?? "", entry.Date);
                    pheromones[key] += contribution;
                }
            }
        }

    }
}
