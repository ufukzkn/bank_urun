using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IDashboardService
{
    Task<DashboardIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        PerformanceMode mode,
        int? groupId,
        int? branchId,
        int? year,
        int? term,
        int? mainProductInstanceId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthlyDetailAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthlyDetailAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default);
}
