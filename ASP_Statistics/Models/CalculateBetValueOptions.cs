using System;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class CalculateBetValueOptions
    {
        public decimal Bank { get; set; }

        public decimal InitialBet { get; set; } = 0;

        public decimal IncreaseBetStep { get; set; } = 0.01M;

        public double CoefficientBankReserve { get; set; } = 0;
        
        public int ThreadNumbers { get; set; } = 4;

        public DateTimeOffset? LowerBound { get; set; } = null;

        public DateTimeOffset? UpperBound { get; set; } = null;

        public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Max;
    }
}