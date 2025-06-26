using GroupPlanner.Application.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupPlanner.Application.AlgorithmResult
{
    public class AlgorithmRunResultDto
    {
        public List<ScheduleEntryDto> Schedule { get; set; } = new();
        public string ParametersJson { get; set; } = "";
        public string ScoreHistoryJson { get; set; } = "";
        public double BestScore { get; set; }
    }

}
