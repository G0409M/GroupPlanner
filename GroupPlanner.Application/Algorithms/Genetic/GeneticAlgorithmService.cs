using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms.Genetic
{
    public class GeneticAlgorithmService : IGeneticAlgorithmService
    {
        private readonly Random _random = new();

        public async Task<List<ScheduleEntryDto>> RunAsync(List<TaskDto> tasks,List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities)
        {
            var population = GenerateInitialPopulation(subtasks, availabilities);

            for (int generation = 0; generation < 50; generation++)
            {
                var evaluated = population.Select(p => new EvaluatedScheduleDto
                {
                    Schedule = p,
                    Score = ScheduleEvaluator.Evaluate(p, subtasks, tasks, availabilities)
                }).OrderBy(x => x.Score).ToList();

                var newPopulation = new List<List<ScheduleEntryDto>>();

                while (newPopulation.Count < population.Count)
                {
                    var parent1 = TournamentSelection(evaluated);
                    var parent2 = TournamentSelection(evaluated);

                    var child = _random.NextDouble() < 0.7
                        ? Crossover(parent1, parent2, subtasks, availabilities)
                        : CloneSchedule(parent1);

                    if (_random.NextDouble() < 0.1)
                    {
                        Mutate(child, availabilities);
                    }

                    newPopulation.Add(child);
                }

                population = newPopulation;
            }

            var best = population.OrderBy(p => ScheduleEvaluator.Evaluate(p, subtasks, tasks, availabilities)).First();

            return best;
        }

        private List<List<ScheduleEntryDto>> GenerateInitialPopulation(List<SubtaskDto> subtasks, List<DailyAvailabilityDto> availabilities)
        {
            var population = new List<List<ScheduleEntryDto>>();
            for (int i = 0; i < 20; i++)
            {
                var individual = new List<ScheduleEntryDto>();
                foreach (var subtask in subtasks)
                {
                    var availability = availabilities[_random.Next(availabilities.Count)];
                    individual.Add(new ScheduleEntryDto
                    {
                        Date = availability.Date,
                        Subtask = subtask,
                        Hours = subtask.EstimatedTime
                    });
                }
                population.Add(individual);
            }
            return population;
        }

        private List<ScheduleEntryDto> TournamentSelection(List<EvaluatedScheduleDto> evaluated)
        {
            var candidates = evaluated.OrderBy(_ => _random.Next()).Take(3).ToList();
            return candidates.OrderBy(x => x.Score).First().Schedule;
        }

        private List<ScheduleEntryDto> Crossover(
            List<ScheduleEntryDto> parent1,
            List<ScheduleEntryDto> parent2,
            List<SubtaskDto> subtasks,
            List<DailyAvailabilityDto> availabilities)
        {
            var child = new List<ScheduleEntryDto>();
            for (int i = 0; i < subtasks.Count; i++)
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
            var index = _random.Next(schedule.Count);
            var newDate = availabilities[_random.Next(availabilities.Count)].Date;
            schedule[index].Date = newDate;
        }
    }



}
