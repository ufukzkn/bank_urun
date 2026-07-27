using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface ITargetManagementService
{
    Task<TargetIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<TargetPageViewModel> GetPageAsync(TargetQuery query, CancellationToken cancellationToken = default);
    Task<TargetEditorViewModel> GetEditorAsync(int parameterId, int portfolioId, CancellationToken cancellationToken = default);
    Task UpdateTargetsAsync(TargetPeriodInput input, string actor, CancellationToken cancellationToken = default);
    Task<TargetWorkbookResult> ExportAsync(TargetQuery query, TargetEntryMode entryMode, bool templateOnly, CancellationToken cancellationToken = default);
    Task<TargetWorkbookResult> ExportSelectedAsync(
        IReadOnlyCollection<TargetContextKey> contextKeys,
        TargetEntryMode entryMode,
        CancellationToken cancellationToken = default);
    Task<TargetImportPreviewViewModel> PreviewImportAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task ConfirmImportAsync(string token, string actor, CancellationToken cancellationToken = default);
}
