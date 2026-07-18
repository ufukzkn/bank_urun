namespace BankUrun.Web.Models;

public class PortfolioMainProductMonthlyTarget
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int GroupId { get; set; }
    public int MainProductParameterId { get; set; }
    public int Month { get; set; }
    public decimal TargetValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Portfolio Portfolio { get; set; } = null!;
    public MainProductParameter MainProductParameter { get; set; } = null!;
}
