using AutoMapper;
using GroupPlanner.Application.Subtask.Commands;
using GroupPlanner.Application.Subtask.Commands.Delete;
using GroupPlanner.Application.Subtask.Queries;
using GroupPlanner.Application.Task.Commands.CreateTask;
using GroupPlanner.Application.Task.Commands.DelateTask;
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
        [Authorize]
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
        [HttpGet]
        [Authorize]
        [Route("Task/{encodedName}/Edit")]
        public async Task<IActionResult> Edit(string encodedName)
        {
            var dto = await _mediator.Send(new GetTaskByEncodedNameQuery(encodedName));
            if (!dto.IsEditable)
            {
                return RedirectToAction("NoAccess", "Home");
            }

            // Tworzymy model `EditTaskCommand`
            EditTaskCommand model = _mapper.Map<EditTaskCommand>(dto);

            // Ustawiamy `TaskDeadline` w `CreateSubtaskCommand`
            var createSubtaskCommand = new CreateSubtaskCommand
            {
                TaskEncodedName = dto.EncodedName,
                TaskDeadline = dto.Deadline // Przekazanie deadline zadania nadrzędnego
            };

            // Przekazujemy `CreateSubtaskCommand` do widoku przez ViewData
            ViewData["CreateSubtaskCommand"] = createSubtaskCommand;

            return View(model);
        }

        [HttpGet]
        [Authorize]
        [Route("Task/{encodedName}/Delete")]
        public async Task<IActionResult> Delete(string encodedName)
        {
            var dto = await _mediator.Send(new GetTaskByEncodedNameQuery(encodedName));
            return View(dto);  // Widok potwierdzenia usunięcia
        }
        [HttpPost]
        [Authorize]
        [Route("Task/{encodedName}/Delete")]
        public async Task<IActionResult> DeleteConfirmed(string encodedName)
        {
            await _mediator.Send(new DeleteTaskCommand(encodedName));
            this.SetNotification("success", "Task deleted successfully");
            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> CreateSubtask(CreateSubtaskCommand command)
        {
            // Pobierz główne zadanie, aby sprawdzić jego deadline
            var parentTask = await _mediator.Send(new GetTaskByEncodedNameQuery(command.TaskEncodedName));

            if (parentTask == null)
            {
                return BadRequest("Parent task not found.");
            }

            if (command.Deadline > parentTask.Deadline)
            {
                ModelState.AddModelError("Deadline", "Subtask deadline cannot exceed parent task deadline.");
                return BadRequest(ModelState); // Zwrot szczegółów błędu do klienta
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // W przypadku innych błędów walidacji
            }

            await _mediator.Send(command);
            return Ok();
        }

        [HttpGet]
        [Route("Task/{encodedName}/Subtask")]
        public async Task<IActionResult> GetSubtasks(string encodedName)
        {

            var data = await _mediator.Send(new GetSubtasksQuery() { EncodedName = encodedName });
            return Ok(data);
        }

        [HttpDelete]
        [Route("Task/{encodedName}/Subtask/{subtaskId}")]
        public async Task<IActionResult> DeleteSubtask(string encodedName, int subtaskId)
        {
            await _mediator.Send(new DeleteSubtaskCommand { Id = subtaskId });
            return Ok(); 
        }
    }
}
