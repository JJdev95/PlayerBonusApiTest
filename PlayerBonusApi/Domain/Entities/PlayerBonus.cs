using PlayerBonusApi.Domain.Enums;

namespace PlayerBonusApi.Domain.Entities;

public sealed class PlayerBonus : AuditableEntity
{
    public required int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public required BonusType BonusType { get; set; }
    public required decimal Amount { get; set; }
    public required bool IsActive { get; set; } = true;
}