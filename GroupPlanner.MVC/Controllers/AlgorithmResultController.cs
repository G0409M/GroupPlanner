using AutoMapper;
using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.Algorithms.Genetic;
using GroupPlanner.Application.Algorithms;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.MVC.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using GroupPlanner.Application.Algorithms.Ant;
using Microsoft.AspNetCore.SignalR;
using GroupPlanner.Application.Hubs;

namespace GroupPlanner.MVC.Controllers
{
    [Authorize]
    public class AlgorithmResultController : Controller
    {
        private readonly IUserContext _userContext;
        private readonly ITaskRepository _taskRepository;
        private readonly ISubtaskRepository _subtaskRepository;
        private readonly IDailyAvailabilityRepository _availabilityRepository;
        private readonly IAlgorithmResultRepository _algorithmResultRepository;
        private readonly IGeneticAlgorithmService _geneticAlgorithmService;
        private readonly IAntAlgorithmService _antAlgorithmService;
        private readonly IMapper _mapper;
        private readonly IHubContext<AlgorithmHub> _hubContext;


        public AlgorithmResultController(
            IUserContext userContext,
            ITaskRepository taskRepository,
            ISubtaskRepository subtaskRepository,
            IDailyAvailabilityRepository availabilityRepository,
            IAlgorithmResultRepository algorithmResultRepository,
            IGeneticAlgorithmService geneticAlgorithmService,
            IAntAlgorithmService antAlgorithmService,
            IMapper mapper,
            IHubContext<AlgorithmHub> hubContext)
        {
            _userContext = userContext;
            _taskRepository = taskRepository;
            _subtaskRepository = subtaskRepository;
            _availabilityRepository = availabilityRepository;
            _algorithmResultRepository = algorithmResultRepository;
            _geneticAlgorithmService = geneticAlgorithmService;
            _antAlgorithmService = antAlgorithmService;
            _mapper = mapper;

            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var user = _userContext.GetCurrentUser();
            var results = await _algorithmResultRepository.GetAllByUserId(user.Id);
            var dtos = _mapper.Map<List<AlgorithmResultDto>>(results);
            return View(dtos);
        }

        public IActionResult Run()
        {
            return View(new AlgorithmRunViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Run(AlgorithmRunViewModel model)
        {
            var user = _userContext.GetCurrentUser();

            // tasks: tylko nieukończone
            var tasks = await _taskRepository.GetAllByUserId(user.Id);
            var filteredTasks = tasks
                .Where(t => t.ProgressStatus != ProgressStatus.Completed)
                .ToList();

            // subtasks: tylko te, które mają EstimatedTime - WorkedHours > 0
            var subtasks = await _subtaskRepository.GetAllByUserId(user.Id);
            var filteredSubtasks = subtasks
                .Where(s => s.EstimatedTime - s.WorkedHours > 0)
                .Select(s =>
                {
                    s.EstimatedTime = s.EstimatedTime - s.WorkedHours;
                    return s;
                })
                .ToList();

            // availability: tylko od dziś do przyszłości
            var availability = await _availabilityRepository.GetAllByUserId(user.Id);
            var today = DateTime.Today;
            var filteredAvailability = availability
                .Where(a => a.Date >= today)
                .ToList();

            // mapowanie
            var taskDtos = _mapper.Map<List<TaskDto>>(filteredTasks);
            var subtaskDtos = _mapper.Map<List<SubtaskDto>>(filteredSubtasks);
            var availabilityDtos = _mapper.Map<List<DailyAvailabilityDto>>(filteredAvailability);

            var start = DateTime.UtcNow;
            AlgorithmRunResultDto runResult;

            if (model.AlgorithmType == AlgorithmType.Genetic)
            {
                var parameters = new GeneticAlgorithmParameters
                {
                    PopulationSize = model.PopulationSize,
                    Generations = model.Generations,
                    CrossoverProbability = model.CrossoverProbability,
                    MutationProbability = model.MutationProbability,
                    TournamentSize = model.TournamentSize
                };

                runResult = await _geneticAlgorithmService.RunAsync(
                    taskDtos,
                    subtaskDtos,
                    availabilityDtos,
                    parameters,
                    async (generation, score) =>
                    {
                        await _hubContext.Clients.User(user.Id.ToString()).SendAsync("AlgorithmProgress", new
                        {
                            generation,
                            score,
                            totalGenerations = parameters.Generations
                        });
                    });
            }
            else
            {
                var parameters = new AntAlgorithmParameters
                {
                    AntCount = model.AntCount,
                    Iterations = model.Iterations,
                    Alpha = model.Alpha,
                    Beta = model.Beta,
                    EvaporationRate = model.EvaporationRate,
                    Q = model.Q
                };

                runResult = await _antAlgorithmService.RunAsync(
                    taskDtos,
                    subtaskDtos,
                    availabilityDtos,
                    parameters,
                    async (iteration, score) =>
                    {
                        await _hubContext.Clients.User(user.Id.ToString()).SendAsync("AlgorithmProgress", new
                        {
                            generation = iteration,
                            score,
                            totalGenerations = parameters.Iterations
                        });
                    });
            }

            var duration = DateTime.UtcNow - start;

            var algorithmResult = new AlgorithmResult
            {
                Algorithm = model.AlgorithmType,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow,
                Duration = duration,
                ResultValue = runResult.BestScore,
                ResultData = JsonConvert.SerializeObject(runResult.Schedule),
                ParametersJson = runResult.ParametersJson,
                ScoreHistoryJson = runResult.ScoreHistoryJson
            };

            await _algorithmResultRepository.Create(algorithmResult);

            return Json(new { success = true, resultId = algorithmResult.Id });
        }





        public async Task<IActionResult> Details(int id)
        {
            var result = await _algorithmResultRepository.GetByIdAsync(id);
            if (result == null) return NotFound();

            var user = _userContext.GetCurrentUser();
            if (result.CreatedById != user.Id && !user.IsInRole("Moderator"))
                return RedirectToAction("NoAccess", "Home");

            var schedule = JsonConvert.DeserializeObject<List<ScheduleEntryDto>>(result.ResultData);

            var tasks = await _taskRepository.GetAllByUserId(user.Id);
            var filteredTasks = tasks
                .Where(t => t.ProgressStatus != ProgressStatus.Completed)
                .ToList();

            var subtasks = await _subtaskRepository.GetAllByUserId(user.Id);
            var filteredSubtasks = subtasks
                .Where(s => s.EstimatedTime - s.WorkedHours > 0)
                .Select(s =>
                {
                    s.EstimatedTime = s.EstimatedTime - s.WorkedHours;
                    return s;
                })
                .ToList();

            var availability = await _availabilityRepository.GetAllByUserId(user.Id);
            var today = DateTime.Today;
            var filteredAvailability = availability
                .Where(a => a.Date >= today)
                .ToList();

            var dto = new AlgorithmResultDetailsDto
            {
                Tasks = _mapper.Map<List<TaskDto>>(filteredTasks),
                Subtasks = _mapper.Map<List<SubtaskDto>>(filteredSubtasks),
                Availability = _mapper.Map<List<DailyAvailabilityDto>>(filteredAvailability)
            };

            ViewBag.Schedule = schedule;
            ViewBag.Algorithm = result.Algorithm;
            ViewBag.Duration = result.Duration;
            ViewBag.ResultValue = Math.Round(result.ResultValue, 2);
            ViewBag.ScoreHistory = result.ScoreHistoryJson;

            var totalHoursPlanned = schedule.Sum(x => x.Hours);
            var totalToPlan = dto.Subtasks.Sum(s => s.EstimatedTime);
            var plannedPercent = totalToPlan > 0 ? 100.0 * totalHoursPlanned / totalToPlan : 0;

            var taskDict = dto.Tasks.ToDictionary(t => t.EncodedName);
            int hoursOnTime = 0;
            int hoursLate = 0;

            foreach (var s in schedule)
            {
                var task = taskDict.GetValueOrDefault(s.TaskEncodedName ?? "");
                if (task != null && task.Deadline.HasValue)
                {
                    if (s.Date <= task.Deadline.Value)
                        hoursOnTime += s.Hours;
                    else
                        hoursLate += s.Hours;
                }
                else
                {
                    hoursOnTime += s.Hours;
                }
            }

            var hoursLatePercent = totalHoursPlanned > 0 ? 100.0 * hoursLate / totalHoursPlanned : 0;

            var availableTotal = dto.Availability.Sum(a => a.AvailableHours);
            var usagePercent = availableTotal > 0 ? (100.0 * totalHoursPlanned / availableTotal) : 0;

            int orderViolations = 0;
            var groupedByTask = dto.Subtasks.GroupBy(s => s.TaskEncodedName);

            foreach (var group in groupedByTask)
            {
                var ordered = group.OrderBy(s => s.Order).ToList();
                DateTime? lastEnd = null;

                foreach (var sub in ordered)
                {
                    var entries = schedule
                        .Where(e => e.Subtask != null
                                    && e.Subtask.TaskEncodedName == sub.TaskEncodedName
                                    && e.Subtask.Order == sub.Order)
                        .ToList();

                    if (entries.Count == 0)
                        continue;

                    var earliest = entries.Min(e => e.Date);
                    var latest = entries.Max(e => e.Date);

                    if (lastEnd.HasValue && earliest < lastEnd.Value)
                    {
                        orderViolations++;
                    }

                    lastEnd = latest;
                }
            }

            var nonSplitSubtasks = dto.Subtasks.Count(sub =>
            {
                var entries = schedule
                    .Where(s => s.Subtask?.Id == sub.Id)
                    .ToList();

                return entries.Count == 1 && entries.Sum(s => s.Hours) >= sub.EstimatedTime;
            });

            var avgDaysPerSubtask = dto.Subtasks
                .Select(sub =>
                {
                    var days = schedule
                        .Where(e => e.Subtask?.Id == sub.Id)
                        .Select(e => e.Date)
                        .Distinct()
                        .Count();
                    return days;
                })
                .DefaultIfEmpty(0)
                .Average();

            var avgBlockSize = schedule
                .GroupBy(x => (x.Date, x.Subtask?.Id))
                .Select(g => g.Sum(x => x.Hours))
                .DefaultIfEmpty(0)
                .Average();

            var overbookedDays = dto.Availability
                .Count(av =>
                {
                    var used = schedule
                        .Where(s => s.Date == av.Date)
                        .Sum(s => s.Hours);
                    return used > av.AvailableHours;
                });

            var avgUsagePerDay = dto.Availability.Any()
                ? dto.Availability.Average(av =>
                {
                    var used = schedule.Where(s => s.Date == av.Date).Sum(s => s.Hours);
                    return 100.0 * used / av.AvailableHours;
                })
                : 0;

            ViewBag.TotalHoursPlanned = totalHoursPlanned;
            ViewBag.TotalHoursPlannedPercent = Math.Round(plannedPercent, 1);
            ViewBag.HoursOnTime = hoursOnTime;
            ViewBag.HoursLate = hoursLate;
            ViewBag.HoursLatePercent = Math.Round(hoursLatePercent, 1);
            ViewBag.UsagePercent = Math.Round(usagePercent, 1);
            ViewBag.OrderViolations = orderViolations;
            ViewBag.NonSplitSubtasks = nonSplitSubtasks;
            ViewBag.AvgDaysPerSubtask = Math.Round(avgDaysPerSubtask, 2);
            ViewBag.AvgBlockSize = Math.Round(avgBlockSize, 2);
            ViewBag.OverbookedDays = overbookedDays;
            ViewBag.AvgUsagePerDay = Math.Round(avgUsagePerDay, 1);

            return View(dto);
        }



        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _algorithmResultRepository.GetByIdAsync(id);
            if (result == null) return NotFound();

            var user = _userContext.GetCurrentUser();
            if (result.CreatedById != user.Id && !user.IsInRole("Moderator"))
                return RedirectToAction("NoAccess", "Home");

            return View(_mapper.Map<AlgorithmResultDto>(result));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _algorithmResultRepository.GetByIdAsync(id);
            if (result == null) return NotFound();

            var user = _userContext.GetCurrentUser();
            if (result.CreatedById != user.Id && !user.IsInRole("Moderator"))
                return RedirectToAction("NoAccess", "Home");

            await _algorithmResultRepository.Delete(id);
            await _algorithmResultRepository.Commit();

            this.SetNotification("success", "Algorithm result deleted successfully.");
            return RedirectToAction(nameof(Index));
        }
    }
}
