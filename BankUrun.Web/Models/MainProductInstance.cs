namespace BankUrun.Web.Models;

public class MainProductInstance
{
    public int Id { get; set; }
    public int MainProductId { get; set; }
    public ProductType MainProductType { get; set; } = ProductType.Main;
    public int Year { get; set; }
    public int Term { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ProductDefinition MainProduct { get; set; } = null!;
    public ICollection<SubProductInstance> SubProductInstances { get; set; } = new List<SubProductInstance>();
}
