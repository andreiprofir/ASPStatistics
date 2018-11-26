using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;
using ASP_Statistics.Utils;

namespace ASP_Statistics.Services
{
    public class ChartService : IChartService
    {
        private readonly IAlgorithmService _algorithmService;

        public ChartService(IAlgorithmService algorithmService)
        {
            _algorithmService = algorithmService;
        }

        public async Task<Dictionary<ChartType, ChartViewModel>> GetGeneralChartsAsync(List<ForecastJson> forecasts, int threadNumbers)
        {
            var result = new Dictionary<ChartType, ChartViewModel>();

            List<WinLoseCountModel> chartData = await _algorithmService.GetWinLoseCountByThreadNumberAsync(forecasts, threadNumbers);

            result[ChartType.WinLose] = GetWinLoseChart(chartData, threadNumbers);

            return result;
        }

        private ChartViewModel GetWinLoseChart(List<WinLoseCountModel> chartData, int threadNumbers)
        {
            var winLoseChart = new ChartViewModel
            {
                Label = "Win/Lose",
                Description = "Grafic care arata care erau treptele de trecere de la cistig la pierderi si invers",
                ChartData = new List<ChartData>()
            };

            var index = 0;

            for (var i = 0; i < threadNumbers; i++)
            {
                foreach (WinLoseCountModel model in chartData.Where(x => x.ThreadNumber == i))
                {
                    winLoseChart.ChartData.Add(new ChartData
                    {
                        Label = model.StartSeries.ToString("d"),
                        Color = HtmlViewHelper.GetColor(model.GameResultType),
                        X = index++,
                        Y = model.Count
                    });
                }

                index += 3;

                winLoseChart.ChartData.Add(new ChartData
                {
                    Label = DateTimeOffset.Now.ToString("d"),
                    Color = HtmlViewHelper.GetColor(GameResultType.Expectation),
                    X = index++,
                    Y = 0
                });
            }
            
            return winLoseChart;
        }
    }
}