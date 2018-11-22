using System;
using System.ComponentModel.DataAnnotations;
using ASP_Statistics.Enums;
using ASP_Statistics.JsonModels;

namespace ASP_Statistics.Models
{
    public class ForecastViewModel
    {
        public long Id { get; set; }

        public double Coefficient { get; set; }

        public string Bet { get; set; }

        public GameResultType GameResultType { get; set; }

        public DateTimeOffset GameAt { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTimeOffset ShowAt { get; set; }

        public string TournamentName { get; set; }

        public Sport Sport { get; set; }

        public string CountryName { get; set; }
        
        public string GameTeams { get; set; }

        public decimal BetValue { get; set; }

        public int? ThreadNumber { get; set; }

        public bool SaveBet { get; set; }
    }
}