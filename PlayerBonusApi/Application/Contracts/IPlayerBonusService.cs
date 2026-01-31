
using PlayerBonusApi.Application.Dtos;
using PlayerBonusApi.Domain.Enums;

namespace PlayerBonusApi.Application.Contracts;

public interface IPlayerBonusService
{
    Task<PagedResult<BonusDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<BonusDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<BonusDto> CreateAsync(int playerId, BonusType bonusType, decimal amount, CancellationToken ct = default);
    Task<BonusDto> UpdateAsync(int id, decimal amount, bool isActive, CancellationToken ct = default);
    Task SoftDeleteAsync(int id, CancellationToken ct = default);
}