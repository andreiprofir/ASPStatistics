using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics.Services
{
    public interface IDataService
    {
        List<ForecastJson> GetResults();

        List<ForecastJson> GetForecasts();

        List<StateJson> GetStates();

        StateJson GetStateByForecastId(long forecastId);

        StateJson GetLastState();

        ForecastJson GetLastCalculatedForecastByIndex(int index);

        Task SaveResultsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Prepend);

        Task SaveForecastsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Prepend);
    }
}