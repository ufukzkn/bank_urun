namespace BankUrun.Web.Models;

public class GroupDefinition
{
    public int Id { get; set; }
    public string GroupNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<GroupUnit> GroupUnits { get; set; } = new List<GroupUnit>();
    public ICollection<GroupProductScore> GroupProductScores { get; set; } = new List<GroupProductScore>();
}
