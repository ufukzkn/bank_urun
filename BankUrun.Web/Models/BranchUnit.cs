namespace BankUrun.Web.Models;

public class BranchUnit
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int UnitId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public UnitDefinition Unit { get; set; } = null!;
}
