namespace BankUrun.Web.Models;

public class BranchSubProductMonthlyMetric
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int SubProductId { get; set; }
    public ProductType ProductDefinitionType { get; set; } = ProductType.Sub;
    public int Year { get; set; }
    public int Term { get; set; }
    public int Month { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public DateOnly? ActualAsOfDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public ProductDefinition SubProduct { get; set; } = null!;
}
