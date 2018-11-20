using System.Collections;
using System.Collections.Generic;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class SettingsAndInfoViewModel
    {
        public double ChainCoefficient { get; set; } = 2.1;

        public decimal ChainBetValue { get; set; } = 1M;

        public decimal BankValue { get; set; }

        public decimal BetValue { get; set; }

        public List<decimal> Bets { get; set; }

        public List<decimal> LoseValues { get; set; }

        public int ThreadNumbers { get; set; } = 4;

        public Dictionary<RepresentsValueType, decimal> BetValueLimits { get; set; }

        public Dictionary<RepresentsValueType, decimal> BankValueLimits { get; set; }

        public SettingsViewModel Settings { get; set; }
    }
}