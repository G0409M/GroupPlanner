



using GroupPlanner.Application.Task;

namespace GroupPlanner.Application.Services
{
    public interface ITaskService
    {
        System.Threading.Tasks.Task Create(TaskDto task);
    }
}