using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class LabelService : ILabelService
{
    private readonly IUnitOfWork _unitOfWork;

    public LabelService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Template CRUD

    public async Task<IEnumerable<LabelTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LabelTemplates.Query()
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<LabelTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LabelTemplates.GetByIdAsync(id, cancellationToken);
    }

    public async Task<LabelTemplate?> GetDefaultTemplateAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LabelTemplates.Query()
            .FirstOrDefaultAsync(t => t.IsDefault, cancellationToken);
    }

    public async Task<LabelTemplate> CreateTemplateAsync(LabelTemplate template, int createdBy, CancellationToken cancellationToken = default)
    {
        template.CreatedBy = createdBy;
        template.CreatedAt = DateTime.UtcNow;

        if (template.IsDefault)
        {
            await ClearDefaultTemplatesAsync(cancellationToken);
        }

        await _unitOfWork.LabelTemplates.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template;
    }

    public async Task<LabelTemplate> UpdateTemplateAsync(LabelTemplate template, int updatedBy, CancellationToken cancellationToken = default)
    {
        template.UpdatedBy = updatedBy;
        template.UpdatedAt = DateTime.UtcNow;

        if (template.IsDefault)
        {
            await ClearDefaultTemplatesAsync(cancellationToken, template.Id);
        }

        _unitOfWork.LabelTemplates.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template;
    }

    public async Task<bool> DeleteTemplateAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var template = await _unitOfWork.LabelTemplates.GetByIdAsync(id, cancellationToken);
        if (template == null)
            return false;

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.UpdatedBy = deletedBy;
        template.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetDefaultTemplateAsync(int id, int updatedBy, CancellationToken cancellationToken = default)
    {
        var template = await _unitOfWork.LabelTemplates.GetByIdAsync(id, cancellationToken);
        if (template == null)
            return false;

        await ClearDefaultTemplatesAsync(cancellationToken, id);

        template.IsDefault = true;
        template.UpdatedBy = updatedBy;
        template.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ClearDefaultTemplatesAsync(CancellationToken cancellationToken, int? exceptId = null)
    {
        var defaultTemplates = await _unitOfWork.LabelTemplates.Query()
            .Where(t => t.IsDefault && (exceptId == null || t.Id != exceptId))
            .ToListAsync(cancellationToken);

        foreach (var t in defaultTemplates)
        {
            t.IsDefault = false;
            t.UpdatedAt = DateTime.UtcNow;
        }
    }

    #endregion

    #region Printer CRUD

    public async Task<IEnumerable<LabelPrinter>> GetPrintersAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.LabelPrinters.Query();

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<LabelPrinter?> GetPrinterByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LabelPrinters.GetByIdAsync(id, cancellationToken);
    }

    public async Task<LabelPrinter?> GetDefaultPrinterAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LabelPrinters.Query()
            .Where(p => p.IsActive)
            .FirstOrDefaultAsync(p => p.IsDefault, cancellationToken);
    }

    public async Task<LabelPrinter> CreatePrinterAsync(LabelPrinter printer, CancellationToken cancellationToken = default)
    {
        printer.CreatedAt = DateTime.UtcNow;

        if (printer.IsDefault)
        {
            await ClearDefaultPrintersAsync(cancellationToken);
        }

        await _unitOfWork.LabelPrinters.AddAsync(printer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return printer;
    }

    public async Task<LabelPrinter> UpdatePrinterAsync(LabelPrinter printer, CancellationToken cancellationToken = default)
    {
        printer.UpdatedAt = DateTime.UtcNow;

        if (printer.IsDefault)
        {
            await ClearDefaultPrintersAsync(cancellationToken, printer.Id);
        }

        _unitOfWork.LabelPrinters.Update(printer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return printer;
    }

    public async Task<bool> DeletePrinterAsync(int id, CancellationToken cancellationToken = default)
    {
        var printer = await _unitOfWork.LabelPrinters.GetByIdAsync(id, cancellationToken);
        if (printer == null)
            return false;

        printer.IsDeleted = true;
        printer.DeletedAt = DateTime.UtcNow;
        printer.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetDefaultPrinterAsync(int id, CancellationToken cancellationToken = default)
    {
        var printer = await _unitOfWork.LabelPrinters.GetByIdAsync(id, cancellationToken);
        if (printer == null)
            return false;

        await ClearDefaultPrintersAsync(cancellationToken, id);

        printer.IsDefault = true;
        printer.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ClearDefaultPrintersAsync(CancellationToken cancellationToken, int? exceptId = null)
    {
        var defaultPrinters = await _unitOfWork.LabelPrinters.Query()
            .Where(p => p.IsDefault && (exceptId == null || p.Id != exceptId))
            .ToListAsync(cancellationToken);

        foreach (var p in defaultPrinters)
        {
            p.IsDefault = false;
            p.UpdatedAt = DateTime.UtcNow;
        }
    }

    #endregion
}
