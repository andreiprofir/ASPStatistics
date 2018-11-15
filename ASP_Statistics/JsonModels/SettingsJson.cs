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

        [JsonProperty("bet_value_increase_step")]
        public decimal BetValueIncreaseStep { get; set; } = 0.01M;

        [JsonProperty("coefficient_bank_reserve")]
        public double CoefficientBankReserve { get; set; }
    }
}