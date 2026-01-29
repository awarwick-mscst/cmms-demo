using CMMS.Shared.DTOs;
using CMMS.Shared.Validators;
using FluentAssertions;
using Xunit;

namespace CMMS.Tests.Unit.Validators;

public class AssetValidatorTests
{
    private readonly CreateAssetRequestValidator _validator;

    public AssetValidatorTests()
    {
        _validator = new CreateAssetRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ReturnsValid()
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            CategoryId = 1,
            Status = "Active",
            Criticality = "Medium"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "",
            CategoryId = 1,
            Status = "Active",
            Criticality = "Medium"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithInvalidCategoryId_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            CategoryId = 0,
            Status = "Active",
            Criticality = "Medium"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void Validate_WithInvalidStatus_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            CategoryId = 1,
            Status = "InvalidStatus",
            Criticality = "Medium"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Validate_WithInvalidCriticality_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            CategoryId = 1,
            Status = "Active",
            Criticality = "InvalidCriticality"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Criticality");
    }

    [Fact]
    public void Validate_WithNegativePurchaseCost_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            CategoryId = 1,
            Status = "Active",
            Criticality = "Medium",
            PurchaseCost = -100
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PurchaseCost");
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Inactive")]
    [InlineData("InMaintenance")]
    [InlineData("Retired")]
    [InlineData("Disposed")]
    public void Validate_WithValidStatuses_ReturnsValid(string status)
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            CategoryId = 1,
            Status = status,
            Criticality = "Medium"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Critical")]
    [InlineData("High")]
    [InlineData("Medium")]
    [InlineData("Low")]
    public void Validate_WithValidCriticalities_ReturnsValid(string criticality)
    {
        // Arrange
        var request = new CreateAssetRequest
        {
            Name = "Test Asset",
            CategoryId = 1,
            Status = "Active",
            Criticality = criticality
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
