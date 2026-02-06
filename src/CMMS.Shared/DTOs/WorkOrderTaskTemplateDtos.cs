namespace CMMS.Shared.DTOs;

/// <summary>
/// Full task template details with items
/// </summary>
public class WorkOrderTaskTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
    public List<WorkOrderTaskTemplateItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Summary view for template lists
/// </summary>
public class WorkOrderTaskTemplateSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Template item details
/// </summary>
public class WorkOrderTaskTemplateItemDto
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

/// <summary>
/// Request to create a new template
/// </summary>
public class CreateWorkOrderTaskTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateWorkOrderTaskTemplateItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Request to update an existing template
/// </summary>
public class UpdateWorkOrderTaskTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<UpdateWorkOrderTaskTemplateItemRequest> Items { get; set; } = new();
}

/// <summary>
/// Item data for creating a template
/// </summary>
public class CreateWorkOrderTaskTemplateItemRequest
{
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Item data for updating a template
/// </summary>
public class UpdateWorkOrderTaskTemplateItemRequest
{
    public int? Id { get; set; } // Null for new items
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Simple dropdown item for template selection
/// </summary>
public class TaskTemplateDropdownDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}
