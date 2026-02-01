using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Inventory Reports

    public async Task<List<ReorderReportItem>> GetReorderReportAsync(ReorderReportFilter? filter = null)
    {
        var query = _unitOfWork.Parts.Query()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Stocks)
            .Where(p => p.Status == PartStatus.Active)
            .AsQueryable();

        if (filter?.CategoryId.HasValue == true)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (filter?.SupplierId.HasValue == true)
        {
            query = query.Where(p => p.SupplierId == filter.SupplierId.Value);
        }

        var parts = await query.ToListAsync();

        var items = new List<ReorderReportItem>();

        foreach (var part in parts)
        {
            var quantityOnHand = part.Stocks.Sum(s => s.QuantityOnHand);
            var quantityReserved = part.Stocks.Sum(s => s.QuantityReserved);
            var quantityAvailable = quantityOnHand - quantityReserved;

            string reorderStatus;
            if (quantityAvailable <= 0)
                reorderStatus = "OutOfStock";
            else if (quantityAvailable <= part.MinStockLevel)
                reorderStatus = "Critical";
            else if (quantityAvailable <= part.ReorderPoint)
                reorderStatus = "Low";
            else
                continue; // Skip items that don't need reordering

            // Apply status filter if specified
            if (!string.IsNullOrWhiteSpace(filter?.ReorderStatus) &&
                !reorderStatus.Equals(filter.ReorderStatus, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var quantityToOrder = part.ReorderQuantity > 0
                ? part.ReorderQuantity
                : Math.Max(0, part.ReorderPoint - quantityAvailable);

            items.Add(new ReorderReportItem
            {
                PartId = part.Id,
                PartNumber = part.PartNumber,
                Name = part.Name,
                CategoryName = part.Category?.Name,
                SupplierName = part.Supplier?.Name,
                QuantityOnHand = quantityOnHand,
                QuantityAvailable = quantityAvailable,
                ReorderPoint = part.ReorderPoint,
                ReorderQuantity = part.ReorderQuantity,
                QuantityToOrder = quantityToOrder,
                UnitCost = part.UnitCost,
                EstimatedCost = quantityToOrder * part.UnitCost,
                ReorderStatus = reorderStatus,
                LeadTimeDays = part.LeadTimeDays
            });
        }

        return items.OrderByDescending(i => i.ReorderStatus switch
        {
            "OutOfStock" => 3,
            "Critical" => 2,
            "Low" => 1,
            _ => 0
        }).ThenBy(i => i.Name).ToList();
    }

    public async Task<InventoryValuationReport> GetInventoryValuationReportAsync(InventoryValuationFilter? filter = null)
    {
        var query = _unitOfWork.Parts.Query()
            .Include(p => p.Category)
            .Include(p => p.Stocks)
                .ThenInclude(s => s.Location)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (filter?.CategoryId.HasValue == true)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        var parts = await query.ToListAsync();

        // Filter by location if specified
        if (filter?.LocationId.HasValue == true)
        {
            parts = parts.Where(p => p.Stocks.Any(s => s.LocationId == filter.LocationId.Value)).ToList();
        }

        var items = new List<InventoryValuationItem>();
        var byCategory = new Dictionary<int?, ValuationByCategory>();
        var byLocation = new Dictionary<int?, ValuationByLocation>();

        foreach (var part in parts)
        {
            var stocks = filter?.LocationId.HasValue == true
                ? part.Stocks.Where(s => s.LocationId == filter.LocationId.Value)
                : part.Stocks;

            var quantityOnHand = stocks.Sum(s => s.QuantityOnHand);
            var totalValue = quantityOnHand * part.UnitCost;

            items.Add(new InventoryValuationItem
            {
                PartId = part.Id,
                PartNumber = part.PartNumber,
                Name = part.Name,
                CategoryName = part.Category?.Name,
                QuantityOnHand = quantityOnHand,
                UnitCost = part.UnitCost,
                TotalValue = totalValue
            });

            // Aggregate by category
            var categoryId = part.CategoryId;
            if (!byCategory.ContainsKey(categoryId))
            {
                byCategory[categoryId] = new ValuationByCategory
                {
                    CategoryId = categoryId,
                    CategoryName = part.Category?.Name ?? "Uncategorized"
                };
            }
            byCategory[categoryId].PartCount++;
            byCategory[categoryId].TotalQuantity += quantityOnHand;
            byCategory[categoryId].TotalValue += totalValue;

            // Aggregate by location
            foreach (var stock in stocks)
            {
                var locationId = stock.LocationId;
                if (!byLocation.ContainsKey(locationId))
                {
                    byLocation[locationId] = new ValuationByLocation
                    {
                        LocationId = locationId,
                        LocationName = stock.Location?.Name ?? "Unknown"
                    };
                }
                byLocation[locationId].TotalQuantity += stock.QuantityOnHand;
                byLocation[locationId].TotalValue += stock.QuantityOnHand * part.UnitCost;
            }
        }

        // Count distinct parts per location
        foreach (var part in parts)
        {
            var stocks = filter?.LocationId.HasValue == true
                ? part.Stocks.Where(s => s.LocationId == filter.LocationId.Value)
                : part.Stocks;

            foreach (var stock in stocks.Where(s => s.QuantityOnHand > 0))
            {
                if (byLocation.ContainsKey(stock.LocationId))
                {
                    byLocation[stock.LocationId].PartCount++;
                }
            }
        }

        return new InventoryValuationReport
        {
            TotalValue = items.Sum(i => i.TotalValue),
            TotalParts = items.Count,
            TotalQuantity = items.Sum(i => i.QuantityOnHand),
            Items = items.OrderByDescending(i => i.TotalValue).ToList(),
            ByCategory = byCategory.Values.OrderByDescending(c => c.TotalValue).ToList(),
            ByLocation = byLocation.Values.OrderByDescending(l => l.TotalValue).ToList()
        };
    }

    public async Task<List<StockMovementItem>> GetStockMovementReportAsync(StockMovementFilter? filter = null)
    {
        var query = _unitOfWork.PartTransactions.Query()
            .Include(t => t.Part)
            .Include(t => t.Location)
            .Include(t => t.ToLocation)
            .Include(t => t.CreatedByUser)
            .AsQueryable();

        if (filter?.FromDate.HasValue == true)
        {
            query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
        }

        if (filter?.ToDate.HasValue == true)
        {
            var endDate = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(t => t.TransactionDate < endDate);
        }

        if (filter?.PartId.HasValue == true)
        {
            query = query.Where(t => t.PartId == filter.PartId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter?.TransactionType) &&
            Enum.TryParse<TransactionType>(filter.TransactionType, true, out var transType))
        {
            query = query.Where(t => t.TransactionType == transType);
        }

        if (filter?.LocationId.HasValue == true)
        {
            query = query.Where(t => t.LocationId == filter.LocationId.Value ||
                                     t.ToLocationId == filter.LocationId.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(1000) // Limit results
            .ToListAsync();

        return transactions.Select(t => new StockMovementItem
        {
            TransactionId = t.Id,
            PartId = t.PartId,
            PartNumber = t.Part.PartNumber,
            PartName = t.Part.Name,
            TransactionType = t.TransactionType.ToString(),
            Quantity = t.Quantity,
            FromLocationName = t.Location?.Name,
            ToLocationName = t.ToLocation?.Name,
            Reference = t.ReferenceType != null ? $"{t.ReferenceType}:{t.ReferenceId}" : null,
            Notes = t.Notes,
            TransactionDate = t.TransactionDate,
            PerformedByName = t.CreatedByUser != null
                ? $"{t.CreatedByUser.FirstName} {t.CreatedByUser.LastName}".Trim()
                : null
        }).ToList();
    }

    #endregion

    #region Maintenance Reports

    public async Task<OverdueMaintenanceReport> GetOverdueMaintenanceReportAsync()
    {
        var today = DateTime.UtcNow.Date;

        // Get overdue PM schedules
        var overduePMs = await _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(pm => pm.Asset)
            .Where(pm => pm.IsActive && !pm.IsDeleted)
            .Where(pm => pm.NextDueDate.HasValue && pm.NextDueDate.Value.Date < today)
            .OrderBy(pm => pm.NextDueDate)
            .ToListAsync();

        var overduePMItems = overduePMs.Select(pm => new OverduePMSchedule
        {
            ScheduleId = pm.Id,
            ScheduleName = pm.Name,
            AssetId = pm.AssetId,
            AssetName = pm.Asset?.Name,
            DueDate = pm.NextDueDate!.Value,
            DaysOverdue = (int)(today - pm.NextDueDate!.Value.Date).TotalDays,
            Priority = pm.Priority.ToString(),
            FrequencyDescription = GetFrequencyDescription(pm.FrequencyType, pm.FrequencyValue)
        }).ToList();

        // Get overdue work orders (not completed/cancelled, past scheduled end date)
        var overdueWorkOrders = await _unitOfWork.WorkOrders.Query()
            .Include(wo => wo.Asset)
            .Include(wo => wo.AssignedTo)
            .Where(wo => !wo.IsDeleted)
            .Where(wo => wo.Status != WorkOrderStatus.Completed && wo.Status != WorkOrderStatus.Cancelled)
            .Where(wo => wo.ScheduledEndDate.HasValue && wo.ScheduledEndDate.Value.Date < today)
            .OrderBy(wo => wo.ScheduledEndDate)
            .ToListAsync();

        var overdueWOItems = overdueWorkOrders.Select(wo => new OverdueWorkOrder
        {
            WorkOrderId = wo.Id,
            WorkOrderNumber = wo.WorkOrderNumber,
            Title = wo.Title,
            Type = wo.Type.ToString(),
            Priority = wo.Priority.ToString(),
            Status = wo.Status.ToString(),
            AssetId = wo.AssetId,
            AssetName = wo.Asset?.Name,
            ScheduledEndDate = wo.ScheduledEndDate,
            DaysOverdue = (int)(today - wo.ScheduledEndDate!.Value.Date).TotalDays,
            AssignedToName = wo.AssignedTo != null
                ? $"{wo.AssignedTo.FirstName} {wo.AssignedTo.LastName}".Trim()
                : null
        }).ToList();

        return new OverdueMaintenanceReport
        {
            TotalOverdue = overduePMItems.Count + overdueWOItems.Count,
            OverduePMCount = overduePMItems.Count,
            OverdueWorkOrderCount = overdueWOItems.Count,
            OverduePMSchedules = overduePMItems,
            OverdueWorkOrders = overdueWOItems
        };
    }

    public async Task<MaintenancePerformedReport> GetMaintenancePerformedReportAsync(MaintenancePerformedFilter? filter = null)
    {
        var query = _unitOfWork.WorkOrders.Query()
            .Include(wo => wo.Asset)
            .Include(wo => wo.AssignedTo)
            .Include(wo => wo.LaborEntries)
                .ThenInclude(le => le.User)
            .Include(wo => wo.Parts)
            .Where(wo => !wo.IsDeleted)
            .Where(wo => wo.Status == WorkOrderStatus.Completed)
            .AsQueryable();

        if (filter?.FromDate.HasValue == true)
        {
            query = query.Where(wo => wo.ActualEndDate >= filter.FromDate.Value);
        }

        if (filter?.ToDate.HasValue == true)
        {
            var endDate = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(wo => wo.ActualEndDate < endDate);
        }

        if (filter?.AssetId.HasValue == true)
        {
            query = query.Where(wo => wo.AssetId == filter.AssetId.Value);
        }

        if (filter?.TechnicianId.HasValue == true)
        {
            query = query.Where(wo => wo.AssignedToId == filter.TechnicianId.Value ||
                                      wo.LaborEntries.Any(le => le.UserId == filter.TechnicianId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter?.WorkOrderType) &&
            Enum.TryParse<WorkOrderType>(filter.WorkOrderType, true, out var woType))
        {
            query = query.Where(wo => wo.Type == woType);
        }

        var workOrders = await query
            .OrderByDescending(wo => wo.ActualEndDate)
            .Take(500)
            .ToListAsync();

        var items = workOrders.Select(wo =>
        {
            var laborHours = wo.LaborEntries.Sum(le => le.HoursWorked);
            var laborCost = wo.LaborEntries.Sum(le => le.HoursWorked * (le.HourlyRate ?? 0));
            var partsCost = wo.Parts.Sum(p => p.QuantityUsed * p.UnitCostAtTime);
            var completedBy = wo.LaborEntries
                .OrderByDescending(le => le.WorkDate)
                .FirstOrDefault()?.User;

            return new MaintenancePerformedItem
            {
                WorkOrderId = wo.Id,
                WorkOrderNumber = wo.WorkOrderNumber,
                Title = wo.Title,
                Type = wo.Type.ToString(),
                AssetId = wo.AssetId,
                AssetName = wo.Asset?.Name,
                CompletedDate = wo.ActualEndDate,
                LaborHours = laborHours,
                LaborCost = laborCost,
                PartsCost = partsCost,
                TotalCost = laborCost + partsCost,
                CompletedByName = completedBy != null
                    ? $"{completedBy.FirstName} {completedBy.LastName}".Trim()
                    : wo.AssignedTo != null
                        ? $"{wo.AssignedTo.FirstName} {wo.AssignedTo.LastName}".Trim()
                        : null
            };
        }).ToList();

        return new MaintenancePerformedReport
        {
            TotalWorkOrders = items.Count,
            TotalLaborHours = items.Sum(i => i.LaborHours),
            TotalLaborCost = items.Sum(i => i.LaborCost),
            TotalPartsCost = items.Sum(i => i.PartsCost),
            TotalCost = items.Sum(i => i.TotalCost),
            Items = items
        };
    }

    public async Task<PMComplianceReport> GetPMComplianceReportAsync(PMComplianceFilter? filter = null)
    {
        var fromDate = filter?.FromDate ?? DateTime.UtcNow.AddMonths(-12);
        var toDate = filter?.ToDate ?? DateTime.UtcNow;

        var query = _unitOfWork.PreventiveMaintenanceSchedules.Query()
            .Include(pm => pm.Asset)
            .Include(pm => pm.GeneratedWorkOrders)
            .Where(pm => !pm.IsDeleted)
            .AsQueryable();

        if (filter?.AssetId.HasValue == true)
        {
            query = query.Where(pm => pm.AssetId == filter.AssetId.Value);
        }

        var schedules = await query.ToListAsync();

        var items = new List<PMComplianceItem>();
        var totalScheduled = 0;
        var totalCompleted = 0;
        var totalMissed = 0;

        foreach (var schedule in schedules)
        {
            // Calculate expected occurrences in the date range based on frequency
            var expectedCount = CalculateExpectedOccurrences(schedule, fromDate, toDate);

            // Count completed work orders in the date range
            var completedCount = schedule.GeneratedWorkOrders
                .Count(wo => wo.Status == WorkOrderStatus.Completed &&
                            wo.ActualEndDate >= fromDate &&
                            wo.ActualEndDate <= toDate);

            var missedCount = Math.Max(0, expectedCount - completedCount);
            var complianceRate = expectedCount > 0
                ? (decimal)completedCount / expectedCount * 100
                : 100;

            items.Add(new PMComplianceItem
            {
                ScheduleId = schedule.Id,
                ScheduleName = schedule.Name,
                AssetId = schedule.AssetId,
                AssetName = schedule.Asset?.Name,
                FrequencyDescription = GetFrequencyDescription(schedule.FrequencyType, schedule.FrequencyValue),
                ScheduledCount = expectedCount,
                CompletedCount = completedCount,
                MissedCount = missedCount,
                ComplianceRate = Math.Round(complianceRate, 1)
            });

            totalScheduled += expectedCount;
            totalCompleted += completedCount;
            totalMissed += missedCount;
        }

        var overallComplianceRate = totalScheduled > 0
            ? (decimal)totalCompleted / totalScheduled * 100
            : 100;

        return new PMComplianceReport
        {
            TotalScheduled = totalScheduled,
            TotalCompleted = totalCompleted,
            TotalMissed = totalMissed,
            ComplianceRate = Math.Round(overallComplianceRate, 1),
            FromDate = fromDate,
            ToDate = toDate,
            Items = items.OrderBy(i => i.ComplianceRate).ToList()
        };
    }

    public async Task<WorkOrderSummaryReport> GetWorkOrderSummaryReportAsync(WorkOrderSummaryFilter? filter = null)
    {
        var query = _unitOfWork.WorkOrders.Query()
            .Where(wo => !wo.IsDeleted)
            .AsQueryable();

        if (filter?.FromDate.HasValue == true)
        {
            query = query.Where(wo => wo.CreatedAt >= filter.FromDate.Value);
        }

        if (filter?.ToDate.HasValue == true)
        {
            var endDate = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(wo => wo.CreatedAt < endDate);
        }

        var workOrders = await query.ToListAsync();
        var total = workOrders.Count;

        var byStatus = workOrders
            .GroupBy(wo => wo.Status)
            .Select(g => new WorkOrderCountByStatus
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round((decimal)g.Count() / total * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byType = workOrders
            .GroupBy(wo => wo.Type)
            .Select(g => new WorkOrderCountByType
            {
                Type = g.Key.ToString(),
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round((decimal)g.Count() / total * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byPriority = workOrders
            .GroupBy(wo => wo.Priority)
            .Select(g => new WorkOrderCountByPriority
            {
                Priority = g.Key.ToString(),
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round((decimal)g.Count() / total * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new WorkOrderSummaryReport
        {
            TotalWorkOrders = total,
            FromDate = filter?.FromDate,
            ToDate = filter?.ToDate,
            ByStatus = byStatus,
            ByType = byType,
            ByPriority = byPriority
        };
    }

    #endregion

    #region Asset Reports

    public async Task<AssetMaintenanceHistoryReport> GetAssetMaintenanceHistoryReportAsync(AssetMaintenanceHistoryFilter filter)
    {
        var asset = await _unitOfWork.Assets.Query()
            .FirstOrDefaultAsync(a => a.Id == filter.AssetId && !a.IsDeleted);

        if (asset == null)
        {
            return new AssetMaintenanceHistoryReport
            {
                AssetId = filter.AssetId,
                AssetTag = "Unknown",
                AssetName = "Asset not found"
            };
        }

        var query = _unitOfWork.WorkOrders.Query()
            .Include(wo => wo.AssignedTo)
            .Include(wo => wo.LaborEntries)
            .Include(wo => wo.Parts)
            .Where(wo => !wo.IsDeleted)
            .Where(wo => wo.AssetId == filter.AssetId)
            .AsQueryable();

        if (filter.FromDate.HasValue)
        {
            query = query.Where(wo => wo.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var endDate = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(wo => wo.CreatedAt < endDate);
        }

        var workOrders = await query
            .OrderByDescending(wo => wo.CreatedAt)
            .ToListAsync();

        var items = workOrders.Select(wo =>
        {
            var laborHours = wo.LaborEntries.Sum(le => le.HoursWorked);
            var laborCost = wo.LaborEntries.Sum(le => le.HoursWorked * (le.HourlyRate ?? 0));
            var partsCost = wo.Parts.Sum(p => p.QuantityUsed * p.UnitCostAtTime);

            return new AssetMaintenanceHistoryItem
            {
                WorkOrderId = wo.Id,
                WorkOrderNumber = wo.WorkOrderNumber,
                Title = wo.Title,
                Type = wo.Type.ToString(),
                Status = wo.Status.ToString(),
                Priority = wo.Priority.ToString(),
                CompletedDate = wo.ActualEndDate,
                LaborHours = laborHours,
                TotalCost = laborCost + partsCost,
                TechnicianName = wo.AssignedTo != null
                    ? $"{wo.AssignedTo.FirstName} {wo.AssignedTo.LastName}".Trim()
                    : null
            };
        }).ToList();

        return new AssetMaintenanceHistoryReport
        {
            AssetId = asset.Id,
            AssetTag = asset.AssetTag,
            AssetName = asset.Name,
            TotalWorkOrders = items.Count,
            TotalLaborHours = items.Sum(i => i.LaborHours),
            TotalCost = items.Sum(i => i.TotalCost),
            Items = items
        };
    }

    #endregion

    #region Helper Methods

    private static string GetFrequencyDescription(FrequencyType frequencyType, int frequencyValue)
    {
        if (frequencyValue == 1)
        {
            return frequencyType switch
            {
                FrequencyType.Daily => "Daily",
                FrequencyType.Weekly => "Weekly",
                FrequencyType.BiWeekly => "Bi-Weekly",
                FrequencyType.Monthly => "Monthly",
                FrequencyType.Quarterly => "Quarterly",
                FrequencyType.SemiAnnually => "Semi-Annually",
                FrequencyType.Annually => "Annually",
                FrequencyType.Custom => "Custom",
                _ => frequencyType.ToString()
            };
        }

        return frequencyType switch
        {
            FrequencyType.Daily => $"Every {frequencyValue} days",
            FrequencyType.Weekly => $"Every {frequencyValue} weeks",
            FrequencyType.Monthly => $"Every {frequencyValue} months",
            _ => $"Every {frequencyValue} {frequencyType.ToString().ToLower()}"
        };
    }

    private static int CalculateExpectedOccurrences(PreventiveMaintenanceSchedule schedule, DateTime fromDate, DateTime toDate)
    {
        if (!schedule.IsActive)
            return 0;

        var days = (toDate - fromDate).TotalDays;

        return schedule.FrequencyType switch
        {
            FrequencyType.Daily => (int)(days / schedule.FrequencyValue),
            FrequencyType.Weekly => (int)(days / (7 * schedule.FrequencyValue)),
            FrequencyType.BiWeekly => (int)(days / (14 * schedule.FrequencyValue)),
            FrequencyType.Monthly => (int)(days / (30 * schedule.FrequencyValue)),
            FrequencyType.Quarterly => (int)(days / (90 * schedule.FrequencyValue)),
            FrequencyType.SemiAnnually => (int)(days / (180 * schedule.FrequencyValue)),
            FrequencyType.Annually => (int)(days / (365 * schedule.FrequencyValue)),
            FrequencyType.Custom => (int)(days / (schedule.FrequencyValue > 0 ? schedule.FrequencyValue : 30)),
            _ => 0
        };
    }

    #endregion
}
