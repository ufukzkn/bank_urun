using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class ParameterIndexViewModel
{
    public IReadOnlyList<ParameterGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<ParameterProductOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<ParameterGamutOptionViewModel> ProductGamuts { get; set; } = [];
    public IReadOnlyList<ParameterPortfolioOptionViewModel> Portfolios { get; set; } = [];
    public IReadOnlyList<int> Years { get; set; } = [];
    public ParameterPageViewModel Page { get; set; } = new();
    public MainProductTargetPageViewModel TargetPage { get; set; } = new();
}

public class ParameterGroupOptionViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GroupType GroupType { get; set; }
    public string Label => $"{GroupNo} - {Name}";
}

public class ParameterProductOptionViewModel
{
    public int Id { get; set; }
    public int MainProductId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{Year}/{Term} · {Code} - {Name}";
}

public class ParameterGamutOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{Code} - {Name}";
}

public class ParameterPortfolioOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int ProductGamutId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string Label => $"{Code} - {Name}";
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
    public int MainProductId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupType GroupType { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public MainProductCalculationType CalculationType { get; set; }
    public decimal CriterionScore { get; set; }
    public bool IsActive { get; set; }
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
}

public class MainProductTargetQuery
{
    public int? GroupId { get; set; }
    public int? ProductGamutId { get; set; }
    public int? PortfolioId { get; set; }
    public int? MainProductId { get; set; }
    public int? Year { get; set; }
    public int? Term { get; set; }
    public string Search { get; set; } = string.Empty;
    public string SortKey { get; set; } = "year";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class MainProductTargetPageViewModel
{
    public IReadOnlyList<MainProductTargetRowViewModel> Rows { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; } = 1;
}

public class MainProductTargetRowViewModel
{
    public int PortfolioId { get; set; }
    public int ParameterId { get; set; }
    public int MainProductId { get; set; }
    public int ProductGamutId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string PortfolioCode { get; set; } = string.Empty;
    public string PortfolioName { get; set; } = string.Empty;
    public string ProductGamutCode { get; set; } = string.Empty;
    public string ProductGamutName { get; set; } = string.Empty;
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public decimal PeriodTarget { get; set; }
    public int EnteredMonthCount { get; set; }
}

public class MainProductTargetEditorViewModel
{
    public int ParameterId { get; set; }
    public int PortfolioId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string PortfolioLabel { get; set; } = string.Empty;
    public string MainProductLabel { get; set; } = string.Empty;
    public IReadOnlyList<ParameterMonthlyTargetViewModel> Months { get; set; } = [];
}

public class ParameterMonthlyTargetViewModel
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public bool HasStoredTarget { get; set; }
}

public class PortfolioMainProductTargetsInput
{
    [Range(1, int.MaxValue)]
    public int ParameterId { get; set; }

    [Range(1, int.MaxValue)]
    public int PortfolioId { get; set; }

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
