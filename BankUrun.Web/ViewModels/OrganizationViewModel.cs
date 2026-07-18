using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class OrganizationIndexViewModel
{
    public IReadOnlyList<GroupRowViewModel> Groups { get; set; } = [];
    public IReadOnlyList<BranchRowViewModel> Branches { get; set; } = [];
    public IReadOnlyList<PortfolioTypeRowViewModel> PortfolioTypes { get; set; } = [];
    public IReadOnlyList<PortfolioRowViewModel> Portfolios { get; set; } = [];
    public IReadOnlyList<BranchMainProductExclusionRowViewModel> ProductExclusions { get; set; } = [];
    public IReadOnlyList<OrganizationProductGamutOptionViewModel> ProductGamuts { get; set; } = [];
    public IReadOnlyList<OrganizationMainProductOptionViewModel> MainProducts { get; set; } = [];
    public string NextGroupNo { get; set; } = "0001";
    public string NextBranchCode { get; set; } = "0001";
}

public class GroupRowViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GroupType GroupType { get; set; }
    public bool IsActive { get; set; }
    public bool BranchPerformanceEnabled { get; set; }
    public bool MiyPerformanceEnabled { get; set; }
    public bool ScaleEnabled { get; set; }
    public int BranchCount { get; set; }
    public int ProductGamutCount { get; set; }
    public int PortfolioCount { get; set; }
    public string Label => $"{GroupNo} - {Name}";
}

public class BranchRowViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupType GroupType { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PortfolioCount { get; set; }
    public int ProductExclusionCount { get; set; }
    public string Label => $"{BranchCode} - {Name}";
}

public class PortfolioTypeRowViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int PortfolioCount { get; set; }
    public string Label => $"{Code} - {Name}";
}

public class PortfolioRowViewModel
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int GroupId { get; set; }
    public int ProductGamutId { get; set; }
    public int PortfolioTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ProductGamutCode { get; set; } = string.Empty;
    public string ProductGamutName { get; set; } = string.Empty;
    public string PortfolioTypeCode { get; set; } = string.Empty;
    public string PortfolioTypeName { get; set; } = string.Empty;
}

public class BranchMainProductExclusionRowViewModel
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int MainProductId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
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

public class OrganizationProductGamutOptionViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Label => $"{Code} - {Name}";
}

public class OrganizationMainProductOptionViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Label => $"{Code} - {Name}";
}

public class GroupInput
{
    public int Id { get; set; }

    [StringLength(24, MinimumLength = 1)]
    public string GroupNo { get; set; } = string.Empty;

    public bool GenerateNumberAutomatically { get; set; }

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public GroupType GroupType { get; set; } = GroupType.Karma;

    public bool IsActive { get; set; } = true;
    public bool BranchPerformanceEnabled { get; set; } = true;
    public bool MiyPerformanceEnabled { get; set; } = true;
    public bool ScaleEnabled { get; set; } = true;
}

public class BranchInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }

    [StringLength(24, MinimumLength = 1)]
    public string BranchCode { get; set; } = string.Empty;

    public bool GenerateNumberAutomatically { get; set; }

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class LinkIdInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}

public class PortfolioTypeInput
{
    public int Id { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9]{2}$", ErrorMessage = "Portföy tipi kodu 2 karakter alfanumerik olmalı.")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class PortfolioInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductGamutId { get; set; }

    [Range(1, int.MaxValue)]
    public int PortfolioTypeId { get; set; }

    [StringLength(40)]
    public string Code { get; set; } = string.Empty;

    public bool GenerateCodeAutomatically { get; set; } = true;

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class BranchMainProductExclusionInput
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

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
