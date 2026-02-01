using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Application.Mappings;
using PlayerBonusApi.Application.Services;
using PlayerBonusApi.Common.Errors;
using PlayerBonusApi.Infrastructure.Persistence;
using PlayerBonusApi.Infrastructure.Repositories;
using System.Text;

namespace PlayerBonusApi.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IPlayerBonusRepository, PlayerBonusRepository>();
        services.AddScoped<IPlayerBonusActionLogRepository, PlayerBonusActionLogRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPlayerBonusService, PlayerBonusService>();

        services.AddAutoMapper(cfg =>
        {
            cfg.LicenseKey = "FREE-KEY";
        }, typeof(BonusMappingProfile).Assembly);

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

                    ClockSkew = TimeSpan.FromSeconds(60)
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "PlayerBonus API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste JWT token here: Bearer {token}"
                });

                c.AddSecurityRequirement(d => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", d)] = []
                });
            });
        return services;
    }
}
