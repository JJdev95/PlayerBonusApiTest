using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Domain.Entities;
using PlayerBonusApi.Infrastructure.Persistence;

namespace PlayerBonusApi.Infrastructure.Repositories;

public sealed class PlayerBonusActionLogRepository(AppDbContext db) : IPlayerBonusActionLogRepository
{
    private readonly AppDbContext _db = db;

    public Task AddAsync(PlayerBonusActionLog log, CancellationToken ct = default)
        => _db.PlayerBonusActionLogs.AddAsync(log, ct).AsTask();
}
