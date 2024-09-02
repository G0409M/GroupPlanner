using AutoMapper;
using GroupPlanner.Application.Dto.Task;
using GroupPlanner.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        public TaskService(ITaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }
        public async System.Threading.Tasks.Task Create(TaskDto taskDto)
        {
           var task= _mapper.Map<Domain.Entities.Task>(taskDto);
            task.EncodeName();
            task.Details.CreatedAt = DateTime.Now;
            await _taskRepository.Create(task);
        }

        public async Task<IEnumerable<TaskDto>> GetAll()
        {
            var tasks =  await _taskRepository.GetAll();
            var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);
            return dtos;
        }
    }
}
