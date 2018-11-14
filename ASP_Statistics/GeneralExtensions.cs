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
                Id = DateTimeOffset.Now.Millisecond,
                Bank = state.Bank,
                InitialBet = state.InitialBet,
                Bets = state.Bets.ToList(),
                LoseValues = state.LoseValues.ToList()
            };
        }
    }
}