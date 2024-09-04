using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Commands.CreateTask
{
    public class CreateTaskCommand : TaskDto, IRequest
    {
    }
}
