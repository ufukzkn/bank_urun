namespace BankUrun.Web.Models;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MainProductPeriod> MainProductPeriods { get; set; } = new List<MainProductPeriod>();
    public ICollection<SubProductAssignment> SubProductAssignments { get; set; } = new List<SubProductAssignment>();
}
