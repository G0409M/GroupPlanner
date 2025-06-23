using AutoMapper;
using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Repositories;
using GroupPlanner.MVC.Extensions;
using GroupPlanner.MVC.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.Json;

namespace GroupPlanner.MVC.Controllers
{
    public class AlgorithmResultController : Controller
    {
        private readonly IAlgorithmResultRepository _algorithmResultRepository;
        private readonly IDailyAvailabilityRepository _availabilityRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ISubtaskRepository _subtaskRepository;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;
        private readonly IHubContext<AlgorithmHub> _hubContext;


        public AlgorithmResultController(IAlgorithmResultRepository algorithmResultRepository,IDailyAvailabilityRepository availabilityRepository, ITaskRepository taskRepository,
        ISubtaskRepository subtaskRepository,IUserContext userContext,IMapper mapper, IHubContext<AlgorithmHub> hubContext)
        {
            _algorithmResultRepository = algorithmResultRepository;
            _availabilityRepository = availabilityRepository;
            _taskRepository = taskRepository;
            _subtaskRepository = subtaskRepository;
            _userContext = userContext;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var results = await _algorithmResultRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<AlgorithmResultDto>>(results);
            return View(dtos);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Run()
        {
            return View(new AlgorithmResultDto());
        }

        [Authorize]
        [HttpPost]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Run(AlgorithmType algorithmType)
        {
            var user = _userContext.GetCurrentUser();

            // 🔄 Powiadomienie klienta, że algorytm się rozpoczął
            await _hubContext.Clients.All.SendAsync("AlgorithmStarted");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var availability = await _availabilityRepository.GetAllByUserId(user.Id);
            var tasks = await _taskRepository.GetAllByUserId(user.Id);

            var allSubtasks = new List<Subtask>();
            foreach (var task in tasks)
            {
                var taskSubtasks = await _subtaskRepository.GetAllByEncodedName(task.EncodedName!);
                allSubtasks.AddRange(taskSubtasks);
            }

            // Symulacja działania algorytmu – sztuczne opóźnienie
            await System.Threading.Tasks.Task.Delay(10_000);

            stopwatch.Stop();

            var resultDetails = new
            {
                Availabilities = availability.Select(a => new { a.Date, a.AvailableHours }),
                Tasks = tasks.Select(t => new { t.Name, t.TaskType, t.Priority }),
                Subtasks = allSubtasks.Select(s => new { s.Description, s.EstimatedTime, s.ProgressStatus })
            };

            var result = new AlgorithmResult
            {
                Algorithm = algorithmType,
                CreatedAt = DateTime.UtcNow,
                CreatedById = user.Id,
                ResultValue = new Random().NextDouble(),
                Duration = stopwatch.Elapsed, // używamy właściwości typu TimeSpan
                ResultData = Newtonsoft.Json.JsonConvert.SerializeObject(resultDetails)
            };

            await _algorithmResultRepository.SaveAsync(result);

            await _hubContext.Clients.All.SendAsync("AlgorithmFinished");

            this.SetNotification("success", $"Algorithm '{algorithmType}' has been executed.");
            return RedirectToAction(nameof(Index));
        }


        private double RunGeneticAlgorithm()
        {
            // Implementacja algorytmu genetycznego
            return new Random().NextDouble();
        }

        private double RunAntAlgorithm()
        {
            // Implementacja algorytmu mrówkowego
            return new Random().NextDouble();
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var entity = (await _algorithmResultRepository.GetAllAsync()).FirstOrDefault(r => r.Id == id);
            if (entity == null)
                return NotFound();

            var dto = _mapper.Map<AlgorithmResultDto>(entity);
            return View(dto);
        }
    }
}
