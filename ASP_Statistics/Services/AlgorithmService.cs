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
        
        public async Task<StateJson> CalculateNextStateAsync(long forecastId, bool allowIncreaseBet = true)
        {
            return await Task.Run(() =>
            {
                ForecastJson forecast = _dataService.GetForecastBy(forecastId);
                ForecastJson previousForecast = _dataService.GetLastCalculatedForecastByIndex(forecast.ThreadNumber);
                SettingsJson settings = _dataService.GetSettings();

                return CalculateNextAlgorithmState(forecast, previousForecast, settings: settings,
                    allowIncreaseBet: allowIncreaseBet);
            });
        }

        public async Task<Dictionary<Month, decimal>> GetCalculatedBankValuesByBetAsync(CalculateBankValuesOptions options, 
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
            {
                LowerBound = lowerBound, 
                UpperBound = upperBound
            });

            return await Task.Run(() => CalculateBankValues(forecasts, options));
        }

        public async Task<Dictionary<CalculationMethod, decimal>> GetBankValuesByBetAsync(CalculateBankValuesOptions options, 
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            return await Task.Run(() =>
            {
                List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
                {
                    LowerBound = lowerBound, 
                    UpperBound = upperBound
                });

                return GetBankValuesByMethod(forecasts, options);
            });
        }

        private Dictionary<CalculationMethod, decimal> GetBankValuesByMethod(List<ForecastJson> forecasts,
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

        public async Task<Dictionary<int, List<WinLoseCountModel>>> GetWinLoseCountByThreadNumber(
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            return await Task.Run(() => CalculateWinLoseCountByThreads(lowerBound, upperBound));
        }

        private Dictionary<int, List<WinLoseCountModel>> CalculateWinLoseCountByThreads(DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            var result = new Dictionary<int, List<WinLoseCountModel>>();

            List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
            {
                LowerBound = lowerBound,
                UpperBound = upperBound
            });

            foreach (var group in forecasts.GroupBy(x => x.ThreadNumber))
            {
                List<WinLoseCountModel> counts = GetWinLoseCount(group.ToList());

                result[group.Key] = counts;
            }

            return result;
        }

        private List<WinLoseCountModel> GetWinLoseCount(List<ForecastJson> forecasts, bool separateCancelResult = false)
        {
            var result = new List<WinLoseCountModel>();

            GameResultType lastResultType =
                forecasts.FirstOrDefault()?.GameResultType ?? GameResultType.Expectation;

            var count = 1;

            foreach (ForecastJson forecast in forecasts.Skip(1))
            {
                if ((!separateCancelResult && forecast.GameResultType == GameResultType.RefundOrCancellation) || 
                    forecast.GameResultType == GameResultType.Expectation)
                    continue;

                if (lastResultType == forecast.GameResultType)
                {
                    count++;
                }
                else
                {
                    result.Add(new WinLoseCountModel
                    {
                        GameResultType = lastResultType,
                        Count = count
                    });
                    
                    lastResultType = forecast.GameResultType;
                    count = 1;
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

            var bankOptions = new CalculateBankValuesOptions
            {
                ThreadNumbers = options.ThreadNumbers,
                Bet = options.InitialBet + options.IncreaseBetStep,
                CoefficientBankReserve = options.CoefficientBankReserve
            };

            while (GetBankValuesByMethod(forecasts, bankOptions)[options.CalculationMethod] < options.Bank)
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
            StateJson lastState = null, SettingsJson settings = null, bool allowIncreaseBet = false)
        {
            if (lastState == null)
                lastState = _dataService.GetLastState() ?? new StateJson();

            StateJson state = lastState.Copy();

            if (previousForecast?.GameResultType == GameResultType.Expectation)
                return state;

            int index = currentForecast.ThreadNumber;

            if (allowIncreaseBet)
                SetNewInitialBetValue(state, settings);

            if (previousForecast == null)
            {
                state.Bets[index] = GetBetValue(state.InitialBet, state.LoseValues[index], currentForecast.Coefficient,
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

            state.Bets[index] = GetBetValue(state.InitialBet, state.LoseValues[index], currentForecast.Coefficient,
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