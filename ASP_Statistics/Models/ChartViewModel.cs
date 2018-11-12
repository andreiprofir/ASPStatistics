using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ASP_Statistics.Models
{
    public class ChartViewModel
    {
        public string Label { get; set; }

        public List<ChartData> ChartData { get; set; } = new List<ChartData>();
        public List<ChartData> SecondChartData { get; set; } = new List<ChartData>();
        public List<ChartData> ThirdChartData { get; set; } = new List<ChartData>();

        public string Description { get; set; }
    }

    //[DataContract]
    public class ChartData
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        //[DataMember(Name = "label")]
        [JsonProperty("label")]
        public string Label { get; set; }

        //[DataMember(Name = "y")]

        [JsonProperty("y")]
        public object Y { get; set; }

        [JsonProperty("x")]
        public object X { get; set; }
    }
}