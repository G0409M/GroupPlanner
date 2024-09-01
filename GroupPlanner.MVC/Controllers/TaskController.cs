using GroupPlanner.Application.Dto.Task;
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
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(TaskDto task)
        {
            if(!ModelState.IsValid)
            {
                return View(task);
            }
            await _taskService.Create(task);
            return RedirectToAction(nameof(Create)); // refactor in future
        }

    }
}
