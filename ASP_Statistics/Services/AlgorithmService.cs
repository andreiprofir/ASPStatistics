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
            ForecastJson forecast = _dataService.GetForecastBy(forecastId);
            ForecastJson previousForecast = _dataService.GetLastCalculatedForecastByIndex(forecast.ThreadNumber);

            return CalculateNextAlgorithmState(forecast, previousForecast);
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

        private StateJson CalculateNextAlgorithmState(ForecastJson currentForecast, 
            ForecastJson previousForecast, StateJson lastState = null)
        {
            if (previousForecast.GameResultType == GameResultType.Expectation) return null;

            if (lastState == null)
                lastState = _dataService.GetLastState();

            StateJson state = lastState.Copy();

            int index = currentForecast.ThreadNumber;

            firstBetValue = CalculateNextBetValue(tempBank, firstBetValue);

            if (CalculateMaxBankValue(firstBetValue + 0.1M) < tempBank)
                firstBetValue += 0.1M;

            decimal result = 
                CalculateResult(state.Bets[index], previousForecast.GameResultType, previousForecast.Coefficient);
            
            if (result < 0)
            {
                state.LoseValues[index] += lastState.Bets[index];

                decimal betValue =
                    (state.InitialBet + state.LoseValues[index]) / (decimal) (currentForecast.Coefficient);

                state.Bets[index] = Math.Round(betValue + 0.005M, 2);
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