using AutoMapper;
using GroupPlanner.Application.Subtask.Commands;
using GroupPlanner.Application.Subtask.Queries;
using GroupPlanner.Application.Task.Commands.CreateTask;
using GroupPlanner.Application.Task.Commands.EditTask;
using GroupPlanner.Application.Task.Queries.GetAllTasks;
using GroupPlanner.Application.Task.Queries.GetTaskByEncodedName;
using GroupPlanner.MVC.Extensions;
using GroupPlanner.MVC.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
            if(!dto.IsEditable)
            {
                return RedirectToAction("NoAccess", "Home");
            }
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
        [Authorize]
        public IActionResult Create()
        {
            
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CreateTaskCommand commad)
        {
            if(!ModelState.IsValid)
            {
                return View(commad);
            }
            await _mediator.Send(commad);
            this.SetNotification("success", $"Created task: {commad.Name}");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [Route("Task/Subtask")]
        public async Task<IActionResult> CreateSubtask(CreateSubtaskCommand commad)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _mediator.Send(commad);
            return Ok();
        }
        [HttpGet]
        [Route("Task/{encodedName}/Subtask")]
        public async Task<IActionResult> GetSubtasks(string encodedName)
        {

            var data = await _mediator.Send(new GetSubtasksQuery() { EncodedName = encodedName });
            return Ok(data);
        }


    }
}
