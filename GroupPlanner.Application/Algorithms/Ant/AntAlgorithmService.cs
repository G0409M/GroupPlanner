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
            
            var pheromone = new Dictionary<(string sub, DateTime d), double>();
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

                double q0 = 0.2 + 0.4 * ((double)iter / (parameters.Iterations - 1));

                for (int k = 0; k < parameters.AntCount; k++)
                {
                    var schedule = BuildAntSchedule(
                        pheromone,
                        subtasks,
                        availabilities,
                        tasks,
                        parameters.Alpha,
                        parameters.Beta,
                        q0,
                        _random
                    );

                    var score = ScheduleEvaluator.Evaluate(schedule, subtasks, tasks, availabilities);
                    solutions.Add(schedule);
                    scores.Add(score);

                    if (score > bestScore || bestSchedule == null)
                    {
                        bestScore = score;
                        bestSchedule = schedule;
                    }
                }

                
                UpdatePheromones(
                    pheromone,
                    solutions,
                    scores,
                    parameters.Q,         
                    parameters.EvaporationRate, 
                    availabilities
                );

                scoreHistory.Add(bestScore);
                if (reportProgress != null)
                    await reportProgress(iter + 1, bestScore);
            }

            bestSchedule ??= new List<ScheduleEntryDto>();
            return new AlgorithmRunResultDto
            {
                BestScore = bestScore,
                Schedule = bestSchedule,
                ScoreHistoryJson = JsonConvert.SerializeObject(scoreHistory),
                ParametersJson = JsonConvert.SerializeObject(parameters)
            };
        }


        
        private List<ScheduleEntryDto> BuildAntSchedule(Dictionary<(string sub, DateTime d), double> pheromone, List<SubtaskDto> subs,List<DailyAvailabilityDto> av,
        List<TaskDto> tasks,double alpha, double beta,double q0, Random rnd)
        {
            var schedule = new List<ScheduleEntryDto>();
            var days = av.Select(a => new AvailableDay { Date = a.Date, HoursLeft = a.AvailableHours })
                         .OrderBy(a => a.Date)
                         .ToDictionary(d => d.Date);

            foreach (var sub in subs.OrderBy(_ => rnd.Next()))
            {
                int remaining = sub.EstimatedTime;
                var deadline = tasks.First(t => t.EncodedName == sub.TaskEncodedName).Deadline
                               ?? DateTime.MaxValue;

                while (remaining > 0)
                {
                    var candidates = days.Values.Where(d => d.HoursLeft > 0).ToList();
                    if (!candidates.Any()) break;

                    // Oblicz probilność z uwzględnieniem feromonów + heurystyki
                    var scored = new List<(AvailableDay day, double p)>();
                    double sum = 0;
                    foreach (var d in candidates)
                    {
                        var tau = pheromone.GetValueOrDefault((sub.TaskEncodedName!, d.Date), 1.0);
                        double dist = Math.Abs((d.Date - deadline).TotalDays) + 1;
                        double eta = (1.0 / dist); // zostawiamy prostą heurystykę
                        double val = Math.Pow(tau, alpha) * Math.Pow(eta, beta);
                        scored.Add((d, val));
                        sum += val;
                    }

                    // Wylosuj z prawdopodobieństwem lub greedy
                    double q = rnd.NextDouble();
                    AvailableDay chosen;
                    if (q < q0)
                    {
                        chosen = scored.OrderByDescending(x => x.p).First().day;
                    }
                    else
                    {
                        // roulette
                        double r = rnd.NextDouble() * sum, acc = 0;
                        chosen = scored.Last().day;
                        foreach (var x in scored)
                        {
                            acc += x.p;
                            if (r <= acc) { chosen = x.day; break; }
                        }
                    }

                    int assign = Math.Min(remaining, chosen.HoursLeft);
                    schedule.Add(new ScheduleEntryDto
                    {
                        Date = chosen.Date,
                        Hours = assign,
                        Subtask = sub
                    });
                    chosen.HoursLeft -= assign;
                    remaining -= assign;
                }
            }

            // (Twój RepairSchedule bez zmian)
            RepairSchedule(schedule, days, av);
            return schedule.OrderBy(x => x.Date)
                           .ThenBy(x => x.Subtask?.Order ?? int.MaxValue)
                           .ToList();
        }


        
        private void RepairSchedule(List<ScheduleEntryDto> sched,Dictionary<DateTime, AvailableDay> work, List<DailyAvailabilityDto> av)
        {
            
            foreach (var g in sched.GroupBy(s => s.Date))
            {
                double used = g.Sum(x => x.Hours);
                double cap = av.First(d => d.Date == g.Key).AvailableHours;
                if (used <= cap) continue;                           

               
                foreach (var entry in g.OrderBy(e => e.Hours).ToList())
                {
                    
                    var target = work.Values.FirstOrDefault(d => d.HoursLeft >= entry.Hours);
                    if (target == null) break;                      

                    work[target.Date].HoursLeft -= entry.Hours;
                    work[entry.Date].HoursLeft += entry.Hours;      
                    entry.Date = target.Date;
                    used -= entry.Hours;
                    if (used <= cap) break;
                }
            }

            
            foreach (var (sub, h) in _unplaced)
            {
                int left = h;
                while (left > 0)
                {
                    var slot = work.Values.FirstOrDefault(d => d.HoursLeft > 0);
                    if (slot == null) break;

                    int assign = Math.Min(left, slot.HoursLeft);
                    sched.Add(new ScheduleEntryDto { Date = slot.Date, Hours = assign, Subtask = sub });
                    slot.HoursLeft -= assign;
                    left -= assign;
                }
            }
            _unplaced.Clear();
        }

       
        private readonly List<(SubtaskDto sub, int hours)> _unplaced = new();

        private void UpdatePheromones(Dictionary<(string sub, DateTime d), double> pher,List<List<ScheduleEntryDto>> ants,List<double> scores,
        double Q, double evap,List<DailyAvailabilityDto> av)
        {
            const double min = 0.1, max = 5.0;
            
            foreach (var key in pher.Keys.ToList())
                pher[key] = Math.Max(min, pher[key] * (1 - evap));

            
            for (int i = 0; i < ants.Count; i++)
            {
                double delta = Q * scores[i];
               
                foreach (var e in ants[i])
                {
                    
                    var key = (e.Subtask!.TaskEncodedName!, e.Date);
                    pher[key] = Math.Min(max, pher[key] + delta);
                }
            }
        }



    }
}
