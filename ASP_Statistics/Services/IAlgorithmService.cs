using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Services
{
    public interface IAlgorithmService
    {
        Task<StateJson> CalculateNextStateAsync(CurrentForecast currentForecast, PreviousForecast previousForecast);
    }
}