using CMMS.Shared.DTOs;
using FluentValidation;

namespace CMMS.Shared.Validators;

public class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Asset name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(BeValidStatus).WithMessage("Invalid status value");

        RuleFor(x => x.Criticality)
            .NotEmpty().WithMessage("Criticality is required")
            .Must(BeValidCriticality).WithMessage("Invalid criticality value");

        RuleFor(x => x.Manufacturer)
            .MaximumLength(100).WithMessage("Manufacturer must not exceed 100 characters");

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model must not exceed 100 characters");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number must not exceed 100 characters");

        RuleFor(x => x.PurchaseCost)
            .GreaterThanOrEqualTo(0).When(x => x.PurchaseCost.HasValue)
            .WithMessage("Purchase cost must be a positive value");

        RuleFor(x => x.ExpectedLifeYears)
            .GreaterThan(0).When(x => x.ExpectedLifeYears.HasValue)
            .WithMessage("Expected life years must be greater than 0");
    }

    private static bool BeValidStatus(string status)
    {
        var validStatuses = new[] { "Active", "Inactive", "InMaintenance", "Retired", "Disposed" };
        return validStatuses.Contains(status);
    }

    private static bool BeValidCriticality(string criticality)
    {
        var validCriticalities = new[] { "Critical", "High", "Medium", "Low" };
        return validCriticalities.Contains(criticality);
    }
}

public class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Asset name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(BeValidStatus).WithMessage("Invalid status value");

        RuleFor(x => x.Criticality)
            .NotEmpty().WithMessage("Criticality is required")
            .Must(BeValidCriticality).WithMessage("Invalid criticality value");

        RuleFor(x => x.Manufacturer)
            .MaximumLength(100).WithMessage("Manufacturer must not exceed 100 characters");

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model must not exceed 100 characters");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100).WithMessage("Serial number must not exceed 100 characters");

        RuleFor(x => x.PurchaseCost)
            .GreaterThanOrEqualTo(0).When(x => x.PurchaseCost.HasValue)
            .WithMessage("Purchase cost must be a positive value");
    }

    private static bool BeValidStatus(string status)
    {
        var validStatuses = new[] { "Active", "Inactive", "InMaintenance", "Retired", "Disposed" };
        return validStatuses.Contains(status);
    }

    private static bool BeValidCriticality(string criticality)
    {
        var validCriticalities = new[] { "Critical", "High", "Medium", "Low" };
        return validCriticalities.Contains(criticality);
    }
}

public class CreateAssetCategoryRequestValidator : AbstractValidator<CreateAssetCategoryRequest>
{
    public CreateAssetCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Category code is required")
            .MaximumLength(20).WithMessage("Code must not exceed 20 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Code can only contain uppercase letters, numbers, and hyphens");
    }
}

public class CreateAssetLocationRequestValidator : AbstractValidator<CreateAssetLocationRequest>
{
    public CreateAssetLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Location code is required")
            .MaximumLength(20).WithMessage("Code must not exceed 20 characters")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Code can only contain uppercase letters, numbers, and hyphens");
    }
}
