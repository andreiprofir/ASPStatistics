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

        Task<Dictionary<Month, decimal>> GetCalculatedBankValuesByBetAsync(SettingsJson settings = null);

        Task<decimal> CalculateBetValueByBankAsync(SettingsJson settings = null);
    }
}