using CMMS.Shared.DTOs;
using FluentValidation;

namespace CMMS.Shared.Validators;

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Supplier name is required")
            .MaximumLength(200).WithMessage("Supplier name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.ContactName)
            .MaximumLength(100).WithMessage("Contact name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters");

        RuleFor(x => x.Website)
            .MaximumLength(500).WithMessage("Website cannot exceed 500 characters");
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Supplier name is required")
            .MaximumLength(200).WithMessage("Supplier name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.ContactName)
            .MaximumLength(100).WithMessage("Contact name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters");

        RuleFor(x => x.Website)
            .MaximumLength(500).WithMessage("Website cannot exceed 500 characters");
    }
}

public class CreatePartCategoryRequestValidator : AbstractValidator<CreatePartCategoryRequest>
{
    public CreatePartCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}

public class UpdatePartCategoryRequestValidator : AbstractValidator<UpdatePartCategoryRequest>
{
    public UpdatePartCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}

public class CreateStorageLocationRequestValidator : AbstractValidator<CreateStorageLocationRequest>
{
    public CreateStorageLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required")
            .MaximumLength(100).WithMessage("Location name cannot exceed 100 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Building)
            .MaximumLength(100).WithMessage("Building cannot exceed 100 characters");

        RuleFor(x => x.Aisle)
            .MaximumLength(50).WithMessage("Aisle cannot exceed 50 characters");

        RuleFor(x => x.Rack)
            .MaximumLength(50).WithMessage("Rack cannot exceed 50 characters");

        RuleFor(x => x.Shelf)
            .MaximumLength(50).WithMessage("Shelf cannot exceed 50 characters");

        RuleFor(x => x.Bin)
            .MaximumLength(50).WithMessage("Bin cannot exceed 50 characters");
    }
}

public class UpdateStorageLocationRequestValidator : AbstractValidator<UpdateStorageLocationRequest>
{
    public UpdateStorageLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required")
            .MaximumLength(100).WithMessage("Location name cannot exceed 100 characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Building)
            .MaximumLength(100).WithMessage("Building cannot exceed 100 characters");

        RuleFor(x => x.Aisle)
            .MaximumLength(50).WithMessage("Aisle cannot exceed 50 characters");

        RuleFor(x => x.Rack)
            .MaximumLength(50).WithMessage("Rack cannot exceed 50 characters");

        RuleFor(x => x.Shelf)
            .MaximumLength(50).WithMessage("Shelf cannot exceed 50 characters");

        RuleFor(x => x.Bin)
            .MaximumLength(50).WithMessage("Bin cannot exceed 50 characters");
    }
}

public class CreatePartRequestValidator : AbstractValidator<CreatePartRequest>
{
    public CreatePartRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Part name is required")
            .MaximumLength(200).WithMessage("Part name cannot exceed 200 characters");

        RuleFor(x => x.PartNumber)
            .MaximumLength(100).WithMessage("Part number cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("Unit cost cannot be negative");

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder point cannot be negative");

        RuleFor(x => x.ReorderQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder quantity cannot be negative");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative");

        RuleFor(x => x.MaxStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum stock level cannot be negative")
            .GreaterThanOrEqualTo(x => x.MinStockLevel).WithMessage("Maximum stock level must be greater than or equal to minimum stock level");

        RuleFor(x => x.LeadTimeDays)
            .GreaterThanOrEqualTo(0).WithMessage("Lead time days cannot be negative");

        RuleFor(x => x.Manufacturer)
            .MaximumLength(200).WithMessage("Manufacturer cannot exceed 200 characters");

        RuleFor(x => x.ManufacturerPartNumber)
            .MaximumLength(100).WithMessage("Manufacturer part number cannot exceed 100 characters");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode cannot exceed 100 characters");
    }
}

public class UpdatePartRequestValidator : AbstractValidator<UpdatePartRequest>
{
    public UpdatePartRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Part name is required")
            .MaximumLength(200).WithMessage("Part name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("Unit cost cannot be negative");

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder point cannot be negative");

        RuleFor(x => x.ReorderQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder quantity cannot be negative");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative");

        RuleFor(x => x.MaxStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum stock level cannot be negative")
            .GreaterThanOrEqualTo(x => x.MinStockLevel).WithMessage("Maximum stock level must be greater than or equal to minimum stock level");

        RuleFor(x => x.LeadTimeDays)
            .GreaterThanOrEqualTo(0).WithMessage("Lead time days cannot be negative");

        RuleFor(x => x.Manufacturer)
            .MaximumLength(200).WithMessage("Manufacturer cannot exceed 200 characters");

        RuleFor(x => x.ManufacturerPartNumber)
            .MaximumLength(100).WithMessage("Manufacturer part number cannot exceed 100 characters");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode cannot exceed 100 characters");
    }
}

public class StockAdjustmentRequestValidator : AbstractValidator<StockAdjustmentRequest>
{
    private static readonly string[] ValidTransactionTypes = { "Receive", "Issue", "Adjust" };

    public StockAdjustmentRequestValidator()
    {
        RuleFor(x => x.LocationId)
            .GreaterThan(0).WithMessage("Location is required");

        RuleFor(x => x.TransactionType)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(x => ValidTransactionTypes.Contains(x))
            .WithMessage("Transaction type must be Receive, Issue, or Adjust");

        RuleFor(x => x.Quantity)
            .NotEqual(0).WithMessage("Quantity cannot be zero");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue)
            .WithMessage("Unit cost cannot be negative");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
    }
}

public class StockTransferRequestValidator : AbstractValidator<StockTransferRequest>
{
    public StockTransferRequestValidator()
    {
        RuleFor(x => x.FromLocationId)
            .GreaterThan(0).WithMessage("From location is required");

        RuleFor(x => x.ToLocationId)
            .GreaterThan(0).WithMessage("To location is required")
            .NotEqual(x => x.FromLocationId).WithMessage("From and To locations must be different");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
    }
}

public class StockReserveRequestValidator : AbstractValidator<StockReserveRequest>
{
    public StockReserveRequestValidator()
    {
        RuleFor(x => x.LocationId)
            .GreaterThan(0).WithMessage("Location is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
    }
}

public class CreateAssetPartRequestValidator : AbstractValidator<CreateAssetPartRequest>
{
    public CreateAssetPartRequestValidator()
    {
        RuleFor(x => x.AssetId)
            .GreaterThan(0).WithMessage("Asset is required");

        RuleFor(x => x.PartId)
            .GreaterThan(0).WithMessage("Part is required");

        RuleFor(x => x.LocationId)
            .GreaterThan(0).WithMessage("Location is required");

        RuleFor(x => x.QuantityUsed)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");

        RuleFor(x => x.UnitCostOverride)
            .GreaterThanOrEqualTo(0).When(x => x.UnitCostOverride.HasValue)
            .WithMessage("Unit cost cannot be negative");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
    }
}
