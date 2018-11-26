using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Services
{
    public class AlgorithmService : IAlgorithmService
    {
        private readonly IDataService _dataService;

        public AlgorithmService(IDataService dataService)
        {
            _dataService = dataService;
        }
        
        public async Task<StateJson> CalculateNextStateAsync(long forecastId, decimal? betValue = null,
            bool? allowIncreaseBet = null)
        {
            return await Task.Run(() =>
            {
                ForecastJson forecast = _dataService.GetForecastBy(forecastId);
                ForecastJson previousForecast = _dataService.GetLastCalculatedForecastByIndex(forecast.ThreadNumber);
                SettingsJson settings = _dataService.GetSettings();

                return CalculateNextAlgorithmState(forecast, previousForecast, settings: settings,
                    allowIncreaseBet: allowIncreaseBet ?? settings.AllowIncreaseBetValue, betValue: betValue);
            });
        }

        public async Task<Dictionary<int, Dictionary<Month, decimal>>> GetCalculatedBankValuesByBetAsync(
            CalculateBankValuesOptions options,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            return await Task.Run(() =>
            {
                var result = new Dictionary<int, Dictionary<Month, decimal>>();

                List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
                {
                    LowerBound = lowerBound,
                    UpperBound = upperBound
                });

                foreach (var group in forecasts.GroupBy(x => x.GameAt.Year))
                {
                    Dictionary<Month, decimal> bankValues = CalculateBankValues(group.ToList(), options);

                    result[group.Key] = bankValues;
                }

                return result;
            });
        }

        public async Task<Dictionary<CalculationMethod, decimal>> GetBankValuesByMethodsAsync(CalculateBankValuesOptions options, 
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            return await Task.Run(() =>
            {
                List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
                {
                    LowerBound = lowerBound, 
                    UpperBound = upperBound
                });

                return GetBankValuesByMethods(forecasts, options);
            });
        }

        public async Task<decimal> GetBankValueByBetAndMethodAsync(CalculateBankValuesOptions options, CalculationMethod calculationMethod,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            return await Task.Run(() =>
            {
                List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
                {
                    LowerBound = lowerBound, 
                    UpperBound = upperBound
                });

                Dictionary<CalculationMethod, decimal> banks = GetBankValuesByMethods(forecasts, options);

                return banks[calculationMethod];
            });
        }

        private Dictionary<CalculationMethod, decimal> GetBankValuesByMethods(List<ForecastJson> forecasts,
            CalculateBankValuesOptions options)
        {
            Dictionary<Month, decimal> bankValues = CalculateBankValues(forecasts, options);

            var result = new Dictionary<CalculationMethod, decimal>
            {
                [CalculationMethod.Average] = Math.Ceiling(bankValues.Average(x => x.Value)),
                [CalculationMethod.Min] = Math.Ceiling(bankValues.Min(x => x.Value)),
                [CalculationMethod.Max] = Math.Ceiling(bankValues.Max(x => x.Value)),
                [CalculationMethod.SecondMax] = bankValues.Count > 1
                    ? bankValues.OrderByDescending(x => x.Value).ElementAt(2).Value
                    : bankValues[0]
            };

            return result;
        }

        public async Task<decimal> CalculateBetValueByBankAsync(CalculateBetValueOptions options)
        {
            return await Task.Run(() => CalculateBetValue(options));
        }

        public async Task<List<WinLoseCountModel>> GetWinLoseCountByThreadNumberAsync(
            List<ForecastJson> forecasts, int threadNumbers)
        {
            return await Task.Run(() => GetWinLoseCount(forecasts, threadNumbers));
        }

        public async Task<List<decimal>> GetDefeatChainBets(decimal bet, double coefficient = 2.1)
        {
            return await Task.Run(() =>
            {
                var result = new List<decimal>();

                for (var i = 0; i < 11; i++)
                {
                    decimal betValue = (bet + result.Sum()) / (decimal) (coefficient - 1);
                    betValue = Math.Round(betValue + 0.005M, 2);

                    result.Add(betValue);
                }

                return result;
            });
        }

        private List<WinLoseCountModel> GetWinLoseCount(List<ForecastJson> forecasts, int threadNumbers)
        {
            var result = new List<WinLoseCountModel>();

            if (forecasts == null || !forecasts.Any()) return result;

            var lastResults = new List<GameResultType>(capacity: threadNumbers);
            var lastDates = new List<DateTimeOffset>(capacity: threadNumbers);

            for (var i = 0; i < threadNumbers; i++)
            {
                lastResults[i] = forecasts.FirstOrDefault(x => x.ThreadNumber == i)?.GameResultType ??
                                 GameResultType.Expectation;
                lastDates[i] = forecasts.FirstOrDefault(x => x.ThreadNumber == i)?.GameAt ?? DateTimeOffset.Now;

                var count = 1;
                
                foreach (ForecastJson forecast in forecasts.Where(x => x.ThreadNumber == i).Skip(1))
                {
                    if (forecast.GameResultType == GameResultType.Expectation)
                        continue;

                    if (lastResults[i] == forecast.GameResultType)
                    {
                        count++;
                    }
                    else
                    {
                        result.Add(new WinLoseCountModel
                        {
                            GameResultType = lastResults[i],
                            Count = count,
                            StartSeries = lastDates[i],
                            ThreadNumber = i
                        });
                    
                        lastResults[i] = forecast.GameResultType;
                        lastDates[i] = forecast.GameAt;
                        count = 1;
                    }
                }
            }

            return result;
        }

        private decimal CalculateBetValue(CalculateBetValueOptions options)
        {
            List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
            {
                LowerBound = options.LowerBound, 
                UpperBound = options.UpperBound
            });

            if (forecasts == null || !forecasts.Any()) return options.InitialBet;

            var bankOptions = new CalculateBankValuesOptions
            {
                ThreadNumbers = options.ThreadNumbers,
                Bet = options.InitialBet + options.IncreaseBetStep,
                CoefficientBankReserve = options.CoefficientBankReserve
            };

            while (GetBankValuesByMethods(forecasts, bankOptions)[options.CalculationMethod] <= options.Bank)
            {
                options.InitialBet += options.IncreaseBetStep;
                bankOptions.Bet += options.IncreaseBetStep;
            }

            return options.InitialBet;
        }


        private Dictionary<Month, decimal> CalculateBankValues(List<ForecastJson> forecasts, CalculateBankValuesOptions options)
        {
            var results = new Dictionary<Month, decimal>();
            
            decimal bank = CalculateBankValue(forecasts, options.Bet, options.CoefficientBankReserve, options.ThreadNumbers);

            results[Month.All] = Math.Ceiling(bank);

            foreach (var group in forecasts.GroupBy(x => x.GameAt.Month))
            {
                bank = CalculateBankValue(group.ToList(), options.Bet, options.CoefficientBankReserve, options.ThreadNumbers);

                results[(Month) group.Key] = Math.Ceiling(bank);
            }

            return results;
        }

        private StateJson CalculateNextAlgorithmState(ForecastJson currentForecast, ForecastJson previousForecast, 
            StateJson lastState = null, SettingsJson settings = null, bool allowIncreaseBet = false, decimal? betValue = null)
        {
            if (currentForecast == null)
                return null;

            if (lastState == null)
                lastState = _dataService.GetLastState();

            StateJson state = lastState.Copy();

            if (previousForecast?.GameResultType == GameResultType.Expectation)
                return state;

            int index = currentForecast.ThreadNumber;

            if (allowIncreaseBet)
                SetNewInitialBetValue(state, settings);

            if (previousForecast == null)
            {
                state.Bets[index] = betValue ?? GetBetValue(state.InitialBet, state.LoseValues[index], currentForecast.Coefficient,
                    settings?.BetValueRoundDecimals ?? 2);

                return state;
            }

            decimal result = 
                CalculateResult(state.Bets[index], previousForecast.GameResultType, previousForecast.Coefficient);
            
            if (result < 0)
            {
                state.LoseValues[index] += lastState.Bets[index];
            }
            else
            {
                state.Bank += lastState.Bets[index];

                if (previousForecast.GameResultType == GameResultType.Win)
                    state.LoseValues[index] = 0;
            }

            state.Bets[index] = betValue ?? GetBetValue(state.InitialBet, state.LoseValues[index], currentForecast.Coefficient,
                settings?.BetValueRoundDecimals ?? 2);

            state.Bank -= state.Bets[index];

            return state;
        }

        private void SetNewInitialBetValue(StateJson state, SettingsJson settings)
        {
            var options = new CalculateBetValueOptions
            {
                InitialBet = state.InitialBet,
                LowerBound = settings?.LowerBound,
                UpperBound = settings?.UpperBound,
                ThreadNumbers = settings?.ThreadNumbers ?? 4,
                IncreaseBetStep = settings?.BetValueIncreaseStep ?? 0.01M,
                CoefficientBankReserve = settings?.CoefficientBankReserve ?? 0,
                Bank = state.Bank,
                CalculationMethod = settings?.CalculationMethod ?? CalculationMethod.Max
            };

            state.InitialBet = CalculateBetValue(options);
        }

        private decimal GetBetValue(decimal initialBet, decimal loseValue, double coefficient, int countOfRoundDecimals = 2)
        {
            decimal betValue = (initialBet + loseValue) / (decimal) (coefficient - 1);

            betValue += 5 / (decimal)Math.Pow(10, countOfRoundDecimals);

            betValue = Math.Round(betValue, countOfRoundDecimals);

            return betValue;
        }

        private decimal CalculateBankValue(List<ForecastJson> forecasts, decimal bet, 
            double coefficientBankReserve, int threadNumbers)
        {
            var lastState = new StateJson
            {
                InitialBet = bet,
                Bets = Enumerable.Repeat(bet, threadNumbers).ToList()
            };

            List<ForecastJson> previousForecasts = Enumerable.Repeat((ForecastJson) null, threadNumbers).ToList();

            decimal maxBank = 0;

            foreach (ForecastJson forecast in forecasts)
            {
                StateJson state =
                    CalculateNextAlgorithmState(forecast, previousForecasts[forecast.ThreadNumber], lastState);

                decimal neededValue = state.Bets.Sum() + state.LoseValues.Sum();
                neededValue += neededValue * (decimal) coefficientBankReserve;
                
                if (maxBank < neededValue)
                    maxBank = neededValue;

                lastState = state;
                previousForecasts[forecast.ThreadNumber] = forecast;
            }

            return maxBank;
        }

        private decimal CalculateResult(decimal betValue, GameResultType forecastGameResultType,
            double forecastCoefficient)
        {
            switch (forecastGameResultType)
            {
                case GameResultType.Expectation:
                    return betValue;
                case GameResultType.Win:
                    return betValue * (decimal) forecastCoefficient;
                case GameResultType.Defeat:
                    return betValue * -1;
                case GameResultType.RefundOrCancellation:
                    return betValue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(forecastGameResultType), forecastGameResultType, null);
            }
        }
    }
}