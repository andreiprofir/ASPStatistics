using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class StateJson
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("forecast_id")]
        public long ForecastId { get; set; }

        [JsonProperty("bank")]
        public decimal Bank { get; set; } 

        [JsonProperty("initial_bet")]
        public decimal InitialBet { get; set; }

        [JsonProperty("bets")]
        public List<decimal> Bets { get; set; } = new List<decimal>(Enumerable.Repeat<decimal>(0, 4));

        [JsonProperty("lose_values")]
        public List<decimal> LoseValues { get; set; } = new List<decimal>(Enumerable.Repeat<decimal>(0, 4));
    }
}