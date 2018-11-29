using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class StateJson
    {
        [JsonProperty("id")]
        public long Id { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        
        [JsonProperty("bank")]
        public decimal Bank { get; set; } 

        [JsonProperty("initial_bet")]
        public decimal InitialBet { get; set; }

        [JsonProperty("bets")]
        public List<decimal> Bets { get; set; } = Enumerable.Repeat<decimal>(0, 4).ToList();

        [JsonProperty("lose_values")]
        public List<decimal> LoseValues { get; set; } = Enumerable.Repeat<decimal>(0, 4).ToList();

        [JsonProperty("forecast_id")]
        public long? ForecastId { get; set; }
    }
}