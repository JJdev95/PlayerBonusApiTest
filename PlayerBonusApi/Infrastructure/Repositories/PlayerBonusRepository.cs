using Microsoft.EntityFrameworkCore;
using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Domain.Entities;
using PlayerBonusApi.Domain.Enums;
using PlayerBonusApi.Infrastructure.Persistence;

namespace PlayerBonusApi.Infrastructure.Repositories;

public sealed class PlayerBonusRepository(AppDbContext db) : IPlayerBonusRepository
{
    private readonly AppDbContext _db = db;

    public async Task<PagedResult<PlayerBonus>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200; // max limit

        var query = _db.PlayerBonuses
            .AsNoTracking()
            .Include(x => x.Player)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<PlayerBonus>.Create(items, page, pageSize, totalCount);
    }

    public Task<PlayerBonus?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _db.PlayerBonuses
            .Include(x => x.Player)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<bool> ExistsActiveBonusAsync(int playerId, BonusType bonusType, CancellationToken ct = default)
    {
        return _db.PlayerBonuses
            .AsNoTracking()
            .AnyAsync(x =>
                x.PlayerId == playerId &&
                x.BonusType == bonusType &&
                x.IsActive, ct);
    }

    public async Task AddAsync(PlayerBonus entity, CancellationToken ct = default)
    {
        await _db.PlayerBonuses.AddAsync(entity, ct);
    }

    public void Update(PlayerBonus entity)
    {
        _db.PlayerBonuses.Update(entity);
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.PlayerBonuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return;

        entity.IsDeleted = true;
        entity.IsActive = false;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}