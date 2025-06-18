using AutoMapper;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;
using GroupPlanner.Domain.Interfaces;
using GroupPlanner.Infrastructure;
using GroupPlanner.MVC.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroupPlanner.MVC.Controllers
{
    public class TaskController : Controller
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ISubtaskRepository _subtaskRepository;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;

        public TaskController(
            ITaskRepository taskRepository,
            ISubtaskRepository subtaskRepository,
            IUserContext userContext,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _subtaskRepository = subtaskRepository;
            _userContext = userContext;
            _mapper = mapper;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = _userContext.GetCurrentUser();
            var tasks = await _taskRepository.GetAllByUserId(user.Id);
            var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);
            return View(dtos);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(TaskDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var task = _mapper.Map<Domain.Entities.Task>(dto);
            var user = _userContext.GetCurrentUser();

            task.CreatedById = user.Id;
            task.EncodeName();
            task.Details.CreatedAt = DateTime.Now;

            await _taskRepository.Create(task);

            this.SetNotification("success", $"Created task: {dto.Name}");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("Task/{encodedName}/Details")]
        public async Task<IActionResult> Details(string encodedName)
        {
            var task = await _taskRepository.GetByEncodedName(encodedName);
            if (task == null) return NotFound();

            var dto = _mapper.Map<TaskDto>(task);
            return View(dto);
        }

        [HttpGet]
        [Route("Task/{encodedName}/Edit")]
        public async Task<IActionResult> Edit(string encodedName)
        {
            var task = await _taskRepository.GetByEncodedName(encodedName);
            if (task == null) return NotFound();

            var user = _userContext.GetCurrentUser();
            if (task.CreatedById != user.Id && !user.IsInRole("Moderator"))
                return RedirectToAction("NoAccess", "Home");

            var dto = _mapper.Map<TaskDto>(task);
            return View(dto);
        }

        [HttpPost]
        [Route("Task/{encodedName}/Edit")]
        public async Task<IActionResult> Edit(string encodedName, TaskDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var task = await _taskRepository.GetByEncodedName(encodedName);
            if (task == null) return NotFound();

            var user = _userContext.GetCurrentUser();
            if (task.CreatedById != user.Id && !user.IsInRole("Moderator"))
                return RedirectToAction("NoAccess", "Home");

            task.Name = dto.Name;
            task.TaskType = dto.TaskType;
            task.Priority = dto.Priority;
            task.ProgressStatus = dto.ProgressStatus;
            task.Details.Description = dto.Description;
            task.Details.Deadline = dto.Deadline;

            await _taskRepository.Commit();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("Task/{encodedName}/Delete")]
        public async Task<IActionResult> Delete(string encodedName)
        {
            var task = await _taskRepository.GetByEncodedName(encodedName);
            if (task == null) return NotFound();

            var dto = _mapper.Map<TaskDto>(task);
            return View(dto);
        }

        [HttpPost]
        [Route("Task/{encodedName}/Delete")]
        public async Task<IActionResult> DeleteConfirmed(string encodedName)
        {
            var task = await _taskRepository.GetByEncodedName(encodedName);
            if (task == null) return NotFound();

            var user = _userContext.GetCurrentUser();
            if (task.CreatedById != user.Id && !user.IsInRole("Moderator"))
                return RedirectToAction("NoAccess", "Home");

            await _taskRepository.Delete(encodedName);
            this.SetNotification("success", "Task deleted successfully");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("Task/Subtask")]
        public async Task<IActionResult> CreateSubtask(SubtaskDto dto)
        {
            var task = await _taskRepository.GetByEncodedName(dto.TaskEncodedName!);
            if (task == null) return BadRequest("Parent task not found");

            var user = _userContext.GetCurrentUser();
            if (task.CreatedById != user.Id && !user.IsInRole("Moderator"))
                return BadRequest("Not authorized");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var subtask = _mapper.Map<Domain.Entities.Subtask>(dto);
            subtask.TaskId = task.Id;

            await _subtaskRepository.Create(subtask);
            return Ok();
        }

        [HttpGet]
        [Route("Task/{encodedName}/Subtask")]
        public async Task<IActionResult> GetSubtasks(string encodedName)
        {
            var subtasks = await _subtaskRepository.GetAllByEncodedName(encodedName);
            var dtos = _mapper.Map<IEnumerable<SubtaskDto>>(subtasks);
            return Ok(dtos);
        }

        [HttpDelete]
        [Route("Task/{encodedName}/Subtask/{subtaskId}")]
        public async Task<IActionResult> DeleteSubtask(string encodedName, int subtaskId)
        {
            var subtask = await _subtaskRepository.GetByIdAsync(subtaskId);
            if (subtask == null) return NotFound();

            await _subtaskRepository.Delete(subtask);
            return Ok();
        }
    }
}
