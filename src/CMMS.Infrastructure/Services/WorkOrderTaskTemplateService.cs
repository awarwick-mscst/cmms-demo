using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class WorkOrderTaskTemplateService : IWorkOrderTaskTemplateService
{
    private readonly IUnitOfWork _unitOfWork;

    public WorkOrderTaskTemplateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<WorkOrderTaskTemplate>> GetTemplatesAsync(WorkOrderTaskTemplateFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.WorkOrderTaskTemplates.Query()
            .Include(t => t.Items)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(search) ||
                (t.Description != null && t.Description.ToLower().Contains(search)));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == filter.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "createdat" => filter.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            "updatedat" => filter.SortDescending ? query.OrderByDescending(t => t.UpdatedAt) : query.OrderBy(t => t.UpdatedAt),
            _ => query.OrderBy(t => t.Name)
        };

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkOrderTaskTemplate>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<WorkOrderTaskTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrderTaskTemplates.Query()
            .Include(t => t.Items)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderTaskTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.WorkOrderTaskTemplates.Query()
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<WorkOrderTaskTemplate> CreateTemplateAsync(WorkOrderTaskTemplate template, IEnumerable<WorkOrderTaskTemplateItem> items, int createdBy, CancellationToken cancellationToken = default)
    {
        template.CreatedBy = createdBy;
        template.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.WorkOrderTaskTemplates.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Add items with the template ID
        var sortOrder = 0;
        foreach (var item in items)
        {
            item.TemplateId = template.Id;
            item.SortOrder = sortOrder++;
            await _unitOfWork.WorkOrderTaskTemplateItems.AddAsync(item, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetTemplateByIdAsync(template.Id, cancellationToken) ?? template;
    }

    public async Task<WorkOrderTaskTemplate> UpdateTemplateAsync(WorkOrderTaskTemplate template, IEnumerable<WorkOrderTaskTemplateItem> items, int updatedBy, CancellationToken cancellationToken = default)
    {
        var existingTemplate = await _unitOfWork.WorkOrderTaskTemplates.Query()
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == template.Id, cancellationToken);

        if (existingTemplate == null)
        {
            throw new InvalidOperationException("Template not found");
        }

        // Update template properties
        existingTemplate.Name = template.Name;
        existingTemplate.Description = template.Description;
        existingTemplate.IsActive = template.IsActive;
        existingTemplate.UpdatedBy = updatedBy;
        existingTemplate.UpdatedAt = DateTime.UtcNow;

        // Get existing item IDs
        var existingItemIds = existingTemplate.Items.Select(i => i.Id).ToHashSet();
        var itemsList = items.ToList();
        var updatedItemIds = itemsList.Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();

        // Delete items that are no longer in the list
        foreach (var item in existingTemplate.Items.ToList())
        {
            if (!updatedItemIds.Contains(item.Id))
            {
                _unitOfWork.WorkOrderTaskTemplateItems.Remove(item);
            }
        }

        // Update or add items
        var sortOrder = 0;
        foreach (var item in itemsList)
        {
            item.SortOrder = sortOrder++;
            item.TemplateId = template.Id;

            if (item.Id > 0 && existingItemIds.Contains(item.Id))
            {
                // Update existing item
                var existingItem = existingTemplate.Items.First(i => i.Id == item.Id);
                existingItem.Description = item.Description;
                existingItem.IsRequired = item.IsRequired;
                existingItem.SortOrder = item.SortOrder;
            }
            else
            {
                // Add new item
                item.Id = 0; // Reset ID for new items
                await _unitOfWork.WorkOrderTaskTemplateItems.AddAsync(item, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetTemplateByIdAsync(template.Id, cancellationToken) ?? existingTemplate;
    }

    public async Task<bool> DeleteTemplateAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var template = await _unitOfWork.WorkOrderTaskTemplates.GetByIdAsync(id, cancellationToken);
        if (template == null)
        {
            return false;
        }

        // Soft delete
        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.UpdatedBy = deletedBy;
        template.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
