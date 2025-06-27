using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using Microsoft.AspNetCore.SignalR;
using GroupPlanner.Application.Hubs;
using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.Task;
using Newtonsoft.Json;

namespace GroupPlanner.Application.Algorithms.Genetic
{
    public class GeneticAlgorithmService : IGeneticAlgorithmService
    {
        private readonly Random _random = new();
        private readonly IHubContext<AlgorithmHub> _hubContext;

        public GeneticAlgorithmService(IHubContext<AlgorithmHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<AlgorithmRunResultDto> RunAsync(List<TaskDto> tasks,List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities, GeneticAlgorithmParameters parameters,
    Func<int, double, System.Threading.Tasks.Task>? reportProgress = null)
        {
            // 1️⃣ Generowanie początkowej populacji
            var population = GenerateInitialPopulation(subtasks, availabilities, parameters);

            var scoreHistory = new List<double>();

            for (int generation = 0; generation < parameters.Generations; generation++)
            {
                // 2️⃣ Ocena populacji
                var evaluated = population
                    .Select(p => new EvaluatedScheduleDto
                    {
                        Schedule = p,
                        Score = ScheduleEvaluator.Evaluate(p, subtasks, tasks, availabilities)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                var bestScore = evaluated.First().Score;
                scoreHistory.Add(bestScore);

                // 3️⃣ Raportowanie do klienta (SignalR, konsola itd.)
                if (reportProgress != null)
                    await reportProgress(generation, bestScore);

                // 4️⃣ Elitaryzm: zachowaj najlepszego
                var newPopulation = new List<List<ScheduleEntryDto>>
                {
                    CloneSchedule(evaluated.First().Schedule)
                };

                // 5️⃣ Krzyżowanie i mutacje
                while (newPopulation.Count < parameters.PopulationSize)
                {
                    // selekcja turniejowa
                    var parent1 = TournamentSelection(evaluated, parameters.TournamentSize);
                    var parent2 = TournamentSelection(evaluated, parameters.TournamentSize);

                    // krzyżowanie
                    var child = _random.NextDouble() < parameters.CrossoverProbability
                        ? Crossover(parent1, parent2, availabilities)
                        : CloneSchedule(parent1);

                    // mutacja
                    if (_random.NextDouble() < parameters.MutationProbability)
                    {
                        Mutate(child, availabilities);
                    }

                    newPopulation.Add(child);
                }

                population = newPopulation;
            }

            // 6️⃣ Ewaluacja populacji końcowej
            var finalEvaluated = population
                .Select(p => new EvaluatedScheduleDto
                {
                    Schedule = p,
                    Score = ScheduleEvaluator.Evaluate(p, subtasks, tasks, availabilities)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            return new AlgorithmRunResultDto
            {
                Schedule = finalEvaluated.First().Schedule,
                BestScore = finalEvaluated.First().Score,
                ParametersJson = JsonConvert.SerializeObject(parameters),
                ScoreHistoryJson = JsonConvert.SerializeObject(scoreHistory)
            };
        }


        private List<List<ScheduleEntryDto>> GenerateInitialPopulation(List<SubtaskDto> subtasks,List<DailyAvailabilityDto> availabilities,GeneticAlgorithmParameters parameters)
        {
            var population = new List<List<ScheduleEntryDto>>();
            var rand = new Random();

            var availabilityByDate = availabilities
            .GroupBy(a => a.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.AvailableHours)
            );


            // podziel subtaski według taskId i Order
            var subtasksByTask = subtasks
                .GroupBy(s => s.TaskEncodedName)
                .ToList();

            for (int i = 0; i < parameters.PopulationSize; i++)
            {
                var schedule = new List<ScheduleEntryDto>();

                foreach (var taskGroup in subtasksByTask)
                {
                    var orderedSubtasks = taskGroup.OrderBy(s => s.Order).ToList();

                    foreach (var subtask in orderedSubtasks)
                    {
                        double remainingTime = subtask.EstimatedTime;

                        var eligibleDates = availabilities
                            .Where(a =>
                                a.Date.Date <= (subtask.TaskDeadline ?? DateTime.MaxValue.Date) &&
                                a.AvailableHours >= 1.0
                            )
                            .Select(a => a.Date.Date)
                            .OrderBy(d => d) // aby trzymać kolejność
                            .ToList();

                        while (remainingTime >= 1.0 && eligibleDates.Any())
                        {
                            var chosenDate = eligibleDates[rand.Next(eligibleDates.Count)];
                            var availableHoursOnDate = availabilityByDate[chosenDate];
                            double alreadyAssigned = schedule
                                .Where(e => e.Date.Date == chosenDate)
                                .Sum(e => e.Hours);

                            double remainingAvailable = availableHoursOnDate - alreadyAssigned;
                            if (remainingAvailable < 1.0)
                            {
                                eligibleDates.Remove(chosenDate);
                                continue;
                            }

                            double maxChunk = Math.Min(remainingTime, remainingAvailable);
                            var possibleChunks = new List<double>();
                            for (double c = 1.0; c <= maxChunk; c += 0.5)
                                possibleChunks.Add(c);

                            var chosenChunk = possibleChunks[rand.Next(possibleChunks.Count)];

                            schedule.Add(new ScheduleEntryDto
                            {
                                Date = chosenDate,
                                Subtask = subtask,
                                Hours = chosenChunk
                            });

                            remainingTime -= chosenChunk;
                        }

                        if (remainingTime > 0 && eligibleDates.Any())
                        {
                            var chosenDate = eligibleDates[rand.Next(eligibleDates.Count)];
                            var availableHoursOnDate = availabilityByDate[chosenDate];
                            double alreadyAssigned = schedule
                                .Where(e => e.Date.Date == chosenDate)
                                .Sum(e => e.Hours);

                            double remainingAvailable = availableHoursOnDate - alreadyAssigned;
                            if (remainingAvailable >= remainingTime)
                            {
                                schedule.Add(new ScheduleEntryDto
                                {
                                    Date = chosenDate,
                                    Subtask = subtask,
                                    Hours = remainingTime
                                });
                                remainingTime = 0;
                            }
                        }
                    }
                }
                population.Add(schedule);
            }

            return population;
        }


        private List<double> SplitIntoRandomChunks(double totalTime)
        {
            var chunks = new List<double>();
            var rand = new Random();

            double remaining = totalTime;

            while (remaining >= 1.0)
            {
                double upperLimit = Math.Floor(remaining / 0.5) * 0.5;

                var possibleChunks = new List<double>();
                for (double c = 1.0; c <= upperLimit; c += 0.5)
                {
                    possibleChunks.Add(c);
                }

                if (possibleChunks.Count == 0)
                {
                    break;
                }

                var chosen = possibleChunks[rand.Next(possibleChunks.Count)];
                chunks.Add(chosen);
                remaining -= chosen;
            }

            if (remaining > 0.0)
            {
                if (chunks.Count > 0)
                {
                    chunks[chunks.Count - 1] += remaining;
                }
                else
                {
                    // nie było chunków - przypisz wszystko naraz
                    chunks.Add(remaining);
                }
            }

            return chunks;
        }


        private List<ScheduleEntryDto> TournamentSelection(List<EvaluatedScheduleDto> evaluated, int k)
        {
            var rand = new Random();

            // turniej nie może być większy niż populacja
            k = Math.Min(k, evaluated.Count);

            // losowo wybierz k osobników (bez powtórzeń)
            var tournamentContestants = evaluated
                .OrderBy(x => rand.Next())
                .Take(k)
                .ToList();

            // znajdź najlepszego
            var bestScore = tournamentContestants.Max(e => e.Score);
            var best = tournamentContestants
                .Where(e => e.Score == bestScore)
                .OrderBy(x => rand.Next()) // jeśli kilku z tym samym score, losowo
                .First();

            // zwracamy kopię Schedule, żeby nie nadpisać oryginału w ewolucji
            return best.Schedule
                .Select(e => new ScheduleEntryDto
                {
                    Date = e.Date,
                    Subtask = e.Subtask,
                    Hours = e.Hours
                })
                .ToList();
        }


        private List<ScheduleEntryDto> Crossover(
    List<ScheduleEntryDto> parentA,
    List<ScheduleEntryDto> parentB,
    List<DailyAvailabilityDto> availabilities)
        {
            var rand = new Random();
            var child = new List<ScheduleEntryDto>();

            // łączona dostępność (sumowana dla dni)
            var availabilityMap = availabilities
                .GroupBy(a => a.Date.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.AvailableHours)
                );

            var usedHoursPerDay = new Dictionary<DateTime, double>();

            var taskNames = parentA
                .Select(e => e.Subtask.TaskEncodedName)
                .Union(parentB.Select(e => e.Subtask.TaskEncodedName))
                .Distinct()
                .ToList();

            // 1️⃣ przydziel zadania od rodziców
            foreach (var taskName in taskNames)
            {
                bool fromA = rand.NextDouble() < 0.5;
                var chosenParent = fromA ? parentA : parentB;

                var taskEntries = chosenParent
                    .Where(e => e.Subtask.TaskEncodedName == taskName)
                    .ToList();

                foreach (var entry in taskEntries)
                {
                    var proposedDate = entry.Date.Date;

                    if (!usedHoursPerDay.ContainsKey(proposedDate))
                        usedHoursPerDay[proposedDate] = 0;

                    // sprawdź czy da się wcisnąć całość
                    while (usedHoursPerDay[proposedDate] + entry.Hours > availabilityMap.GetValueOrDefault(proposedDate, 0) ||
                           entry.Hours < 1.0)
                    {
                        // szukaj kolejnego dnia
                        proposedDate = proposedDate.AddDays(1);

                        if (!availabilityMap.ContainsKey(proposedDate))
                        {
                            break;
                        }
                        if (!usedHoursPerDay.ContainsKey(proposedDate))
                            usedHoursPerDay[proposedDate] = 0;
                    }

                    if (availabilityMap.ContainsKey(proposedDate) &&
                        usedHoursPerDay[proposedDate] + entry.Hours <= availabilityMap[proposedDate] &&
                        entry.Hours >= 1.0)
                    {
                        child.Add(new ScheduleEntryDto
                        {
                            Date = proposedDate,
                            Subtask = entry.Subtask,
                            Hours = entry.Hours
                        });
                        usedHoursPerDay[proposedDate] += entry.Hours;
                    }
                }
            }

            // 2️⃣ pozostałe zadania (nieprzydzielone)
            var alreadyPlanned = child.Select(e => e.Subtask.Id).Distinct().ToHashSet();

            var allSubtasks = parentA
                .Concat(parentB)
                .Select(e => e.Subtask)
                .DistinctBy(s => s.Id)
                .Where(s => !alreadyPlanned.Contains(s.Id))
                .ToList();

            foreach (var subtask in allSubtasks)
            {
                double timeLeft = subtask.EstimatedTime;

                foreach (var day in availabilities.OrderBy(a => a.Date))
                {
                    if (timeLeft <= 0) break;

                    var date = day.Date.Date;

                    if (!usedHoursPerDay.ContainsKey(date))
                        usedHoursPerDay[date] = 0;

                    double freeHours = availabilityMap[date] - usedHoursPerDay[date];

                    // ignoruj dni gdzie wolne < 1h
                    if (freeHours < 1.0) continue;

                    var hoursToAssign = Math.Min(freeHours, timeLeft);

                    // nie dziel mniejszych niż 1h
                    if (hoursToAssign < 1.0)
                        continue;

                    child.Add(new ScheduleEntryDto
                    {
                        Date = date,
                        Subtask = subtask,
                        Hours = hoursToAssign
                    });

                    usedHoursPerDay[date] += hoursToAssign;
                    timeLeft -= hoursToAssign;
                }

                // jeżeli zostało np. 0.5h na końcu → dołącz do poprzedniego
                if (timeLeft > 0 && timeLeft < 1.0)
                {
                    var lastEntry = child.LastOrDefault(e => e.Subtask.Id == subtask.Id);
                    if (lastEntry != null)
                    {
                        lastEntry.Hours += timeLeft;
                        usedHoursPerDay[lastEntry.Date] += timeLeft;
                        timeLeft = 0;
                    }
                }
            }

            // sortowanie
            return child
                .OrderBy(e => e.Subtask.TaskEncodedName)
                .ThenBy(e => e.Subtask.Order)
                .ThenBy(e => e.Date)
                .ToList();
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
            var rand = new Random();

            if (!schedule.Any())
                return;

            var subtaskIds = schedule.Select(e => e.Subtask.Id).Distinct().ToList();
            var selectedSubtaskId = subtaskIds[rand.Next(subtaskIds.Count)];

            var selectedSubtaskEntries = schedule
                .Where(e => e.Subtask.Id == selectedSubtaskId)
                .ToList();

            if (!selectedSubtaskEntries.Any())
                return;

            var subtask = selectedSubtaskEntries.First().Subtask;
            var estimatedTime = subtask.EstimatedTime;

            schedule.RemoveAll(e => e.Subtask.Id == selectedSubtaskId);

            var availableDates = availabilities
                .Where(a =>
                    a.Date.Date <= (subtask.TaskDeadline ?? DateTime.MaxValue.Date) &&
                    a.AvailableHours >= 1.0
                )
                .Select(a => a.Date.Date)
                .OrderBy(d => d)
                .ToList();

            if (!availableDates.Any())
                return;

            // kolejność — uwzględnij tylko, jeśli to nie jest pierwszy subtask
            DateTime? earliestAllowedDate = null;
            if (subtask.Order > 1)
            {
                var previousSubtaskOrder = subtask.Order - 1;

                var previousSubtaskEntries = schedule
                    .Where(e => e.Subtask.TaskEncodedName == subtask.TaskEncodedName &&
                                e.Subtask.Order == previousSubtaskOrder)
                    .ToList();

                if (previousSubtaskEntries.Any())
                {
                    earliestAllowedDate = previousSubtaskEntries.Max(e => e.Date);
                }
            }

            if (earliestAllowedDate != null)
            {
                availableDates = availableDates
                    .Where(d => d >= earliestAllowedDate.Value.Date)
                    .ToList();
            }

            // przypisanie na nowo
            double remaining = estimatedTime;

            while (remaining >= 1.0 && availableDates.Any())
            {
                var chosenDate = availableDates[rand.Next(availableDates.Count)];
                var availableHours = availabilities.First(a => a.Date.Date == chosenDate).AvailableHours;

                double alreadyAssigned = schedule
                    .Where(e => e.Date.Date == chosenDate)
                    .Sum(e => e.Hours);

                double remainingAvailable = availableHours - alreadyAssigned;
                if (remainingAvailable < 1.0)
                {
                    availableDates.Remove(chosenDate);
                    continue;
                }

                double maxChunk = Math.Min(remaining, remainingAvailable);
                var possibleChunks = new List<double>();
                for (double c = 1.0; c <= maxChunk; c += 0.5)
                    possibleChunks.Add(c);

                var chosenChunk = possibleChunks[rand.Next(possibleChunks.Count)];

                schedule.Add(new ScheduleEntryDto
                {
                    Date = chosenDate,
                    Subtask = subtask,
                    Hours = chosenChunk
                });

                remaining -= chosenChunk;
            }

            // jeśli zostało jeszcze coś < 1h
            if (remaining > 0 && availableDates.Any())
            {
                var chosenDate = availableDates[rand.Next(availableDates.Count)];
                var availableHours = availabilities.First(a => a.Date.Date == chosenDate).AvailableHours;

                double alreadyAssigned = schedule
                    .Where(e => e.Date.Date == chosenDate)
                    .Sum(e => e.Hours);

                double remainingAvailable = availableHours - alreadyAssigned;
                if (remainingAvailable >= remaining)
                {
                    schedule.Add(new ScheduleEntryDto
                    {
                        Date = chosenDate,
                        Subtask = subtask,
                        Hours = remaining
                    });
                }
            }
        }


    }
}