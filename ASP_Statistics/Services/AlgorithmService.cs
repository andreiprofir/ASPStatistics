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
            bool? allowIncreaseBet = null, StateJson lastState = null)
        {
            return await Task.Run(() =>
            {
                ForecastJson forecast = _dataService.GetForecastBy(forecastId);
                ForecastJson previousForecast = _dataService.GetLastCalculatedForecastByIndex(forecast.ThreadNumber);
                SettingsJson settings = _dataService.GetSettings();

                return CalculateNextAlgorithmState(forecast, previousForecast, lastState: lastState, settings: settings,
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
                }, false);

                foreach (var group in forecasts.GroupBy(x => x.GameAt.Year))
                {
                    Dictionary<Month, decimal> bankValues = CalculateBankValues(group.ToList(), options)
                        .ToDictionary(x => x.Key, y => y.Value.Bank);

                    result[group.Key] = bankValues;
                }

                return result;
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
                }, false);

                return GetBankValueByMethod(forecasts, options, calculationMethod, out long? _);
            });
        }

        private decimal GetBankValueByMethod(List<ForecastJson> forecasts,
            CalculateBankValuesOptions options, CalculationMethod calculationMethod, out long? forecastId)
        {
            Dictionary<Month, CalculateBankValueResult> bankValues = CalculateBankValues(forecasts, options);

            decimal bank;

            switch (calculationMethod)
            {
                case CalculationMethod.Average:
                    bank = bankValues.Average(x => x.Value.Bank);
                    forecastId = null;
                    break;
                case CalculationMethod.Min:
                    bank = bankValues.Min(x => x.Value.Bank);
                    break;
                case CalculationMethod.SecondMax:
                    decimal? bankResult = bankValues.OrderByDescending(x => x.Value.Bank)
                        .FirstOrDefault(x => x.Value.Bank < bankValues.Max(y => y.Value.Bank)).Value?.Bank;

                    bank = bankResult ?? bankValues.FirstOrDefault().Value?.Bank ?? 0;
                    break;
                default:
                    bank = bankValues.Max(x => x.Value.Bank);
                    break;
            }

            if (calculationMethod == CalculationMethod.Average)
                forecastId = null;
            else
                forecastId = bankValues.FirstOrDefault(x => x.Value.Bank == bank).Value?.ForecastId;

            return Math.Ceiling(bank);
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

        public async Task<List<StateJson>> CalculateStatesAsync(List<ForecastJson> forecasts, decimal initialBank, decimal bet, int threadNumbers, bool allowIncreaseBets)
        {
            return await Task.Run(() => CalculateStates(forecasts, initialBank, bet, threadNumbers, allowIncreaseBets));
        }

        private List<WinLoseCountModel> GetWinLoseCount(List<ForecastJson> forecasts, int threadNumbers)
        {
            var result = new List<WinLoseCountModel>();

            if (forecasts == null || !forecasts.Any()) return result;

            var lastResults = new List<GameResultType>(Enumerable.Repeat(GameResultType.Expectation, threadNumbers));
            var lastDates = new List<DateTimeOffset>(Enumerable.Repeat(DateTimeOffset.Now, threadNumbers));

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
            }, false);

            if (forecasts == null || !forecasts.Any()) return options.InitialBet;

            var bankOptions = new CalculateBankValuesOptions
            {
                ThreadNumbers = options.ThreadNumbers,
                Bet = options.InitialBet + options.IncreaseBetStep,
                CoefficientBankReserve = options.CoefficientBankReserve
            };

            decimal bank = GetBankValueByMethod(forecasts, bankOptions, 
                options.CalculationMethod, out long? forecastId);

            if (bank > options.Bank)
                return options.InitialBet;

            forecasts = GetForecastsLoseSeries(forecasts, forecastId, bankOptions.ThreadNumbers);

            do
            {
                options.InitialBet += options.IncreaseBetStep;

                bankOptions.Bet += options.IncreaseBetStep;
                bank = CalculateBankValue(forecasts, bankOptions.Bet, bankOptions.CoefficientBankReserve,
                    bankOptions.ThreadNumbers).Bank;
            } while (bank <= options.Bank);

            return options.InitialBet;
        }

        private List<ForecastJson> GetForecastsLoseSeries(List<ForecastJson> forecasts, long? checkPointForecastId, int threadNumbers)
        {
            ForecastJson forecast = forecasts.FirstOrDefault(x => x.Id == checkPointForecastId);

            if (forecast == null) return forecasts;

            var dates = new List<DateTimeOffset>();

            for (var i = 0; i < threadNumbers; i++)
            {
                int index = i;
                
                dates.Add(forecasts.LastOrDefault(x =>
                              x.ThreadNumber == index && x.GameResultType == GameResultType.Win &&
                              x.GameAt < forecast.GameAt.AddDays(-3))?.GameAt ?? forecast.GameAt);
                dates.Add(forecasts.FirstOrDefault(x =>
                              x.ThreadNumber == index && x.GameResultType == GameResultType.Win &&
                              x.GameAt > forecast.GameAt.AddDays(3))?.GameAt ?? forecast.GameAt);
            }

            DateTimeOffset minDate = dates.Min().AddDays(-3);
            DateTimeOffset maxDate = dates.Max().AddDays(3);

            return forecasts.Where(x => x.GameAt < maxDate && x.GameAt > minDate).ToList();
        }


        private Dictionary<Month, CalculateBankValueResult> CalculateBankValues(List<ForecastJson> forecasts, CalculateBankValuesOptions options)
        {
            var results = new Dictionary<Month, CalculateBankValueResult>();
            
            CalculateBankValueResult result = CalculateBankValue(forecasts, options.Bet, options.CoefficientBankReserve, options.ThreadNumbers);

            results[Month.All] = result;

            foreach (var group in forecasts.GroupBy(x => x.GameAt.Month))
            {
                result = CalculateBankValue(group.ToList(), options.Bet, options.CoefficientBankReserve, options.ThreadNumbers);

                results[(Month) group.Key] = result;
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
            state.ForecastId = currentForecast.Id;

            if (previousForecast?.GameResultType == GameResultType.Expectation)
                return state;

            int index = currentForecast.ThreadNumber;

            if (allowIncreaseBet)
                SetNewInitialBetValue(state, settings);

            if (previousForecast == null)
            {
                state.Bets[index] = betValue ?? GetBetValue(state.InitialBet, state.LoseValues[index], currentForecast.Coefficient,
                    settings?.BetValueRoundDecimals ?? 2);

                state.Bank -= state.Bets[index];

                return state;
            }

            decimal result = 
                CalculateResult(lastState.Bets[index], previousForecast.GameResultType, previousForecast.Coefficient);
            
            if (result < 0)
            {
                state.LoseValues[index] += lastState.Bets[index];
            }
            else
            {
                if (previousForecast.GameResultType == GameResultType.Expectation)
                    state.Bank += lastState.Bets[index];
                else
                    state.Bank += result;

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

        private CalculateBankValueResult CalculateBankValue(List<ForecastJson> forecasts, decimal bet, 
            double coefficientBankReserve, int threadNumbers)
        {
            List<StateJson> states = CalculateStates(forecasts, 0, bet, threadNumbers, false);

            StateJson maxState = states.OrderByDescending(x => x.Bets.Sum() + x.LoseValues.Sum()).FirstOrDefault();
            
            decimal maxBank = maxState?.Bets.Sum() + maxState?.LoseValues.Sum() ?? 0;
            
            maxBank += maxBank * (decimal) coefficientBankReserve;
            
            return new CalculateBankValueResult
            {
                Bank = Math.Ceiling(maxBank),
                ForecastId = maxState?.ForecastId
            };
        }

        private List<StateJson> CalculateStates(List<ForecastJson> forecasts, decimal initialBank, decimal bet, int threadNumbers, bool allowIncreaseBets)
        {
            var lastState = new StateJson
            {
                Bank = initialBank,
                InitialBet = bet,
                Bets = Enumerable.Repeat(0M, threadNumbers).ToList(),
                LoseValues = Enumerable.Repeat(0M, threadNumbers).ToList()
            };

            List<ForecastJson> previousForecasts = Enumerable.Repeat((ForecastJson) null, threadNumbers).ToList();
            var results = new List<StateJson>();
            SettingsJson settings = _dataService.GetSettings();
            
            foreach (ForecastJson forecast in forecasts)
            {
                StateJson state =
                    CalculateNextAlgorithmState(forecast, previousForecasts[forecast.ThreadNumber], lastState,
                        settings: settings, allowIncreaseBet: allowIncreaseBets);

                state.Id = forecast.GameAt.ToUnixTimeMilliseconds();

                results.Add(state);

                lastState = state;
                previousForecasts[forecast.ThreadNumber] = forecast;
            }

            return results;
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