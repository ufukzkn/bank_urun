using BankUrun.Web.ViewModels;

namespace BankUrun.Tests;

public class ProductScopeRemovalContractTests
{
    [Fact]
    public void RemovalInput_SupportsEveryExistingProductScope()
    {
        Assert.Equal(
            [MainProductRemovalScope.Group, MainProductRemovalScope.ProductGamut, MainProductRemovalScope.Branch],
            Enum.GetValues<MainProductRemovalScope>());
    }

    [Fact]
    public void ProductCatalog_OffersGroupGamutAndBranchRemovalWithImpactPreview()
    {
        var source = ReadWebFile("Views", "Products", "Index.cshtml");

        Assert.Contains("data-product-scope-removal", source);
        Assert.Contains("MainProductScopeRemovalImpact", source);
        Assert.Contains("RemoveMainProductFromScope", source);
        Assert.Contains("data-segmented-value=\"Group\"", source);
        Assert.Contains("data-segmented-value=\"ProductGamut\"", source);
        Assert.Contains("data-segmented-value=\"Branch\"", source);
        Assert.Contains("data-removal-scope-field=\"Group\"", source);
        Assert.Contains("data-removal-scope-field=\"ProductGamut\"", source);
        Assert.Contains("data-removal-scope-field=\"Branch\"", source);
    }

    [Fact]
    public void ScopeRemovalClient_DisablesHiddenInputsAndBuildsScopedImpactRequest()
    {
        var source = ReadWebFile("wwwroot", "js", "site.js");

        Assert.Contains("[data-product-scope-removal]", source);
        Assert.Contains("[data-removal-scope-field]", source);
        Assert.Contains("input.disabled = !active", source);
        Assert.Contains("ProductGamutId", source);
        Assert.Contains("BranchId", source);
    }

    [Fact]
    public void GamutRemoval_UsesHistoricalAssignmentLifecycleInsteadOfPermanentDelete()
    {
        var source = ReadWebFile("Services", "ProductManagementService.cs");
        var start = source.IndexOf("RemoveMainProductFromProductGamutAsync", StringComparison.Ordinal);
        var end = source.IndexOf("private int ApplyAssignmentRemoval", start, StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start);
        var method = source[start..end];
        Assert.Contains("ApplyAssignmentRemoval", method);
        Assert.Contains("geçmiş", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadWebFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var project = Path.Combine(directory.FullName, "BankUrun.Web");
            if (Directory.Exists(project))
                return File.ReadAllText(Path.Combine([project, .. pathParts]));
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("BankUrun.Web project root could not be found.");
    }
}
