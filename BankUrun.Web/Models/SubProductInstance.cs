namespace BankUrun.Web.Models;

public class SubProductInstance
{
    public int Id { get; set; }
    public int MainProductInstanceId { get; set; }
    public int SubProductId { get; set; }
    public ProductType SubProductType { get; set; } = ProductType.Sub;
    public DateTimeOffset CreatedAt { get; set; }

    public MainProductInstance MainProductInstance { get; set; } = null!;
    public ProductDefinition SubProduct { get; set; } = null!;
}
