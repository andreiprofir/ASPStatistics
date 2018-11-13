using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<StateJson> CalculateNextStateAsync(CurrentForecast currentForecast, PreviousForecast previousForecast)
        {
            return await CalculateNextAlgorithmStateAsync(currentForecast, previousForecast);
        }

        private async Task<StateJson> CalculateNextAlgorithmStateAsync(CurrentForecast currentForecast, PreviousForecast previousForecast, StateJson previousState = null)
        {
            if (previousState == null)
                previousState = await GetPreviousStateAsync(previousForecast.Id) ?? new StateJson();

            var state = new StateJson
            {
                Id = DateTimeOffset.Now.ToUnixTimeSeconds(),
                InitialBet = previousState.InitialBet,

            };

            
                //decimal tempBank = bank;

                //for (int j = 0; j < index; j++)
                //{
                //    tempBank -= oneBetValues[j];
                //}

                //firstBetValue = CalculateNextBetValue(tempBank, firstBetValue);

                //if (CalculateMaxBankValue(firstBetValue + 0.1M) < tempBank)
                //    firstBetValue += 0.1M;

                
                    

                    
                    decimal result = CalculateResult(oneBetValues[j], forecast.GameResultType, forecast.Coefficient);

                    //bank -= oneBetValues[j];
                    //currentBank -= oneBetValues[j];

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
                    }
                
            

            return state;
        }

        private async Task<StateJson> GetPreviousStateAsync(long forecastId)
        {
            List<StateJson> states = await _dataService.GetAlgorithmStatesAsync();

            return states.FirstOrDefault(x => x.ForecastId == forecastId);
        }
    }
}