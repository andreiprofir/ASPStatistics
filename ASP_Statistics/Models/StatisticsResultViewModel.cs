using System.Collections.Generic;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics.Models
{
    public class StatisticsResultViewModel
    {
        public List<ForecastJson> ForecastResults { get; set; }

        public Dictionary<ChartType, ChartViewModel> GeneralChartsData { get; set; }
    }
}