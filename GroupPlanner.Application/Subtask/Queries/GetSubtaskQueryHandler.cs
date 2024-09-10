using AutoMapper;
using GroupPlanner.Domain.Interfaces;
using MediatR;

namespace GroupPlanner.Application.Subtask.Queries
{
    public class GetSubtaskQueryHandler : IRequestHandler<GetSubtasksQuery, IEnumerable<SubtaskDto>>

    {
        private readonly ISubtaskRepository _subtaskRepository;
        private readonly IMapper _mapper;

        public GetSubtaskQueryHandler(ISubtaskRepository subtaskRepository, IMapper mapper)
        {
            _subtaskRepository = subtaskRepository;
            _mapper = mapper;
        }
        public async Task<IEnumerable<SubtaskDto>> Handle(GetSubtasksQuery request, CancellationToken cancellationToken)
        {
            var result = await _subtaskRepository.GetAllByEncodedName(request.EncodedName);
            var dtos = _mapper.Map<IEnumerable<SubtaskDto>>(result);
            return dtos;
        }
    }
}
