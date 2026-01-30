namespace CMMS.Shared.DTOs;

public class StorageLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Level { get; set; }
    public string? FullPath { get; set; }
    public string? Building { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public bool IsActive { get; set; }
    public int StockItemCount { get; set; }
    public List<StorageLocationDto> Children { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateStorageLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? Building { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateStorageLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? Building { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public bool IsActive { get; set; } = true;
}
