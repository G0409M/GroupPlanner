using GroupPlanner.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupPlanner.MVC.Controllers
{
    public class TaskController:Controller
    {
        private readonly ITaskService _taskService;
        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }
        [HttpPost]
        public async Task<IActionResult> Create(Domain.Entities.Task task)
        {
            await _taskService.Create(task);
            return RedirectToAction(nameof(Create)); // refactor in future
        }

    }
}
