using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface ILabelService
{
    // Template CRUD
    Task<IEnumerable<LabelTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<LabelTemplate?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LabelTemplate?> GetDefaultTemplateAsync(CancellationToken cancellationToken = default);
    Task<LabelTemplate> CreateTemplateAsync(LabelTemplate template, int createdBy, CancellationToken cancellationToken = default);
    Task<LabelTemplate> UpdateTemplateAsync(LabelTemplate template, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteTemplateAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
    Task<bool> SetDefaultTemplateAsync(int id, int updatedBy, CancellationToken cancellationToken = default);

    // Printer CRUD
    Task<IEnumerable<LabelPrinter>> GetPrintersAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<LabelPrinter?> GetPrinterByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LabelPrinter?> GetDefaultPrinterAsync(CancellationToken cancellationToken = default);
    Task<LabelPrinter> CreatePrinterAsync(LabelPrinter printer, CancellationToken cancellationToken = default);
    Task<LabelPrinter> UpdatePrinterAsync(LabelPrinter printer, CancellationToken cancellationToken = default);
    Task<bool> DeletePrinterAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> SetDefaultPrinterAsync(int id, CancellationToken cancellationToken = default);
}
