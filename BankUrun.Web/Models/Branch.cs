namespace BankUrun.Web.Models;

public class Branch
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GroupDefinition Group { get; set; } = null!;
    public ICollection<BranchMainProductMonthlyMetric> MonthlyMetrics { get; set; } = new List<BranchMainProductMonthlyMetric>();
    public ICollection<BranchSubProductMonthlyMetric> SubProductMonthlyMetrics { get; set; } = new List<BranchSubProductMonthlyMetric>();
}
