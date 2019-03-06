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
        public List<decimal> Bets { get; set; }

        [JsonProperty("lose_values")]
        public List<decimal> LoseValues { get; set; }

        [JsonProperty("lose_numbers")]
        public List<int> LoseNumbers { get; set; }

        [JsonProperty("forecast_id")]
        public long? ForecastId { get; set; }

        public static StateJson Build(int threadNumber = 4)
        {
            var state = new StateJson
            {
                Bets = Enumerable.Repeat(0M, threadNumber).ToList(),
                LoseValues = Enumerable.Repeat(0M, threadNumber).ToList(),
                LoseNumbers = Enumerable.Repeat(0, threadNumber).ToList()
            };

            return state;
        }
    }
}