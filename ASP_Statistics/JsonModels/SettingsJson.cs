using System;
using ASP_Statistics.Enums;
using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class SettingsJson
    {
        [JsonProperty("initial_bank")]
        public decimal InitialBank { get; set; }
        
        [JsonProperty("initial_bet_value")]
        public decimal InitialBetValue { get; set; }

        [JsonProperty("bet_value_round_decimals")]
        public int BetValueRoundDecimals { get; set; } = 2;

        [JsonProperty("allow_increase_bet_value")]
        public bool AllowIncreaseBetValue { get; set; }

        [JsonProperty("bet_value_increase_step")]
        public decimal BetValueIncreaseStep { get; set; } = 0.01M;

        [JsonProperty("coefficient_bank_reserve")]
        public double CoefficientBankReserve { get; set; }

        [JsonProperty("thread_numbers")]
        public int ThreadNumbers { get; set; } = 4;

        [JsonProperty("calculation_method")]
        public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Max;

        [JsonProperty("default_lower_bound")]
        public DateTimeOffset? LowerBound { get; set; } = new DateTime(2018, 4, 1);

        [JsonProperty("default_upper_bound")]
        public DateTimeOffset? UpperBound { get; set; }
    }
}