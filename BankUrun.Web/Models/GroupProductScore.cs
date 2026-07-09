namespace BankUrun.Web.Models;

public class GroupProductScore
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int SubProductInstanceId { get; set; }
    public decimal Score { get; set; }
    public decimal TargetValue { get; set; }
    public decimal HgoShare { get; set; }
    public decimal DevelopmentShare { get; set; }
    public decimal SizeShare { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public GroupDefinition Group { get; set; } = null!;
    public SubProductInstance SubProductInstance { get; set; } = null!;
}
