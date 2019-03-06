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
                LoseValues = state.LoseValues.ToList(),
                LoseNumbers = state.LoseNumbers.ToList()
            };
        }

        public static SettingsJson Copy(this SettingsJson settings)
        {
            if (settings == null)
                return null;

            return new SettingsJson
            {
                ThreadNumbers = settings.ThreadNumbers,
                InitialBetValue = settings.InitialBetValue,
                InitialBank = settings.InitialBank,
                BetValueIncreaseStep = settings.BetValueIncreaseStep,
                BetValueRoundDecimals = settings.BetValueRoundDecimals,
                CoefficientBankReserve = settings.CoefficientBankReserve,
                LowerBound = settings.LowerBound,
                UpperBound = settings.UpperBound,
                CalculationMethod = settings.CalculationMethod,
                AllowIncreaseBetValue = settings.AllowIncreaseBetValue,
                IncreaseBetValueWhenDefeat = settings.IncreaseBetValueWhenDefeat
            };
        }
    }
}