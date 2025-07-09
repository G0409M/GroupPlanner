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

            // Pobierz harmonogram u¿ytkownika z bazy (dla kropek w kalendarzu)
            var latestSchedule = await _userScheduleRepository.GetLatestByUserId(user.Id);
            List<ScheduleEntryDto> plannedEntries = new();
            if (latestSchedule != null && !string.IsNullOrEmpty(latestSchedule.ScheduleDataJson))
            {
                plannedEntries = JsonConvert.DeserializeObject<List<ScheduleEntryDto>>(latestSchedule.ScheduleDataJson)
                                 ?? new List<ScheduleEntryDto>();
            }

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

            return View(model);
        }
    }
}
