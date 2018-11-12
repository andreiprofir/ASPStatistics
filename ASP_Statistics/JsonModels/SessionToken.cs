using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class SessionToken
    {
        [JsonProperty("csrfToken")]
        public string Token { get; set; }
    }
}