using AutoMapper;
using Microsoft.Extensions.Logging;
using PlayerBonusApi.Application.Mappings;

namespace PlayerBonusApi.Tests.TestUtils;

public static class AutoMapperFactory
{
    public static IMapper Create()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });

        var config = new MapperConfiguration(
            cfg =>
            {
                cfg.AddMaps(typeof(BonusMappingProfile).Assembly);
            },
            loggerFactory
        );

        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }
}