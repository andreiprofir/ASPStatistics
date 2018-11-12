using System;
using ASP_Statistics.Classes;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class RequestViewModel
    {
        public GameResultType? GameResultType { get; set; }

        public ForecastType? ForecastType { get; set; }

        public Month? Month { get; set; }

        public int? Year { get; set; }

        public DateTimeOffset? LowerBound { get; set; }

        private DateTimeOffset? _upperBound;
        public DateTimeOffset? UpperBound
        {
            get => _upperBound;
            set
            {
                if (!value.HasValue)
                    _upperBound = null;
                else
                    _upperBound = new DateTimeOffset(value.Value.Date.Year, value.Value.Month, value.Value.Day, 
                        23, 59, 59, value.Value.Offset);
            }
        }
    }
}