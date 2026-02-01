using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IPrintService
{
    Task<string> GenerateZplAsync(Part part, LabelTemplate template, CancellationToken cancellationToken = default);
    Task<bool> PrintLabelAsync(Part part, LabelTemplate template, LabelPrinter printer, int quantity = 1, CancellationToken cancellationToken = default);
    Task<bool> TestPrinterConnectionAsync(LabelPrinter printer, CancellationToken cancellationToken = default);
}
