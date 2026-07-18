namespace BankUrun.Web.Models;

public class ProductGamut
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GroupDefinition Group { get; set; } = null!;
    public ICollection<ProductGamutMainProductAssignment> MainProductAssignments { get; set; } = new List<ProductGamutMainProductAssignment>();
    public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
}
