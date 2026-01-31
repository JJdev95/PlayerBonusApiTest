using PlayerBonusApi.Domain.Entities;
using PlayerBonusApi.Domain.Enums;

namespace PlayerBonusApi.Application.Contracts;

public interface IPlayerBonusRepository
{
    Task<PagedResult<PlayerBonus>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<PlayerBonus?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsActiveBonusAsync(int playerId, BonusType bonusType, CancellationToken ct = default);

    Task AddAsync(PlayerBonus entity, CancellationToken ct = default);

    void Update(PlayerBonus entity);

    Task SoftDeleteAsync(int id, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
