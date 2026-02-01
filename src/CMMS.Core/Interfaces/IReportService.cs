namespace CMMS.Core.Interfaces;

public interface IReportService
{
    // Inventory Reports
    Task<List<ReorderReportItem>> GetReorderReportAsync(ReorderReportFilter? filter = null);
    Task<InventoryValuationReport> GetInventoryValuationReportAsync(InventoryValuationFilter? filter = null);
    Task<List<StockMovementItem>> GetStockMovementReportAsync(StockMovementFilter? filter = null);

    // Maintenance Reports
    Task<OverdueMaintenanceReport> GetOverdueMaintenanceReportAsync();
    Task<MaintenancePerformedReport> GetMaintenancePerformedReportAsync(MaintenancePerformedFilter? filter = null);
    Task<PMComplianceReport> GetPMComplianceReportAsync(PMComplianceFilter? filter = null);
    Task<WorkOrderSummaryReport> GetWorkOrderSummaryReportAsync(WorkOrderSummaryFilter? filter = null);

    // Asset Reports
    Task<AssetMaintenanceHistoryReport> GetAssetMaintenanceHistoryReportAsync(AssetMaintenanceHistoryFilter filter);
}

#region Reorder Report

public class ReorderReportItem
{
    public int PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? SupplierName { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityAvailable { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public int QuantityToOrder { get; set; }
    public decimal UnitCost { get; set; }
    public decimal EstimatedCost { get; set; }
    public string ReorderStatus { get; set; } = string.Empty;
    public int LeadTimeDays { get; set; }
}

public class ReorderReportFilter
{
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public string? ReorderStatus { get; set; }
}

#endregion

#region Inventory Valuation Report

public class InventoryValuationReport
{
    public decimal TotalValue { get; set; }
    public int TotalParts { get; set; }
    public int TotalQuantity { get; set; }
    public List<InventoryValuationItem> Items { get; set; } = new();
    public List<ValuationByCategory> ByCategory { get; set; } = new();
    public List<ValuationByLocation> ByLocation { get; set; } = new();
}

public class InventoryValuationItem
{
    public int PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
}

public class ValuationByCategory
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int PartCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class ValuationByLocation
{
    public int? LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int PartCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class InventoryValuationFilter
{
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }
}

#endregion

#region Stock Movement Report

public class StockMovementItem
{
    public int TransactionId { get; set; }
    public int PartId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? FromLocationName { get; set; }
    public string? ToLocationName { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? PerformedByName { get; set; }
}

public class StockMovementFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? PartId { get; set; }
    public string? TransactionType { get; set; }
    public int? LocationId { get; set; }
}

#endregion

#region Overdue Maintenance Report

public class OverdueMaintenanceReport
{
    public int TotalOverdue { get; set; }
    public int OverduePMCount { get; set; }
    public int OverdueWorkOrderCount { get; set; }
    public List<OverduePMSchedule> OverduePMSchedules { get; set; } = new();
    public List<OverdueWorkOrder> OverdueWorkOrders { get; set; } = new();
}

public class OverduePMSchedule
{
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string FrequencyDescription { get; set; } = string.Empty;
}

public class OverdueWorkOrder
{
    public int WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public int DaysOverdue { get; set; }
    public string? AssignedToName { get; set; }
}

#endregion

#region Maintenance Performed Report

public class MaintenancePerformedReport
{
    public int TotalWorkOrders { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalPartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public List<MaintenancePerformedItem> Items { get; set; } = new();
}

public class MaintenancePerformedItem
{
    public int WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? CompletedByName { get; set; }
}

public class MaintenancePerformedFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? AssetId { get; set; }
    public int? TechnicianId { get; set; }
    public string? WorkOrderType { get; set; }
}

#endregion

#region PM Compliance Report

public class PMComplianceReport
{
    public int TotalScheduled { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalMissed { get; set; }
    public decimal ComplianceRate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<PMComplianceItem> Items { get; set; } = new();
}

public class PMComplianceItem
{
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public string? AssetName { get; set; }
    public string FrequencyDescription { get; set; } = string.Empty;
    public int ScheduledCount { get; set; }
    public int CompletedCount { get; set; }
    public int MissedCount { get; set; }
    public decimal ComplianceRate { get; set; }
}

public class PMComplianceFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? AssetId { get; set; }
}

#endregion

#region Work Order Summary Report

public class WorkOrderSummaryReport
{
    public int TotalWorkOrders { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<WorkOrderCountByStatus> ByStatus { get; set; } = new();
    public List<WorkOrderCountByType> ByType { get; set; } = new();
    public List<WorkOrderCountByPriority> ByPriority { get; set; } = new();
}

public class WorkOrderCountByStatus
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class WorkOrderCountByType
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class WorkOrderCountByPriority
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class WorkOrderSummaryFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

#endregion

#region Asset Maintenance History Report

public class AssetMaintenanceHistoryReport
{
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int TotalWorkOrders { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalCost { get; set; }
    public List<AssetMaintenanceHistoryItem> Items { get; set; } = new();
}

public class AssetMaintenanceHistoryItem
{
    public int WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
    public decimal LaborHours { get; set; }
    public decimal TotalCost { get; set; }
    public string? TechnicianName { get; set; }
}

public class AssetMaintenanceHistoryFilter
{
    public int AssetId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

#endregion
