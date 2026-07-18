using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class ProductIndexViewModel
{
    public IReadOnlyList<ProductRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<MainProductOptionViewModel> MainProducts { get; set; } = [];
    public IReadOnlyList<ProductGroupOptionViewModel> Groups { get; set; } = [];
    public IReadOnlyList<ProductDefinitionOptionViewModel> MainProductDefinitions { get; set; } = [];
    public IReadOnlyList<ProductGamutRowViewModel> ProductGamuts { get; set; } = [];
}

public class ProductGroupOptionViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{GroupNo} - {Name}";
}

public class ProductDefinitionOptionViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Label => $"{Code} - {Name}";
}

public class ProductGamutRowViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int PortfolioCount { get; set; }
    public IReadOnlyList<ProductGamutAssignmentRowViewModel> Assignments { get; set; } = [];
}

public class ProductGamutAssignmentRowViewModel
{
    public int Id { get; set; }
    public int MainProductId { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public int EffectiveFromYear { get; set; }
    public int EffectiveFromTerm { get; set; }
    public int? EffectiveToYear { get; set; }
    public int? EffectiveToTerm { get; set; }
    public string EffectivePeriodLabel => EffectiveToYear.HasValue
        ? $"{EffectiveFromYear}/{EffectiveFromTerm} - {EffectiveToYear}/{EffectiveToTerm}"
        : $"{EffectiveFromYear}/{EffectiveFromTerm} - devam ediyor";
}

public class ProductRowViewModel
{
    public int MainProductId { get; set; }
    public int MainProductInstanceId { get; set; }
    public int? SubProductId { get; set; }
    public int? SubProductInstanceId { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }
    public string MainProductCode { get; set; } = string.Empty;
    public string MainProductName { get; set; } = string.Empty;
    public bool MainProductActive { get; set; }
    public string? SubProductCode { get; set; }
    public string? SubProductName { get; set; }
    public bool? SubProductActive { get; set; }
}

public class MainProductOptionViewModel
{
    public int Id { get; set; }
    public int MainProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Term { get; set; }
    public bool IsActive { get; set; }
    public string Label => $"{Code} - {Name} ({Year}/{Term})";
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

    [Range(2000, 2100)]
    public int? Year { get; set; }

    [Range(1, 2)]
    public int? Term { get; set; }

    public int? MainProductInstanceId { get; set; }
}

public class RenameProductInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    public ProductType Type { get; set; }

    [StringLength(2, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9]{2}$", ErrorMessage = "Kod 2 karakter alfanumerik olmalı.")]
    public string? Code { get; set; }

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class ProductIdInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    public ProductType Type { get; set; }

    public int? MainProductInstanceId { get; set; }

    public int? SubProductInstanceId { get; set; }

    public string DeleteScope { get; set; } = "Single";

    [StringLength(2)]
    public string? ConfirmationCode { get; set; }
}

public class ProductGamutInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9]{2}$", ErrorMessage = "Ürün gamı kodu 2 karakter alfanumerik olmalı.")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class ProductGamutAssignmentInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductGamutId { get; set; }

    [Range(1, int.MaxValue)]
    public int MainProductId { get; set; }

    [Range(2000, 2100)]
    public int EffectiveFromYear { get; set; }

    [Range(1, 2)]
    public int EffectiveFromTerm { get; set; }

    [Range(2000, 2100)]
    public int? EffectiveToYear { get; set; }

    [Range(1, 2)]
    public int? EffectiveToTerm { get; set; }
}

public class GroupMainProductRemovalInput
{
    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }

    [Range(1, int.MaxValue)]
    public int MainProductId { get; set; }

    [Range(2000, 2100)]
    public int EffectiveFromYear { get; set; }

    [Range(1, 2)]
    public int EffectiveFromTerm { get; set; }
}
