namespace BankUrun.Web.Models;

public class MainProductPeriod
{
    public int Id { get; set; }
    public int MainProductId { get; set; }
    public int PeriodId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Product MainProduct { get; set; } = null!;
    public Period Period { get; set; } = null!;
    public ICollection<SubProductAssignment> SubProductAssignments { get; set; } = new List<SubProductAssignment>();
}
