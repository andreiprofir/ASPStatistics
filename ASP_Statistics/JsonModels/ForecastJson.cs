using System;
using ASP_Statistics.Enums;
using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class ForecastJson
    {
        [JsonProperty("forecast_id")]
        public long Id { get; set; }

        [JsonProperty("forecast_type")]
        public ForecastType ForecastType { get; set; }

        [JsonProperty("forecast_coefficient")]
        public double Coefficient { get; set; }

        [JsonProperty("forecast_bet")]
        public string Bet { get; set; }

        [JsonProperty("forecast_result")]
        public GameResultType GameResultType { get; set; }

        [JsonProperty("game_match_at")]
        public DateTimeOffset GameAt { get; set; }

        [JsonProperty("forecast_show_at")]
        public DateTimeOffset ShowAt { get; set; }

        [JsonProperty("tournament_name")]
        public string TournamentName { get; set; }

        [JsonProperty("sport_id")]
        public Sport Sport { get; set; }

        [JsonProperty("country_name")]
        public string CountryName { get; set; }
        
        [JsonProperty("game")]
        public GameJson Game { get; set; }

        [JsonProperty("bet_value")]
        public decimal BetValue { get; set; }

        [JsonProperty("number_of_thread")]
        public int ThreadNumber { get; set; }
    }
}