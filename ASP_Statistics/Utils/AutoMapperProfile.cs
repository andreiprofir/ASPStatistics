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
        }
    }
}