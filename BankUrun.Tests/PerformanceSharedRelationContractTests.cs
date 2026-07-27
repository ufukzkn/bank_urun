namespace BankUrun.Tests;

public class PerformanceSharedRelationContractTests
{
    [Fact]
    public void ContributionFlow_UsesPeriodScopedSharedMainProductCount()
    {
        var service = ReadWebFile("Services", "DashboardService.cs");
        var branchPartial = ReadWebFile(
            "Views", "Performance", "_SubProductContributions.cshtml");
        var portfolioPartial = ReadWebFile(
            "Views", "Performance", "_PortfolioContributions.cshtml");

        Assert.Contains("candidate.MainProductInstance.Year", service);
        Assert.Contains("== link.MainProductInstance.Year", service);
        Assert.Contains("candidate.MainProductInstance.Term", service);
        Assert.Contains("== link.MainProductInstance.Term", service);
        Assert.Equal(
            2,
            service.Split(
                "SharedRelationCount = group.Max(item => item.SharedRelationCount)",
                StringSplitOptions.None).Length - 1);
        Assert.Contains(
            "SharedRelationCount = item.SharedRelationCount",
            branchPartial);
        Assert.Contains(
            "SharedRelationCount = item.SharedRelationCount",
            portfolioPartial);
    }

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
