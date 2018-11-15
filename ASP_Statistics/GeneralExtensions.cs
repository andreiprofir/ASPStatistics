using System;
using System.Linq;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics
{
    public static class GeneralExtensions
    {
        public static StateJson Copy(this StateJson state)
        {
            return new StateJson
            {
                Id = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Bank = state.Bank,
                InitialBet = state.InitialBet,
                Bets = state.Bets.ToList(),
                LoseValues = state.LoseValues.ToList()
            };
        }

        public static SettingsJson Copy(this SettingsJson settings)
        {
            return new SettingsJson
            {
                ThreadNumbers = settings.ThreadNumbers,
                InitialBetValue = settings.InitialBetValue,
                InitialBank = settings.InitialBank,
                BetValueIncreaseStep = settings.BetValueIncreaseStep,
                BetValueRoundDecimals = settings.BetValueRoundDecimals,
                CoefficientBankReserve = settings.CoefficientBankReserve
            };
        }
    }
}