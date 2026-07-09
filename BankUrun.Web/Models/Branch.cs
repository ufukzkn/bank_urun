namespace BankUrun.Web.Models;

public class Branch
{
    public int Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BranchType BranchType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<BranchUnit> BranchUnits { get; set; } = new List<BranchUnit>();
}
