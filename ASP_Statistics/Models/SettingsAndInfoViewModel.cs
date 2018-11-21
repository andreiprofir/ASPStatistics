using System.Collections;
using System.Collections.Generic;
using ASP_Statistics.Enums;

namespace ASP_Statistics.Models
{
    public class SettingsAndInfoViewModel
    {
        public double ChainCoefficient { get; set; } = 2.1;

        public decimal ChainBetValue { get; set; } = 1M;

        public SettingsViewModel Settings { get; set; }

        public StateViewModel LastState { get; set; }
    }
}