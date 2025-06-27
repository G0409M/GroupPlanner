using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;

namespace GroupPlanner.Application.Algorithms.Ant
{
    public interface IAntAlgorithmService
    {
        Task<AlgorithmRunResultDto> RunAsync(
            List<TaskDto> tasks,
            List<SubtaskDto> subtasks,
            List<DailyAvailabilityDto> availabilities,
            AntAlgorithmParameters parameters,
            Func<int, double, System.Threading.Tasks.Task>? reportProgress = null);
    }
}

