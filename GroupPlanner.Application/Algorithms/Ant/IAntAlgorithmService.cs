using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Algorithms.Ant
{
    public interface IAntAlgorithmService
    {
        Task<AlgorithmRunResultDto> RunAsync(List<TaskDto> tasks, List<SubtaskDto> subtasks, List<DailyAvailabilityDto> availabilities, AntAlgorithmParameters parameters);
    }
}
