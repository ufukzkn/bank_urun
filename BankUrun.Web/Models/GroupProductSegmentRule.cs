namespace BankUrun.Web.Models;

public class GroupProductSegmentRule
{
    public int Id { get; set; }
    public int GroupProductParameterId { get; set; }
    public PerformanceSegment PerformanceSegment { get; set; }
    public int SortOrder { get; set; }
    public decimal TargetShare { get; set; }
    public decimal SizeShare { get; set; }
    public decimal ScaleShare { get; set; }
    public decimal AllocatedScore { get; set; }
    public decimal HgoWeight { get; set; }
    public decimal DevelopmentWeight { get; set; }
    public decimal SizeWeight { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GroupProductParameter GroupProductParameter { get; set; } = null!;
    public ICollection<BranchProductMetricResult> MetricResults { get; set; } = new List<BranchProductMetricResult>();
}
