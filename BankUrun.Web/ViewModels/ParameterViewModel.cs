using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class ParameterIndexViewModel
{
    public IReadOnlyList<ParameterGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<ParameterProductOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<int> Years { get; set; } = [];
    public ParameterPageViewModel Page { get; set; } = new();
}

public class ParameterGroupOptionViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public string Label => $"{GroupNo} - {Name}";
}

public class ParameterProductOptionViewModel
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{Year}/{Term} · {Code} - {Name}";
}

public class ParameterBranchOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public string Label => $"{BranchCode} - {Name}";
}

public class ParameterQuery
{
    public int? GroupId { get; set; }
    public int? Year { get; set; }
    public int? Term { get; set; }
    public string Search { get; set; } = string.Empty;
    public MainProductCalculationType? CalculationType { get; set; }
    public string SortKey { get; set; } = "year";
    public string SortDirection { get; set; } = "desc";
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
    public int ParameterId { get; set; }
    public int GroupId { get; set; }
    public int MainProductInstanceId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public MainProductCalculationType CalculationType { get; set; }
    public decimal CriterionScore { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<MainProductSegmentRuleViewModel> Rules { get; set; } = [];
    public IReadOnlyList<ParameterBranchOptionViewModel> Branches { get; set; } = [];
}

public class MainProductSegmentRuleViewModel
{
    public int Id { get; set; }
    public PerformanceSegment PerformanceSegment { get; set; }
    public int SortOrder { get; set; }
    public decimal TargetShare { get; set; }
    public decimal SizeShare { get; set; }
    public decimal ScaleShare { get; set; }
    public decimal AllocatedScore { get; set; }
    public decimal HgoWeight { get; set; }
    public decimal DevelopmentWeight { get; set; }
    public decimal SizeWeight { get; set; }
}

public class ParameterTargetEditorViewModel
{
    public int ParameterId { get; set; }
    public int BranchId { get; set; }
    public string BranchLabel { get; set; } = string.Empty;
    public IReadOnlyList<ParameterMonthlyMetricViewModel> Months { get; set; } = [];
}

public class ParameterMonthlyMetricViewModel
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public DateOnly? ActualAsOfDate { get; set; }
}

public class MainProductParameterInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }

    [Range(1, int.MaxValue)]
    public int MainProductInstanceId { get; set; }

    [EnumDataType(typeof(MainProductCalculationType))]
    public MainProductCalculationType CalculationType { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal CriterionScore { get; set; }

    public bool IsActive { get; set; } = true;

    public List<MainProductSegmentRuleInput> Rules { get; set; } = [];
}

public class MainProductSegmentRuleInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [EnumDataType(typeof(PerformanceSegment))]
    public PerformanceSegment PerformanceSegment { get; set; }

    [Range(1, int.MaxValue)]
    public int SortOrder { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal TargetShare { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal SizeShare { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal ScaleShare { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal AllocatedScore { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal HgoWeight { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal DevelopmentWeight { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal SizeWeight { get; set; }
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
