using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public enum TargetEntryMode
{
    SixMonth,
    ThreeMonth,
    Monthly
}

public class TargetIndexViewModel
{
    public IReadOnlyList<TargetGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<TargetGamutOptionViewModel> ProductGamuts { get; set; } = [];
    public IReadOnlyList<TargetPortfolioOptionViewModel> Portfolios { get; set; } = [];
    public IReadOnlyList<TargetProductOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<int> Years { get; set; } = [];
    public TargetPageViewModel Page { get; set; } = new();
}

public class TargetGroupOptionViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TargetGamutOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TargetPortfolioOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int BranchId { get; set; }
    public int ProductGamutId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TargetProductOptionViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class TargetQuery
{
    public int? GroupId { get; set; }
    public int? BranchId { get; set; }
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

public class TargetPageViewModel
{
    public IReadOnlyList<TargetRowViewModel> Rows { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; } = 1;
}

public class TargetRowViewModel
{
    public int PortfolioId { get; set; }
    public int ParameterId { get; set; }
    public int MainProductId { get; set; }
    public int ProductGamutId { get; set; }
    public int BranchId { get; set; }
    public int GroupId { get; set; }
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
    public MainProductCalculationType CalculationType { get; set; }
    public decimal PeriodTarget { get; set; }
    public int EnteredMonthCount { get; set; }
    public bool HasCompleteTarget => EnteredMonthCount == 6;
}

public class TargetEditorViewModel
{
    public int ParameterId { get; set; }
    public int PortfolioId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public MainProductCalculationType CalculationType { get; set; }
    public string GroupLabel { get; set; } = string.Empty;
    public string BranchLabel { get; set; } = string.Empty;
    public string PortfolioLabel { get; set; } = string.Empty;
    public string ProductGamutLabel { get; set; } = string.Empty;
    public string MainProductLabel { get; set; } = string.Empty;
    public decimal SixMonthTarget { get; set; }
    public decimal FirstThreeMonthTarget { get; set; }
    public decimal SecondThreeMonthTarget { get; set; }
    public decimal? PortfolioPeriodTarget { get; set; }
    public IReadOnlyList<TargetMonthViewModel> Months { get; set; } = [];
    public IReadOnlyList<ProductFlowItemViewModel> PortfolioTargetProducts { get; set; } = [];
}

public class TargetMonthViewModel
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public bool HasStoredTarget { get; set; }
}

public class TargetPeriodInput
{
    [Range(1, int.MaxValue)]
    public int ParameterId { get; set; }

    [Range(1, int.MaxValue)]
    public int PortfolioId { get; set; }

    [EnumDataType(typeof(TargetEntryMode))]
    public TargetEntryMode EntryMode { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? SixMonthTarget { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? FirstThreeMonthTarget { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal? SecondThreeMonthTarget { get; set; }

    public List<TargetMonthInput> Months { get; set; } = [];
}

public sealed record TargetContextKey(int PortfolioId, int ParameterId);

public class TargetSelectedExportInput
{
    public const int MaximumContextCount = 500;

    [EnumDataType(typeof(TargetEntryMode))]
    public TargetEntryMode EntryMode { get; set; } = TargetEntryMode.SixMonth;

    public List<string> ContextKeys { get; set; } = [];
}

public class TargetMonthInput
{
    [Range(1, 12)]
    public int Month { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal TargetValue { get; set; }
}

public sealed record TargetWorkbookResult(byte[] Content, string FileName);

public class TargetImportPreviewViewModel
{
    public string Token { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int SourceRowCount { get; set; }
    public int TargetContextCount { get; set; }
    public int MonthlyValueCount { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
    public IReadOnlyList<TargetImportPreviewRowViewModel> Rows { get; set; } = [];
    public bool CanConfirm => Errors.Count == 0 && !string.IsNullOrWhiteSpace(Token);
}

public class TargetImportPreviewRowViewModel
{
    public string Context { get; set; } = string.Empty;
    public string EntryMode { get; set; } = string.Empty;
    public decimal PeriodTarget { get; set; }
}
