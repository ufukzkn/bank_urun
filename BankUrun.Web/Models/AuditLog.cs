namespace BankUrun.Web.Models;

public class AuditLog
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Actor { get; set; } = "system";
    public DateTimeOffset CreatedAt { get; set; }
}
