using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IDashboardService
{
    Task<DashboardIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        int? branchId,
        int? year,
        int? term,
        CancellationToken cancellationToken = default);
}
