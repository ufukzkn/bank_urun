using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class DashboardIndexViewModel
{
    public IReadOnlyList<ParameterGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<ParameterBranchOptionViewModel> Branches { get; set; } = [];
    public IReadOnlyList<ParameterProductOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<int> Years { get; set; } = [];
    public int SelectedGroupId { get; set; }
    public int SelectedBranchId { get; set; }
    public int SelectedYear { get; set; }
    public int SelectedTerm { get; set; }
    public DateOnly BatchDate { get; set; }
    public DashboardSnapshotViewModel Snapshot { get; set; } = new();
}

public class DashboardSnapshotViewModel
{
    public int GroupId { get; set; }
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public decimal AssignedScore { get; set; }
    public decimal EligibleScore { get; set; }
    public decimal EarnedScore { get; set; }
    public decimal SuccessPercent { get; set; }
    public int ActiveProductCount { get; set; }
    public int CompleteProductCount { get; set; }
    public int PendingProductCount { get; set; }
    public int? BranchRank { get; set; }
    public int RankedBranchCount { get; set; }
    public IReadOnlyList<DashboardBranchPerformanceViewModel> BranchRanking { get; set; } = [];
    public IReadOnlyList<DashboardProductPerformanceViewModel> Products { get; set; } = [];
}

public class DashboardBranchPerformanceViewModel
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public decimal AssignedScore { get; set; }
    public decimal EarnedScore { get; set; }
    public decimal EligibleScore { get; set; }
    public decimal SuccessPercent { get; set; }
    public int ActiveProductCount { get; set; }
    public int CompleteProductCount { get; set; }
    public int PendingProductCount { get; set; }
    public int? Rank { get; set; }
    public bool IsSelected { get; set; }
}

public class DashboardProductPerformanceViewModel
{
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
    public IReadOnlyList<DashboardProductMonthViewModel> Months { get; set; } = [];
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
