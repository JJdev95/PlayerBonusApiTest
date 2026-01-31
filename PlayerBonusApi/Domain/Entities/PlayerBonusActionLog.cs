using PlayerBonusApi.Domain.Enums;

namespace PlayerBonusApi.Domain.Entities;

public sealed class PlayerBonusActionLog : AuditableEntity
{
    public int PlayerBonusId { get; set; }
    public PlayerBonus PlayerBonus { get; set; } = null!;
    public BonusActionType ActionType { get; set; }
    public required string OperatorUserId { get; set; }
    public required string OperatorUserName { get; set; }
    public string? Note { get; set; }
}