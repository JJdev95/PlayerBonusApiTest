using AutoMapper;
using PlayerBonusApi.Application.Dtos;
using PlayerBonusApi.Domain.Entities;

namespace PlayerBonusApi.Application.Mappings;

public sealed class BonusMappingProfile : Profile
{
    public BonusMappingProfile()
    {
        CreateMap<PlayerBonus, BonusDto>()
            .ForMember(d => d.PlayerName, opt => opt.MapFrom(s => s.Player.Name))
            .ForMember(d => d.PlayerEmail, opt => opt.MapFrom(s => s.Player.Email));
    }
}
