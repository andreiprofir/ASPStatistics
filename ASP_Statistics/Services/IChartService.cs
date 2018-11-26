using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Services
{
    public interface IChartService
    {
        Task<Dictionary<ChartType, ChartViewModel>> GetGeneralChartsAsync(List<ForecastJson> forecasts,
            int threadNumbers);
    }
}