namespace PlayerBonusApi.Domain.Entities;

public sealed class Player : AuditableEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }

    // navigation
    public ICollection<PlayerBonus> Bonuses { get; set; } = [];
}
