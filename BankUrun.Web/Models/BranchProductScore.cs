namespace BankUrun.Web.Models;

public class BranchProductScore
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int SubProductInstanceId { get; set; }
    public decimal Score { get; set; }
    public decimal TargetValue { get; set; }
    public decimal HgoShare { get; set; }
    public decimal DevelopmentShare { get; set; }
    public decimal SizeShare { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public SubProductInstance SubProductInstance { get; set; } = null!;
}
