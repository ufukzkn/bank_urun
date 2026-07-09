using System.ComponentModel.DataAnnotations;
using BankUrun.Web.Models;

namespace BankUrun.Web.ViewModels;

public class OrganizationIndexViewModel
{
    public IReadOnlyList<GroupRowViewModel> Groups { get; set; } = [];
    public IReadOnlyList<BranchRowViewModel> Branches { get; set; } = [];
}

public class GroupRowViewModel
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public bool IsActive { get; set; }
    public bool BranchPerformanceEnabled { get; set; }
    public bool MiyPerformanceEnabled { get; set; }
    public bool ScaleEnabled { get; set; }
    public int BranchCount { get; set; }
    public string Label => $"{GroupNo} - {Name}";
}

public class BranchRowViewModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label => $"{BranchCode} - {Name}";
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

    [Required]
    public GroupSegment GroupSegment { get; set; } = GroupSegment.Karma;

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

    [Required]
    [StringLength(24, MinimumLength = 1)]
    public string BranchCode { get; set; } = string.Empty;

    [Required]
    [StringLength(180, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public class LinkIdInput
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
