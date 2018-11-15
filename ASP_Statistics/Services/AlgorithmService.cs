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

                return CalculateNextAlgorithmState(forecast, previousForecast,
                    roundDecimals: settings.BetValueRoundDecimals, allowIncreaseBet: allowIncreaseBet);
            });
        }

        public async Task<Dictionary<Month, decimal>> GetCalculatedBankValuesByBetAsync(SettingsJson settings = null)
        {
            return await Task.Run(() => CalculateBankValues(settings ?? _dataService.GetSettings()));
        }

        public async Task<decimal> CalculateBetValueByBankAsync(SettingsJson settings = null)
        {
            return await Task.Run(() => CalculateBetValue(settings));
        }

        private decimal CalculateBetValue(SettingsJson settings)
        {
            settings = settings.Copy() ?? _dataService.GetSettings();
            
            settings.InitialBank -= settings.InitialBank * (decimal) settings.CoefficientBankReserve;
            settings.InitialBetValue += settings.BetValueIncreaseStep;
            
            while (CalculateBankValues(settings).All(x => x.Value < settings.InitialBank)) 
                settings.InitialBetValue += settings.BetValueIncreaseStep;

            return settings.InitialBetValue - settings.BetValueIncreaseStep;
        }

        private StateJson CalculateNextAlgorithmState(ForecastJson currentForecast, 
            ForecastJson previousForecast, StateJson lastState = null, int roundDecimals = 2, bool allowIncreaseBet = false)
        {
            if (lastState == null)
                lastState = _dataService.GetLastState();

            StateJson state = lastState.Copy();

            if (previousForecast == null || previousForecast.GameResultType == GameResultType.Expectation)
                return lastState?.Copy();

            int index = currentForecast.ThreadNumber;

            if (allowIncreaseBet)
            {
                SettingsJson settings = _dataService.GetSettings();
                settings.InitialBetValue = state.InitialBet;

                state.InitialBet = CalculateBetValue(settings);
                settings.InitialBetValue = state.InitialBet;
            }

            decimal result = 
                CalculateResult(state.Bets[index], previousForecast.GameResultType, previousForecast.Coefficient);
            
            if (result < 0)
            {
                state.LoseValues[index] += lastState.Bets[index];

                decimal betValue =
                    (state.InitialBet + state.LoseValues[index]) / (decimal) (currentForecast.Coefficient);

                betValue += 5 / (decimal)Math.Pow(10, roundDecimals);

                state.Bets[index] = Math.Round(betValue, roundDecimals);
            }
            else
            {
                state.Bank += lastState.Bets[index];

                if (previousForecast.GameResultType == GameResultType.Win)
                {
                    state.LoseValues[index] = 0;
                    state.Bets[index] = state.InitialBet;
                }
            }

            state.Bank -= state.Bets[index];

            return state;
        }

        private Dictionary<Month, decimal> CalculateBankValues(SettingsJson settings)
        {
            List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
            {
                LowerBound = settings.LowerBound, 
                UpperBound = settings.UpperBound
            });

            var results = new Dictionary<Month, decimal>();
            
            decimal bank = CalculateBankValue(forecasts, settings.InitialBetValue, 
                settings.CoefficientBankReserve, settings.ThreadNumbers);

            results[Month.All] = Math.Ceiling(bank);

            foreach (var group in forecasts.GroupBy(x => x.GameAt.Month))
            {
                bank = CalculateBankValue(group.ToList(), settings.InitialBetValue, 
                    settings.CoefficientBankReserve, settings.ThreadNumbers);

                results[(Month) group.Key] = Math.Ceiling(bank);
            }

            return results;
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