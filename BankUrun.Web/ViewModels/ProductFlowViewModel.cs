namespace BankUrun.Web.ViewModels;

public sealed class ProductFlowViewModel
{
    public string CenterTitle { get; set; } = string.Empty;
    public string CenterSubtitle { get; set; } = string.Empty;
    public string CenterLabel { get; set; } = "Ana ürün";
    public string Heading { get; set; } = "Besleyen ürünler";
    public string Description { get; set; } = string.Empty;
    public string ValueLabel { get; set; } = "Gerçekleşme";
    public string TotalLabel { get; set; } = "Toplam katkı";
    public string EmptyMessage { get; set; } = "Alt ürün bağlantısı yok.";
    public string SharedRelationLabel { get; set; } = "ana ürün";
    public bool ShowDistribution { get; set; }
    public IReadOnlyList<ProductFlowItemViewModel> Items { get; set; } = [];
}

public sealed class ProductFlowItemViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public int SharedRelationCount { get; set; }
}

public sealed class InteractiveDonutChartViewModel
{
    public string AriaLabel { get; set; } = "Dağılım grafiği";
    public string TotalLabel { get; set; } = "Toplam";
    public string ValueLabel { get; set; } = "Değer";
    public IReadOnlyList<InteractiveDonutSegmentViewModel> Segments { get; set; } = [];
}

public sealed class InteractiveDonutSegmentViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Color { get; set; } = "#0076a8";
}

public sealed class MonthlyComparisonChartViewModel
{
    public string Title { get; set; } = "Altı aylık hedef ve gerçekleşme karşılaştırması";
    public string Description { get; set; } =
        "Hedef ve gerçekleşme değerlerinin ay bazındaki karşılaştırması.";
    public IReadOnlyList<DashboardProductMonthViewModel> Months { get; set; } = [];
}
