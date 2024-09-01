using AutoMapper;
using GroupPlanner.Application.Dto.Task;
using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Mapping
{
    public class TaskMappingProfile: Profile
    {
        public TaskMappingProfile()
        {
            CreateMap<TaskDto, Domain.Entities.Task>()
                .ForMember(e => e.Details, opt => opt.MapFrom(src => new TaskDetails()
                {
                    Description= src.Description,
                    Deadline= src.Deadline,
                }));

        }
    }
}
