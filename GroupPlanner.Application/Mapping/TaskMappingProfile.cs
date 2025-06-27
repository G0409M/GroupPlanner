using AutoMapper;
using GroupPlanner.Application.AlgorithmResult;
using GroupPlanner.Application.Algorithms;
using GroupPlanner.Application.ApplicationUser;
using GroupPlanner.Application.DailyAvailability;
using GroupPlanner.Application.Subtask;
using GroupPlanner.Application.Task;
using GroupPlanner.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Mapping
{
    public class TaskMappingProfile : Profile
    {
        public TaskMappingProfile(IUserContext userContext)
        {
            var user = userContext.GetCurrentUser();

            // DTO -> Entity
            CreateMap<TaskDto, Domain.Entities.Task>()
                .ForMember(e => e.Details, opt => opt.MapFrom(src => new TaskDetails
                {
                    Description = src.Description,
                    Deadline = src.Deadline,
                }))
                .ForMember(dest => dest.Subtasks, opt => opt.MapFrom(src => src.Subtasks)); // 🆕

            // Entity -> DTO
            CreateMap<Domain.Entities.Task, TaskDto>()
                .ForMember(dto => dto.IsEditable, opt => opt.MapFrom(src =>
                    user != null && (src.CreatedById == user.Id || user.IsInRole("Moderator"))))
                .ForMember(dto => dto.Description, opt => opt.MapFrom(src => src.Details.Description))
                .ForMember(dto => dto.Deadline, opt => opt.MapFrom(src => src.Details.Deadline))
                .ForMember(dto => dto.Subtasks, opt => opt.MapFrom(src => src.Subtasks)); // 🆕

            // Subtask
            CreateMap<SubtaskDto, Domain.Entities.Subtask>()
                .ForMember(e => e.Order, opt => opt.MapFrom(src => src.Order))
                .ReverseMap();


            // Availability
            CreateMap<GroupPlanner.Domain.Entities.DailyAvailability, DailyAvailabilityDto>().ReverseMap();

            // Algorithm results
            CreateMap<Domain.Entities.AlgorithmResult, AlgorithmResultDto>()
                .ForMember(dto => dto.IsEditable, opt => opt.MapFrom(src =>
                    user != null && (src.CreatedById == user.Id || user.IsInRole("Moderator"))));

            CreateMap<AlgorithmResultDto, Domain.Entities.AlgorithmResult>();

            CreateMap<GroupPlanner.Application.AlgorithmResult.AlgorithmResultDto, GroupPlanner.Domain.Entities.AlgorithmResult>()
                .ForMember(dest => dest.Algorithm, opt => opt.MapFrom(src => src.Algorithm));

            


        }
    }

}
