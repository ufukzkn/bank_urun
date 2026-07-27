namespace BankUrun.Tests;

public class PerformanceVisualContractTests
{
    [Fact]
    public void PerformancePage_LoadsDedicatedVisualStylesheet()
    {
        var source = ReadWebFile("Views", "Performance", "Index.cshtml");

        Assert.Contains("~/css/performance-visuals.css", source);
        Assert.Contains("asp-append-version=\"true\"", source);
    }

    [Fact]
    public void LazyMonthlySections_UseAccessibleNativeSvgAndKeepMonthlyCards()
    {
        var chart = ReadWebFile("Views", "Shared", "_MonthlyComparisonChart.cshtml");
        var branchSeries = ReadWebFile("Views", "Performance", "_MonthlySeries.cshtml");
        var portfolioSeries = ReadWebFile("Views", "Performance", "_PortfolioMonthlySeries.cshtml");

        Assert.Contains("<svg", chart);
        Assert.Contains("role=\"img\"", chart);
        Assert.Contains("month-chart-bar is-target", chart);
        Assert.Contains("is-actual", chart);
        Assert.Contains("Batch bekleniyor", chart);
        Assert.Contains("performance-month-detail-grid", branchSeries);
        Assert.Contains("performance-month-detail-grid", portfolioSeries);
        Assert.Contains("_MonthlyComparisonChart", branchSeries);
        Assert.Contains("_MonthlyComparisonChart", portfolioSeries);
    }

    [Fact]
    public void ProductFlowPartial_IsReusableAndDoesNotDependOnPerformanceModel()
    {
        var model = ReadWebFile("ViewModels", "ProductFlowViewModel.cs");
        var partial = ReadWebFile("Views", "Shared", "_ProductFlowVisual.cshtml");
        var contributions = ReadWebFile("Views", "Performance", "_SubProductContributions.cshtml");

        Assert.Contains("class ProductFlowViewModel", model);
        Assert.Contains("IReadOnlyList<ProductFlowItemViewModel>", model);
        Assert.Contains("@model ProductFlowViewModel", partial);
        Assert.DoesNotContain("DashboardMonthlyDetailViewModel", partial);
        Assert.Contains("product-flow-connector", partial);
        Assert.Contains("product-flow-donut", partial);
        Assert.Contains("Ortak ·", partial);
        Assert.Contains("SharedRelationLabel", model);
        Assert.Contains("positiveItems.Count > colors.Length", partial);
        Assert.Contains("\"Diğer\"", partial);
        Assert.Contains("_ProductFlowVisual", contributions);
    }

    [Fact]
    public void CatalogAndOrganizationDetails_ReuseRelationshipFlowVisual()
    {
        var products = ReadWebFile("Views", "Products", "Index.cshtml");
        var organization = ReadWebFile("Views", "Organization", "Index.cshtml");
        var controller = ReadWebFile("Controllers", "ProductsController.cs");
        var service = ReadWebFile("Services", "ProductManagementService.cs");
        var client = ReadWebFile("wwwroot", "js", "site.js");

        Assert.Contains("data-product-flow-load", products);
        Assert.Contains("data-product-flow-url", products);
        Assert.Contains("ProductFlow(", controller);
        Assert.Contains("GetProductFlowAsync", service);
        Assert.Contains("productFlowCache", client);
        Assert.Contains("gamutFlow", products);
        Assert.Contains("SharedRelationLabel = \"ürün gamı\"", products);
        Assert.Contains("branchFlow", organization);
        Assert.Contains("Şubenin portföy yapısı", organization);
        Assert.Contains("~/css/performance-visuals.css", products);
        Assert.Contains("~/css/performance-visuals.css", organization);
        Assert.Equal(2, Count(products + organization, "_ProductFlowVisual"));
    }

    [Fact]
    public void PortfolioBreakdown_ShowsDerivedTargetCompositionAndExactLabels()
    {
        var source = ReadWebFile("Views", "Performance", "_PortfolioProductBreakdown.cshtml");

        Assert.Contains("Portföyün bağımsız hedefi yoktur", source);
        Assert.Contains("portfolio-target-donut", source);
        Assert.Contains("Portföy hedefindeki pay", source);
        Assert.Contains("H/G bekleniyor", source);
        Assert.Contains("role=\"img\"", source);
    }

    [Fact]
    public void EveryPerformanceRank_HasTextAndAccessiblePositionTrack()
    {
        var rows = ReadWebFile("Views", "Performance", "_PerformanceRows.cshtml");
        var snapshot = ReadWebFile("Views", "Performance", "_Snapshot.cshtml");

        Assert.Equal(5, Count(rows, "class=\"performance-rank-track\""));
        Assert.Equal(5, Count(rows, "class=\"performance-rank-position\""));
        Assert.Contains("role=\"img\"", rows);
        Assert.Contains("Ürün içi sıra", snapshot);
        Assert.DoesNotContain(">Segment sırası", snapshot);
    }

    private static int Count(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string ReadWebFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var project = Path.Combine(directory.FullName, "BankUrun.Web");
            if (Directory.Exists(project))
            {
                return File.ReadAllText(Path.Combine([project, .. pathParts]));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("BankUrun.Web proje klasörü bulunamadı.");
    }
}
