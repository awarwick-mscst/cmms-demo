using System.Text;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    #region Inventory Reports

    [HttpGet("reorder")]
    public async Task<IActionResult> GetReorderReport(
        [FromQuery] int? categoryId,
        [FromQuery] int? supplierId,
        [FromQuery] string? status,
        [FromQuery] string? format)
    {
        var filter = new ReorderReportFilter
        {
            CategoryId = categoryId,
            SupplierId = supplierId,
            ReorderStatus = status
        };

        var items = await _reportService.GetReorderReportAsync(filter);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GenerateReorderCsv(items);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "reorder-report.csv");
        }

        return Ok(ApiResponse<List<ReorderReportItem>>.Ok(items));
    }

    [HttpGet("inventory-valuation")]
    public async Task<IActionResult> GetInventoryValuationReport(
        [FromQuery] int? categoryId,
        [FromQuery] int? locationId,
        [FromQuery] string? format)
    {
        var filter = new InventoryValuationFilter
        {
            CategoryId = categoryId,
            LocationId = locationId
        };

        var report = await _reportService.GetInventoryValuationReportAsync(filter);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GenerateInventoryValuationCsv(report);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "inventory-valuation-report.csv");
        }

        return Ok(ApiResponse<InventoryValuationReport>.Ok(report));
    }

    [HttpGet("stock-movement")]
    public async Task<IActionResult> GetStockMovementReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? partId,
        [FromQuery] string? transactionType,
        [FromQuery] int? locationId,
        [FromQuery] string? format)
    {
        var filter = new StockMovementFilter
        {
            FromDate = fromDate,
            ToDate = toDate,
            PartId = partId,
            TransactionType = transactionType,
            LocationId = locationId
        };

        var items = await _reportService.GetStockMovementReportAsync(filter);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GenerateStockMovementCsv(items);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "stock-movement-report.csv");
        }

        return Ok(ApiResponse<List<StockMovementItem>>.Ok(items));
    }

    #endregion

    #region Maintenance Reports

    [HttpGet("overdue-maintenance")]
    public async Task<IActionResult> GetOverdueMaintenanceReport([FromQuery] string? format)
    {
        var report = await _reportService.GetOverdueMaintenanceReportAsync();

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GenerateOverdueMaintenanceCsv(report);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "overdue-maintenance-report.csv");
        }

        return Ok(ApiResponse<OverdueMaintenanceReport>.Ok(report));
    }

    [HttpGet("maintenance-performed")]
    public async Task<IActionResult> GetMaintenancePerformedReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? assetId,
        [FromQuery] int? technicianId,
        [FromQuery] string? workOrderType,
        [FromQuery] string? format)
    {
        var filter = new MaintenancePerformedFilter
        {
            FromDate = fromDate,
            ToDate = toDate,
            AssetId = assetId,
            TechnicianId = technicianId,
            WorkOrderType = workOrderType
        };

        var report = await _reportService.GetMaintenancePerformedReportAsync(filter);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GenerateMaintenancePerformedCsv(report);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "maintenance-performed-report.csv");
        }

        return Ok(ApiResponse<MaintenancePerformedReport>.Ok(report));
    }

    [HttpGet("pm-compliance")]
    public async Task<IActionResult> GetPMComplianceReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? assetId,
        [FromQuery] string? format)
    {
        var filter = new PMComplianceFilter
        {
            FromDate = fromDate,
            ToDate = toDate,
            AssetId = assetId
        };

        var report = await _reportService.GetPMComplianceReportAsync(filter);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GeneratePMComplianceCsv(report);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "pm-compliance-report.csv");
        }

        return Ok(ApiResponse<PMComplianceReport>.Ok(report));
    }

    [HttpGet("work-order-summary")]
    public async Task<IActionResult> GetWorkOrderSummaryReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? format)
    {
        var filter = new WorkOrderSummaryFilter
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        var report = await _reportService.GetWorkOrderSummaryReportAsync(filter);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GenerateWorkOrderSummaryCsv(report);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "work-order-summary-report.csv");
        }

        return Ok(ApiResponse<WorkOrderSummaryReport>.Ok(report));
    }

    #endregion

    #region Asset Reports

    [HttpGet("asset-maintenance-history/{assetId}")]
    public async Task<IActionResult> GetAssetMaintenanceHistoryReport(
        int assetId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? format)
    {
        var filter = new AssetMaintenanceHistoryFilter
        {
            AssetId = assetId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var report = await _reportService.GetAssetMaintenanceHistoryReportAsync(filter);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csv = GenerateAssetMaintenanceHistoryCsv(report);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"asset-{assetId}-maintenance-history.csv");
        }

        return Ok(ApiResponse<AssetMaintenanceHistoryReport>.Ok(report));
    }

    #endregion

    #region CSV Generation

    private static string GenerateReorderCsv(List<ReorderReportItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Part Number,Name,Category,Supplier,Qty On Hand,Qty Available,Reorder Point,Reorder Qty,Qty To Order,Unit Cost,Estimated Cost,Status,Lead Time Days");

        foreach (var item in items)
        {
            sb.AppendLine($"{CsvEscape(item.PartNumber)},{CsvEscape(item.Name)},{CsvEscape(item.CategoryName)},{CsvEscape(item.SupplierName)},{item.QuantityOnHand},{item.QuantityAvailable},{item.ReorderPoint},{item.ReorderQuantity},{item.QuantityToOrder},{item.UnitCost:F2},{item.EstimatedCost:F2},{item.ReorderStatus},{item.LeadTimeDays}");
        }

        return sb.ToString();
    }

    private static string GenerateInventoryValuationCsv(InventoryValuationReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Part Number,Name,Category,Qty On Hand,Unit Cost,Total Value");

        foreach (var item in report.Items)
        {
            sb.AppendLine($"{CsvEscape(item.PartNumber)},{CsvEscape(item.Name)},{CsvEscape(item.CategoryName)},{item.QuantityOnHand},{item.UnitCost:F2},{item.TotalValue:F2}");
        }

        sb.AppendLine();
        sb.AppendLine($"Total Parts,{report.TotalParts}");
        sb.AppendLine($"Total Quantity,{report.TotalQuantity}");
        sb.AppendLine($"Total Value,{report.TotalValue:F2}");

        return sb.ToString();
    }

    private static string GenerateStockMovementCsv(List<StockMovementItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Part Number,Part Name,Transaction Type,Quantity,From Location,To Location,Reference,Performed By,Notes");

        foreach (var item in items)
        {
            sb.AppendLine($"{item.TransactionDate:yyyy-MM-dd HH:mm},{CsvEscape(item.PartNumber)},{CsvEscape(item.PartName)},{item.TransactionType},{item.Quantity},{CsvEscape(item.FromLocationName)},{CsvEscape(item.ToLocationName)},{CsvEscape(item.Reference)},{CsvEscape(item.PerformedByName)},{CsvEscape(item.Notes)}");
        }

        return sb.ToString();
    }

    private static string GenerateOverdueMaintenanceCsv(OverdueMaintenanceReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Type,ID,Name/Number,Title,Asset,Due Date,Days Overdue,Priority,Assigned To");

        foreach (var pm in report.OverduePMSchedules)
        {
            sb.AppendLine($"PM Schedule,{pm.ScheduleId},{CsvEscape(pm.ScheduleName)},{CsvEscape(pm.FrequencyDescription)},{CsvEscape(pm.AssetName)},{pm.DueDate:yyyy-MM-dd},{pm.DaysOverdue},{pm.Priority},");
        }

        foreach (var wo in report.OverdueWorkOrders)
        {
            sb.AppendLine($"Work Order,{wo.WorkOrderId},{CsvEscape(wo.WorkOrderNumber)},{CsvEscape(wo.Title)},{CsvEscape(wo.AssetName)},{wo.ScheduledEndDate:yyyy-MM-dd},{wo.DaysOverdue},{wo.Priority},{CsvEscape(wo.AssignedToName)}");
        }

        return sb.ToString();
    }

    private static string GenerateMaintenancePerformedCsv(MaintenancePerformedReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Work Order Number,Title,Type,Asset,Completed Date,Labor Hours,Labor Cost,Parts Cost,Total Cost,Completed By");

        foreach (var item in report.Items)
        {
            sb.AppendLine($"{CsvEscape(item.WorkOrderNumber)},{CsvEscape(item.Title)},{item.Type},{CsvEscape(item.AssetName)},{item.CompletedDate:yyyy-MM-dd},{item.LaborHours:F2},{item.LaborCost:F2},{item.PartsCost:F2},{item.TotalCost:F2},{CsvEscape(item.CompletedByName)}");
        }

        sb.AppendLine();
        sb.AppendLine($"Total Work Orders,{report.TotalWorkOrders}");
        sb.AppendLine($"Total Labor Hours,{report.TotalLaborHours:F2}");
        sb.AppendLine($"Total Labor Cost,{report.TotalLaborCost:F2}");
        sb.AppendLine($"Total Parts Cost,{report.TotalPartsCost:F2}");
        sb.AppendLine($"Total Cost,{report.TotalCost:F2}");

        return sb.ToString();
    }

    private static string GeneratePMComplianceCsv(PMComplianceReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Schedule Name,Asset,Frequency,Scheduled,Completed,Missed,Compliance Rate %");

        foreach (var item in report.Items)
        {
            sb.AppendLine($"{CsvEscape(item.ScheduleName)},{CsvEscape(item.AssetName)},{CsvEscape(item.FrequencyDescription)},{item.ScheduledCount},{item.CompletedCount},{item.MissedCount},{item.ComplianceRate:F1}");
        }

        sb.AppendLine();
        sb.AppendLine($"Total Scheduled,{report.TotalScheduled}");
        sb.AppendLine($"Total Completed,{report.TotalCompleted}");
        sb.AppendLine($"Total Missed,{report.TotalMissed}");
        sb.AppendLine($"Overall Compliance Rate,{report.ComplianceRate:F1}%");

        return sb.ToString();
    }

    private static string GenerateWorkOrderSummaryCsv(WorkOrderSummaryReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Work Order Summary Report");
        sb.AppendLine($"Total Work Orders,{report.TotalWorkOrders}");
        sb.AppendLine();

        sb.AppendLine("By Status");
        sb.AppendLine("Status,Count,Percentage");
        foreach (var item in report.ByStatus)
        {
            sb.AppendLine($"{item.Status},{item.Count},{item.Percentage:F1}%");
        }

        sb.AppendLine();
        sb.AppendLine("By Type");
        sb.AppendLine("Type,Count,Percentage");
        foreach (var item in report.ByType)
        {
            sb.AppendLine($"{item.Type},{item.Count},{item.Percentage:F1}%");
        }

        sb.AppendLine();
        sb.AppendLine("By Priority");
        sb.AppendLine("Priority,Count,Percentage");
        foreach (var item in report.ByPriority)
        {
            sb.AppendLine($"{item.Priority},{item.Count},{item.Percentage:F1}%");
        }

        return sb.ToString();
    }

    private static string GenerateAssetMaintenanceHistoryCsv(AssetMaintenanceHistoryReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Asset Maintenance History - {report.AssetTag}: {report.AssetName}");
        sb.AppendLine();
        sb.AppendLine("Work Order Number,Title,Type,Status,Priority,Completed Date,Labor Hours,Total Cost,Technician");

        foreach (var item in report.Items)
        {
            sb.AppendLine($"{CsvEscape(item.WorkOrderNumber)},{CsvEscape(item.Title)},{item.Type},{item.Status},{item.Priority},{item.CompletedDate:yyyy-MM-dd},{item.LaborHours:F2},{item.TotalCost:F2},{CsvEscape(item.TechnicianName)}");
        }

        sb.AppendLine();
        sb.AppendLine($"Total Work Orders,{report.TotalWorkOrders}");
        sb.AppendLine($"Total Labor Hours,{report.TotalLaborHours:F2}");
        sb.AppendLine($"Total Cost,{report.TotalCost:F2}");

        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    #endregion
}
