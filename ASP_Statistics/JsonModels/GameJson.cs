using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class GameJson
    {
        [JsonProperty("home")]
        public TeamJson HomeTeam { get; set; }

        [JsonProperty("guest")]
        public TeamJson GuestTeam { get; set; }

        public override string ToString()
        {
            return $"{HomeTeam.Name} - {GuestTeam.Name}";
        }
    }
}