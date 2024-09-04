using GroupPlanner.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.Task.Queries.GetTaskByEncodedName
{
    public class GetTaskByEncodedNameQuery:IRequest<TaskDto>
    {
        public string EncodedName { get; set; }

        public GetTaskByEncodedNameQuery(string encodedName)
        {
            EncodedName = encodedName;
        }
    }
}
