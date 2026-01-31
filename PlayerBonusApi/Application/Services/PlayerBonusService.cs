using AutoMapper;
using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Application.Dtos;
using PlayerBonusApi.Common.Errors;
using PlayerBonusApi.Domain.Entities;
using PlayerBonusApi.Domain.Enums;

namespace PlayerBonusApi.Application.Services;

public sealed class PlayerBonusService(
    IPlayerBonusRepository bonuses,
    IPlayerBonusActionLogRepository logs,
    ICurrentUserService currentUser,
    IMapper mapper) : IPlayerBonusService
{
    private readonly IPlayerBonusRepository _bonuses = bonuses;
    private readonly IPlayerBonusActionLogRepository _logs = logs;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMapper _mapper = mapper;

    public async Task<PagedResult<BonusDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _bonuses.GetAllPagedAsync(page, pageSize, ct);

        return PagedResult<BonusDto>.Create(
            items: _mapper.Map<IReadOnlyList<BonusDto>>(result.Items),
            page: result.Page,
            pageSize: result.PageSize,
            totalCount: result.TotalCount
        );
    }

    public async Task<BonusDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _bonuses.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"Bonus with id={id} not found.");
        return _mapper.Map<BonusDto>(entity);
    }

    public async Task<BonusDto> CreateAsync(int playerId, BonusType bonusType, decimal amount, CancellationToken ct = default)
    {
        var exists = await _bonuses.ExistsActiveBonusAsync(playerId, bonusType, ct);
        if (exists)
            throw new ApiException(StatusCodes.Status409Conflict,
                "Player already has an active bonus of this type.");

        var entity = new PlayerBonus
        {
            PlayerId = playerId,
            BonusType = bonusType,
            Amount = amount,
            IsActive = true,
            IsDeleted = false
        };

        await _bonuses.AddAsync(entity, ct);
        await _bonuses.SaveChangesAsync(ct);

        var bonusWithPlayerToReturn = await _bonuses.GetByIdAsync(entity.Id, ct) ?? entity;

        return _mapper.Map<BonusDto>(bonusWithPlayerToReturn);
    }

    public async Task<BonusDto> UpdateAsync(int id, decimal amount, bool isActive, CancellationToken ct = default)
    {
        var entity = await _bonuses.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"Bonus with id={id} not found.");
        if (isActive && !entity.IsActive)
        {
            var exists = await _bonuses.ExistsActiveBonusAsync(entity.PlayerId, entity.BonusType, ct);
            if (exists)
                throw new ApiException(StatusCodes.Status409Conflict,
                    "Player already has an active bonus of this type.");
        }

        entity.Amount = amount;
        entity.IsActive = isActive;

        _bonuses.Update(entity);

        await _logs.AddAsync(new PlayerBonusActionLog
        {
            PlayerBonusId = entity.Id,
            ActionType = BonusActionType.Updated,
            OperatorUserId = _currentUser.UserId,
            OperatorUserName = _currentUser.UserName,
            Note = "Bonus updated"
        }, ct);

        await _bonuses.SaveChangesAsync(ct);

        var withNav = await _bonuses.GetByIdAsync(entity.Id, ct) ?? entity;
        return _mapper.Map<BonusDto>(withNav);
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _bonuses.GetByIdAsync(id, ct);
        if (entity is null)
            return;

        await _bonuses.SoftDeleteAsync(id, ct);

        await _logs.AddAsync(new PlayerBonusActionLog
        {
            PlayerBonusId = entity.Id,
            ActionType = BonusActionType.Deleted,
            OperatorUserId = _currentUser.UserId,
            OperatorUserName = _currentUser.UserName,
            Note = "Bonus deleted"
        }, ct);

        await _bonuses.SaveChangesAsync(ct);
    }
}