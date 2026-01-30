namespace CMMS.Shared.DTOs;

public class PartCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Level { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int PartCount { get; set; }
    public List<PartCategoryDto> Children { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePartCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePartCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
