using AutoMapper;
using GroupPlanner.Application.Algorithms;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance.Repositories;
using GroupPlanner.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace GroupPlanner.MVC.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly IAlgorithmResultRepository _algorithmResultRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IDailyAvailabilityRepository _repository;
        private readonly IUserContext _userContext;
        private readonly IUserScheduleRepository _userScheduleRepository;
        private readonly ISubtaskRepository _subtaskRepository;

        public CalendarController(IAlgorithmResultRepository algorithmResultRepository, ITaskRepository taskRepository, IDailyAvailabilityRepository repository, IUserContext userContext, IUserScheduleRepository userScheduleRepository, ISubtaskRepository subtaskRepository)
        {
            _taskRepository = taskRepository;
            _algorithmResultRepository = algorithmResultRepository;
            _repository = repository;
            _userContext = userContext;
            _userScheduleRepository = userScheduleRepository;
            _subtaskRepository = subtaskRepository;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = _userContext.GetCurrentUser();
            var results = await _algorithmResultRepository.GetAllByUserId(currentUser.Id);

            ViewBag.AlgorithmResults = results.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = $"{r.Algorithm} | wynik: {r.ResultValue:F2} | {r.CreatedAt:yyyy-MM-dd}"
            }).ToList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveAvailability([FromBody] DailyAvailabilityDto availabilityDto)
        {
            var currentUser = _userContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var availability = new DailyAvailability
            {
                Date = availabilityDto.Date,
                AvailableHours = availabilityDto.AvailableHours,
                CreatedById = currentUser.Id
            };

            await _repository.Create(availability);
            await _repository.Commit();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailabilities()
        {
            var currentUser = _userContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var availabilities = await _repository.GetAllByUserId(currentUser.Id);

            var result = availabilities.Select(x => new DailyAvailabilityDto
            {
                Id = x.Id,
                Date = x.Date,
                AvailableHours = x.AvailableHours
            });

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAvailability([FromBody] DailyAvailabilityDto availabilityDto)
        {
            var currentUser = _userContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var existingAvailability = await _repository.GetByDateAndUserId(availabilityDto.Date, currentUser.Id);
            if (existingAvailability == null || existingAvailability.CreatedById != currentUser.Id)
            {
                return NotFound();
            }

            existingAvailability.AvailableHours = availabilityDto.AvailableHours;
            await _repository.Update(existingAvailability);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAvailability([FromBody] int id)
        {
            var currentUser = _userContext.GetCurrentUser();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var availability = await _repository.GetById(id);
            if (availability == null || availability.CreatedById != currentUser.Id)
            {
                return NotFound();
            }

            await _repository.Delete(id);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAlgorithmResults()
        {
            var user = _userContext.GetCurrentUser();
            if (user == null)
                return Unauthorized();

            var results = await _algorithmResultRepository.GetAllByUserId(user.Id);

            var dto = results.Select(r => new
            {
                r.Id,
                Algorithm = r.Algorithm == AlgorithmType.Genetic ? "Genetic" : "Ant",
                Created = r.CreatedAt,
                Score = r.ResultValue
            });

            return Json(dto);
        }

        [HttpGet]
        public async Task<IActionResult> GetPlannedEntriesForResult(int resultId)
        {
            var user = _userContext.GetCurrentUser();
            if (user == null)
                return Unauthorized();

            var result = await _algorithmResultRepository.GetByIdAsync(resultId);
            if (result == null || result.CreatedById != user.Id)
                return NotFound();

            var schedule = JsonConvert.DeserializeObject<List<ScheduleEntryDto>>(result.ResultData);
            if (schedule == null)
                return Json(new List<object>());

            var tasks = await _taskRepository.GetAllByUserId(user.Id);
            var taskNameMap = tasks.ToDictionary(t => t.EncodedName, t => t.Name);

            var grouped = schedule
                .GroupBy(s => new { s.Date, s.SubtaskDescription, s.Subtask.TaskEncodedName })
                .Select(g => new
                {
                    title = $"{taskNameMap.GetValueOrDefault(g.Key.TaskEncodedName)} - {g.Sum(x => x.Hours)}h",
                    start = g.Key.Date.ToString("yyyy-MM-dd"),
                    color = "#FF9800",
                    extendedProps = new
                    {
                        subtaskId = g.First().Subtask.Id,
                        subtaskDescription = g.Key.SubtaskDescription,
                        hours = g.Sum(x => x.Hours),
                        isPlanned = true,
                        taskEncodedName = g.Key.TaskEncodedName,
                        taskName = taskNameMap.GetValueOrDefault(g.Key.TaskEncodedName) ?? "(Unknown Task)"
                    }
                })
                .ToList();

            return Json(grouped);
        }

        [HttpPost]
        public async Task<IActionResult> SaveUserSchedule([FromBody] List<ScheduleEntryDto> modifiedSchedule)
        {
            var user = _userContext.GetCurrentUser();
            if (user == null)
                return Unauthorized();

            var userSubtasks = await _subtaskRepository.GetAllByUserId(user.Id);
            var subtaskMap = userSubtasks.ToDictionary(s => s.Id);

            foreach (var entry in modifiedSchedule)
            {
                if (entry.Subtask != null && subtaskMap.TryGetValue(entry.Subtask.Id, out var fullSubtask))
                {
                    entry.Subtask.Description = fullSubtask.Description;
                    entry.Subtask.ProgressStatus = fullSubtask.ProgressStatus;
                    entry.Subtask.EstimatedTime = fullSubtask.EstimatedTime;
                    entry.Subtask.WorkedHours = fullSubtask.WorkedHours;
                    entry.Subtask.TaskEncodedName = fullSubtask.Task.EncodedName;
                    entry.Subtask.TaskDeadline = fullSubtask.Task.Details.Deadline;
                    entry.Subtask.Order = fullSubtask.Order;
                }
            }

            var schedule = new UserSchedule
            {
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow,
                ScheduleDataJson = JsonConvert.SerializeObject(modifiedSchedule)
            };

            await _userScheduleRepository.Create(schedule);
            await _userScheduleRepository.Commit();

            return Ok(new { success = true, scheduleId = schedule.Id });
        }
    }
}
