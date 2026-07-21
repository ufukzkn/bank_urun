namespace BankUrun.Tests;

public class PerformanceViewContractTests
{
    [Fact]
    public void Index_IsMetadataOnlyAndStartsWithASkeleton()
    {
        var source = ReadWebFile("Views", "Performance", "Index.cshtml");

        Assert.Contains("data-snapshot-url", source);
        Assert.Contains("data-rows-url", source);
        Assert.Contains("data-dashboard-snapshot", source);
        Assert.Contains("performance-dashboard-skeleton", source);
        Assert.DoesNotContain("Model.Snapshot", source);
        Assert.DoesNotContain("PartialAsync(\"_Snapshot\"", source);
    }

    [Fact]
    public void RowPartial_UsesLazySectionUrlsInsteadOfRenderingDetailDatasets()
    {
        var source = ReadWebFile("Views", "Performance", "_PerformanceRows.cshtml");

        Assert.Contains("data-lazy-performance-url", source);
        Assert.Contains("section = \"months\"", source);
        Assert.Contains("section = \"contributions\"", source);
        Assert.Contains("section = \"products\"", source);
        Assert.Contains("data-lazy-performance-target", source);
        Assert.DoesNotContain("PartialAsync(\"_Monthly", source);
        Assert.DoesNotContain("row.Months", source);
        Assert.DoesNotContain("row.Contributions", source);
        Assert.DoesNotContain("row.Products", source);
    }

    [Fact]
    public void DetailHeaderPath_DoesNotRequestTargetOrMetricFactSections()
    {
        var source = ReadWebFile("Services", "DashboardService.cs");

        Assert.Contains("DetailSection.Header => FactSections.Metadata", source);
        Assert.Contains("Metadata = 0", source);
        Assert.Contains(
            "DetailSection.Contributions =>\r\n            FactSections.Metadata | FactSections.SubProducts | FactSections.Metrics",
            source.ReplaceLineEndings("\r\n"));
    }

    [Fact]
    public void PerformanceClient_UsesRemotePagingDebounceRaceProtectionAndBoundedCaches()
    {
        var source = ReadWebFile("wwwroot", "js", "site.js");

        Assert.Contains("function setupRemoteList", source);
        Assert.Contains("eventName === \"input\" ? 250 : 0", source);
        Assert.Contains("const scheduleDashboardRefresh", source);
        Assert.Contains("setTimeout(() => refreshDashboard(), 250)", source);
        Assert.Contains("const cacheLifetime = 60_000", source);
        Assert.Contains("const maxPanelCacheEntries = 8", source);
        Assert.Contains("const maxDetailCacheEntries = 32", source);
        Assert.Contains("performanceCacheExpiresAt", source);
        Assert.Contains("X-Performance-Cache-Max-Age-Ms", source);
        Assert.Contains("responseCacheExpiresAt", source);
        Assert.Contains("detailCacheGeneration", source);
        Assert.Contains("detailRequestControllers", source);
        Assert.Contains("new AbortController()", source);
        Assert.Contains("dashboardRequestGeneration", source);
        Assert.Contains("rowsRequestGeneration", source);
        Assert.Contains("requestController === currentController", source);
        Assert.Contains("snapshot.classList.remove(\"is-leaving\")", source);
        Assert.Contains("ForceRefresh", source);
        Assert.Contains("X-Performance-Force-Refresh", source);
        Assert.DoesNotContain("prefetch", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServerPage_NeverReturnsMoreThanRequestedRows()
    {
        var page = BankUrun.Web.Services.PerformanceQueryProcessor.Page(
            Enumerable.Range(1, 42),
            requestedPage: 2,
            pageSize: 10);

        Assert.Equal(42, page.TotalCount);
        Assert.Equal(5, page.TotalPages);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(Enumerable.Range(11, 10), page.Items);
    }

    [Fact]
    public void FactLoader_UsesOneFixedNineQueryBatchWithoutPeriodLoops()
    {
        var source = ReadWebFile("Services", "DashboardService.cs");
        var start = source.IndexOf(
            "private async Task<PerformanceFactSet> LoadFactSetAsync",
            StringComparison.Ordinal);
        var end = source.IndexOf(
            "private List<PortfolioProductRecord> BuildPortfolioProductRecords",
            start,
            StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var loader = source[start..end];

        Assert.Equal(9, loader.Split(".ToListAsync(", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("foreach", loader);
        Assert.DoesNotContain(".OrderBy", loader);
        Assert.Contains(
            "periodCodes.Contains(instance.Year * 10 + instance.Term)",
            loader);
        Assert.Contains(
            "periodCodes.Contains(metric.Year * 10 + metric.Term)",
            loader);
    }

    [Fact]
    public void PerformanceCache_IsInvalidatedAfterEveryManagementMutationFlow()
    {
        var controllerSources = new[]
        {
            ReadWebFile("Controllers", "ProductsController.cs"),
            ReadWebFile("Controllers", "OrganizationController.cs"),
            ReadWebFile("Controllers", "ParametersController.cs")
        };
        var program = ReadWebFile("Program.cs");

        Assert.All(controllerSources, source =>
        {
            Assert.Contains("await action();", source);
            Assert.Contains("performanceCacheInvalidator.Invalidate();", source);
            Assert.True(
                source.IndexOf("await action();", StringComparison.Ordinal)
                < source.IndexOf(
                    "performanceCacheInvalidator.Invalidate();",
                    StringComparison.Ordinal));
        });
        Assert.Contains("AddSingleton<IPerformanceCacheInvalidator>", program);
    }

    [Fact]
    public void DynamicResponses_EnableHttpsBrotliAndGzipCompression()
    {
        var program = ReadWebFile("Program.cs");

        Assert.Contains("AddResponseCompression", program);
        Assert.Contains("EnableForHttps = true", program);
        Assert.Contains("BrotliCompressionProvider", program);
        Assert.Contains("GzipCompressionProvider", program);
        Assert.Contains("UseResponseCompression", program);
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
