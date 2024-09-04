using AutoMapper;
using GroupPlanner.Application.Task.Commands.CreateTask;
using GroupPlanner.Application.Task.Commands.EditTask;
using GroupPlanner.Application.Task.Queries.GetAllTasks;
using GroupPlanner.Application.Task.Queries.GetTaskByEncodedName;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GroupPlanner.MVC.Controllers
{
    public class TaskController:Controller
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public TaskController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        public async Task<IActionResult> Index()
        {
            var task = await _mediator.Send(new GetAllTasksQuery());
            return View(task);
        }

        [Route("Task/{encodedName}/Details")]
        public async Task<IActionResult> Details(string encodedName)
        {
          var dto = await  _mediator.Send(new GetTaskByEncodedNameQuery(encodedName));
            return View(dto);
        }
        [Route("Task/{encodedName}/Edit")]
        public async Task<IActionResult> Edit(string encodedName)
        {
            var dto = await _mediator.Send(new GetTaskByEncodedNameQuery(encodedName));
            EditTaskCommand model = _mapper.Map<EditTaskCommand>(dto);
            return View(model);
        }
        [HttpPost]

        [Route("Task/{encodedName}/Edit")]
        public async Task<IActionResult> Create(string encodedName,EditTaskCommand commad)
        {
            if (!ModelState.IsValid)
            {
                return View(commad);
            }
            await _mediator.Send(commad);
            return RedirectToAction(nameof(Index));
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
