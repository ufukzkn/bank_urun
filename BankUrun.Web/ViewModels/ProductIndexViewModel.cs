using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class ProductIndexViewModel
{
    public IReadOnlyList<ProductRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<MainProductOptionViewModel> MainProducts { get; set; } = [];
}

public class ProductRowViewModel
{
    public int MainProductId { get; set; }
    public int? SubProductId { get; set; }
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

    [Range(1, 12)]
    public int? Term { get; set; }

    public int? MainProductId { get; set; }
}

public class RenameProductInput
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    public ProductType Type { get; set; }

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
}
