﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;

namespace ASP_Statistics.Services
{
    public interface IAlgorithmService
    {
        Task<StateJson> CalculateNextStateAsync(long forecastId, bool allowIncreaseBet = true);

        Task<Dictionary<Month, decimal>> GetCalculatedBankValuesByBetAsync(CalculateBankValuesOptions options,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);

        Task<Dictionary<CalculationMethod, decimal>> GetBankValuesByBetAsync(CalculateBankValuesOptions options,
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);

        Task<decimal> CalculateBetValueByBankAsync(CalculateBetValueOptions options);

        Task<Dictionary<int, List<WinLoseCountModel>>> GetWinLoseCountByThreadNumber(
            DateTimeOffset? lowerBound = null, DateTimeOffset? upperBound = null);
    }
}