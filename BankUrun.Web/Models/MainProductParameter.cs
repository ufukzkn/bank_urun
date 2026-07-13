namespace BankUrun.Web.Models;

public class MainProductParameter
{
    public int Id { get; set; }
    public int MainProductInstanceId { get; set; }
    public MainProductCalculationType CalculationType { get; set; }
    public decimal CriterionScore { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MainProductInstance MainProductInstance { get; set; } = null!;
    public ICollection<BranchMainProductMonthlyMetric> MonthlyMetrics { get; set; } = new List<BranchMainProductMonthlyMetric>();
}
