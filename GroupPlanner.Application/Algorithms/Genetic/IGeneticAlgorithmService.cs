﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupPlanner.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Application.AlgorithmResult;


namespace GroupPlanner.Application.Algorithms.Genetic
{
    public interface IGeneticAlgorithmService
    {
        Task<AlgorithmRunResultDto> RunAsync(List<TaskDto> tasks, List<SubtaskDto> subtasks, List<DailyAvailabilityDto> availabilities, GeneticAlgorithmParameters parameters,
               Func<int, double, System.Threading.Tasks.Task>? reportProgress = null);
    }


}
