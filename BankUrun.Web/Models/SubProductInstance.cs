namespace BankUrun.Web.Models;

public class SubProductInstance
{
    public int Id { get; set; }
    public int MainProductInstanceId { get; set; }
    public int SubProductId { get; set; }
    public ProductType ProductDefinitionType { get; set; } = ProductType.Sub;
    public DateTimeOffset CreatedAt { get; set; }

    public MainProductInstance MainProductInstance { get; set; } = null!;
    public ProductDefinition SubProduct { get; set; } = null!;
    public ICollection<BranchProductScore> BranchProductScores { get; set; } = new List<BranchProductScore>();
}
