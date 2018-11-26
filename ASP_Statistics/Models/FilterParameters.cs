using System;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class FilterParameters
    {
        public GameResultType? GameResultType { get; set; }

        public ForecastType ForecastType { get; set; } = ForecastType.Paid;

        public DateTimeOffset? LowerBound { get; set; }

        public DateTimeOffset? UpperBound { get; set; }

        public GameResultType? ExcludedGameResultType { get; set; }
    }
}