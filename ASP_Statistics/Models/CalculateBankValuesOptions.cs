using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class CalculateBankValuesOptions
    {
        public decimal Bet { get; set; }

        public double CoefficientBankReserve { get; set; } = 0;
        
        public int ThreadNumbers { get; set; } = 4;
    }
}