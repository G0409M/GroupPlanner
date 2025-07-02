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
            var tasks = await _taskRepository.GetAllByUserId(user.Id);
            var subtasks = await _subtaskRepository.GetAllByUserId(user.Id);
            var availability = await _availabilityRepository.GetAllByUserId(user.Id);

            var taskDtos = _mapper.Map<List<TaskDto>>(tasks);
            var subtaskDtos = _mapper.Map<List<SubtaskDto>>(subtasks);
            var availabilityDtos = _mapper.Map<List<DailyAvailabilityDto>>(availability);

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
            var subtasks = await _subtaskRepository.GetAllByUserId(user.Id);
            var availability = await _availabilityRepository.GetAllByUserId(user.Id);

            var dto = new AlgorithmResultDetailsDto
            {
                Tasks = _mapper.Map<List<TaskDto>>(tasks),
                Subtasks = _mapper.Map<List<SubtaskDto>>(subtasks),
                Availability = _mapper.Map<List<DailyAvailabilityDto>>(availability)
            };

            ViewBag.Schedule = schedule;
            ViewBag.Algorithm = result.Algorithm;
            ViewBag.Duration = result.Duration;
            ViewBag.ResultValue = result.ResultValue;
            ViewBag.ScoreHistory = result.ScoreHistoryJson;



            // KPI do widoku
            var totalHoursPlanned = schedule.Sum(x => x.Hours);

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

            var availableTotal = dto.Availability.Sum(a => a.AvailableHours);
            var usagePercent = availableTotal > 0 ? (100.0 * totalHoursPlanned / availableTotal) : 0;

            // liczba naruszeń kolejności
            int orderViolations = 0;
            var groupedByTask = dto.Subtasks.GroupBy(s => s.TaskEncodedName);

            foreach (var group in groupedByTask)
            {
                var ordered = group.OrderBy(s => s.Order).ToList();
                DateTime? lastEnd = null;

                foreach (var sub in ordered)
                {
                    var entries = schedule
                        .Where(e => e.Subtask != null && e.Subtask.TaskEncodedName == sub.TaskEncodedName)
                        .OrderBy(e => e.Date)
                        .ToList();

                    if (entries.Count == 0)
                        continue;

                    var earliest = entries.First().Date;
                    if (lastEnd.HasValue && earliest < lastEnd.Value)
                    {
                        orderViolations++;
                    }
                    lastEnd = entries.Last().Date;
                }
            }


            // do ViewBag
            ViewBag.TotalHoursPlanned = totalHoursPlanned;
            ViewBag.HoursOnTime = hoursOnTime;
            ViewBag.HoursLate = hoursLate;
            ViewBag.UsagePercent = Math.Round(usagePercent, 1);
            ViewBag.OrderViolations = orderViolations;

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
