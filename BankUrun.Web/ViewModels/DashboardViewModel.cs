using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class DashboardIndexViewModel
{
    public IReadOnlyList<ParameterGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<DashboardBranchOptionViewModel> Branches { get; set; } = [];
    public IReadOnlyList<DashboardProductOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<DashboardProductGamutOptionViewModel> ProductGamuts { get; set; } = [];
    public IReadOnlyList<DashboardPortfolioTypeOptionViewModel> PortfolioTypes { get; set; } = [];
    public IReadOnlyList<DashboardPeriodOptionViewModel> Periods { get; set; } = [];
    public PerformanceMode SelectedMode { get; set; } = PerformanceMode.BranchProduct;
    public int? SelectedYear { get; set; }
    public int? SelectedTerm { get; set; }
    public DateOnly BatchDate { get; set; }
}

public class DashboardBranchOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupType GroupType { get; set; }
    public string Label => $"{BranchCode} - {Name}";
}

public class DashboardProductOptionViewModel
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{Code} - {Name}";
}

public class DashboardProductGamutOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{Code} - {Name}";
}

public class DashboardPortfolioTypeOptionViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{Code} - {Name}";
}

public class DashboardPeriodOptionViewModel
{
    public int Year { get; set; }
    public int Term { get; set; }
    public string Label => $"{Year} / {Term}. Dönem";
}

public sealed class PerformanceQuery
{
    public PerformanceMode Mode { get; set; } = PerformanceMode.BranchProduct;
    public int? GroupId { get; set; }
    public int? BranchId { get; set; }
    public int? Year { get; set; }
    public int? Term { get; set; }
    public int? MainProductId { get; set; }
    public int? ProductGamutId { get; set; }
    public int? PortfolioTypeId { get; set; }
    public string? Search { get; set; }
    public string? SortKey { get; set; }
    public string? SortDirection { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public bool ForceRefresh { get; set; }
}

public interface IPerformancePage
{
    int TotalCount { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalPages { get; }
    int ItemCount { get; }
}

public sealed class PerformancePage<T> : IPerformancePage
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public int ItemCount => Items.Count;
}

public sealed class DashboardPerformanceTimingViewModel
{
    public bool CacheHit { get; set; }
    public int PeriodCount { get; set; }
    public int FactCount { get; set; }
    public int CandidateCount { get; set; }
    public int ReturnedCount { get; set; }
    public double DatabaseMilliseconds { get; set; }
    public double CalculationMilliseconds { get; set; }
    public double TotalMilliseconds { get; set; }
    public double CacheRemainingMilliseconds { get; set; }

    public string ServerTiming =>
        FormattableString.Invariant(
            $"db;dur={DatabaseMilliseconds:0.##}, calc;dur={CalculationMilliseconds:0.##}, total;dur={TotalMilliseconds:0.##}");
}

public class DashboardSnapshotViewModel
{
    public PerformanceMode Mode { get; set; } = PerformanceMode.BranchProduct;
    public bool HasSelectedBranch { get; set; }
    public int? GroupId { get; set; }
    public int? BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int? Term { get; set; }
    public string PeriodScopeLabel => Year.HasValue
        ? Term.HasValue ? $"{Year} / {Term}. Dönem" : $"{Year} / Tüm dönemler"
        : Term.HasValue ? $"Tüm yıllar / {Term}. Dönem" : "Tüm yıllar / Tüm dönemler";
    public decimal AssignedScore { get; set; }
    public decimal? EarnedScore { get; set; }
    public decimal? SuccessPercent { get; set; }
    public bool HasCompletePeriodData { get; set; }
    public int? BranchRank { get; set; }
    public int RankedBranchCount { get; set; }
    public IPerformancePage Results { get; set; } = new PerformancePage<object>();
    public int TotalCount => Results.TotalCount;
    public int TotalPages => Results.TotalPages;
    public int Page => Results.Page;
    public int PageSize => Results.PageSize;
    public DashboardPerformanceTimingViewModel Timing { get; set; } = new();

    public PerformancePage<T> GetResults<T>() =>
        Results as PerformancePage<T> ?? new PerformancePage<T>();
}

public class DashboardBranchPerformanceViewModel
{
    public int Year { get; set; }
    public int Term { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal CriterionScore { get; set; }
    public decimal? HgoScore { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? SuccessPercent { get; set; }
    public bool HasCompletePeriodData { get; set; }
    public int CompleteProductCount { get; set; }
    public int ProductCount { get; set; }
    public int? Rank { get; set; }
    public int RankCandidateCount { get; set; }
}

public class DashboardProductPerformanceViewModel
{
    public int Year { get; set; }
    public int Term { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int MainProductInstanceId { get; set; }
    public int MainProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public MainProductCalculationType CalculationType { get; set; }
    public decimal CriterionScore { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public decimal? HgRatioPercent { get; set; }
    public decimal? HgoScore { get; set; }
    public decimal? TotalScore { get; set; }
    public int? SegmentRank { get; set; }
    public int SegmentRankCandidateCount { get; set; }
    public bool HasCompleteBatchData { get; set; }
    public bool HasSubProductConfiguration { get; set; }
    public bool HasCompleteTargetData { get; set; }
    public bool HasParameterConfiguration { get; set; }
    public int SubProductCount { get; set; }
}

public class DashboardMainProductPerformanceViewModel
{
    public int Year { get; set; }
    public int Term { get; set; }
    public int MainProductInstanceId { get; set; }
    public int MainProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int SubProductCount { get; set; }
    public int BranchCount { get; set; }
    public decimal CriterionScore { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public decimal? HgRatioPercent { get; set; }
    public decimal? HgoScore { get; set; }
    public decimal? TotalScore { get; set; }
    public int? Rank { get; set; }
    public int RankCandidateCount { get; set; }
    public bool HasCompleteBatchData { get; set; }
    public bool HasSubProductConfiguration { get; set; }
    public bool HasCompleteTargetData { get; set; }
    public bool HasParameterConfiguration { get; set; }
}

public class DashboardPortfolioPerformanceViewModel
{
    public int Year { get; set; }
    public int Term { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int PortfolioId { get; set; }
    public string PortfolioCode { get; set; } = string.Empty;
    public string PortfolioName { get; set; } = string.Empty;
    public int ProductGamutId { get; set; }
    public string ProductGamutCode { get; set; } = string.Empty;
    public string ProductGamutName { get; set; } = string.Empty;
    public int PortfolioTypeId { get; set; }
    public string PortfolioTypeCode { get; set; } = string.Empty;
    public string PortfolioTypeName { get; set; } = string.Empty;
    public decimal CriterionScore { get; set; }
    public decimal? HgoScore { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? SuccessPercent { get; set; }
    public bool HasCompletePeriodData { get; set; }
    public int CompleteProductCount { get; set; }
    public int ProductCount { get; set; }
    public int? OfficialRank { get; set; }
    public int OfficialRankCandidateCount { get; set; }
    public int? BranchRank { get; set; }
    public int BranchRankCandidateCount { get; set; }
}

public class DashboardMonthlyDetailViewModel
{
    public PerformanceMode Mode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public MainProductCalculationType CalculationType { get; set; }
    public IReadOnlyList<DashboardProductMonthViewModel> Months { get; set; } = [];
    public IReadOnlyList<DashboardSubProductContributionViewModel> Contributions { get; set; } = [];
}

public class DashboardProductMonthViewModel
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public DateOnly? ActualAsOfDate { get; set; }
    public decimal? HgRatioPercent { get; set; }
}

public class DashboardSubProductContributionViewModel
{
    public int SubProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? ActualValue { get; set; }
}

public class DashboardPortfolioDetailViewModel
{
    public int PortfolioId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public IReadOnlyList<DashboardPortfolioProductDetailViewModel> Products { get; set; } = [];
}

public class DashboardPortfolioProductDetailViewModel
{
    public int MainProductInstanceId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public MainProductCalculationType CalculationType { get; set; }
    public bool HasParameterConfiguration { get; set; }
    public bool HasCompleteTargetData { get; set; }
    public decimal CriterionScore { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public decimal? HgRatioPercent { get; set; }
    public decimal? TotalScore { get; set; }
    public IReadOnlyList<DashboardProductMonthViewModel> Months { get; set; } = [];
    public IReadOnlyList<DashboardSubProductContributionViewModel> Contributions { get; set; } = [];
}
