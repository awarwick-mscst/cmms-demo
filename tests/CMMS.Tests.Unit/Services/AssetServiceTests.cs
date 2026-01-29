using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Infrastructure.Services;
using FluentAssertions;
using Moq;
using Xunit;
using System.Linq.Expressions;

namespace CMMS.Tests.Unit.Services;

public class AssetServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AssetService _assetService;

    public AssetServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _assetService = new AssetService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAssetByIdAsync_WhenAssetExists_ReturnsAsset()
    {
        // Arrange
        var assetId = 1;
        var expectedAsset = new Asset
        {
            Id = assetId,
            Name = "Test Asset",
            AssetTag = "TEST-001",
            CategoryId = 1,
            Status = AssetStatus.Active,
            Criticality = AssetCriticality.Medium
        };

        var mockRepo = new Mock<IRepository<Asset>>();
        mockRepo.Setup(r => r.Query())
            .Returns(new List<Asset> { expectedAsset }.AsQueryable());

        _unitOfWorkMock.Setup(u => u.Assets).Returns(mockRepo.Object);

        // Act
        var result = await _assetService.GetAssetByIdAsync(assetId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(assetId);
        result.Name.Should().Be("Test Asset");
    }

    [Fact]
    public async Task GetAssetByIdAsync_WhenAssetDoesNotExist_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IRepository<Asset>>();
        mockRepo.Setup(r => r.Query())
            .Returns(new List<Asset>().AsQueryable());

        _unitOfWorkMock.Setup(u => u.Assets).Returns(mockRepo.Object);

        // Act
        var result = await _assetService.GetAssetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAssetAsync_WithValidData_CreatesAndReturnsAsset()
    {
        // Arrange
        var newAsset = new Asset
        {
            Name = "New Asset",
            CategoryId = 1,
            Status = AssetStatus.Active,
            Criticality = AssetCriticality.High
        };

        var category = new AssetCategory { Id = 1, Code = "TEST" };

        var assetRepoMock = new Mock<IRepository<Asset>>();
        assetRepoMock.Setup(r => r.AddAsync(It.IsAny<Asset>(), default))
            .ReturnsAsync((Asset a, CancellationToken _) =>
            {
                a.Id = 1;
                return a;
            });
        assetRepoMock.Setup(r => r.Query())
            .Returns(new List<Asset>().AsQueryable());

        var categoryRepoMock = new Mock<IRepository<AssetCategory>>();
        categoryRepoMock.Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(category);

        _unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.AssetCategories).Returns(categoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _assetService.CreateAssetAsync(newAsset, createdBy: 1);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Asset");
        result.AssetTag.Should().StartWith("TEST-");
        result.CreatedBy.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAssetAsync_WhenAssetExists_SoftDeletesAndReturnsTrue()
    {
        // Arrange
        var asset = new Asset
        {
            Id = 1,
            Name = "Asset to Delete",
            IsDeleted = false
        };

        var mockRepo = new Mock<IRepository<Asset>>();
        mockRepo.Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(asset);

        _unitOfWorkMock.Setup(u => u.Assets).Returns(mockRepo.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _assetService.DeleteAssetAsync(1, deletedBy: 1);

        // Assert
        result.Should().BeTrue();
        asset.IsDeleted.Should().BeTrue();
        asset.DeletedAt.Should().NotBeNull();
        asset.UpdatedBy.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAssetAsync_WhenAssetDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var mockRepo = new Mock<IRepository<Asset>>();
        mockRepo.Setup(r => r.GetByIdAsync(999, default))
            .ReturnsAsync((Asset?)null);

        _unitOfWorkMock.Setup(u => u.Assets).Returns(mockRepo.Object);

        // Act
        var result = await _assetService.DeleteAssetAsync(999, deletedBy: 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAssetTagAsync_GeneratesUniqueTag()
    {
        // Arrange
        var category = new AssetCategory { Id = 1, Code = "HVAC" };

        var categoryRepoMock = new Mock<IRepository<AssetCategory>>();
        categoryRepoMock.Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(category);

        var assetRepoMock = new Mock<IRepository<Asset>>();
        assetRepoMock.Setup(r => r.Query())
            .Returns(new List<Asset>().AsQueryable());

        _unitOfWorkMock.Setup(u => u.AssetCategories).Returns(categoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);

        // Act
        var result = await _assetService.GenerateAssetTagAsync(1);

        // Assert
        result.Should().Be("HVAC-000001");
    }
}
