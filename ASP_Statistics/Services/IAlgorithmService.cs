using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Services
{
    public interface IAlgorithmService
    {
        Task<StateJson> CalculateNextStateAsync(long forecastId, decimal? betValue = null, bool? allowIncreaseBet = null);

        Task<Dictionary<int, Dictionary<Month, decimal>>> GetCalculatedBankValuesByBetAsync(CalculateBankValuesOptions options,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);

        Task<Dictionary<CalculationMethod, decimal>> GetBankValuesByMethodsAsync(CalculateBankValuesOptions options,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);

        Task<decimal> GetBankValueByBetAndMethodAsync(CalculateBankValuesOptions options, CalculationMethod calculationMethod,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);

        Task<decimal> CalculateBetValueByBankAsync(CalculateBetValueOptions options);

        Task<List<WinLoseCountModel>> GetWinLoseCountByThreadNumberAsync(List<ForecastJson> forecasts,
            int threadNumbers);

        Task<List<decimal>> GetDefeatChainBets(decimal bet, double coefficient = 2.1D);

        Task<List<StateJson>> CalculateStatesAsync(List<ForecastJson> forecasts, decimal initialBank, decimal bet,
            int threadNumbers, bool allowIncreaseBets);
    }
}