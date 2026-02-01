namespace CMMS.Shared.DTOs;

/// <summary>
/// Work order comment details
/// </summary>
public class WorkOrderCommentDto
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request to create a new comment on a work order
/// </summary>
public class CreateWorkOrderCommentRequest
{
    public string Comment { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}
