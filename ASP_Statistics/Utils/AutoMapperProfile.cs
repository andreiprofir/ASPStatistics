using ASP_Statistics.JsonModels;
using ASP_Statistics.Models;
using AutoMapper;

namespace ASP_Statistics.Utils
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<ForecastJson, ForecastViewModel>()
                .ForMember(dest => dest.GameTeams, opt => opt.MapFrom(src => src.Game.ToString()));
            CreateMap<ForecastViewModel, ForecastJson>();

            CreateMap<SettingsViewModel, CalculateBankValuesOptions>()
                .ForMember(dest => dest.Bet, opt => opt.MapFrom(src => src.InitialBetValue));

            CreateMap<SettingsViewModel, CalculateBetValueOptions>()
                .ForMember(dest => dest.Bank, opt => opt.MapFrom(src => src.InitialBank))
                .ForMember(dest => dest.InitialBet, opt => opt.MapFrom(src => src.InitialBetValue))
                .ForMember(dest => dest.IncreaseBetStep, opt => opt.MapFrom(src => src.BetValueIncreaseStep));

            CreateMap<StateJson, StateViewModel>().ReverseMap();
        }
    }
}