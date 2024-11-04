using AutoMapper;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Application.Task.Commands.EditTask;
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
        public TaskMappingProfile( IUserContext userContext)
        {
            var user = userContext.GetCurrentUser();
            CreateMap<TaskDto, Domain.Entities.Task>()
                .ForMember(e => e.Details, opt => opt.MapFrom(src => new TaskDetails()
                {
                    Description= src.Description,
                    Deadline= src.Deadline,
                }));
            CreateMap<Domain.Entities.Task, TaskDto>()
                .ForMember(dto => dto.IsEditable, opt => opt.MapFrom(src => user!= null 
                                    && (src.CreatedById == user.Id || user.IsInRole("Moderator"))))
                .ForMember(dto => dto.Description, opt => opt.MapFrom(src => src.Details.Description))
                .ForMember(dto => dto.Deadline, opt => opt.MapFrom(src => src.Details.Deadline));
            CreateMap<TaskDto, EditTaskCommand>();
            CreateMap<SubtaskDto, Domain.Entities.Subtask>()
                .ReverseMap();
            CreateMap<GroupPlanner.Domain.Entities.DailyAvailability, DailyAvailabilityDto>().ReverseMap();

        }
    }
}
