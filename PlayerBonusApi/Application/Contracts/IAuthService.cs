using PlayerBonusApi.Application.Dtos;

namespace PlayerBonusApi.Application.Contracts;

public interface IAuthService
{
    DevTokenResponse CreateDevToken(DevTokenRequest request);
}