using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class ProductIndexViewModel
{
    public ProductFilterInput Filter { get; set; } = new();
    public IReadOnlyList<ProductRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<ProductOptionViewModel> MainProducts { get; set; } = [];
    public IReadOnlyList<ProductOptionViewModel> SubProducts { get; set; } = [];
    public IReadOnlyList<MainProductPeriodOptionViewModel> MainProductPeriods { get; set; } = [];
}

public class ProductFilterInput
{
    public int? Year { get; set; }
    public int? Term { get; set; }
    public string? MainQuery { get; set; }
    public string? SubQuery { get; set; }
    public bool IncludeInactive { get; set; }
}

public class ProductRowViewModel
{
    public int MainProductPeriodId { get; set; }
    public int? AssignmentId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public int MainProductId { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public bool MainProductActive { get; set; }
    public int? SubProductId { get; set; }
    public string? SubProductCode { get; set; }
    public string? SubProductName { get; set; }
    public bool? SubProductActive { get; set; }
}

public class ProductOptionViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public bool IsActive { get; set; }
    public string Label => $"{Code} - {Name}";
}

public class MainProductPeriodOptionViewModel
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public string Label => $"{Year}/{Term} - {MainProductCode} - {MainProductName}";
}

public class CreateProductInput
{
    [Required]
    public ProductType Type { get; set; }

    [Required]
    [StringLength(12)]
    public string CodeMode { get; set; } = "Auto";

    [StringLength(2, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9]{2}$", ErrorMessage = "Kod 2 karakter alfanumerik olmalı.")]
    public string? ManualCode { get; set; }

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class AddMainToPeriodInput
{
    [Range(1, int.MaxValue)]
    public int MainProductId { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Term { get; set; }
}

public class AssignSubProductInput
{
    [Range(1, int.MaxValue)]
    public int MainProductPeriodId { get; set; }

    [Range(1, int.MaxValue)]
    public int SubProductId { get; set; }
}

public class RenameProductInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class ProductIdInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
}

public class EntityIdInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
