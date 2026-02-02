using Microsoft.IdentityModel.Tokens;
using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Application.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PlayerBonusApi.Application.Services;

public sealed class AuthService(IConfiguration config) : IAuthService
{
    private readonly IConfiguration _config = config;

    public DevTokenResponse CreateDevToken(DevTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.UserName))
            throw new ArgumentException("UserId and UserName are required.");

        var jwtSection = _config.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, request.UserId),
            new(ClaimTypes.Name, request.UserName),
            new(JwtRegisteredClaimNames.Sub, request.UserId),
            new("name", request.UserName),
        };

        if (!string.IsNullOrWhiteSpace(request.Role))
            claims.Add(new Claim(ClaimTypes.Role, request.Role));

        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new DevTokenResponse(tokenString, expires);
    }
}