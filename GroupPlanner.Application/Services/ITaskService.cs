



using GroupPlanner.Application.Dto.Task;

namespace GroupPlanner.Application.Services
{
    public interface ITaskService
    {
        System.Threading.Tasks.Task Create(TaskDto task);
    }
}