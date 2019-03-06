using System.Collections.Generic;
using System.Linq;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class StateViewModel
    {
        public decimal Bank { get; set; } 

        public decimal InitialBet { get; set; }

        public List<decimal> Bets { get; set; }

        public List<decimal> LoseValues { get; set; }

        public List<int> LoseNumbers { get; set; }

        public int ThreadNumbers { get; set; }

        public Dictionary<RepresentsValueType, decimal> BetValueLimits { get; set; }

        public Dictionary<RepresentsValueType, decimal> BankValueLimits { get; set; }

        public Dictionary<GameResultType, double> CoefficientAverages { get; set; }
    }
}