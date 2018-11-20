using System;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class SettingsViewModel
    {
        public decimal InitialBank { get; set; }
        
        public decimal InitialBetValue { get; set; }

        public int BetValueRoundDecimals { get; set; }

        public bool AllowIncreaseBetValue { get; set; }

        public decimal BetValueIncreaseStep { get; set; }

        public double CoefficientBankReserve { get; set; }

        public int ThreadNumbers { get; set; }

        public CalculationMethod CalculationMethod { get; set; }

        public DateTimeOffset? LowerBound { get; set; }

        public DateTimeOffset? UpperBound { get; set; }
    }
}