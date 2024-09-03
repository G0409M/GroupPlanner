using GroupPlanner.Application.Dto.Task;
using GroupPlanner.Application.Task.Commands.CreateTask;
using GroupPlanner.Application.Task.Queries.GetAllTasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GroupPlanner.MVC.Controllers
{
    public class TaskController:Controller
    {
        private readonly IMediator _mediator;

        public TaskController(IMediator mediator)
        {
            _mediator = mediator;
        }
        public async Task<IActionResult> Index()
        {
            var task = await _mediator.Send(new GetAllTasksQuery());
            return View(task);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateTaskCommand commad)
        {
            if(!ModelState.IsValid)
            {
                return View(commad);
            }
            await _mediator.Send(commad);
            return RedirectToAction(nameof(Index));
        }

    }
}
