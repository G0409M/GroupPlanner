using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms.Genetic
{
    public class GeneticAlgorithmService : IGeneticAlgorithmService
    {
        private readonly Random _random = new();

        public async Task<List<ScheduleEntryDto>> RunAsync(List<TaskDto> tasks, List<SubtaskDto> subtasks, List<DailyAvailabilityDto> availabilities, GeneticAlgorithmParameters parameters)
        {
            var population = GenerateInitialPopulation(subtasks, availabilities, parameters);

            for (int generation = 0; generation < parameters.Generations; generation++)
            {
                var evaluated = population.Select(p => new EvaluatedScheduleDto
                {
                    Schedule = p,
                    Score = ScheduleEvaluator.Evaluate(p, subtasks, tasks, availabilities)
                }).OrderByDescending(x => x.Score).ToList();

                var newPopulation = new List<List<ScheduleEntryDto>>();

                while (newPopulation.Count < population.Count)
                {
                    var parent1 = TournamentSelection(evaluated, parameters.TournamentSize);
                    var parent2 = TournamentSelection(evaluated, parameters.TournamentSize);

                    var child = _random.NextDouble() < parameters.CrossoverProbability
                        ? Crossover(parent1, parent2)
                        : CloneSchedule(parent1);

                    if (_random.NextDouble() < parameters.MutationProbability)
                    {
                        Mutate(child, availabilities);
                    }

                    newPopulation.Add(child);
                }

                population = newPopulation;
            }

            var finalEvaluated = population.Select(p => new EvaluatedScheduleDto
            {
                Schedule = p,
                Score = ScheduleEvaluator.Evaluate(p, subtasks, tasks, availabilities)
            }).OrderByDescending(x => x.Score).ToList();

            return finalEvaluated.First().Schedule;
        }

        private List<List<ScheduleEntryDto>> GenerateInitialPopulation(
            List<SubtaskDto> subtasks,
            List<DailyAvailabilityDto> availabilities,
            GeneticAlgorithmParameters parameters)
        {
            var population = new List<List<ScheduleEntryDto>>();

            for (int i = 0; i < parameters.PopulationSize; i++)
            {
                var individual = new List<ScheduleEntryDto>();

                foreach (var subtask in subtasks)
                {
                    var chunks = SplitIntoRandomChunks(subtask.EstimatedTime);

                    foreach (var hours in chunks)
                    {
                        var availability = availabilities[_random.Next(availabilities.Count)];
                        individual.Add(new ScheduleEntryDto
                        {
                            Date = availability.Date,
                            Subtask = subtask,
                            Hours = hours
                        });
                    }
                }

                population.Add(individual);
            }

            return population;
        }
        private List<double> SplitIntoRandomChunks(double totalTime)
        {
            var chunks = new List<double>();
            double remaining = Math.Round(totalTime, 2);

            while (remaining >= 1.0)
            {
                var max = remaining - 1.0;
                if (max < 1.0)
                {
                    chunks.Add(Math.Round(remaining, 1));
                    break;
                }

                var possible = Enumerable.Range(2, (int)((max - 1.0) * 2 + 1))
                                         .Select(i => i * 0.5)
                                         .Where(x => x <= remaining)
                                         .ToList();

                if (!possible.Any())
                {
                    break;
                }

                var selected = possible[_random.Next(possible.Count)];
                chunks.Add(Math.Round(selected, 1));
                remaining = Math.Round(remaining - selected, 2);
            }

            // jeśli nic nie zostało dodane — dodaj cały czas jako jeden chunk
            if (chunks.Count == 0 && totalTime > 0)
            {
                chunks.Add(Math.Round(totalTime, 1));
            }

            return chunks;
        }

        private List<ScheduleEntryDto> TournamentSelection(List<EvaluatedScheduleDto> evaluated, int k)
        {
            var candidates = evaluated.OrderBy(_ => _random.Next()).Take(k).ToList();
            return candidates.OrderByDescending(x => x.Score).First().Schedule;
        }

        private List<ScheduleEntryDto> Crossover(List<ScheduleEntryDto> parent1, List<ScheduleEntryDto> parent2)
        {
            var child = new List<ScheduleEntryDto>();
            int count = Math.Min(parent1.Count, parent2.Count);

            for (int i = 0; i < count; i++)
            {
                var source = _random.NextDouble() < 0.5 ? parent1 : parent2;
                child.Add(CloneEntry(source[i]));
            }

            return child;
        }

        private List<ScheduleEntryDto> CloneSchedule(List<ScheduleEntryDto> schedule)
        {
            return schedule.Select(CloneEntry).ToList();
        }

        private ScheduleEntryDto CloneEntry(ScheduleEntryDto entry)
        {
            return new ScheduleEntryDto
            {
                Date = entry.Date,
                Subtask = entry.Subtask,
                Hours = entry.Hours
            };
        }

        private void Mutate(List<ScheduleEntryDto> schedule, List<DailyAvailabilityDto> availabilities)
        {
            if (schedule.Count == 0 || availabilities.Count == 0)
                return;

            var index = _random.Next(schedule.Count);
            var newDate = availabilities[_random.Next(availabilities.Count)].Date;
            schedule[index].Date = newDate;
        }
    }
}