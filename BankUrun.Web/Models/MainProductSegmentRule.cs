namespace BankUrun.Web.Models;

public class MainProductSegmentRule
{
    public int Id { get; set; }
    public int MainProductParameterId { get; set; }
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

    public MainProductParameter MainProductParameter { get; set; } = null!;
}
