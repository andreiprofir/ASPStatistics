using System;
using System.ComponentModel.DataAnnotations;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class StatisticsViewModel
    {
        public GameResultType? GameResultType { get; set; }

        public ForecastType ForecastType { get; set; } = ForecastType.Paid;

        public int ThreadNumbers { get; set; }

        public bool ExcludeRefundResults { get; set; } = true;

        public bool AllowIncreaseBets { get; set; } = true;

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTimeOffset? LowerBound { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTimeOffset? UpperBound { get; set; }
    }
}