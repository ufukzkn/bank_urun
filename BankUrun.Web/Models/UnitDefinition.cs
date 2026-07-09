namespace BankUrun.Web.Models;

public class UnitDefinition
{
    public int Id { get; set; }
    public string UnitNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<GroupUnit> GroupUnits { get; set; } = new List<GroupUnit>();
    public ICollection<BranchUnit> BranchUnits { get; set; } = new List<BranchUnit>();
}
