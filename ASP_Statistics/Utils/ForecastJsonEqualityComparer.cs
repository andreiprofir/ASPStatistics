using System.Collections.Generic;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics.Utils
{
    public class ForecastJsonEqualityComparer : IEqualityComparer<ForecastJson>
    {
        public bool Equals(ForecastJson x, ForecastJson y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(ForecastJson obj)
        {
            return base.GetHashCode();
        }
    }
}