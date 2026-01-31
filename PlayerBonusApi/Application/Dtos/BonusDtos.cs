using PlayerBonusApi.Domain.Enums;

namespace PlayerBonusApi.Application.Dtos;

public sealed record CreateBonusRequest(
    int PlayerId,
    BonusType BonusType,
    decimal Amount
);

public sealed record UpdateBonusRequest(
    decimal Amount,
    bool IsActive
);

public sealed record BonusDto(
    int Id,
    int PlayerId,
    string PlayerName,
    string PlayerEmail,
    BonusType BonusType,
    decimal Amount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
