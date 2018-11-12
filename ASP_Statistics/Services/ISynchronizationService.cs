using System.Threading.Tasks;

namespace ASP_Statistics.Services
{
    public interface ISynchronizationService
    {
        Task SynchronizeForecastResultsAsync(bool rewriteAllExistingData = false);

        Task SynchronizeForecastsAsync();
    }
}