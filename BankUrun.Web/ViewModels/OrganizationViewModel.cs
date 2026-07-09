using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class OrganizationIndexViewModel
{
    public IReadOnlyList<GroupRowViewModel> Groups { get; set; } = [];
    public IReadOnlyList<UnitRowViewModel> Units { get; set; } = [];
    public IReadOnlyList<BranchRowViewModel> Branches { get; set; } = [];
    public IReadOnlyList<GroupUnitRowViewModel> GroupUnits { get; set; } = [];
    public IReadOnlyList<BranchUnitRowViewModel> BranchUnits { get; set; } = [];
}

public class GroupRowViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{GroupNo} - {Name}";
}

public class UnitRowViewModel
{
    public int Id { get; set; }
    public string UnitNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{UnitNo} - {Name}";
}

public class BranchRowViewModel
{
    public int Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BranchType BranchType { get; set; }
    public string Label => $"{BranchCode} - {Name} ({BranchType})";
}

public class GroupUnitRowViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int UnitId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string UnitNo { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
}

public class BranchUnitRowViewModel
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int UnitId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public BranchType BranchType { get; set; }
    public string UnitNo { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
}

public class GroupInput
{
    public int Id { get; set; }

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string GroupNo { get; set; } = string.Empty;

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class UnitInput
{
    public int Id { get; set; }

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string UnitNo { get; set; } = string.Empty;

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class BranchInput
{
    public int Id { get; set; }

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string BranchCode { get; set; } = string.Empty;

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public BranchType BranchType { get; set; }
}

public class GroupUnitInput
{
    [Range(1, int.MaxValue)]
    public int GroupId { get; set; }

    [Range(1, int.MaxValue)]
    public int UnitId { get; set; }
}

public class BranchUnitInput
{
    [Range(1, int.MaxValue)]
    public int BranchId { get; set; }

    [Range(1, int.MaxValue)]
    public int UnitId { get; set; }
}

public class LinkIdInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
