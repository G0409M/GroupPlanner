using AutoMapper;
using GroupPlanner.Application.Algorithms;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure.Persistance.Repositories;
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

        public CalendarController(IAlgorithmResultRepository algorithmResultRepository, ITaskRepository taskRepository, IDailyAvailabilityRepository repository, IUserContext userContext)
        {
            _taskRepository = taskRepository;
            _algorithmResultRepository = algorithmResultRepository;
            _repository = repository;
            _userContext = userContext;
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
                Score = r.ResultValue   // z wielkiej litery!
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

            // Rozpakuj JSON z ResultData
            var schedule = JsonConvert.DeserializeObject<List<ScheduleEntryDto>>(result.ResultData);
            if (schedule == null)
                return Json(new List<object>());

            // pobierz słownik nazw tasków użytkownika
            var tasks = await _taskRepository.GetAllByUserId(user.Id);
            var taskNameMap = tasks.ToDictionary(t => t.EncodedName, t => t.Name);

            // przekształć na eventy dla FullCalendar
            var grouped = schedule
                .GroupBy(s => new { s.Date, s.SubtaskDescription, s.Subtask.TaskEncodedName })
                .Select(g => new
                {
                    title = $"{taskNameMap.GetValueOrDefault(g.Key.TaskEncodedName)} - {g.Sum(x => x.Hours)}h",
                    start = g.Key.Date.ToString("yyyy-MM-dd"),
                    color = "#FF9800", // default, nadpiszemy w JS
                    extendedProps = new
                    {
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




    }
}
