namespace BankUrun.Tests;

public class TargetEntryContractTests
{
    [Fact]
    public void TargetEntry_IsATopLevelMenuAndNoLongerAParameterMode()
    {
        var layout = ReadWebFile("Views", "Shared", "_Layout.cshtml");
        var parameters = ReadWebFile("Views", "Parameters", "Index.cshtml");
        var targets = ReadWebFile("Views", "Targets", "Index.cshtml");

        Assert.Contains("asp-controller=\"Targets\"", layout);
        Assert.Contains("Hedef Girişi", layout);
        Assert.DoesNotContain("Ana ürün hedefleri", parameters);
        Assert.Contains("data-target-management", targets);
        Assert.Contains("data-list=\"targets\"", targets);
    }

    [Fact]
    public void TargetList_KeepsEveryContextAndRemoteListBehavior()
    {
        var index = ReadWebFile("Views", "Targets", "Index.cshtml");
        var rows = ReadWebFile("Views", "Targets", "_TargetRows.cshtml");
        var client = ReadWebFile("wwwroot", "js", "site.js");

        Assert.Contains("data-list-filter=\"groupId\"", index);
        Assert.Contains("data-list-filter=\"branchId\"", index);
        Assert.Contains("data-list-filter=\"portfolioId\"", index);
        Assert.Contains("data-list-filter=\"mainProductId\"", index);
        Assert.Contains("data-target-detail", rows);
        Assert.Contains("setupList(targetRoot", client);
        Assert.Contains("BranchId: filterValue(\"branchId\")", client);
        Assert.Contains("colspan: 12", client);
    }

    [Fact]
    public void Editor_OffersSixThreeAndMonthlyEntryWithoutPreRenderingOtherRows()
    {
        var editor = ReadWebFile("Views", "Targets", "_TargetEditor.cshtml");
        var client = ReadWebFile("wwwroot", "js", "site.js");

        Assert.Contains("data-segmented-value=\"SixMonth\"", editor);
        Assert.Contains("data-segmented-value=\"ThreeMonth\"", editor);
        Assert.Contains("data-segmented-value=\"Monthly\"", editor);
        Assert.Contains("FirstThreeMonthTarget", editor);
        Assert.Contains("SecondThreeMonthTarget", editor);
        Assert.Contains("Months[@index].TargetValue", editor);
        Assert.Contains("CultureInfo.InvariantCulture", editor);
        Assert.Contains("pageEntryMode?.value || \"SixMonth\"", client);
    }

    [Fact]
    public void ExcelFlow_UsesPreviewAndAtomicConfirmationEndpoints()
    {
        var controller = ReadWebFile("Controllers", "TargetsController.cs");
        var service = ReadWebFile("Services", "TargetManagementService.cs");
        var project = ReadWebFile("BankUrun.Web.csproj");

        Assert.Contains("ClosedXML", project);
        Assert.Contains("ImportPreview", controller);
        Assert.Contains("ImportConfirm", controller);
        Assert.Contains("BeginTransactionAsync", service);
        Assert.Contains("TargetImportPreviewStore", service);
        Assert.Contains("Aynı ürün farklı grup veya portföylerde ayrı hedef bağlamıdır.", service);
    }

    [Fact]
    public void SeedContainsProductsAcrossGroupsAndTwoPortfolioContexts()
    {
        var seed = ReadProjectFile("scripts", "seed-mock-data.sql");

        Assert.Contains("cross join group_definitions groups", seed);
        Assert.Contains("scope.branch_code = '120'", seed);
        Assert.Contains("'P' || scope.branch_code || '-' || scope.gamut_code || '02'", seed);
        Assert.Contains("mock-v21", seed);
    }

    [Fact]
    public void TargetAndPerformanceAggregation_PreserveIndependentPortfolioContexts()
    {
        var targetService = ReadWebFile("Services", "TargetManagementService.cs");
        var dashboard = ReadWebFile("Services", "DashboardService.cs");

        Assert.Contains(
            "targetMap.TryGetValue((portfolio.Id, parameter.Id)",
            targetService);
        Assert.Contains(
            "BranchId = record.Branch.Id",
            dashboard);
        Assert.Contains(
            "InstanceId = record.Instance.Id",
            dashboard);
        Assert.Contains(
            "items.Select(item => item.Months).ToList()",
            dashboard);
    }

    private static string ReadWebFile(params string[] pathParts)
    {
        var directory = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([directory, "BankUrun.Web", .. pathParts]));
    }

    private static string ReadProjectFile(params string[] pathParts)
    {
        var directory = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([directory, .. pathParts]));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "BankUrun.Web")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Proje kökü bulunamadı.");
    }
}
