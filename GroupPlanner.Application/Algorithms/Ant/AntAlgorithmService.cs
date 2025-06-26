using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace GroupPlanner.Application.Algorithms.Ant
{
    public class AntAlgorithmService : IAntAlgorithmService
    {
        private readonly Random _random = new();

        public async Task<AlgorithmRunResultDto> RunAsync(
    List<TaskDto> tasks,
    List<SubtaskDto> subtasks,
    List<DailyAvailabilityDto> availabilities,
    AntAlgorithmParameters parameters)
        {
            var pheromones = new Dictionary<(int subtaskId, DateTime date), double>();
            foreach (var subtask in subtasks)
            {
                foreach (var day in availabilities.Select(a => a.Date))
                {
                    pheromones[(subtask.Id, day)] = 1.0;
                }
            }

            List<ScheduleEntryDto> bestSchedule = new();
            double bestScore = double.MinValue;
            List<double> scoreHistory = new();

            for (int iter = 0; iter < parameters.Iterations; iter++)
            {
                var solutions = new List<(List<ScheduleEntryDto> schedule, double score)>();

                for (int ant = 0; ant < parameters.AntCount; ant++)
                {
                    var schedule = new List<ScheduleEntryDto>();

                    foreach (var subtask in subtasks)
                    {
                        var possibleDays = availabilities.Select(a => a.Date).ToList();

                        var probabilities = possibleDays.Select(day =>
                        {
                            var pheromone = pheromones[(subtask.Id, day)];
                            var heuristic = 1.0 / (1.0 + Overload(schedule, day, availabilities));
                            return Math.Pow(pheromone, parameters.Alpha) * Math.Pow(heuristic, parameters.Beta);
                        }).ToList();

                        var total = probabilities.Sum();
                        var normalized = probabilities.Select(p => p / total).ToList();

                        var selectedDate = RouletteSelect(possibleDays, normalized);

                        schedule.Add(new ScheduleEntryDto
                        {
                            Date = selectedDate,
                            Subtask = subtask,
                            Hours = subtask.EstimatedTime
                        });
                    }

                    var score = ScheduleEvaluator.Evaluate(schedule, subtasks, tasks, availabilities);
                    solutions.Add((schedule, score));

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSchedule = schedule;
                    }
                }

                scoreHistory.Add(bestScore);

                foreach (var key in pheromones.Keys.ToList())
                {
                    pheromones[key] *= (1 - parameters.EvaporationRate);
                }

                foreach (var (schedule, score) in solutions)
                {
                    foreach (var entry in schedule)
                    {
                        var key = (entry.Subtask!.Id, entry.Date);
                        pheromones[key] += parameters.Q * (score / 100.0);
                    }
                }
            }

            return new AlgorithmRunResultDto
            {
                Schedule = bestSchedule,
                BestScore = bestScore,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ScoreHistoryJson = JsonConvert.SerializeObject(scoreHistory)
            };
        }

        private double Overload(List<ScheduleEntryDto> schedule, DateTime day, List<DailyAvailabilityDto> availabilities)
        {
            var used = schedule.Where(e => e.Date == day).Sum(e => e.Hours);
            var available = availabilities.FirstOrDefault(a => a.Date == day)?.AvailableHours ?? 0;
            return Math.Max(0, used - available);
        }

        private DateTime RouletteSelect(List<DateTime> options, List<double> probabilities)
        {
            var rand = new Random();
            var r = rand.NextDouble();
            double cumulative = 0.0;

            for (int i = 0; i < options.Count; i++)
            {
                cumulative += probabilities[i];
                if (r < cumulative)
                {
                    return options[i];
                }
            }
            return options.Last();
        }


       
       
    }

}
