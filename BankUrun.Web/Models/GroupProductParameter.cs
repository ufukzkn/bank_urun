namespace BankUrun.Web.Models;

public class GroupProductParameter
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int SubProductInstanceId { get; set; }
    public decimal TotalScore { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GroupDefinition Group { get; set; } = null!;
    public SubProductInstance SubProductInstance { get; set; } = null!;
    public ICollection<GroupProductSegmentRule> SegmentRules { get; set; } = new List<GroupProductSegmentRule>();
}
