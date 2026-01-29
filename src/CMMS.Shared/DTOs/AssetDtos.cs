namespace CMMS.Shared.DTOs;

public class AssetDto
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public int? ExpectedLifeYears { get; set; }
    public DateTime? InstallationDate { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? ParentAssetId { get; set; }
    public string? ParentAssetName { get; set; }
    public int? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAssetRequest
{
    public string? AssetTag { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }
    public string Status { get; set; } = "Active";
    public string Criticality { get; set; } = "Medium";
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public int? ExpectedLifeYears { get; set; }
    public DateTime? InstallationDate { get; set; }
    public int? ParentAssetId { get; set; }
    public int? AssignedTo { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAssetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }
    public string Status { get; set; } = "Active";
    public string Criticality { get; set; } = "Medium";
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public int? ExpectedLifeYears { get; set; }
    public DateTime? InstallationDate { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? ParentAssetId { get; set; }
    public int? AssignedTo { get; set; }
    public string? Notes { get; set; }
}

public class AssetCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Level { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<AssetCategoryDto> Children { get; set; } = new();
}

public class CreateAssetCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
}

public class AssetLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Level { get; set; }
    public string? FullPath { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public bool IsActive { get; set; }
    public List<AssetLocationDto> Children { get; set; } = new();
}

public class CreateAssetLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
}
