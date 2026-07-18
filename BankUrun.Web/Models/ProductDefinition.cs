namespace BankUrun.Web.Models;

public class ProductDefinition
{
    public int Id { get; set; }
    public ProductType Type { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MainProductInstance> MainProductInstances { get; set; } = new List<MainProductInstance>();
    public ICollection<SubProductInstance> SubProductInstances { get; set; } = new List<SubProductInstance>();
    public ICollection<ProductGamutMainProductAssignment> ProductGamutAssignments { get; set; } = new List<ProductGamutMainProductAssignment>();
    public ICollection<BranchMainProductExclusion> BranchExclusions { get; set; } = new List<BranchMainProductExclusion>();
    public ICollection<PortfolioSubProductMonthlyMetric> PortfolioMonthlyMetrics { get; set; } = new List<PortfolioSubProductMonthlyMetric>();
}
