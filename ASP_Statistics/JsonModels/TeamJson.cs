using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class TeamJson
    {
        [JsonProperty("team_name")]
        public string Name { get; set; }
    }
}