namespace BankUrun.Web.Models;

public class BranchMainProductMonthlyMetric
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int MainProductParameterId { get; set; }
    public int Month { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public DateOnly? ActualAsOfDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public MainProductParameter MainProductParameter { get; set; } = null!;
}
