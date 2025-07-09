using AutoMapper;
using GroupPlanner.Application.Algorithms;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GroupPlanner.MVC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IUserContext _userContext;
        private readonly ISubtaskRepository _subtaskRepository;
        private readonly IUserScheduleRepository _userScheduleRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public HomeController(
            IUserContext userContext,
            ISubtaskRepository subtaskRepository,
            IUserScheduleRepository userScheduleRepository,
            ITaskRepository taskRepository,
            IMapper mapper)
        {
            _userContext = userContext;
            _subtaskRepository = subtaskRepository;
            _userScheduleRepository = userScheduleRepository;
            _taskRepository = taskRepository;
            _mapper = mapper;
        }


        public async Task<IActionResult> Index()
        {
            var user = _userContext.GetCurrentUser();
            if (user == null) return Unauthorized();

            // Pobierz wszystkie zadania użytkownika
            var tasks = await _taskRepository.GetAllByUserId(user.Id);

            // Zmapuj podzadania wraz z dodatkowymi informacjami
            var subtasks = tasks.SelectMany(t =>
            {
                var taskEncoded = t.EncodedName;
                return t.Subtasks.Select(s =>
                {
                    var dto = _mapper.Map<SubtaskDto>(s);
                    dto.TaskEncodedName = taskEncoded;
                    dto.TaskDeadline = t.Details.Deadline;
                    return dto;
                });
            }).ToList();

            // Mapa nazw zadań
            var taskNameMap = tasks.ToDictionary(t => t.EncodedName!, t => t.Name);

            // Statystyki KPI
            var total = subtasks.Count;
            var completed = subtasks.Count(s => s.ProgressStatus == ProgressStatus.Completed);
            var inProgress = subtasks.Count(s => s.ProgressStatus == ProgressStatus.InProgress);
            var notStarted = subtasks.Count(s => s.ProgressStatus == ProgressStatus.NotStarted);
            var totalEstimated = subtasks.Sum(s => s.EstimatedTime);
            var totalWorked = subtasks.Sum(s => s.WorkedHours);

            // Odczyt harmonogramu użytkownika z bazy
            var latestSchedule = await _userScheduleRepository.GetLatestByUserId(user.Id);
            List<ScheduleEntryDto> plannedEntries = new();
            if (latestSchedule != null && !string.IsNullOrEmpty(latestSchedule.ScheduleDataJson))
            {
                plannedEntries = JsonConvert.DeserializeObject<List<ScheduleEntryDto>>(latestSchedule.ScheduleDataJson)
                                 ?? new List<ScheduleEntryDto>();
            }

            // Aktualizacja workedHours z bazy (by zgadzało się z bieżącymi danymi)
            foreach (var entry in plannedEntries)
            {
                if (entry.Subtask != null)
                {
                    var subtaskId = entry.Subtask.Id;
                    var latest = subtasks.FirstOrDefault(s => s.Id == subtaskId);
                    if (latest != null)
                    {
                        entry.Subtask.WorkedHours = latest.WorkedHours;
                    }
                }
            }

            // ✅ Remaining time per task (dla wykresu)
            // ✅ Oblicz dane dla wykresu "Remaining Time per Task" bazując na danych z tasks
            var taskRemainingData = tasks
                .Where(t => t.Subtasks != null && t.Subtasks.Any())
                .Select(t => new
                {
                    TaskName = t.Name,
                    Remaining = t.Subtasks.Sum(st => Math.Max(st.EstimatedTime - st.WorkedHours, 0))
                })
                .Where(t => t.Remaining > 0)
                .ToList();

            ViewBag.TaskRemainingData = taskRemainingData;


            // Model do widoku
            var model = new DashboardViewModel
            {
                Subtasks = subtasks,
                TaskNameMap = taskNameMap,
                PlannedEntries = plannedEntries,
                KPI = new KPIViewModel
                {
                    CompletedCount = completed,
                    CompletedPercent = total == 0 ? 0 : (int)(completed * 100.0 / total),
                    InProgressCount = inProgress,
                    InProgressPercent = total == 0 ? 0 : (int)(inProgress * 100.0 / total),
                    NotStartedCount = notStarted,
                    NotStartedPercent = total == 0 ? 0 : (int)(notStarted * 100.0 / total),
                    TotalEstimated = totalEstimated,
                    TotalWorked = totalWorked
                }
            };

            // Sekcja "Upcoming 3 days"
            var upcomingDays = plannedEntries
                .Where(e => e.Date >= DateTime.Today && e.Subtask != null)
                .GroupBy(e => e.Date.Date)
                .OrderBy(g => g.Key)
                .Take(3)
                .Select(g => new UpcomingDayDto
                {
                    Date = g.Key,
                    Entries = g.Select(e => new UpcomingEntryDto
                    {
                        TaskName = e.Subtask!.TaskEncodedName != null && taskNameMap.TryGetValue(e.Subtask.TaskEncodedName, out var name) ? name : "(unknown)",
                        SubtaskDescription = e.Subtask.Description ?? "(no description)",
                        Hours = e.Hours,
                        SubtaskId = e.Subtask.Id,
                        WorkedHours = e.Subtask.WorkedHours
                    }).ToList()
                }).ToList();

            model.UpcomingDays = upcomingDays;

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> MarkWorkedByHours([FromBody] MarkWorkedRequest request)
        {
            var user = _userContext.GetCurrentUser();
            if (user == null) return Unauthorized();

            var subtask = await _subtaskRepository.GetByIdAsync(request.SubtaskId);
            if (subtask == null) return NotFound();

            int updatedHours = subtask.WorkedHours + request.Hours;
            await _subtaskRepository.UpdateWorkedHours(request.SubtaskId, updatedHours);

            return Json(new { success = true });
        }



    }
    public class MarkWorkedRequest
    {
        public int SubtaskId { get; set; }
        public int Hours { get; set; }
    }
}
