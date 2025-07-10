using GroupPlanner.Application.Task;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Algorithms.Genetic;
using System.Text.Json;
using GroupPlanner.Application.AlgorithmResult;

namespace GroupPlanner.Application.Algorithms.Genetic
{
    public class GeneticAlgorithmService : IGeneticAlgorithmService
    {
        private readonly Random random = new Random();

        public async Task<AlgorithmRunResultDto> RunAsync(List<TaskDto> tasks,List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availability,
                        GeneticAlgorithmParameters parameters,Func<int, double, System.Threading.Tasks.Task> progressCallback)
        {
            var random = new Random();

            int populationSize = parameters.PopulationSize;
            int generations = parameters.Generations;
            double crossoverProb = parameters.CrossoverProbability;
            double mutationProb = parameters.MutationProbability;
            int tournamentSize = parameters.TournamentSize;

            // inicjalna populacja
            var population = GenerateInitialPopulation(populationSize, subtasks, availability, tasks);

            double bestScore = double.MinValue;
            List<ScheduleEntryDto> bestSchedule = null;

            var scoreHistory = new List<double>();

            for (int generation = 0; generation < generations; generation++)
            {
                var evaluated = population
                    .Select(p => new EvaluatedSchedule
                    {
                        Schedule = p,
                        Score = ScheduleEvaluator.Evaluate(p, subtasks, tasks, availability)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                var bestInGen = evaluated.First();

                if (bestInGen.Score > bestScore)
                {
                    bestScore = bestInGen.Score;
                    bestSchedule = bestInGen.Schedule.Select(CloneScheduleEntry).ToList();
                }

                scoreHistory.Add(bestInGen.Score);

                if (progressCallback != null)
                {
                    await progressCallback(generation + 1, bestInGen.Score);
                }

                var newPopulation = new List<List<ScheduleEntryDto>>();

                while (newPopulation.Count < populationSize - 1)
                {
                    var parent1 = TournamentSelection(evaluated, tournamentSize, random);
                    var parent2 = TournamentSelection(evaluated, tournamentSize, random);

                    List<ScheduleEntryDto> child;

                    if (random.NextDouble() < crossoverProb)
                        child = Crossover(parent1, parent2, subtasks, availability, tasks, random);
                    else
                        child = parent1.Select(CloneScheduleEntry).ToList();

                    if (random.NextDouble() < mutationProb)
                        Mutate(child, subtasks, availability, random);

                    newPopulation.Add(child);
                }

                // elitaryzm
                newPopulation.Add(bestInGen.Schedule.Select(CloneScheduleEntry).ToList());

                population = newPopulation;
            }

            if (bestSchedule != null)
            {
                var availableHoursPerDay = availability.ToDictionary(a => a.Date, a => a.AvailableHours);

                foreach (var entry in bestSchedule.Where(x => x.Subtask != null))
                {
                    if (availableHoursPerDay.ContainsKey(entry.Date))
                    {
                        availableHoursPerDay[entry.Date] -= entry.Hours;
                        if (availableHoursPerDay[entry.Date] < 0)
                        {
                            // log lub rzucenie wyjątku
                            Console.WriteLine($"[WARN] Day {entry.Date} overbooked below zero hours.");
                        }
                    }
                }
            }

            return new AlgorithmRunResultDto
            {
                BestScore = bestScore,
                Schedule = bestSchedule
                    .Where(x => x.Subtask != null)
                    .ToList(),
                ScoreHistoryJson = JsonSerializer.Serialize(scoreHistory),
                ParametersJson = JsonSerializer.Serialize(parameters)
            };
        }


        private List<List<ScheduleEntryDto>> GenerateInitialPopulation(int size, List<SubtaskDto> subtasks, List<DailyAvailabilityDto> availability, List<TaskDto> tasks)
        {
            var pop = new List<List<ScheduleEntryDto>>();
            for (int i = 0; i < size; i++)
                pop.Add(GenerateRandomSchedule(subtasks, availability, tasks));
            return pop;
        }

        private List<ScheduleEntryDto> GenerateRandomSchedule(List<SubtaskDto> subtasks, List<DailyAvailabilityDto> availability, List<TaskDto> tasks)
        {
            var schedule = new List<ScheduleEntryDto>();

            var availableDays = availability
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

            int maxOrder = subtasks.Max(s => s.Order);

            for (int orderLevel = 1; orderLevel <= maxOrder; orderLevel++)
            {
                var subtasksInLevel = subtasks
                    .Where(s => s.Order == orderLevel)
                    .OrderBy(_ => random.Next())
                    .ToList();

                foreach (var subtask in subtasksInLevel)
                {
                    if (subtask.TaskEncodedName == null)
                        continue;

                    var task = taskMap.GetValueOrDefault(subtask.TaskEncodedName);
                    var deadline = task?.Deadline ?? DateTime.MaxValue;
                    int remaining = subtask.EstimatedTime;

                    while (remaining > 0)
                    {
                        var candidateDays = availableDays
                            .Where(d => d.Date <= deadline && d.HoursLeft > 0)
                            .OrderByDescending(d => d.HoursLeft)
                            .ToList();

                        if (!candidateDays.Any())
                            break;

                        var day = candidateDays.First();
                        int allocatable = Math.Min(remaining, day.HoursLeft);
                        int assigned = random.Next(1, allocatable + 1);

                        schedule.Add(new ScheduleEntryDto
                        {
                            Date = day.Date,
                            Hours = assigned,
                            Subtask = subtask
                        });

                        day.HoursLeft -= assigned;
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

                        if (remaining > 0)
                        {
                            Console.WriteLine($"[WARN] Could not assign {remaining}h for subtask {subtask.Description}");
                        }
                    }
                }
            }

            return schedule
                .OrderBy(s => s.Date)
                .ThenBy(s => s.Subtask?.TaskEncodedName ?? "")
                .ThenBy(s => s.Subtask?.Order ?? int.MaxValue)
                .ToList();
        }

        private List<ScheduleEntryDto> TournamentSelection(List<EvaluatedSchedule> evaluated,int tournamentSize,Random random)
        {
            var candidates = evaluated.OrderByDescending(_ => random.Next()).Take(tournamentSize).ToList();

            return candidates.OrderByDescending(x => x.Score).First().Schedule;
        }

        private List<ScheduleEntryDto> Crossover(List<ScheduleEntryDto> parent1,List<ScheduleEntryDto> parent2,List<SubtaskDto> subtasks,
                List<DailyAvailabilityDto> availability,List<TaskDto> tasks,Random random)
        {
            var offspring = new List<ScheduleEntryDto>();

            var availableDays = availability
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

            var groups1 = parent1
                .Where(e => e.Subtask != null)
                .GroupBy(e => e.Subtask)
                .ToDictionary(g => g.Key, g => g.ToList());

            var groups2 = parent2
                .Where(e => e.Subtask != null)
                .GroupBy(e => e.Subtask)
                .ToDictionary(g => g.Key, g => g.ToList());

            var shuffledSubtasks = subtasks.OrderBy(_ => random.Next()).ToList();

            foreach (var subtask in shuffledSubtasks)
            {
                if (subtask.TaskEncodedName == null)
                    continue;

                var task = taskMap.GetValueOrDefault(subtask.TaskEncodedName);
                var deadline = task?.Deadline ?? DateTime.MaxValue;
                int remaining = subtask.EstimatedTime;

                List<ScheduleEntryDto>? sourceEntries = null;

                if (groups1.ContainsKey(subtask) && groups2.ContainsKey(subtask))
                    sourceEntries = random.Next(2) == 0 ? groups1[subtask] : groups2[subtask];
                else if (groups1.ContainsKey(subtask))
                    sourceEntries = groups1[subtask];
                else if (groups2.ContainsKey(subtask))
                    sourceEntries = groups2[subtask];

                if (sourceEntries != null)
                {
                    foreach (var entry in sourceEntries.OrderBy(_ => random.Next()))
                    {
                        if (entry.Date > deadline)
                            continue;

                        var day = availableDays.FirstOrDefault(d => d.Date == entry.Date);
                        if (day != null && day.HoursLeft >= entry.Hours)
                        {
                            offspring.Add(new ScheduleEntryDto
                            {
                                Date = entry.Date,
                                Hours = entry.Hours,
                                Subtask = subtask
                            });
                            day.HoursLeft -= entry.Hours;
                            remaining -= entry.Hours;
                            if (remaining <= 0)
                                break;
                        }
                    }
                }

                while (remaining > 0)
                {
                    var candidateDays = availableDays
                        .Where(d => d.Date <= deadline && d.HoursLeft > 0)
                        .OrderByDescending(d => d.HoursLeft)
                        .ToList();

                    if (!candidateDays.Any())
                        break;

                    var day = candidateDays.First();
                    int allocatable = Math.Min(remaining, day.HoursLeft);
                    int assigned = random.Next(1, allocatable + 1);

                    offspring.Add(new ScheduleEntryDto
                    {
                        Date = day.Date,
                        Hours = assigned,
                        Subtask = subtask
                    });
                    day.HoursLeft -= assigned;
                    remaining -= assigned;
                }

                if (remaining > 0)
                {
                    var fallbackDays = availableDays.OrderBy(_ => random.Next()).ToList();
                    foreach (var day in fallbackDays)
                    {
                        if (remaining <= 0) break;
                        int assigned = Math.Min(remaining, 4);
                        offspring.Add(new ScheduleEntryDto
                        {
                            Date = day.Date,
                            Hours = assigned,
                            Subtask = subtask
                        });
                        remaining -= assigned;
                    }

                    if (remaining > 0)
                    {
                        Console.WriteLine($"[WARN] Crossover: could not assign {remaining}h for subtask {subtask.Description}");
                    }
                }
            }

            return offspring
                .OrderBy(s => s.Date)
                .ThenBy(s => s.Subtask?.TaskEncodedName ?? "")
                .ThenBy(s => s.Subtask?.Order ?? int.MaxValue)
                .ToList();
        }


        private void Mutate(List<ScheduleEntryDto> schedule,List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availability,Random random)
        {
            
            var dayHoursLeft = availability
                .ToDictionary(a => a.Date, a => a.AvailableHours);

           
            foreach (var s in schedule.Where(x => x.Subtask != null))
            {
                dayHoursLeft[s.Date] -= s.Hours;
            }

            var taskSubentries = schedule.Where(x => x.Subtask != null).ToList();
            int mutationCount = Math.Max(1, (int)Math.Ceiling(taskSubentries.Count * 0.05));

            for (int i = 0; i < mutationCount; i++)
            {
                var entry = taskSubentries[random.Next(taskSubentries.Count)];

                
                dayHoursLeft[entry.Date] += entry.Hours;
                schedule.Remove(entry);

               
                var candidateDays = dayHoursLeft
                    .Where(kv => kv.Value > 0)
                    .Select(kv => kv.Key)
                    .ToList();

                if (candidateDays.Any())
                {
                    var newDay = candidateDays[random.Next(candidateDays.Count)];
                    int available = dayHoursLeft[newDay];
                    int hoursToAssign = Math.Min(available, entry.Hours);

                    schedule.Add(new ScheduleEntryDto
                    {
                        Date = newDay,
                        Hours = hoursToAssign,
                        Subtask = entry.Subtask
                    });

                    dayHoursLeft[newDay] -= hoursToAssign;

                    
                    int leftover = entry.Hours - hoursToAssign;
                    if (leftover > 0)
                    {
                        schedule.Add(new ScheduleEntryDto
                        {
                            Date = entry.Date,
                            Hours = leftover,
                            Subtask = entry.Subtask
                        });
                        dayHoursLeft[entry.Date] -= leftover;
                    }
                }
                else
                {
                    
                    schedule.Add(entry);
                    dayHoursLeft[entry.Date] -= entry.Hours;
                }
            }
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
            public List<ScheduleEntryDto> Schedule { get; set; }
            public double Score { get; set; }
        }
    }

    class AvailableDay
    {
        public DateTime Date { get; set; }
        public int HoursLeft { get; set; }
    }
}
