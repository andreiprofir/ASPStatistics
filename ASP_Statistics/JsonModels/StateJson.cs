using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class StateJson
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("forecast_id")]
        public long? ForecastId { get; set; }
    }
}