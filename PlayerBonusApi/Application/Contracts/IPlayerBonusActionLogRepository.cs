using PlayerBonusApi.Domain.Entities;

namespace PlayerBonusApi.Application.Contracts;

public interface IPlayerBonusActionLogRepository
{
    Task AddAsync(PlayerBonusActionLog log, CancellationToken ct = default);
}