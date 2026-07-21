using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IDashboardService
{
    Task<DashboardIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        PerformanceQuery query,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetBranchProductDetailHeaderAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthsAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetBranchProductContributionsAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthlyDetailAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetMainProductDetailHeaderAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthsAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetMainProductContributionsAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default);
    Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthlyDetailAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default);
    Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailHeaderAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default);
    Task<DashboardPortfolioDetailViewModel?> GetPortfolioProductsAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default);
    Task<DashboardPortfolioDetailViewModel?> GetPortfolioMonthsAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default);
    Task<DashboardPortfolioDetailViewModel?> GetPortfolioContributionsAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default);
    Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default);
}
