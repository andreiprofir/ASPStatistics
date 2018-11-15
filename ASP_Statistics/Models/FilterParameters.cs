using System;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class FilterParameters
    {
        public GameResultType? GameResultType { get; set; }

        public ForecastType? ForecastType { get; set; }

        public Month Month { get; set; } = Month.All;

        public int? Year { get; set; }

        public DateTimeOffset? LowerBound { get; set; }

        public DateTimeOffset? UpperBound { get; set; }
    }
}