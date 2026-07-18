namespace BankUrun.Web.ViewModels;

public sealed class ManagementImpactViewModel
{
    public string Operation { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public bool Allowed { get; set; } = true;
    public IReadOnlyList<ManagementImpactCountViewModel> Counts { get; set; } = [];
    public IReadOnlyList<string> Warnings { get; set; } = [];
    public IReadOnlyList<string> Blockers { get; set; } = [];
}

public sealed class ManagementImpactCountViewModel
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
