using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;
using ASP_Statistics.Utils;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace ASP_Statistics.Classes
{
    public class DataOldService : IDataOldService
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private static List<ForecastJson> _forecasts;
        private readonly string _forecastsFile = "forecasts.json";

        public List<ForecastJson> Forecasts
        {
            get => _forecasts;
            set => _forecasts = value;
        }

        public DataOldService(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;

            if (_forecasts == null)
                _forecasts = GetForecasts();
        }

        public List<ForecastJson> Filter(RequestViewModel model)
        {
            if (model == null)
                return _forecasts;

            IEnumerable<ForecastJson> query = _forecasts;

            if (model.ForecastType != null)
                query = query.Where(x => x.ForecastType == model.ForecastType);

            if (model.GameResultType != null)
                query = query.Where(x => x.GameResultType == model.GameResultType);

            if (model.Month != null)
                query = query.Where(x => x.GameAt.Month == (int) model.Month);

            if (model.Year != null)
                query = query.Where(x => x.GameAt.Year == model.Year);

            return new List<ForecastJson>(query.OrderByDescending(x => x.GameAt));
        }

        public Dictionary<ChartType, ChartViewModel> GetChartData(List<ForecastJson> filteredForecasts,
            GameResultType? gameResultType, bool excludeCanceled = false)
        {
            if (filteredForecasts == null) return null;

            filteredForecasts = filteredForecasts.AsEnumerable().Reverse()
                //.ThenBy(x => x.Id)
                .ToList();

            var model = new Dictionary<ChartType, ChartViewModel>();

            ChartViewModel winLoseChart = GetWinLoseChart(filteredForecasts, gameResultType);
            ChartViewModel winLoseChart4Line = GetWinLose4ChartLine(filteredForecasts, gameResultType, excludeCanceled);
            ChartViewModel winLoseChart4Pursuit =
                GetWinLose4ChartPursuit(filteredForecasts, gameResultType, excludeCanceled);
            ChartViewModel winLoseCountsChart = GetWinLoseCountsChart(winLoseChart);
            ChartViewModel winLoseCounts4Chart = GetWinLoseCounts4Chart(winLoseChart4Line);
            ChartViewModel winLosePercentage = GetWinLoseRefundCount(filteredForecasts);


            model.Add(ChartType.WinLose, winLoseChart);
            model.Add(ChartType.WinLose4Line, winLoseChart4Line);
            model.Add(ChartType.WinLose4Pursuit, winLoseChart4Pursuit);
            model.Add(ChartType.WinLoseCount, winLoseCountsChart);
            model.Add(ChartType.WinLoseCount4, winLoseCounts4Chart);
            model.Add(ChartType.WinLoseRefundCount, winLosePercentage);

            return model;
        }

        private ChartViewModel GetWinLoseRefundCount(List<ForecastJson> filteredForecasts)
        {
            ChartViewModel result = new ChartViewModel
            {
                Label = "Win/Lose/Refund Count",
                Description = "Numarul de cistiguri, pierderi si intoarceri"
            };

            int index = 1;

            result.ChartData.Add(new ChartData
            {
                Color = GetColor(GameResultType.Win),
                X = index,
                Y = filteredForecasts.Count(x => x.GameResultType == GameResultType.Win),
                Label = "Cite Unu"
            });

            result.SecondChartData.Add(new ChartData
            {
                Color = GetColor(GameResultType.Defeat),
                X = index,
                Y = filteredForecasts.Count(x => x.GameResultType == GameResultType.Defeat),
                Label = "Cite Unu"
            });

            result.ThirdChartData.Add(new ChartData
            {
                Color = GetColor(GameResultType.RefundOrCancellation),
                X = index,
                Y = filteredForecasts.Count(x => x.GameResultType == GameResultType.RefundOrCancellation),
                Label = "Cite Unu"
            });

            int winCount = 0;
            int loseCount = 0;
            int refundCount = 0;

            for (int i = 0; i < 2; i++)
            {
                index += 1;

                winCount = 0;
                loseCount = 0;
                refundCount = 0;

                for (var j = i; j < filteredForecasts.Count; j += 2)
                {
                    if (filteredForecasts[j].GameResultType == GameResultType.Win) winCount++;
                    if (filteredForecasts[j].GameResultType == GameResultType.Defeat) loseCount++;
                    if (filteredForecasts[j].GameResultType == GameResultType.RefundOrCancellation) refundCount++;
                }

                result.ChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.Win),
                    X = index,
                    Y = winCount,
                    Label = $"Cite Doua({i + 1} din 2)"
                });

                result.SecondChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.Defeat),
                    X = index,
                    Y = loseCount,
                    Label = $"Cite Doua({i + 1} din 2)"
                });

                result.ThirdChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.RefundOrCancellation),
                    X = index,
                    Y = refundCount,
                    Label = $"Cite Doua({i + 1} din 2)"
                });
            }

            for (int i = 0; i < 3; i++)
            {
                index += 1;

                winCount = 0;
                loseCount = 0;
                refundCount = 0;

                for (var j = i; j < filteredForecasts.Count; j += 3)
                {
                    if (filteredForecasts[j].GameResultType == GameResultType.Win) winCount++;
                    if (filteredForecasts[j].GameResultType == GameResultType.Defeat) loseCount++;
                    if (filteredForecasts[j].GameResultType == GameResultType.RefundOrCancellation) refundCount++;
                }

                result.ChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.Win),
                    X = index,
                    Y = winCount,
                    Label = $"Cite Trei({i + 1} din 3)"
                });

                result.SecondChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.Defeat),
                    X = index,
                    Y = loseCount,
                    Label = $"Cite Trei({i + 1} din 3)"
                });

                result.ThirdChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.RefundOrCancellation),
                    X = index,
                    Y = refundCount,
                    Label = $"Cite Trei({i + 1} din 3)"
                });
            }


            for (int i = 0; i < 4; i++)
            {
                index += 1;

                winCount = 0;
                loseCount = 0;
                refundCount = 0;

                for (var j = i; j < filteredForecasts.Count; j += 4)
                {
                    if (filteredForecasts[j].GameResultType == GameResultType.Win) winCount++;
                    if (filteredForecasts[j].GameResultType == GameResultType.Defeat) loseCount++;
                    if (filteredForecasts[j].GameResultType == GameResultType.RefundOrCancellation) refundCount++;
                }

                result.ChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.Win),
                    X = index,
                    Y = winCount,
                    Label = $"Cite Patru({i + 1} din 4)"
                });

                result.SecondChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.Defeat),
                    X = index,
                    Y = loseCount,
                    Label = $"Cite Patru({i + 1} din 4)"
                });

                result.ThirdChartData.Add(new ChartData
                {
                    Color = GetColor(GameResultType.RefundOrCancellation),
                    X = index,
                    Y = refundCount,
                    Label = $"Cite Patru({i + 1} din 4)"
                });
            }

            return result;
        }

        public Dictionary<ChartType, ChartViewModel> GetStrategyChartData(List<ForecastJson> filteredForecasts,
            decimal firstBetValue, decimal initialBank)
        {
            if (filteredForecasts == null) return null;

            filteredForecasts = filteredForecasts
                .AsEnumerable().Reverse()
                //.ThenBy(x => x.Id)
                .ToList();

            var model = new Dictionary<ChartType, ChartViewModel>();
            List<ChartData> bets;
            List<ChartData> profits;

            ChartViewModel loseBankChart = new ChartViewModel
            {
                Label = "Lose Bank Count",
                Description = "Numarul de luzuri a bankului"
            };

            ChartViewModel bankValueChart = new ChartViewModel
            {
                Label = "Bank Value(MIN/MAX)",
                Description = "Valoarea minimala si maximala a bancului total"
            };

            ChartViewModel betLimitsChart = new ChartViewModel
            {
                Label = "BET Value(MIN/AVG/MAX)",
                Description = "Valoarea minimala, medie si maximala a unei stavshi"
            };

            ChartViewModel bankMaxChart = new ChartViewModel
            {
                Label = "BANK MAX",
                Description = "Valoarea maximala a bankului care va fi necesara in caz de serie de pierderi"
            };

            ChartViewModel profitMinMaxChart = new ChartViewModel
            {
                Label = "Profit(MIN/MAX)",
                Description = "Valoarea minimala si maximala a profitului curat"
            };

            int index = 1;
            Dictionary<StatType, decimal> stats;

            ChartViewModel flatStrategyBankChart =
                GetFlatStrategyBankChart(filteredForecasts, 500, initialBank, out bets, out profits, out stats);
            model.Add(ChartType.FlatBankValue, flatStrategyBankChart);
            model.Add(ChartType.FlatBankValueBet,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.FlatBankValueClean,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.FlatBankValue, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.FlatBankValue, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.FlatBankValue, index);
            loseBankChart.ChartData.Add(new ChartData
                {Label = ChartType.FlatBankValue.ToString(), Y = stats[StatType.LoseBankCount], X = index});
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.FlatBankValue.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;


            //ChartViewModel softFlatDagonStrategyBankChart = GetSoftFlatDagonStrategyBankChart(filteredForecasts, firstBetValue, initialBank, out bets, out profits, out stats, formula1);
            ChartViewModel softFlatDagonStrategyBankChart = GetSoftDagon(filteredForecasts, firstBetValue, initialBank,
                out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonBankValue, softFlatDagonStrategyBankChart);
            model.Add(ChartType.SoftFlatDagonBankValueBet,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonBankValueClean,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonBankValue, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonBankValue, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonBankValue, index);
            loseBankChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue.ToString(), Y = stats[StatType.LoseBankCount], X = index});
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;

            ChartViewModel softFlatDagonStrategyBankChart2 = GetSoftDagonPursuit(filteredForecasts, firstBetValue,
                initialBank, out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonBankValue2, softFlatDagonStrategyBankChart2);
            model.Add(ChartType.SoftFlatDagonBankValueBet2,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonBankValueClean2,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonBankValue2, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonBankValue2, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonBankValue2, index);
            loseBankChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue2.ToString(), Y = stats[StatType.LoseBankCount], X = index});
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue2.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;

            ChartViewModel softFlatDagonStrategyBankChart3 = GetSoftDagon(filteredForecasts, firstBetValue, initialBank,
                out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonBankValue3, softFlatDagonStrategyBankChart3);
            model.Add(ChartType.SoftFlatDagonBankValueBet3,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonBankValueClean3,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonBankValue3, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonBankValue3, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonBankValue3, index);
            loseBankChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue3.ToString(), Y = stats[StatType.LoseBankCount], X = index});
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue3.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;

            ChartViewModel softFlatDagonStrategyBankChart4 = GetSoftDagon(filteredForecasts, firstBetValue, initialBank,
                out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonBankValue4, softFlatDagonStrategyBankChart4);
            model.Add(ChartType.SoftFlatDagonBankValueBet4,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonBankValueClean4,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonBankValue4, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonBankValue4, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonBankValue4, index);
            loseBankChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue4.ToString(), Y = stats[StatType.LoseBankCount], X = index});
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonBankValue4.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;

            //ChartViewModel softFlatDagonStrategyS1BankChart = GetSoftFlatDagonStrategyS1BankChart(filteredForecasts, firstBetValue, initialBank, out bets, out profits, out stats);
            //ChartViewModel softFlatDagonStrategyS1BankChart = GetSoftFlatDagonStrategyBankChart(filteredForecasts, firstBetValue, initialBank, out bets, out profits, out stats, formula2);
            ChartViewModel softFlatDagonStrategyS1BankChart = GetSoftDagon(filteredForecasts, firstBetValue,
                initialBank, out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonModBankValue, softFlatDagonStrategyS1BankChart);
            model.Add(ChartType.SoftFlatDagonModBankValueBet,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonModBankValueClean,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonModBankValue, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonModBankValue, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonModBankValue, index);
            loseBankChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonModBankValue.ToString(), Y = stats[StatType.LoseBankCount], X = index});
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonModBankValue.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;

            //ChartViewModel softFlatDagonStrategyS1BankChart2 = GetSoftFlatDagonStrategyBankChartIndex(filteredForecasts, firstBetValue, initialBank, out bets, out profits, out stats, 2, formula2);
            ChartViewModel softFlatDagonStrategyS1BankChart2 = GetSoftDagon(filteredForecasts, firstBetValue,
                initialBank, out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonModBankValue2, softFlatDagonStrategyS1BankChart2);
            model.Add(ChartType.SoftFlatDagonModBankValueBet2,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonModBankValueClean2,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonModBankValue2, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonModBankValue2, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonModBankValue2, index);
            loseBankChart.ChartData.Add(new ChartData
            {
                Label = ChartType.SoftFlatDagonModBankValue2.ToString(), Y = stats[StatType.LoseBankCount], X = index
            });
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonModBankValue2.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;

            //ChartViewModel softFlatDagonStrategyS1BankChart3 = GetSoftFlatDagonStrategyBankChartIndex(filteredForecasts, firstBetValue, initialBank, out bets, out profits, out stats, 3, formula2);
            ChartViewModel softFlatDagonStrategyS1BankChart3 = GetSoftDagon(filteredForecasts, firstBetValue,
                initialBank, out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonModBankValue3, softFlatDagonStrategyS1BankChart3);
            model.Add(ChartType.SoftFlatDagonModBankValueBet3,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonModBankValueClean3,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonModBankValue3, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonModBankValue3, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonModBankValue3, index);
            loseBankChart.ChartData.Add(new ChartData
            {
                Label = ChartType.SoftFlatDagonModBankValue3.ToString(), Y = stats[StatType.LoseBankCount], X = index
            });
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonModBankValue3.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;

            //ChartViewModel softFlatDagonStrategyS1BankChart4 = GetSoftFlatDagonStrategyBankChartIndex(filteredForecasts, firstBetValue, initialBank, out bets, out profits, out stats, 4, formula2);
            ChartViewModel softFlatDagonStrategyS1BankChart4 = GetSoftDagon(filteredForecasts, firstBetValue,
                initialBank, out bets, out profits, out stats, 4);
            model.Add(ChartType.SoftFlatDagonModBankValue4, softFlatDagonStrategyS1BankChart4);
            model.Add(ChartType.SoftFlatDagonModBankValueBet4,
                new ChartViewModel {Label = "BET", Description = "Marimea beturilor", ChartData = bets});
            model.Add(ChartType.SoftFlatDagonModBankValueClean4,
                new ChartViewModel {Label = "PROFIT", Description = "Marimea profitului", ChartData = profits});
            AddBankValues(stats, bankValueChart, ChartType.SoftFlatDagonModBankValue4, index);
            AddProfitValues(stats, profitMinMaxChart, ChartType.SoftFlatDagonModBankValue4, index);
            AddBetLimitValues(stats, betLimitsChart, ChartType.SoftFlatDagonModBankValue4, index);
            loseBankChart.ChartData.Add(new ChartData
            {
                Label = ChartType.SoftFlatDagonModBankValue4.ToString(), Y = stats[StatType.LoseBankCount], X = index
            });
            bankMaxChart.ChartData.Add(new ChartData
                {Label = ChartType.SoftFlatDagonModBankValue4.ToString(), Y = stats[StatType.BankMax], X = index});
            index++;


            model.Add(ChartType.BankValueChart, bankValueChart);
            model.Add(ChartType.BetMinAvgMaxChart, betLimitsChart);
            model.Add(ChartType.LoseBankCount, loseBankChart);
            model.Add(ChartType.BankMaxChart, bankMaxChart);
            model.Add(ChartType.ProfitMinMaxChart, profitMinMaxChart);

            return model;
        }

        public decimal CalculateNextBetValue(decimal bank, decimal initialBet = 0M, decimal step = 0.1M,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            decimal bet = initialBet;

            while (CheckIfAllowChangeBetValue(bet, bank, 4, step, lowerBound, upperBound))
                bet += step;

            return bet;
        }

        public decimal CalculateMaxBankValue(decimal bet, DateTimeOffset? lowerBound = null,
            DateTimeOffset? upperBound = null)
        {
            List<ForecastJson> forecasts = GetForecastsForCalculations(lowerBound, upperBound);

            decimal maxBank = CalculateBankValue(forecasts, bet);

            foreach (var f in forecasts.GroupBy(x => x.GameAt.Month))
            {
                decimal bank = CalculateBankValue(f.ToList(), bet);

                if (bank > maxBank)
                    maxBank = bank;
            }

            return Math.Ceiling(maxBank);
        }

        private List<ForecastJson> GetForecastsForCalculations(DateTimeOffset? lowerBound, DateTimeOffset? upperBound)
        {
            var query = _forecasts.AsEnumerable()
                .Reverse();

            if (lowerBound != null)
                query = query.Where(x => x.GameAt >= lowerBound);
            else
                query = query.Where(x => x.GameAt.Year >= 2018 && x.GameAt.Month >= 4);

            if (upperBound != null)
                query = query.Where(x => x.GameAt <= upperBound);

            return query.ToList();
        }

        public decimal CalculateMaxBankValuePursuit(decimal bet)
        {
            List<ForecastJson> forecasts = _forecasts.AsEnumerable()
                .Reverse()
                .Where(x => x.GameAt.Year == 2018 && x.GameAt.Month >= 4)
                .ToList();

            var banks = new List<decimal>();

            decimal maxBank = CalculateBankValuePursuit(forecasts, bet);

            banks.Add(maxBank);

            foreach (var f in forecasts.GroupBy(x => x.GameAt.Month))
            {
                decimal bank = CalculateBankValuePursuit(f.ToList(), 5);
                banks.Add(bank);
                if (bank > maxBank)
                    maxBank = bank;
            }

            decimal avg = banks.Average();

            return Math.Ceiling(maxBank);
        }

        private decimal CalculateBankValue(List<ForecastJson> forecasts, decimal bet)
        {
            var loseValues = new List<decimal>();
            var numOfLoses = new List<int>();
            var oneBetValues = new List<decimal>();

            int index = 4;

            for (int i = 0; i < index; i++)
            {
                loseValues.Add(0);
                numOfLoses.Add(0);
                oneBetValues.Add(bet);
            }

            decimal bank = 0;
            decimal maxBank = 0;

            for (var i = 0; i < forecasts.Count - index; i += index)
            {
                if (maxBank < oneBetValues.Sum() + loseValues.Sum())
                    maxBank = oneBetValues.Sum() + loseValues.Sum();

                for (int j = 0; j < index; j++)
                {
                    ForecastJson forecast = forecasts[i + j];

                    bank -= oneBetValues[j];

                    decimal result = CalculateResult(oneBetValues[j], forecast.GameResultType, forecast.Coefficient);

                    if (result < 0)
                    {
                        loseValues[j] += oneBetValues[j];
                        numOfLoses[j] += 1;

                        oneBetValues[j] = (bet + loseValues[j]) / (decimal) (forecast.Coefficient - 1);
                    }
                    else if (result > oneBetValues[j])
                    {
                        numOfLoses[j] -= 1;

                        if (loseValues[j] <= result)
                        {
                            loseValues[j] = 0;
                            numOfLoses[j] = 0;
                            oneBetValues[j] = bet;
                        }
                        else
                        {
                            loseValues[j] -= result;

                            oneBetValues[j] = (bet + loseValues[j]) / (decimal) (forecast.Coefficient - 1);
                        }

                        bank += result;
                    }
                    else
                    {
                        bank += oneBetValues[j];
                    }
                }
            }

            return Math.Abs(maxBank);
        }

        class MyClass
        {
            public decimal LoseValue { get; set; }

            public decimal BetValue { get; set; }

            public int NumOfLose { get; set; }
        }

        private decimal CalculateBankValuePursuit(List<ForecastJson> forecasts, decimal bet)
        {
            //var loseValues = new List<decimal>();
            //var numOfLoses = new List<int>();
            //var oneBetValues = new List<decimal>();
            var tempForecasts = new List<ForecastJson>();
            var values = new List<MyClass>();

            int index = 4;

            for (int i = 0; i < index; i++)
            {
                values.Add(new MyClass
                {
                    BetValue = bet,
                    LoseValue = 0,
                    NumOfLose = 0
                });
                //loseValues.Add(0);
                //numOfLoses.Add(0);
                //oneBetValues.Add(bet);
            }

            decimal bank = 0;
            decimal maxBank = 0;

            for (var i = 0; i < forecasts.Count - index; i += index)
            {
                if (maxBank < values.Select(x => x.BetValue).Sum() + values.Select(x => x.LoseValue).Sum())
                    maxBank = values.Select(x => x.BetValue).Sum() + values.Select(x => x.LoseValue).Sum();

                for (int j = 0; j < index; j++)
                {
                    tempForecasts.Add(forecasts[i + j]);
                }

                tempForecasts = tempForecasts.OrderByDescending(x => x.Coefficient).ToList();

                List<MyClass> tempValues = values.OrderByDescending(x => x.NumOfLose)
                    .ToList();

                foreach (var val in tempValues)
                {
                    ForecastJson forecast = tempForecasts.First();

                    bank -= val.BetValue;

                    decimal result = CalculateResult(val.BetValue, forecast.GameResultType, forecast.Coefficient);

                    if (result < 0)
                    {
                        val.LoseValue += val.BetValue;
                        val.NumOfLose += 1;

                        val.BetValue = (bet + val.LoseValue) / (decimal) (forecast.Coefficient - 1);
                    }
                    else if (result > val.BetValue)
                    {
                        val.NumOfLose -= 1;

                        if (val.LoseValue <= result)
                        {
                            val.LoseValue = 0;
                            val.NumOfLose = 0;
                            val.BetValue = bet;
                        }
                        else
                        {
                            val.LoseValue -= result;

                            val.BetValue = (bet + val.LoseValue) / (decimal) (forecast.Coefficient - 1);
                        }

                        bank += result;
                    }
                    else
                    {
                        bank += val.BetValue;
                    }

                    tempForecasts.Remove(forecast);
                }
            }

            return Math.Abs(maxBank);
        }

        private void AddBetLimitValues(Dictionary<StatType, decimal> stats, ChartViewModel valueChart,
            ChartType chartType, int index)
        {
            valueChart.ChartData.Add(
                new ChartData {X = index, Label = chartType.ToString(), Y = stats[StatType.BetMin]});
            valueChart.SecondChartData.Add(new ChartData
                {X = index, Label = chartType.ToString(), Y = stats[StatType.BetAvg]});
            valueChart.ThirdChartData.Add(new ChartData
                {X = index, Label = chartType.ToString(), Y = stats[StatType.BetMax]});
        }

        private void AddBankValues(Dictionary<StatType, decimal> stats, ChartViewModel valueChart, ChartType chartType,
            int index)
        {
            valueChart.ChartData.Add(new ChartData
                {X = index, Label = chartType.ToString(), Y = stats[StatType.BankValueMin]});
            valueChart.SecondChartData.Add(new ChartData
                {X = index, Label = chartType.ToString(), Y = stats[StatType.BankValueMax]});
        }

        private void AddProfitValues(Dictionary<StatType, decimal> stats, ChartViewModel valueChart,
            ChartType chartType, int index)
        {
            valueChart.ChartData.Add(new ChartData
                {X = index, Label = chartType.ToString(), Y = stats[StatType.ProfitMin]});
            valueChart.SecondChartData.Add(new ChartData
                {X = index, Label = chartType.ToString(), Y = stats[StatType.ProfitMax]});
        }

        private ChartViewModel GetSoftDagonPursuit(List<ForecastJson> filteredForecasts,
            decimal firstBetValue,
            decimal initialBank, out List<ChartData> bets, out List<ChartData> profits,
            out Dictionary<StatType, decimal> stats, int index)
        {
            var model = new ChartViewModel
            {
                Label = "Bank Value",
                Description = "Grafic care arata marimea Bankului necesar pentru a efectua stavshi",
                ChartData = new List<ChartData>()
            };

            bets = new List<ChartData>();
            profits = new List<ChartData>();
            stats = new Dictionary<StatType, decimal>();
            var values = new List<MyClass>();

            for (int i = 0; i < index; i++)
            {
                values.Add(new MyClass
                {
                    BetValue = firstBetValue,
                    LoseValue = 0,
                    NumOfLose = 0
                });
            }

            decimal bank = initialBank;
            decimal currentBank = initialBank;

            //decimal oneBetValue = firstBetValue;
            //decimal loseValue = 0;
            var x = 1;
            //int numOfLose = 0;

            decimal profit = 0;

            stats[StatType.LoseBankCount] = 0;
            stats[StatType.BankMax] = 0;
            stats[StatType.BankValueMax] = initialBank;
            stats[StatType.BankValueMin] = initialBank;
            stats[StatType.BetAvg] = firstBetValue;
            stats[StatType.BetMin] = firstBetValue;
            stats[StatType.BetMax] = firstBetValue;
            stats[StatType.ProfitMax] = 0;
            stats[StatType.ProfitMin] = 0;
            var tempForecasts = new List<ForecastJson>();

            for (var i = 0; i < filteredForecasts.Count - index; i += index)
            {
                decimal tempBank = bank;

                for (int j = 0; j < index; j++)
                {
                    tempBank -= values[j].BetValue;
                    tempForecasts.Add(filteredForecasts[i + j]);
                }

                tempForecasts = tempForecasts.OrderBy(f => f.Coefficient).ToList();

                List<MyClass> tempValues = values.OrderByDescending(f => f.NumOfLose)
                    .ToList();
                //firstBetValue = CalculateNextBetValue(tempBank, firstBetValue);

                //if (CalculateMaxBankValue(firstBetValue + 0.1M) < tempBank)
                //    firstBetValue += 0.1M;

                model.ChartData.Add(new ChartData
                {
                    Y = tempBank,
                    Label = filteredForecasts[i].GameAt.ToString("d"),
                    X = x++
                });


                foreach (var val in tempValues)
                {
                    ForecastJson forecast = tempForecasts.First();

                    if (currentBank - val.BetValue < 0)
                    {
                        currentBank += initialBank;
                        profit -= initialBank;

                        stats[StatType.LoseBankCount]++;
                    }

                    bank -= val.BetValue;
                    currentBank -= val.BetValue;
                    stats[StatType.BankMax] += val.BetValue;
                    stats[StatType.BankValueMax] =
                        bank > stats[StatType.BankValueMax] ? bank : stats[StatType.BankValueMax];
                    stats[StatType.BankValueMin] =
                        bank < stats[StatType.BankValueMin] ? bank : stats[StatType.BankValueMin];

                    model.ChartData.Add(new ChartData
                    {
                        Y = bank,
                        Label = filteredForecasts[i].GameAt.ToString("d"),
                        X = x++
                    });

                    bets.Add(new ChartData
                    {
                        Y = val.BetValue,
                        Label = forecast.GameAt.ToString("d"),
                        X = x
                    });

                    decimal result = CalculateResult(val.BetValue, forecast.GameResultType, forecast.Coefficient);

                    if (result < 0)
                    {
                        val.LoseValue += val.BetValue;
                        val.NumOfLose += 1;

                        val.BetValue = (firstBetValue + val.LoseValue) / (decimal) (forecast.Coefficient - 1);
                    }
                    else if (result > val.BetValue)
                    {
                        profit += (result - val.BetValue);
                        val.NumOfLose -= 1;

                        currentBank += val.BetValue;
                        stats[StatType.BankMax] -= val.BetValue;

                        if (val.LoseValue <= result)
                        {
                            val.LoseValue = 0;
                            val.NumOfLose = 0;
                            val.BetValue = firstBetValue;
                        }
                        else
                        {
                            val.LoseValue -= result;

                            val.BetValue = (firstBetValue + val.LoseValue) / (decimal) (forecast.Coefficient - 1);
                        }

                        bank += result;
                    }
                    else
                    {
                        bank += val.BetValue;
                        currentBank += val.BetValue;
                        stats[StatType.BankMax] -= val.BetValue;
                    }

                    decimal p = profit + (currentBank - initialBank);

                    profits.Add(new ChartData
                    {
                        X = x,
                        Label = forecast.GameAt.ToString("d"),
                        Y = p
                    });

                    stats[StatType.ProfitMin] = p < stats[StatType.ProfitMin] ? p : stats[StatType.ProfitMin];
                    stats[StatType.ProfitMax] = p > stats[StatType.ProfitMax] ? p : stats[StatType.ProfitMax];

                    tempForecasts.Remove(forecast);
                }
            }

            stats[StatType.BetMin] = (decimal) bets.Min(b => b.Y);
            stats[StatType.BetMax] = (decimal) bets.Max(b => b.Y);
            stats[StatType.BetAvg] = bets.Average(b => (decimal) b.Y);

            return model;
        }

        private ChartViewModel GetSoftDagon(List<ForecastJson> filteredForecasts,
            decimal firstBetValue,
            decimal initialBank, out List<ChartData> bets, out List<ChartData> profits,
            out Dictionary<StatType, decimal> stats, int index)
        {
            var model = new ChartViewModel
            {
                Label = "Bank Value",
                Description = "Grafic care arata marimea Bankului necesar pentru a efectua stavshi",
                ChartData = new List<ChartData>()
            };

            bets = new List<ChartData>();
            profits = new List<ChartData>();
            stats = new Dictionary<StatType, decimal>();

            var loseValues = new List<decimal>();
            var numOfLoses = new List<int>();
            var numOfDoubleLoses = new List<int>();
            var oneBetValues = new List<decimal>();

            for (int i = 0; i < index; i++)
            {
                loseValues.Add(0);
                numOfLoses.Add(0);
                numOfDoubleLoses.Add(0);
                oneBetValues.Add(firstBetValue);
            }

            decimal bank = initialBank;
            decimal currentBank = initialBank;

            //decimal oneBetValue = firstBetValue;
            //decimal loseValue = 0;
            var x = 1;
            //int numOfLose = 0;

            decimal profit = 0;

            stats[StatType.LoseBankCount] = 0;
            stats[StatType.BankMax] = 0;
            stats[StatType.BankValueMax] = initialBank;
            stats[StatType.BankValueMin] = initialBank;
            stats[StatType.BetAvg] = firstBetValue;
            stats[StatType.BetMin] = firstBetValue;
            stats[StatType.BetMax] = firstBetValue;
            stats[StatType.ProfitMax] = 0;
            stats[StatType.ProfitMin] = 0;

            for (var i = 0; i < filteredForecasts.Count - index; i += index)
            {
                decimal tempBank = bank;

                for (int j = 0; j < index; j++)
                {
                    tempBank -= oneBetValues[j];
                }

                //firstBetValue = CalculateNextBetValue(tempBank, firstBetValue);

                //if (CalculateMaxBankValue(firstBetValue + 0.1M) < tempBank)
                //    firstBetValue += 0.1M;

                model.ChartData.Add(new ChartData
                {
                    Y = tempBank,
                    Label = filteredForecasts[i].GameAt.ToString("d"),
                    X = x++
                });

                for (int j = 0; j < index; j++)
                {
                    ForecastJson forecast = filteredForecasts[i + j];

                    if (currentBank - oneBetValues[j] < 0)
                    {
                        currentBank += initialBank;
                        profit -= initialBank;

                        stats[StatType.LoseBankCount]++;
                    }

                    bank -= oneBetValues[j];
                    currentBank -= oneBetValues[j];
                    stats[StatType.BankMax] += oneBetValues[j];
                    stats[StatType.BankValueMax] =
                        bank > stats[StatType.BankValueMax] ? bank : stats[StatType.BankValueMax];
                    stats[StatType.BankValueMin] =
                        bank < stats[StatType.BankValueMin] ? bank : stats[StatType.BankValueMin];

                    model.ChartData.Add(new ChartData
                    {
                        Y = bank,
                        Label = filteredForecasts[i].GameAt.ToString("d"),
                        X = x++
                    });

                    bets.Add(new ChartData
                    {
                        Y = oneBetValues[j],
                        Label = forecast.GameAt.ToString("d"),
                        X = x
                    });

                    decimal result = CalculateResult(oneBetValues[j], forecast.GameResultType, forecast.Coefficient);

                    if (result < 0)
                    {
                        loseValues[j] += oneBetValues[j];
                        numOfLoses[j] += 1;
                        numOfDoubleLoses[j] += 1;

                        oneBetValues[j] = (firstBetValue + loseValues[j]) / (decimal) (forecast.Coefficient - 1);
                    }
                    else if (result > oneBetValues[j])
                    {
                        profit += (result - oneBetValues[j]);
                        numOfLoses[j] -= 1;

                        currentBank += oneBetValues[j];
                        stats[StatType.BankMax] -= oneBetValues[j];

                        if (loseValues[j] <= result)
                        {
                            loseValues[j] = 0;
                            numOfLoses[j] = 0;
                            numOfDoubleLoses[j] = 0;
                            oneBetValues[j] = firstBetValue;
                        }
                        else
                        {
                            loseValues[j] -= result;

                            oneBetValues[j] = (firstBetValue + loseValues[j]) / (decimal) (forecast.Coefficient - 1);
                        }

                        bank += result;
                    }
                    else
                    {
                        bank += oneBetValues[j];
                        currentBank += oneBetValues[j];
                        stats[StatType.BankMax] -= oneBetValues[j];
                    }

                    decimal p = profit + (currentBank - initialBank);

                    profits.Add(new ChartData
                    {
                        X = x,
                        Label = forecast.GameAt.ToString("d"),
                        Y = p
                    });

                    stats[StatType.ProfitMin] = p < stats[StatType.ProfitMin] ? p : stats[StatType.ProfitMin];
                    stats[StatType.ProfitMax] = p > stats[StatType.ProfitMax] ? p : stats[StatType.ProfitMax];
                }
            }

            stats[StatType.BetMin] = (decimal) bets.Min(b => b.Y);
            stats[StatType.BetMax] = (decimal) bets.Max(b => b.Y);
            stats[StatType.BetAvg] = bets.Average(b => (decimal) b.Y);

            return model;
        }

        private bool CheckIfAllowChangeBetValue(decimal bet, decimal bank, int index, decimal step = 0.1M,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            List<ForecastJson> forecasts = GetForecastsForCalculations(lowerBound, upperBound);

            foreach (var f in forecasts.GroupBy(z => z.GameAt.Month))
            {
                decimal calculatedMaxBank = IsValidNewBetValue(f.ToList(), bet + step, bank, index);

                if (calculatedMaxBank > bank) return false;
            }

            return IsValidNewBetValue(forecasts, bet + step, bank, index) < bank;
        }

        private decimal IsValidNewBetValue(List<ForecastJson> forecasts, decimal bet, decimal bank, int index)
        {
            var loseValues = new List<decimal>();
            var oneBetValues = new List<decimal>();
            decimal maxBankValues = 0;

            for (int i = 0; i < index; i++)
            {
                loseValues.Add(0);
                oneBetValues.Add(bet);
            }

            for (var i = 0; i < forecasts.Count - index; i += index)
            {
                if (maxBankValues < oneBetValues.Sum() + loseValues.Sum())
                    maxBankValues = oneBetValues.Sum() + loseValues.Sum();

                for (int j = 0; j < index; j++)
                {
                    ForecastJson forecast = forecasts[i + j];

                    bank -= oneBetValues[j];

                    if (forecast.GameResultType == GameResultType.Expectation ||
                        forecast.GameResultType == GameResultType.RefundOrCancellation)
                    {
                        bank += oneBetValues[j];
                    }
                    else
                    {
                        decimal result = CalculateResult(oneBetValues[j], forecast.GameResultType,
                            forecast.Coefficient);

                        if (result < 0)
                        {
                            loseValues[j] += oneBetValues[j];

                            oneBetValues[j] = (bet + loseValues[j]) / (decimal) (forecast.Coefficient - 1);
                        }
                        else
                        {
                            oneBetValues[j] = bet;
                            loseValues[j] = 0;

                            bank += result;
                        }
                    }
                }
            }

            return maxBankValues;
        }

        private ChartViewModel GetFlatStrategyBankChart(List<ForecastJson> filteredForecasts, decimal firstBetValue,
            decimal initialBank, out List<ChartData> bets, out List<ChartData> profits,
            out Dictionary<StatType, decimal> stats)
        {
            var model = new ChartViewModel
            {
                Label = "Bank Value - First Bet(10)",
                Description = "Grafic care arata marimea Bankului necesar pentru a efectua stavshi",
                ChartData = new List<ChartData>()
            };

            bets = new List<ChartData>();
            profits = new List<ChartData>();
            stats = new Dictionary<StatType, decimal>();

            decimal bank = initialBank;
            decimal currentBank = initialBank;
            decimal oneBetValue = firstBetValue;
            var index = 1;

            decimal profit = 0;

            stats[StatType.LoseBankCount] = 0;
            stats[StatType.BankMax] = 0;
            stats[StatType.BankValueMax] = initialBank;
            stats[StatType.BankValueMin] = initialBank;
            stats[StatType.BetAvg] = firstBetValue;
            stats[StatType.BetMin] = firstBetValue;
            stats[StatType.BetMax] = firstBetValue;
            stats[StatType.ProfitMax] = 0;
            stats[StatType.ProfitMin] = 0;

            foreach (ForecastJson forecast in filteredForecasts)
            {
                bank -= oneBetValue;
                stats[StatType.BankMax] += oneBetValue;
                stats[StatType.BankValueMax] =
                    bank > stats[StatType.BankValueMax] ? bank : stats[StatType.BankValueMax];
                stats[StatType.BankValueMin] =
                    bank < stats[StatType.BankValueMin] ? bank : stats[StatType.BankValueMin];

                if (currentBank - oneBetValue < 0)
                {
                    //profit += currentBank;
                    currentBank += initialBank;
                    profit -= initialBank;
                    stats[StatType.LoseBankCount]++;
                }

                currentBank -= oneBetValue;

                model.ChartData.Add(new ChartData
                {
                    Y = bank,
                    Label = forecast.GameAt.ToString("d"),
                    X = index++
                });

                bets.Add(new ChartData
                {
                    Y = oneBetValue,
                    Label = forecast.GameAt.ToString("d"),
                    X = index
                });

                decimal result = CalculateResult(oneBetValue, forecast.GameResultType, forecast.Coefficient);

                if (result > 0 && result > oneBetValue)
                {
                    profit += (result - oneBetValue);
                    stats[StatType.BankMax] -= oneBetValue;
                }
                else if (result == oneBetValue)
                {
                    stats[StatType.BankMax] -= oneBetValue;
                }

                decimal p = profit + (currentBank - initialBank);

                profits.Add(new ChartData
                {
                    Y = p,
                    X = index
                });

                bank += result > 0 ? result : 0;

                stats[StatType.ProfitMin] = p < stats[StatType.ProfitMin] ? p : stats[StatType.ProfitMin];
                stats[StatType.ProfitMax] = p > stats[StatType.ProfitMax] ? p : stats[StatType.ProfitMax];

                currentBank += result < 0 ? 0 : oneBetValue;
            }

            stats[StatType.BetMin] = (decimal) bets.Min(x => x.Y);
            stats[StatType.BetMax] = (decimal) bets.Max(x => x.Y);
            stats[StatType.BetAvg] = bets.Average(x => (decimal) x.Y);

            return model;
        }

        private decimal CalculateResult(decimal oneBetValue, GameResultType forecastGameResultType,
            double forecastCoefficient)
        {
            switch (forecastGameResultType)
            {
                case GameResultType.Expectation:
                    return oneBetValue;
                case GameResultType.Win:
                    return oneBetValue * (decimal) forecastCoefficient;
                case GameResultType.Defeat:
                    return oneBetValue * -1;
                case GameResultType.RefundOrCancellation:
                    return oneBetValue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(forecastGameResultType), forecastGameResultType, null);
            }
        }

        private ChartViewModel GetWinLoseCountsChart(ChartViewModel winLoseChart)
        {
            var model = new ChartViewModel
            {
                Label = "Win/Lose - Count",
                Description = "Grafic care arata numarul de serii de cistiguri sau pierderi",
                ChartData = new List<ChartData>()
            };

            List<ChartData> chartData = winLoseChart.ChartData
                .GroupBy(x => new {x.Color, x.Y})
                .Select((x, i) => new ChartData
                {
                    Color = x.Key.Color,
                    Y = x.Key.Y,
                    Label = x.Count().ToString()
                })
                .OrderByDescending(x => int.Parse(x.Label)).ThenByDescending(x => x.Y)
                .ToList();

            for (int i = 0; i < chartData.Count; i++)
            {
                chartData[i].X = i + 1;
            }

            model.ChartData = chartData;

            return model;
        }

        private ChartViewModel GetWinLoseCounts4Chart(ChartViewModel winLoseChart)
        {
            var model = new ChartViewModel
            {
                Label = "Win/Lose - Count(pentru nestandarte)",
                Description = "Grafic care arata numarul de serii de cistiguri sau pierderi",
                ChartData = new List<ChartData>()
            };

            List<ChartData> chartData = winLoseChart.ChartData
                .GroupBy(x => new {x.Color, x.Y})
                .Select((x, i) => new ChartData
                {
                    Color = x.Key.Color,
                    Y = x.Key.Y,
                    Label = x.Count().ToString()
                })
                .OrderByDescending(x => int.Parse(x.Label)).ThenByDescending(x => x.Y)
                .ToList();

            for (int i = 0; i < chartData.Count; i++)
            {
                chartData[i].X = i + 1;
            }

            model.ChartData = chartData;

            return model;
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

        private ChartViewModel GetWinLose4ChartLine(List<ForecastJson> filteredForecasts,
            GameResultType? gameResultType, bool excludeCanceled = false)
        {
            var winLoseChart = new ChartViewModel
            {
                Label = "Win/Lose(4in4 - Line)",
                Description = "Grafic care arata care erau treptele de trecere de la cistig la pierderi si invers",
                ChartData = new List<ChartData>()
            };

            int index = 1;

            for (int i = 0; i < 4; i++)
            {
                GameResultType lastResultType = filteredForecasts[i].GameResultType;
                string lastLabel = filteredForecasts[i].GameAt.ToString("d");

                int count = 0;

                for (var j = i; j < filteredForecasts.Count; j += 4)
                {
                    if (j >= filteredForecasts.Count) continue;

                    var forecast = filteredForecasts[j];

                    if (excludeCanceled && (forecast.GameResultType == GameResultType.Expectation ||
                                            forecast.GameResultType == GameResultType.RefundOrCancellation))
                        continue;

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

                winLoseChart.ChartData.Add(new ChartData
                {
                    Color = "black",
                    Label = lastLabel,
                    X = index++,
                    Y = 0
                });
            }

            return winLoseChart;
        }

        class Temp
        {
            public GameResultType Last { get; set; }

            public int Count { get; set; }
        }

        private ChartViewModel GetWinLose4ChartPursuit(List<ForecastJson> filteredForecasts,
            GameResultType? gameResultType, bool excludeCanceled = false)
        {
            var winLoseChart = new ChartViewModel
            {
                Label = "Win/Lose(4in4 - Pursuit)",
                Description =
                    "Grafic care arata care erau treptele de trecere de la cistig la pierderi si invers se alege urmatorul meci cu cel mai mare coeficient",
                ChartData = new List<ChartData>()
            };

            var lasts = new List<Temp>
            {
                new Temp {Last = filteredForecasts[0].GameResultType, Count = 1},
                new Temp {Last = filteredForecasts[1].GameResultType, Count = 1},
                new Temp {Last = filteredForecasts[2].GameResultType, Count = 1},
                new Temp {Last = filteredForecasts[3].GameResultType, Count = 1}
            };
            var tempForecasts = new List<ForecastJson>();

            int index = 1;

            for (int i = 4; i < filteredForecasts.Count - 4; i += 4)
            {
                //GameResultType lastResultType = filteredForecasts[i].GameResultType;
                //string lastLabel = filteredForecasts[i].GameAt.ToString("d");

                for (int j = 0; j < 4; j++)
                {
                    tempForecasts.Add(filteredForecasts[i + j]);
                }

                tempForecasts = tempForecasts.OrderByDescending(x => x.Coefficient).ToList();

                List<Temp> defeats = lasts.Where(x => x.Last == GameResultType.Defeat).OrderByDescending(x => x.Count)
                    .ToList();

                List<Temp> others = lasts.Where(x => x.Last != GameResultType.Defeat).ToList();


                foreach (var o in defeats)
                {
                    var forecast = tempForecasts.First();

                    //if (excludeCanceled && (forecast.GameResultType == GameResultType.Expectation ||
                    //                        forecast.GameResultType == GameResultType.RefundOrCancellation))
                    //    continue;

                    if (o.Last == forecast.GameResultType)
                        o.Count++;
                    else
                    {
                        //if (gameResultType == null || lastResultType == gameResultType)
                        //{
                        winLoseChart.ChartData.Add(new ChartData
                        {
                            Color = GetColor(o.Last),
                            //Label = lastLabel,
                            X = index++,
                            Y = o.Count
                        });
                        //}

                        //lastLabel = forecast.GameAt.ToString("d");

                        o.Last = forecast.GameResultType;
                        o.Count = 1;
                    }

                    tempForecasts.Remove(forecast);
                }

                foreach (var o in others)
                {
                    var forecast = tempForecasts.First();

                    //if (excludeCanceled && (forecast.GameResultType == GameResultType.Expectation ||
                    //                        forecast.GameResultType == GameResultType.RefundOrCancellation))
                    //    continue;

                    if (o.Last == forecast.GameResultType)
                        o.Count++;
                    else
                    {
                        //if (gameResultType == null || lastResultType == gameResultType)
                        //{
                        winLoseChart.ChartData.Add(new ChartData
                        {
                            Color = GetColor(o.Last),
                            //Label = lastLabel,
                            X = index++,
                            Y = o.Count
                        });
                        //}

                        //lastLabel = forecast.GameAt.ToString("d");

                        o.Last = forecast.GameResultType;
                        o.Count = 1;
                    }

                    tempForecasts.Remove(forecast);
                }

                //winLoseChart.ChartData.Add(new ChartData
                //{
                //    Color = "black",
                //    Label = lastLabel,
                //    X = index++,
                //    Y = 0
                //});
            }

            return winLoseChart;
        }

        private string GetColor(GameResultType gameResultType)
        {
            switch (gameResultType)
            {
                case GameResultType.Expectation:
                    return "#6c757d";
                case GameResultType.Win:
                    return "#28a745";
                case GameResultType.Defeat:
                    return "#dc3545";
                case GameResultType.RefundOrCancellation:
                    return "#ffc107";
                default:
                    return "#343a40";
            }
        }

        private List<ForecastJson> GetForecasts()
        {
            //ReinitializeData();

            //var forecasts = JsonConvert
            //    .DeserializeObject<List<ForecastJson>>(ReadAllText(_forecastsFile))
            //    .Distinct(new ForecastEqualityComparer());
            //.OrderByDescending(x => x.GameAt);

            return new List<ForecastJson>();
            //return new List<ForecastJson>(forecasts);
        }

        private void ReinitializeData()
        {
            List<ForecastJson> forecasts = JsonConvert
                .DeserializeObject<List<PageJson>>(ReadAllText("ASB_2018_Stat.json"))
                //.DeserializeObject<List<PageJson>>(ReadAllText("asb_stat_initial.json"))
                .SelectMany(x => x.Forecasts)
                .Distinct(new ForecastJsonEqualityComparer())
                //.Reverse()
                .ToList();

            WriteAllText(_forecastsFile, JsonConvert.SerializeObject(forecasts));
        }

        private void WriteAllText(string fileName, string content)
        {
            File.WriteAllText(GetFilePath(fileName), content);
        }

        private string ReadAllText(string fileName)
        {
            return File.ReadAllText(GetFilePath(fileName));
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(_hostingEnvironment.ContentRootPath, "files", fileName);
        }
    }
}