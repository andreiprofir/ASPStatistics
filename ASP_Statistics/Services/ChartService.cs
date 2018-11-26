using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Services
{
    public class ChartService : IChartService
    {
        private readonly IAlgorithmService _algorithmService;
        private readonly IDataService _dataService;

        public ChartService(IAlgorithmService algorithmService, IDataService dataService)
        {
            _algorithmService = algorithmService;
            _dataService = dataService;
        }

        public async Task<Dictionary<ChartType, ChartViewModel>> GetGeneralChartsAsync(List<ForecastJson> forecasts, int threadNumbers)
        {
            var result = new Dictionary<ChartType, ChartViewModel>();

            List<WinLoseCountModel> chartData = await _algorithmService.GetWinLoseCountByThreadNumberAsync(forecasts, threadNumbers);

            return result;
        }

        private ChartViewModel GetWinLoseChart(List<ForecastJson> filteredForecasts, GameResultType? gameResultType)
        {
            var winLoseChart = new ChartViewModel
            {
                Label = "Win/Lose",
                Description = "Grafic care arata care erau treptele de trecere de la cistig la pierderi si invers",
                ChartData = new List<ChartData>()
            };

            GameResultType lastResultType =
                filteredForecasts.FirstOrDefault()?.GameResultType ?? GameResultType.Expectation;
            string lastLabel = filteredForecasts.FirstOrDefault()?.GameAt.ToString("d");

            int count = 0;
            int index = 1;

            foreach (ForecastJson forecast in filteredForecasts)
            {
                if (lastResultType == forecast.GameResultType)
                    count++;
                else
                {
                    if (gameResultType == null || lastResultType == gameResultType)
                    {
                        winLoseChart.ChartData.Add(new ChartData
                        {
                            Color = GetColor(lastResultType),
                            Label = lastLabel,
                            X = index++,
                            Y = count
                        });
                    }

                    lastLabel = forecast.GameAt.ToString("d");
                    lastResultType = forecast.GameResultType;
                    count = 1;
                }
            }

            return winLoseChart;
        }
    }
}