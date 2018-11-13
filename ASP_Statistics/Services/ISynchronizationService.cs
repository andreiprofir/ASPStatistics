using System.Threading.Tasks;

namespace ASP_Statistics.Services
{
    public interface ISynchronizationService
    {
        Task SynchronizeResultsAsync(bool rewriteAllExistingData = false);

        Task SynchronizeForecastsAsync();
    }
}