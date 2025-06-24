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
        //private readonly IAntAlgorithmService _antAlgorithmService;
        private readonly IMapper _mapper;

        public AlgorithmResultController(
            IUserContext userContext,
            ITaskRepository taskRepository,
            ISubtaskRepository subtaskRepository,
            IDailyAvailabilityRepository availabilityRepository,
            IAlgorithmResultRepository algorithmResultRepository,
            IGeneticAlgorithmService geneticAlgorithmService,
            //IAntAlgorithmService antAlgorithmService,
            IMapper mapper)
        {
            _userContext = userContext;
            _taskRepository = taskRepository;
            _subtaskRepository = subtaskRepository;
            _availabilityRepository = availabilityRepository;
            _algorithmResultRepository = algorithmResultRepository;
            _geneticAlgorithmService = geneticAlgorithmService;
            //_antAlgorithmService = antAlgorithmService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var user = _userContext.GetCurrentUser();
            var results = await _algorithmResultRepository.GetAllByUserId(user.Id);
            var dtos = _mapper.Map<List<AlgorithmResultDto>>(results);
            return View(dtos);
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

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Run(AlgorithmType algorithm)
        {
            var user = _userContext.GetCurrentUser();

            var tasks = await _taskRepository.GetAllByUserId(user.Id);
            var subtasks = await _subtaskRepository.GetAllByUserId(user.Id);
            var availability = await _availabilityRepository.GetAllByUserId(user.Id);

            var taskDtos = _mapper.Map<List<TaskDto>>(tasks);
            var subtaskDtos = _mapper.Map<List<SubtaskDto>>(subtasks);
            var availabilityDtos = _mapper.Map<List<DailyAvailabilityDto>>(availability);

            var start = DateTime.UtcNow;
            List<ScheduleEntryDto> result;

            if (algorithm == AlgorithmType.Genetic)
                result = await _geneticAlgorithmService.RunAsync(taskDtos, subtaskDtos, availabilityDtos);
            else
                result = await _geneticAlgorithmService.RunAsync(taskDtos, subtaskDtos, availabilityDtos);

            var duration = DateTime.UtcNow - start;
            var resultValue = ScheduleEvaluator.Evaluate(result, subtaskDtos, taskDtos, availabilityDtos);

            var algorithmResult = new AlgorithmResult
            {
                Algorithm = algorithm,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow,
                Duration = duration,
                ResultValue = resultValue,
                ResultData = JsonConvert.SerializeObject(result)
            };

            await _algorithmResultRepository.Create(algorithmResult);

            this.SetNotification("success", $"Algorithm {algorithm} completed successfully.");
            return RedirectToAction(nameof(Index));
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
