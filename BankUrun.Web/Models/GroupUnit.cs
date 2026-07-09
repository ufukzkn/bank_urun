namespace BankUrun.Web.Models;

public class GroupUnit
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int UnitId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public GroupDefinition Group { get; set; } = null!;
    public UnitDefinition Unit { get; set; } = null!;
}
