namespace BankUrun.Web.Models;

public class Portfolio
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int GroupId { get; set; }
    public int ProductGamutId { get; set; }
    public int PortfolioTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public ProductGamut ProductGamut { get; set; } = null!;
    public PortfolioType PortfolioType { get; set; } = null!;
    public ICollection<PortfolioMainProductMonthlyTarget> MainProductMonthlyTargets { get; set; } = new List<PortfolioMainProductMonthlyTarget>();
    public ICollection<PortfolioSubProductMonthlyMetric> SubProductMonthlyMetrics { get; set; } = new List<PortfolioSubProductMonthlyMetric>();
}
