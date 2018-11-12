using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ASP_Statistics.JsonModels
{
    public class PageJson
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("per_page")]
        public int PerPage { get; set; }

        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }

        [JsonProperty("last_page")]
        public int LastPage { get; set; }

        [JsonProperty("next_page_url")]
        public Uri NextPageUri { get; set; }

        [JsonProperty("prev_page_url")]
        public Uri PrevPageUri { get; set; }

        [JsonProperty("data")]
        public List<ForecastJson> Forecasts { get; set; }
    }
}