using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class WinLoseCountModel
    {
        public GameResultType GameResultType { get; set; }

        public int Count { get; set; }
    }
}