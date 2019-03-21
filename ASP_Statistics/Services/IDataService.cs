using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Services
{
    public interface IDataService
    {
        List<ForecastJson> GetResults(FilterParameters filterParameters = null, bool reverse = false);

        List<ForecastJson> GetForecasts(bool reverse = true);

        List<StateJson> GetStates();

        StateJson GetLastState();

        SettingsJson GetSettings();

        ForecastJson GetForecastBy(long forecastId);

        ForecastJson GetLastCalculatedForecastByIndex(int index);

        Task SaveResultsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Append);

        Task SaveForecastsAsync(List<ForecastJson> forecasts, bool resetThreadNumbers = true, SaveMethod saveMethod = SaveMethod.Append);

        Task SaveSettingsAsync(SettingsJson settings);

        Task SaveStateAsync(params StateJson[] states);

        Task UpdateForecastsAsync(List<ForecastJson> forecasts);

        void InitializeThreadNumbers(List<ForecastJson> forecasts);
    }
}