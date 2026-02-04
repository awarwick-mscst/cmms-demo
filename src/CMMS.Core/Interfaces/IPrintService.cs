using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IPrintService
{
    Task<string> GenerateLabelCommandsAsync(Part part, LabelTemplate template, PrinterLanguage language, CancellationToken cancellationToken = default);
    Task<bool> PrintLabelAsync(Part part, LabelTemplate template, LabelPrinter printer, int quantity = 1, CancellationToken cancellationToken = default);
    Task<bool> TestPrinterConnectionAsync(LabelPrinter printer, CancellationToken cancellationToken = default);
    Task<bool> PrintTestLabelAsync(LabelPrinter printer, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetWindowsPrintersAsync(CancellationToken cancellationToken = default);
}
