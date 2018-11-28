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

        public async Task<Dictionary<ChartType, ChartViewModel>> GetWinLoseChartsAsync(List<ForecastJson> forecasts, int threadNumbers)
        {
            var result = new Dictionary<ChartType, ChartViewModel>();

            List<WinLoseCountModel> chartData = await _algorithmService.GetWinLoseCountByThreadNumberAsync(forecasts, threadNumbers);
            
            result[ChartType.WinLose] = GetWinLoseChart(chartData, threadNumbers);
            result[ChartType.WinLoseCount] = GetWinLoseCountsChart(forecasts);
            result[ChartType.WinLoseSeriesCount] = GetWinLoseSeriesCountChart(chartData, threadNumbers);

            return result;
        }

        public async Task<ChartViewModel> GetBankValuesChartAsync(List<ForecastJson> forecasts, decimal initialBank, 
            decimal initialBet, int threadNumbers, bool allowIncreaseBets)
        {
            return await Task.Run(async () =>
            {
                List<StateJson> states =
                    await _algorithmService.CalculateStatesAsync(forecasts, initialBank, initialBet, threadNumbers, allowIncreaseBets);

                return GetBankValueChart(states);
            });
        }

        private ChartViewModel GetBankValueChart(List<StateJson> states)
        {
            var banValueChart = new ChartViewModel
            {
                Label = "Bank Values",
                Description = "Grafic care arata dinamica bancului disponibil",
                ChartData = new List<ChartData>()
            };

            var index = 0;
            
            foreach (var state in states)
            {
                banValueChart.ChartData.Add(new ChartData
                {
                    Label = DateTimeOffset.FromUnixTimeMilliseconds(state.Id).ToString("d"),
                    X = index++,
                    Y = state.Bank
                });
            }
            
            
            return banValueChart;
        }

        private ChartViewModel GetWinLoseSeriesCountChart(List<WinLoseCountModel> chartData, int threadNumbers)
        {
            var winLoseChart = new ChartViewModel
            {
                Label = "Win/Lose(Series Count)",
                Description = "Grafic care arata numarul de serii de fiecare tip",
                ChartData = new List<ChartData>()
            };

            var index = 0;

            for (var i = 0; i < threadNumbers; i++)
            {
                var query = chartData
                    .Where(x => x.ThreadNumber == i)
                    .GroupBy(x => new {x.GameResultType, x.Count})
                    .OrderByDescending(x => x.Count())
                    .ToList();

                foreach (var group in query)
                {
                    winLoseChart.ChartData.Add(new ChartData
                    {
                        Label = group.Count().ToString(),
                        Color = HtmlViewHelper.GetColor(group.Key.GameResultType),
                        X = index++,
                        Y = group.Key.Count
                    });
                }

                index += 3;

                winLoseChart.ChartData.Add(new ChartData
                {
                    Label = "",
                    Color = HtmlViewHelper.GetColor(GameResultType.Expectation),
                    X = index++,
                    Y = 0
                });
            }
            
            return winLoseChart;
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

        private ChartViewModel GetWinLoseCountsChart(List<ForecastJson> forecasts)
        {
            var winLoseCountChart = new ChartViewModel
            {
                Label = "Win/Lose(Count)",
                Description = "Grafic care arata numarul de pierderi/cistiguri pe fiecare fir aparte",
                ChartData = new List<ChartData>()
            };

            var index = 0;

            foreach (var group in forecasts.GroupBy(x => new {x.GameAt.Year, x.GameAt.Month}).OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month))
            {
                foreach (var forecastsGroup in group.GroupBy(x => x.ThreadNumber).OrderBy(x => x.Key))
                {
                    foreach (var item in forecastsGroup.GroupBy(x => x.GameResultType))
                    {
                        winLoseCountChart.ChartData.Add(new ChartData
                        {
                            Label = $"[{group.Key.Year}]{((Month)group.Key.Month).ToString()}",
                            Color = HtmlViewHelper.GetColor(item.Key),
                            X = index++,
                            Y = item.Count()
                        });
                    }

                    index += 1;

                    winLoseCountChart.ChartData.Add(new ChartData
                    {
                        Label = $"[{group.Key.Year}]{((Month)group.Key.Month).ToString()}",
                        Color = HtmlViewHelper.GetColor(GameResultType.Expectation),
                        X = index++,
                        Y = 0
                    });
                }

                index += 1;

                foreach (var forecastGroup in group.GroupBy(x => x.GameResultType))
                {
                    winLoseCountChart.ChartData.Add(new ChartData
                    {
                        Label = $"[{group.Key.Year}]{((Month)group.Key.Month).ToString()}",
                        Color = HtmlViewHelper.GetColor(forecastGroup.Key),
                        X = index++,
                        Y = forecastGroup.Count()
                    });
                }

                index += 2;
            }

            return winLoseCountChart;
        }
    }
}