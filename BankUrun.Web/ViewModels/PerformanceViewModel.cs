using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class PerformanceIndexViewModel
{
    public IReadOnlyList<PerformanceGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<PerformanceBranchOptionViewModel> Branches { get; set; } = [];
    public IReadOnlyList<PerformanceProductOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<PerformanceParameterRowViewModel> Parameters { get; set; } = [];
    public IReadOnlyList<PerformanceResultRowViewModel> Results { get; set; } = [];
}

public class PerformanceGroupOptionViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{GroupNo} - {Name}";
}

public class PerformanceBranchOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    public string Label => $"{BranchCode} - {Name}";
}

public class PerformanceProductOptionViewModel
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public string SubProductCode { get; set; } = string.Empty;
    public string SubProductName { get; set; } = string.Empty;
    public string Label => $"{Year}/{Term} {MainProductCode} - {SubProductCode} {SubProductName}";
}

public class PerformanceParameterRowViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int SubProductInstanceId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public string SubProductCode { get; set; } = string.Empty;
    public string SubProductName { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<PerformanceSegmentRuleViewModel> Rules { get; set; } = [];
}

public class PerformanceSegmentRuleViewModel
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

public class PerformanceResultRowViewModel
{
    public int BranchId { get; set; }
    public int GroupId { get; set; }
    public int ParameterId { get; set; }
    public int RuleId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public string SubProductCode { get; set; } = string.Empty;
    public string SubProductName { get; set; } = string.Empty;
    public PerformanceSegment PerformanceSegment { get; set; }
    public decimal AllocatedScore { get; set; }
    public decimal HgoAchievement { get; set; }
    public decimal DevelopmentAchievement { get; set; }
    public decimal SizeAchievement { get; set; }
    public decimal HgoWeight { get; set; }
    public decimal DevelopmentWeight { get; set; }
    public decimal SizeWeight { get; set; }
    public decimal WeightedAchievement { get; set; }
    public decimal EarnedScore { get; set; }
    public bool IsMissing { get; set; }
}

public class PerformanceParameterInput
{
    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }

    [Range(1, int.MaxValue)]
    public int SubProductInstanceId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal TotalScore { get; set; }
}

public class PerformanceParameterUpdateInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal TotalScore { get; set; }

    public bool IsActive { get; set; } = true;
    public List<PerformanceSegmentRuleInput> Rules { get; set; } = [];
}

public class PerformanceSegmentRuleInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    public PerformanceSegment PerformanceSegment { get; set; }
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

public class MetricResultInput
{
    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Range(1, int.MaxValue)]
    public int RuleId { get; set; }

    [Range(typeof(decimal), "0", "200")]
    public decimal HgoAchievement { get; set; }

    [Range(typeof(decimal), "0", "200")]
    public decimal DevelopmentAchievement { get; set; }

    [Range(typeof(decimal), "0", "200")]
    public decimal SizeAchievement { get; set; }
}

public class PerformanceParameterIdInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
