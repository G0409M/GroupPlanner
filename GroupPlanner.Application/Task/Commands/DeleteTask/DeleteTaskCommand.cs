using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Commands.DelateTask
{
    public class DeleteTaskCommand : IRequest
    {
        public string EncodedName { get; set; }

        public DeleteTaskCommand(string encodedName)
        {
            EncodedName = encodedName;
        }
    }
}
