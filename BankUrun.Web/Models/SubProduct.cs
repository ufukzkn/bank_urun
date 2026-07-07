namespace BankUrun.Web.Models;

public class SubProduct
{
    public int Id { get; set; }
    public int MainProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MainProduct MainProduct { get; set; } = null!;
}
