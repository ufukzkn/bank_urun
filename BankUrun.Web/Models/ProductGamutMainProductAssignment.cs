namespace BankUrun.Web.Models;

public class ProductGamutMainProductAssignment
{
    public int Id { get; set; }
    public int ProductGamutId { get; set; }
    public int MainProductId { get; set; }
    public ProductType ProductDefinitionType { get; set; } = ProductType.Main;
    public int EffectiveFromYear { get; set; }
    public int EffectiveFromTerm { get; set; }
    public int? EffectiveToYear { get; set; }
    public int? EffectiveToTerm { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ProductGamut ProductGamut { get; set; } = null!;
    public ProductDefinition MainProduct { get; set; } = null!;
}
