using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Application.Services;
using PlayerBonusApi.Common.Errors;
using PlayerBonusApi.Domain.Entities;
using PlayerBonusApi.Domain.Enums;
using PlayerBonusApi.Tests.TestUtils;

namespace PlayerBonusApi.Tests.Services;

public sealed class PlayerBonusServiceTests
{
    private readonly Mock<IPlayerBonusRepository> _bonusRepo = new();
    private readonly Mock<IPlayerBonusActionLogRepository> _logRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly IMapper _mapper = AutoMapperFactory.Create();

    private PlayerBonusService CreateService()
    {
        _currentUser.SetupGet(x => x.UserId).Returns("42");
        _currentUser.SetupGet(x => x.UserName).Returns("George Dev");

        return new PlayerBonusService(
            _bonusRepo.Object,
            _logRepo.Object,
            _currentUser.Object,
            _mapper);
    }


    // Add tests
    [Fact]
    public async Task CreateAsync_WhenNoActiveBonusExists_CreatesBonusAndReturnsDto()
    {
        // Arrange
        const int playerId = 1;
        const decimal amount = 50m;
        var bonusType = BonusType.Welcome;

        _bonusRepo
            .Setup(r => r.ExistsActiveBonusAsync(playerId, bonusType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _bonusRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new PlayerBonus
            {
                Id = id,
                PlayerId = playerId,
                Player = new Player { Id = playerId, Name = "Alice Johnson", Email = "alice.johnson@example.com" },
                BonusType = bonusType,
                Amount = amount,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-1),
            });

        PlayerBonus? added = null;

        _bonusRepo
            .Setup(r => r.AddAsync(It.IsAny<PlayerBonus>(), It.IsAny<CancellationToken>()))
            .Callback<PlayerBonus, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        _bonusRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (added is not null && added.Id == 0)
                    added.Id = 123;
            })
            .ReturnsAsync(1);

        var service = CreateService();

        // Act
        var dto = await service.CreateAsync(playerId, bonusType, amount);

        // Assert
        added.Should().NotBeNull();
        added!.PlayerId.Should().Be(playerId);
        added.BonusType.Should().Be(bonusType);
        added.Amount.Should().Be(amount);
        added.IsActive.Should().BeTrue();
        added.IsDeleted.Should().BeFalse();

        dto.Id.Should().Be(123);
        dto.PlayerId.Should().Be(playerId);
        dto.PlayerName.Should().Be("Alice Johnson");
        dto.BonusType.Should().Be(bonusType);
        dto.Amount.Should().Be(amount);
        dto.IsActive.Should().BeTrue();

        _bonusRepo.Verify(r => r.AddAsync(It.IsAny<PlayerBonus>(), It.IsAny<CancellationToken>()), Times.Once);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveBonusExists_Throws409Conflict()
    {
        // Arrange
        const int playerId = 1;
        var bonusType = BonusType.Welcome;

        _bonusRepo
            .Setup(r => r.ExistsActiveBonusAsync(playerId, bonusType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var act = () => service.CreateAsync(playerId, bonusType, 10m);

        // Assert
        await act.Should().ThrowAsync<ApiException>()
            .Where(e => e.StatusCode == 409);

        _bonusRepo.Verify(r => r.AddAsync(It.IsAny<PlayerBonus>(), It.IsAny<CancellationToken>()), Times.Never);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenBonusTypeIsInvalid_Throws400AndDoesNotCallRepo()
    {
        // Arrange
        const int playerId = 1;
        const decimal amount = 50m;
        var invalidBonusType = (BonusType)999;

        var service = CreateService();

        // Act
        var act = () => service.CreateAsync(playerId, invalidBonusType, amount);

        // Assert
        await act.Should().ThrowAsync<ApiException>()
            .Where(e => e.StatusCode == StatusCodes.Status400BadRequest);

        _bonusRepo.Verify(r => r.ExistsActiveBonusAsync(It.IsAny<int>(), It.IsAny<BonusType>(), It.IsAny<CancellationToken>()), Times.Never);
        _bonusRepo.Verify(r => r.AddAsync(It.IsAny<PlayerBonus>(), It.IsAny<CancellationToken>()), Times.Never);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _bonusRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    // Update tests

    [Fact]
    public async Task UpdateAsync_WhenBonusExists_UpdatesFields_AndWritesUpdatedLog()
    {
        // Arrange
        const int bonusId = 10;
        const int playerId = 1;
        var bonusType = BonusType.Welcome;

        var entity = new PlayerBonus
        {
            Id = bonusId,
            PlayerId = playerId,
            Player = new Player { Id = playerId, Name = "Alice Johnson", Email = "alice.johnson@example.com" },
            BonusType = bonusType,
            Amount = 10m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        _bonusRepo
            .Setup(r => r.GetByIdAsync(bonusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _bonusRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
            {
                entity.Player ??= new Player { Id = playerId, Name = "Alice Johnson", Email = "alice.johnson@example.com" };
                return entity;
            });

        PlayerBonusActionLog? addedLog = null;
        _logRepo
            .Setup(r => r.AddAsync(It.IsAny<PlayerBonusActionLog>(), It.IsAny<CancellationToken>()))
            .Callback<PlayerBonusActionLog, CancellationToken>((l, _) => addedLog = l)
            .Returns(Task.CompletedTask);

        _bonusRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        // Act
        var dto = await service.UpdateAsync(bonusId, amount: 99m, isActive: true);

        // Assert
        entity.Amount.Should().Be(99m);
        entity.IsActive.Should().BeTrue();

        _bonusRepo.Verify(r => r.Update(entity), Times.Once);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        addedLog.Should().NotBeNull();
        addedLog!.PlayerBonusId.Should().Be(bonusId);
        addedLog.ActionType.Should().Be(BonusActionType.Updated);
        addedLog.OperatorUserId.Should().Be("42");
        addedLog.OperatorUserName.Should().Be("George Dev");
        dto.Id.Should().Be(bonusId);
        dto.PlayerId.Should().Be(playerId);
        dto.PlayerName.Should().Be("Alice Johnson");
        dto.Amount.Should().Be(99m);
        dto.IsActive.Should().BeTrue();
        dto.BonusType.Should().Be(bonusType);
    }

    [Fact]
    public async Task UpdateAsync_WhenActivatingAndActiveBonusAlreadyExists_Throws409Conflict()
    {
        // Arrange
        const int bonusId = 10;
        const int playerId = 1;
        var bonusType = BonusType.Welcome;

        var entity = new PlayerBonus
        {
            Id = bonusId,
            PlayerId = playerId,
            BonusType = bonusType,
            Amount = 10m,
            IsActive = false, // important: transitioning false -> true
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        _bonusRepo
            .Setup(r => r.GetByIdAsync(bonusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _bonusRepo
            .Setup(r => r.ExistsActiveBonusAsync(playerId, bonusType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var act = () => service.UpdateAsync(bonusId, amount: 20m, isActive: true);

        // Assert
        await act.Should().ThrowAsync<ApiException>()
            .Where(e => e.StatusCode == 409);

        _logRepo.Verify(r => r.AddAsync(It.IsAny<PlayerBonusActionLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenBonusNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        const int bonusId = 999;

        _bonusRepo
            .Setup(r => r.GetByIdAsync(bonusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerBonus?)null);

        var service = CreateService();

        // Act
        var act = () => service.UpdateAsync(bonusId, amount: 10m, isActive: true);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();

        _logRepo.Verify(r => r.AddAsync(It.IsAny<PlayerBonusActionLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }


    // Delete tests

    [Fact]
    public async Task SoftDeleteAsync_WhenBonusExists_SoftDeletes_AndWritesDeletedLog()
    {
        // Arrange
        const int bonusId = 10;
        const int playerId = 1;

        var entity = new PlayerBonus
        {
            Id = bonusId,
            PlayerId = playerId,
            BonusType = BonusType.Welcome,
            Amount = 10m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        _bonusRepo
            .Setup(r => r.GetByIdAsync(bonusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // simulate repo behavior (what your repo does: IsDeleted=true, IsActive=false)
        _bonusRepo
            .Setup(r => r.SoftDeleteAsync(bonusId, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                entity.IsDeleted = true;
                entity.IsActive = false;
            })
            .Returns(Task.CompletedTask);

        PlayerBonusActionLog? addedLog = null;
        _logRepo
            .Setup(r => r.AddAsync(It.IsAny<PlayerBonusActionLog>(), It.IsAny<CancellationToken>()))
            .Callback<PlayerBonusActionLog, CancellationToken>((l, _) => addedLog = l)
            .Returns(Task.CompletedTask);

        _bonusRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        // Act
        await service.SoftDeleteAsync(bonusId);

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.IsActive.Should().BeFalse();

        _bonusRepo.Verify(r => r.SoftDeleteAsync(bonusId, It.IsAny<CancellationToken>()), Times.Once);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        addedLog.Should().NotBeNull();
        addedLog!.PlayerBonusId.Should().Be(bonusId);
        addedLog.ActionType.Should().Be(BonusActionType.Deleted);
        addedLog.OperatorUserId.Should().Be("42");
        addedLog.OperatorUserName.Should().Be("George Dev");
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenBonusNotFound_DoesNothing()
    {
        // Arrange
        const int bonusId = 999;

        _bonusRepo
            .Setup(r => r.GetByIdAsync(bonusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerBonus?)null);

        var service = CreateService();

        // Act
        await service.SoftDeleteAsync(bonusId);

        // Assert
        _bonusRepo.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _logRepo.Verify(r => r.AddAsync(It.IsAny<PlayerBonusActionLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _bonusRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Get tests

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsMappedDto()
    {
        // Arrange
        const int bonusId = 10;
        const int playerId = 1;

        var entity = new PlayerBonus
        {
            Id = bonusId,
            PlayerId = playerId,
            Player = new Player { Id = playerId, Name = "Alice Johnson", Email = "alice.johnson@example.com" },
            BonusType = BonusType.Welcome,
            Amount = 50m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        };

        _bonusRepo
            .Setup(r => r.GetByIdAsync(bonusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        var dto = await service.GetByIdAsync(bonusId);

        // Assert
        dto.Id.Should().Be(bonusId);
        dto.PlayerId.Should().Be(playerId);
        dto.PlayerName.Should().Be("Alice Johnson");
        dto.PlayerEmail.Should().Be("alice.johnson@example.com");
        dto.BonusType.Should().Be(BonusType.Welcome);
        dto.Amount.Should().Be(50m);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        const int bonusId = 999;

        _bonusRepo
            .Setup(r => r.GetByIdAsync(bonusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerBonus?)null);

        var service = CreateService();

        // Act
        var act = () => service.GetByIdAsync(bonusId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedMappedDtos()
    {
        // Arrange
        const int page = 1;
        const int pageSize = 20;

        var items = new List<PlayerBonus>
    {
        new()
        {
            Id = 1,
            PlayerId = 1,
            Player = new Player { Id = 1, Name = "Alice Johnson", Email = "alice.johnson@example.com" },
            BonusType = BonusType.Welcome,
            Amount = 50m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
        },
        new()
        {
            Id = 2,
            PlayerId = 2,
            Player = new Player { Id = 2, Name = "Mark Petrov", Email = "mark.petrov@example.com" },
            BonusType = BonusType.Cashback,
            Amount = 15m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        }
    };

        var paged = PagedResult<PlayerBonus>.Create(items, page, pageSize, totalCount: 2);

        _bonusRepo
            .Setup(r => r.GetAllPagedAsync(page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetAllAsync(page, pageSize);

        // Assert
        result.Page.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);


        result.Items[0].Id.Should().Be(1);
        result.Items[0].PlayerName.Should().Be("Alice Johnson");
        result.Items[0].PlayerEmail.Should().Be("alice.johnson@example.com");
        result.Items[1].Id.Should().Be(2);
        result.Items[1].PlayerName.Should().Be("Mark Petrov");
        result.Items[1].PlayerEmail.Should().Be("mark.petrov@example.com");

        _bonusRepo.Verify(r => r.GetAllPagedAsync(page, pageSize, It.IsAny<CancellationToken>()), Times.Once);
    }
}