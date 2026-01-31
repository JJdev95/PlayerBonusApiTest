using Microsoft.EntityFrameworkCore;
using PlayerBonusApi.Domain.Entities;

namespace PlayerBonusApi.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlayerBonus> PlayerBonuses => Set<PlayerBonus>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerBonusActionLog> PlayerBonusActionLogs => Set<PlayerBonusActionLog>();

    public override int SaveChanges()
    {
        ApplyAuditRules();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerBonus>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<PlayerBonus>(b =>
        {
            b.ToTable("player_bonuses");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.BonusType).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();

            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.IsDeleted).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();

            b.HasOne(x => x.Player)
                .WithMany(x => x.Bonuses)
                .HasForeignKey(x => x.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enforce one active bonus per type per player 
            b.HasIndex(x => new { x.PlayerId, x.BonusType })
                .IsUnique()
                .HasFilter(@"""IsActive"" = TRUE AND ""IsDeleted"" = FALSE");
        });

        modelBuilder.Entity<Player>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Player>(p =>
        {
            p.ToTable("players");

            p.HasKey(x => x.Id);
            p.Property(x => x.Id).ValueGeneratedOnAdd();

            p.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            p.Property(x => x.Name)
                .HasMaxLength(120)
                .IsRequired();

            p.Property(x => x.Email)
                .HasMaxLength(255)
                .IsRequired();

            p.Property(x => x.IsDeleted).IsRequired();
            p.Property(x => x.CreatedAt).IsRequired();

            p.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<PlayerBonusActionLog>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<PlayerBonusActionLog>(l =>
        {
            l.ToTable("player_bonus_action_logs");

            l.HasKey(x => x.Id);
            l.Property(x => x.Id).ValueGeneratedOnAdd();

            l.Property(x => x.PlayerBonusId).IsRequired();
            l.Property(x => x.ActionType).IsRequired();

            l.Property(x => x.OperatorUserId).HasMaxLength(100).IsRequired();
            l.Property(x => x.OperatorUserName).HasMaxLength(200).IsRequired();
            l.Property(x => x.Note).HasMaxLength(500);

            l.Property(x => x.IsDeleted).IsRequired();
            l.Property(x => x.CreatedAt).IsRequired();

            l.HasOne(x => x.PlayerBonus)
                .WithMany()
                .HasForeignKey(x => x.PlayerBonusId)
                .OnDelete(DeleteBehavior.Restrict);

            l.HasIndex(x => x.PlayerBonusId);
            l.HasIndex(x => x.ActionType);
        });

    }

    private void ApplyAuditRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}