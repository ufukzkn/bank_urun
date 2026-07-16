using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class DashboardIndexViewModel
{
    public IReadOnlyList<ParameterGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<ParameterBranchOptionViewModel> Branches { get; set; } = [];
    public IReadOnlyList<ParameterProductOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<DashboardPeriodOptionViewModel> Periods { get; set; } = [];
    public PerformanceMode SelectedMode { get; set; } = PerformanceMode.BranchProduct;
    public int SelectedYear { get; set; }
    public int SelectedTerm { get; set; }
    public DateOnly BatchDate { get; set; }
    public DashboardSnapshotViewModel Snapshot { get; set; } = new();
}

public class DashboardPeriodOptionViewModel
{
    public int Year { get; set; }
    public int Term { get; set; }
    public string Label => $"{Year} / {Term}. Dönem";
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
    public int Year { get; set; }
    public int Term { get; set; }
    public decimal AssignedScore { get; set; }
    public decimal? EarnedScore { get; set; }
    public decimal? SuccessPercent { get; set; }
    public bool HasCompletePeriodData { get; set; }
    public int? BranchRank { get; set; }
    public int RankedBranchCount { get; set; }
    public IReadOnlyList<DashboardBranchPerformanceViewModel> Branches { get; set; } = [];
    public IReadOnlyList<DashboardProductPerformanceViewModel> BranchProducts { get; set; } = [];
    public IReadOnlyList<DashboardMainProductPerformanceViewModel> MainProducts { get; set; } = [];
}

public class DashboardBranchPerformanceViewModel
{
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
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int MainProductInstanceId { get; set; }
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
    public int SubProductCount { get; set; }
}

public class DashboardMainProductPerformanceViewModel
{
    public int MainProductInstanceId { get; set; }
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
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
}
