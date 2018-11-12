using System;
using System.Collections.Generic;
using System.Linq;

namespace ASP_Statistics.Classes
{
    public static class Helper
    {
        public static List<decimal> GetBetChain(decimal bet, decimal coefficient, int countOfSteps = 10, bool isCeiling = true)
        {
            var result = new List<decimal>();

            for (int i = 0; i < countOfSteps; i++)
            {
                decimal newBet = (bet + result.Sum()) / (coefficient - 1);

                if (isCeiling)
                    newBet = Math.Ceiling(newBet);

                result.Add(newBet);
            }

            return result;
        }
    }
}