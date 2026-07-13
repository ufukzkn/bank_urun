namespace BankUrun.Web.Models;

public class GroupDefinition
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GroupSegment GroupSegment { get; set; } = GroupSegment.Karma;
    public bool IsActive { get; set; } = true;
    public bool BranchPerformanceEnabled { get; set; } = true;
    public bool MiyPerformanceEnabled { get; set; } = true;
    public bool ScaleEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
