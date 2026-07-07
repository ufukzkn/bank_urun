namespace BankUrun.Web.Models;

public class SubProductAssignment
{
    public int Id { get; set; }
    public int MainProductPeriodId { get; set; }
    public int SubProductId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MainProductPeriod MainProductPeriod { get; set; } = null!;
    public Product SubProduct { get; set; } = null!;
}
