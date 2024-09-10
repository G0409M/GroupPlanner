using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Subtask.Commands
{
    public class CreateSubtaskCommand:SubtaskDto, IRequest
    {
        public string TaskEncodedName { get; set; } = default!;

    }
}
