namespace BankUrun.Web.Models;

public class Period
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Term { get; set; }

    public ICollection<MainProductPeriod> MainProductPeriods { get; set; } = new List<MainProductPeriod>();
}
