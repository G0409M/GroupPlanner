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

            // Pobierz wszystkie zadania z podzadaniami
            var tasks = await _taskRepository.GetAllByUserId(user.Id);

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

            var taskNameMap = tasks.ToDictionary(t => t.EncodedName!, t => t.Name);

            // KPI
            var total = subtasks.Count;
            var completed = subtasks.Count(s => s.ProgressStatus == ProgressStatus.Completed);
            var inProgress = subtasks.Count(s => s.ProgressStatus == ProgressStatus.InProgress);
            var notStarted = subtasks.Count(s => s.ProgressStatus == ProgressStatus.NotStarted);
            var totalEstimated = subtasks.Sum(s => s.EstimatedTime);
            var totalWorked = subtasks.Sum(s => s.WorkedHours);

            // Harmonogram
            var latestSchedule = await _userScheduleRepository.GetLatestByUserId(user.Id);
            List<ScheduleEntryDto> plannedEntries = new();
            if (latestSchedule != null && !string.IsNullOrEmpty(latestSchedule.ScheduleDataJson))
            {
                plannedEntries = JsonConvert.DeserializeObject<List<ScheduleEntryDto>>(latestSchedule.ScheduleDataJson)
                                 ?? new List<ScheduleEntryDto>();
            }

            // Uzupe³nij dane w podzadaniach harmonogramu
            var subtaskIds = plannedEntries
                .Where(e => e.Subtask != null)
                .Select(e => e.Subtask.Id)
                .Distinct()
                .ToList();

            var dbSubtasks = await _subtaskRepository.GetByIds(subtaskIds);
            var subtaskMap = dbSubtasks
                .Select(s =>
                {
                    var dto = _mapper.Map<SubtaskDto>(s);
                    dto.TaskEncodedName = s.Task?.EncodedName;
                    return dto;
                })
                .ToDictionary(s => s.Id, s => s);

            foreach (var entry in plannedEntries)
            {
                if (entry.Subtask != null && subtaskMap.TryGetValue(entry.Subtask.Id, out var enriched))
                {
                    entry.Subtask = enriched;
                }
            }

            var today = DateTime.Today;
            var upcoming = plannedEntries
                .Where(e => e.Date >= today && e.Date <= today.AddDays(3))
                .OrderBy(e => e.Date)
                .ToList();

            var model = new DashboardViewModel
            {
                Subtasks = subtasks,
                TaskNameMap = taskNameMap,
                LatestSchedule = latestSchedule,
                PlannedEntries = plannedEntries,
                UpcomingTasks = upcoming,
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

            var userScheduleGrouped = plannedEntries
                .GroupBy(e => e.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Entries = g.ToList()
                })
                .ToList();
            ViewBag.GroupedSchedule = userScheduleGrouped;


            return View(model);
        }
    }
}
