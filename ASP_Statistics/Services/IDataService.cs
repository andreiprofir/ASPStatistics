using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics.Services
{
    public interface IDataService
    {
        Task<List<ForecastJson>> GetResultsAsync();

        Task SaveResultsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Prepend);

        Task SaveForecastsAsync(List<ForecastJson> forecasts, SaveMethod saveMethod = SaveMethod.Prepend);

        Task<List<ForecastJson>> GetForecastsAsync();

        Task<List<StateJson>> GetAlgorithmStatesAsync();
    }
}