using System;
using System.Collections.Generic;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Classes
{
    public interface IDataOldService
    {
        List<ForecastJson> Forecasts { get; set; }

        List<ForecastJson> Filter(RequestViewModel model);

        Dictionary<ChartType, ChartViewModel> GetChartData(List<ForecastJson> filteredForecasts, GameResultType? gameResultType, bool excludeCanceled = false);

        Dictionary<ChartType, ChartViewModel> GetStrategyChartData(List<ForecastJson> filteredForecasts, decimal firstBetValue, decimal initialBank);
        
        decimal CalculateNextBetValue(decimal bank, decimal initialBet = 0, decimal step = 0.1M, DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);

        decimal CalculateMaxBankValue(decimal bet, DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);

        decimal CalculateMaxBankValuePursuit(decimal bet);
    }
}