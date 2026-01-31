using System.Security.Claims;
using PlayerBonusApi.Application.Contracts;

namespace PlayerBonusApi.Infrastructure.Security;

public sealed class CurrentUserService(IHttpContextAccessor http) : ICurrentUserService
{
    private readonly IHttpContextAccessor _http = http;

    public string UserId => GetClaimValue(ClaimTypes.NameIdentifier)
                            ?? GetClaimValue("sub")
                            ?? "unknown";

    public string UserName => GetClaimValue(ClaimTypes.Name)
                              ?? GetClaimValue("name")
                              ?? GetClaimValue(ClaimTypes.Email)
                              ?? "unknown";

    private string? GetClaimValue(string type)
        => _http.HttpContext?.User?.FindFirst(type)?.Value;
}
