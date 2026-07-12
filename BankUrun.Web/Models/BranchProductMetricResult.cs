namespace BankUrun.Web.Models;

public class BranchProductMetricResult
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int GroupProductSegmentRuleId { get; set; }
    public decimal HgoAchievement { get; set; }
    public decimal DevelopmentAchievement { get; set; }
    public decimal SizeAchievement { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public GroupProductSegmentRule GroupProductSegmentRule { get; set; } = null!;
}
