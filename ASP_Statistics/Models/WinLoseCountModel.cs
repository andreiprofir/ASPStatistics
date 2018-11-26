using System;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class WinLoseCountModel
    {
        public GameResultType GameResultType { get; set; }

        public int Count { get; set; }

        public int ThreadNumber { get; set; }

        public DateTimeOffset StartSeries { get; set; }
    }
}