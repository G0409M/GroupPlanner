using Microsoft.AspNetCore.Authorization;
using GroupPlanner.Domain.Interfaces;
using System.Linq;
using GroupPlanner.Application.ApplicationUser;
using Microsoft.AspNetCore.Mvc;

namespace GroupPlanner.MVC.Controllers;

[Authorize]
public class AvailabilityController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDailyAvailabilityRepository _availabilityRepository;
    private readonly IUserContext _userContext;

    public AvailabilityController(ILogger<HomeController> logger, IDailyAvailabilityRepository availabilityRepository, IUserContext userContext)
    {
        _logger = logger;
        _availabilityRepository = availabilityRepository;
        _userContext = userContext;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetDailyAvailabilityData()
    {
        var currentUser = _userContext.GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var availabilities = await _availabilityRepository.GetAllByUserId(currentUser.Id);

        var result = availabilities
        .OrderBy(a => a.Date)
        .Select(a => new
        {
            Date = a.Date.ToString("yyyy-MM-dd"),
            AvailableHours = a.AvailableHours
        }).ToList();

        return Json(result);
    }
}
