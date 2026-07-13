using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class ParameterIndexViewModel
{
    public IReadOnlyList<ParameterBranchOptionViewModel> Branches { get; set; } = [];
    public IReadOnlyList<int> Years { get; set; } = [];
    public int SelectedBranchId { get; set; }
    public int SelectedYear { get; set; }
    public int SelectedTerm { get; set; }
    public DateOnly BatchDate { get; set; }
    public ParameterPageViewModel Page { get; set; } = new();
}

public class ParameterBranchOptionViewModel
{
    public int Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public string Label => $"{BranchCode} - {Name}";
    public string Description => $"{GroupNo} - {GroupName} / {GroupSegment}";
}

public class ParameterQuery
{
    public int? BranchId { get; set; }
    public int? Year { get; set; }
    public int? Term { get; set; }
    public string Search { get; set; } = string.Empty;
    public MainProductCalculationType? CalculationType { get; set; }
    public string SortKey { get; set; } = "product";
    public string SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ParameterPageViewModel
{
    public IReadOnlyList<ParameterRowViewModel> Rows { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; } = 1;
}

public class ParameterRowViewModel
{
    public int MainProductInstanceId { get; set; }
    public int? ParameterId { get; set; }
    public int BranchId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public MainProductCalculationType? CalculationType { get; set; }
    public decimal? CriterionScore { get; set; }
    public bool IsActive { get; set; }
    public decimal PeriodTarget { get; set; }
    public decimal? PeriodActual { get; set; }
    public decimal? HgRatio { get; set; }
    public decimal? HgoScore { get; set; }
    public decimal? TotalScore { get; set; }
    public int? ProductRank { get; set; }
    public int? SegmentRank { get; set; }
    public bool HasCompleteBatchData { get; set; }
    public IReadOnlyList<ParameterMonthlyMetricViewModel> Months { get; set; } = [];
}

public class ParameterMonthlyMetricViewModel
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public DateOnly? ActualAsOfDate { get; set; }
    public bool IsIncludedInCalculation { get; set; }
    public bool IsBatchExpected { get; set; }
}

public class MainProductParameterInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int MainProductInstanceId { get; set; }

    [EnumDataType(typeof(MainProductCalculationType))]
    public MainProductCalculationType CalculationType { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal CriterionScore { get; set; }

    public bool IsActive { get; set; } = true;
}

public class MonthlyTargetsInput
{
    [Range(1, int.MaxValue)]
    public int ParameterId { get; set; }

    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    public List<MonthlyTargetInput> Months { get; set; } = [];
}

public class MonthlyTargetInput
{
    [Range(1, 12)]
    public int Month { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal TargetValue { get; set; }
}

public class ParameterIdInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
