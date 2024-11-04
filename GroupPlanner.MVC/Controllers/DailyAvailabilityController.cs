using AutoMapper;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Task.Queries.GetAllTasks;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroupPlanner.MVC.Controllers
{
    [Authorize]
    public class DailyAvailabilityController : Controller
    {
        private readonly IDailyAvailabilityRepository _repository;
        private readonly IUserContext _userContext;

        public DailyAvailabilityController(IDailyAvailabilityRepository repository, IUserContext userContext)
        {
            _repository = repository;
            _userContext = userContext;
        }

        public IActionResult Index()
        {
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
            return Json(availabilities);
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
    }
}
