using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask.Queries
{
    public class GetSubtasksQuery:IRequest<IEnumerable<SubtaskDto>>
    {
        public string EncodedName { get; set; }

    }
}
