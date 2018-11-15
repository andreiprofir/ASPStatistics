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
        
        public StateJson CalculateNextState(long forecastId)
        {
            //todo: previous de aici poate veni NULL
            ForecastJson forecast = _dataService.GetForecastBy(forecastId);
            ForecastJson previousForecast = _dataService.GetLastCalculatedForecastByIndex(forecast.ThreadNumber);
            SettingsJson settings = _dataService.GetSettings();

            return CalculateNextAlgorithmState(forecast, previousForecast,
                roundDecimals: settings.BetValueRoundDecimals);
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

        //previous poate veni ca null
        private StateJson CalculateNextAlgorithmState(ForecastJson currentForecast, 
            ForecastJson previousForecast, StateJson lastState = null, int roundDecimals = 2, bool allowIncreaseBet = false)
        {
            if (previousForecast.GameResultType == GameResultType.Expectation) return null;

            if (lastState == null)
                lastState = _dataService.GetLastState();

            StateJson state = lastState.Copy();

            int index = currentForecast.ThreadNumber;
            
            //todo
            //firstBetValue = CalculateNextBetValue(tempBank, firstBetValue);

            //if (CalculateMaxBankValue(firstBetValue + 0.1M) < tempBank)
            //    firstBetValue += 0.1M;

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

        public decimal CalculateNextBetValue(decimal bank, decimal initialBet = 0M, decimal step = 0.1M,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            decimal bet = initialBet;

            while (CheckIfAllowChangeBetValue(bet, bank, 4, step, lowerBound, upperBound))
                bet += step;

            return bet;
        }

        public Dictionary<Month, decimal> CalculateBankValuesByPeriod(decimal bet, double coefficientBankReserve = 0, 
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null)
        {
            return CalculateBankValues(bet, coefficientBankReserve, lowerBound, upperBound);
        }

        private Dictionary<Month, decimal> CalculateBankValues(decimal bet, double coefficientBankReserve, DateTimeOffset? lowerBound, DateTimeOffset? upperBound)
        {
            List<ForecastJson> forecasts = _dataService.GetResults(new FilterParameters
            {
                LowerBound = lowerBound, 
                UpperBound = upperBound
            });

            var results = new Dictionary<Month, decimal>();
            
            decimal bank = CalculateBankValue(forecasts, bet, coefficientBankReserve);

            results[Month.All] = Math.Ceiling(bank);

            foreach (var group in forecasts.GroupBy(x => x.GameAt.Month))
            {
                bank = CalculateBankValue(group.ToList(), bet, coefficientBankReserve);

                results[(Month) group.Key] = Math.Ceiling(bank);
            }

            return results;
        }

        private decimal CalculateBankValue(List<ForecastJson> forecasts, decimal bet, double coefficientBankReserve)
        {
            var lastState = new StateJson
            {
                InitialBet = bet,
                Bets = Enumerable.Repeat(bet, 4).ToList()
            };

            decimal maxBank = lastState.Bets.Sum();
            maxBank += maxBank * (decimal) coefficientBankReserve;

            foreach (ForecastJson forecast in forecasts)
            {
                StateJson state = CalculateNextAlgorithmState(forecast, forecast, lastState);

                decimal neededValue = state.Bets.Sum() + state.LoseValues.Sum();
                neededValue += neededValue * (decimal) coefficientBankReserve;
                
                if (maxBank < neededValue)
                    maxBank = neededValue;

                lastState = state;
            }

            return maxBank;
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
            //herneaua asta nado pererabotati

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
    }
}