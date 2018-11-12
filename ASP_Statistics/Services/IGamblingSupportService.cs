using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics.Services
{
    public interface IGamblingSupportService
    {
        Task<List<ForecastJson>> GetForecastsAsync(int? numberOfPages = null);
        
        Task<List<ForecastJson>> GetStatisticsAsync(int? numberOfPages = null);
    }
}