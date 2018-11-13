using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class PreviousForecast
    {
        public long Id { get; set; }

        public double Coefficient { get; set; }

        public GameResultType GameResultType { get; set; }

        public int? ThreadNumber { get; set; }
    }
}