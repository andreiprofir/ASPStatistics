using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics.Services
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly IGamblingSupportService _gamblingSupportService;
        private readonly IDataService _dataService;

        public SynchronizationService(IGamblingSupportService gamblingSupportService, IDataService dataService)
        {
            _gamblingSupportService = gamblingSupportService;
            _dataService = dataService;
        }

        public async Task SynchronizeResultsAsync(bool rewriteAllExistingData = false)
        {
            int? numberOfPages = 1;
            var saveMethod = SaveMethod.Prepend;

            if (rewriteAllExistingData)
            {
                numberOfPages = null;
                saveMethod = SaveMethod.Rewrite;
            }

            List<ForecastJson> forecasts = await _gamblingSupportService.GetStatisticsAsync(numberOfPages);

            await _dataService.SaveResultsAsync(forecasts, saveMethod);
        }

        public async Task SynchronizeForecastsAsync()
        {
            List<ForecastJson> existingData = await _dataService.GetForecasts();
            List<ForecastJson> forecasts = await _gamblingSupportService.GetForecastsAsync(1);

            SetThreadNumbers(forecasts);

            foreach (ForecastJson forecast in forecasts)
            {
                ForecastJson existsForecast = existingData.FirstOrDefault(x => x.Id == forecast.Id);

                if (existsForecast != null)
                {
                    forecast.Coefficient = existsForecast.Coefficient;
                    forecast.GameResultType = GetGameResultType(existsForecast.GameResultType, forecast.GameResultType);
                    forecast.ShowAt = existsForecast.ShowAt;
                    forecast.BetValue = existsForecast.BetValue;
                    forecast.ThreadNumber = existsForecast.ThreadNumber;
                }
            }

            await _dataService.SaveForecastsAsync(forecasts);
        }

        private void SetThreadNumbers(List<ForecastJson> forecasts)
        {
            foreach (var group in forecasts.GroupBy(x => new {x.ShowAt.Year, x.ShowAt.Month, x.ShowAt.Day}))
            {
                int index = 0;

                foreach (ForecastJson forecast in group)
                {
                    forecast.ThreadNumber = index++;
                }
            }
        }

        private GameResultType GetGameResultType(GameResultType existsForecast, GameResultType forecast)
        {
            switch (existsForecast)
            {
                case GameResultType.Defeat:
                case GameResultType.RefundOrCancellation:
                case GameResultType.Win:
                    return existsForecast;
                default:
                    return forecast;
            }
        }
    }
}