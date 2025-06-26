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
            var population = GenerateInitialPopulation(subtasks, availabilities, parameters);
            var scoreHistory = new List<double>();

            for (int generation = 0; generation < parameters.Generations; generation++)
            {
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

                if (reportProgress != null)
                    await reportProgress(generation, bestScore);

                // Elitaryzm: zachowaj najlepszego osobnika
                var newPopulation = new List<List<ScheduleEntryDto>> { CloneSchedule(evaluated.First().Schedule) };

                while (newPopulation.Count < parameters.PopulationSize)
                {
                    var parent1 = TournamentSelection(evaluated, parameters.TournamentSize);
                    var parent2 = TournamentSelection(evaluated, parameters.TournamentSize);

                    var child = _random.NextDouble() < parameters.CrossoverProbability
                        ? Crossover(parent1, parent2)
                        : CloneSchedule(parent1);

                    if (_random.NextDouble() < parameters.MutationProbability)
                        Mutate(child, availabilities);

                    newPopulation.Add(child);
                }

                population = newPopulation;
            }

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


        private List<List<ScheduleEntryDto>> GenerateInitialPopulation(
            List<SubtaskDto> subtasks,
            List<DailyAvailabilityDto> availabilities,
            GeneticAlgorithmParameters parameters)
        {
            var population = new List<List<ScheduleEntryDto>>();

            for (int i = 0; i < parameters.PopulationSize; i++)
            {
                var individual = new List<ScheduleEntryDto>();

                // kopiujemy dostępność na słownik: Date => RemainingHours
                var remainingPerDay = availabilities
                    .GroupBy(a => a.Date)
                    .ToDictionary(g => g.Key, g => g.First().AvailableHours);

                foreach (var subtask in subtasks)
                {
                    var chunks = SplitIntoRandomChunks(subtask.EstimatedTime);

                    foreach (var hours in chunks)
                    {
                        // znajdź dzień, który ma wystarczającą ilość godzin
                        var candidate = remainingPerDay
                            .Where(kvp => kvp.Value >= hours)
                            .OrderBy(_ => _random.Next()) // losowo wśród pasujących
                            .FirstOrDefault();

                        DateTime selectedDate;

                        if (candidate.Key != default)
                        {
                            // pasujący dzień znaleziony
                            selectedDate = candidate.Key;
                            remainingPerDay[selectedDate] -= hours;
                        }
                        else
                        {
                            // fallback: wybierz dzień z największą pozostałością
                            selectedDate = remainingPerDay
                                .OrderByDescending(kvp => kvp.Value)
                                .First().Key;

                            // przekroczymy — ale trudno
                            remainingPerDay[selectedDate] -= hours;
                        }

                        individual.Add(new ScheduleEntryDto
                        {
                            Date = selectedDate,
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
            const double step = 0.5;
            const double minChunk = 1.0;

            var chunks = new List<double>();
            double remaining = Math.Round(totalTime, 1);

            if (remaining < minChunk)
                return new List<double> { remaining }; // fallback: nie da się podzielić

            // Wygeneruj wszystkie dopuszczalne wartości (1.0, 1.5, 2.0, ..., <= totalTime)
            var allowedValues = Enumerable.Range(2, (int)((totalTime - minChunk) / step) + 1)
                                          .Select(i => Math.Round(i * step, 1))
                                          .Prepend(minChunk)
                                          .Where(v => v <= totalTime)
                                          .ToList();

            while (remaining >= minChunk)
            {
                var valid = allowedValues.Where(v => v <= remaining).ToList();

                if (!valid.Any())
                    break;

                var selected = valid[_random.Next(valid.Count)];
                chunks.Add(selected);
                remaining = Math.Round(remaining - selected, 1);
            }

            // Poprawka – jeśli pozostało coś między 0.1 a 0.9h, dodaj do ostatniego chanka
            if (remaining > 0.01 && chunks.Count > 0)
            {
                chunks[chunks.Count - 1] += remaining;
                chunks[chunks.Count - 1] = Math.Round(chunks[chunks.Count - 1], 1);
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

            // Grupujemy dostępności: Date -> AvailableHours
            var availableMap = availabilities
                .GroupBy(a => a.Date)
                .ToDictionary(g => g.Key, g => g.First().AvailableHours);

            // Wybierz losowy subtask (mutujemy całość)
            var subtaskGroups = schedule.GroupBy(s => s.Subtask?.Id).ToList();
            var group = subtaskGroups[_random.Next(subtaskGroups.Count)];

            // Suma czasu tego subtaska
            double totalHours = group.Sum(e => e.Hours);

            // Spróbuj znaleźć dzień (lub dni) z wystarczającą dostępnością
            var candidateDates = availableMap
                .Where(a => a.Value >= totalHours)
                .Select(a => a.Key)
                .ToList();

            // Jeśli taki dzień istnieje, przypisz wszystko na ten dzień
            if (candidateDates.Any())
            {
                var newDate = candidateDates[_random.Next(candidateDates.Count)];

                foreach (var entry in group)
                    entry.Date = newDate;
            }
            else
            {
                // Fallback: losowo rozłóż na kilka dni
                var chunks = SplitIntoRandomChunks(totalHours);
                int i = 0;

                foreach (var entry in group)
                {
                    var newDate = availabilities[_random.Next(availabilities.Count)].Date;
                    entry.Date = newDate;

                    // opcjonalnie: rozdziel też godziny
                    if (i < chunks.Count)
                        entry.Hours = chunks[i++];
                }
            }
        }
    }
}