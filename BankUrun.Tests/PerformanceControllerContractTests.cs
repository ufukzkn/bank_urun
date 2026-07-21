using BankUrun.Web.Controllers;
using BankUrun.Web.Models;
using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace BankUrun.Tests;

public class PerformanceControllerContractTests
{
    [Fact]
    public async Task Index_LoadsOnlyFilterMetadata()
    {
        var service = new RecordingDashboardService();
        var controller = CreateController(service);

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(service.IndexResult, view.Model);
        Assert.Equal(["index"], service.Calls);
    }

    [Theory]
    [InlineData(false, "_Snapshot")]
    [InlineData(true, "_PerformanceRows")]
    public async Task SnapshotAndRows_ReturnActiveModePageAndPerformanceHeaders(
        bool rowsOnly,
        string expectedPartial)
    {
        var service = new RecordingDashboardService
        {
            SnapshotResult = new DashboardSnapshotViewModel
            {
                Mode = PerformanceMode.MainProduct,
                Results = new PerformancePage<DashboardMainProductPerformanceViewModel>
                {
                    Page = 2,
                    PageSize = 10,
                    TotalCount = 37,
                    Items =
                    [
                        new DashboardMainProductPerformanceViewModel
                        {
                            MainProductId = 7,
                            ProductCode = "KR",
                            ProductName = "Kredi"
                        }
                    ]
                },
                Timing = new DashboardPerformanceTimingViewModel
                {
                    CacheHit = true,
                    DatabaseMilliseconds = 12.5,
                    CalculationMilliseconds = 3.25,
                    TotalMilliseconds = 17,
                    CacheRemainingMilliseconds = 45_250
                }
            }
        };
        var controller = CreateController(service);
        var query = new PerformanceQuery
        {
            Mode = PerformanceMode.MainProduct,
            Page = 2,
            PageSize = 10,
            Search = "kredi"
        };

        var result = rowsOnly
            ? await controller.Rows(query, CancellationToken.None)
            : await controller.Snapshot(query, CancellationToken.None);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal(expectedPartial, partial.ViewName);
        Assert.Same(service.SnapshotResult, partial.Model);
        Assert.Equal(["snapshot:MainProduct:2:10:kredi"], service.Calls);
        var activePage = Assert.IsType<
            PerformancePage<DashboardMainProductPerformanceViewModel>>(
            service.SnapshotResult.Results);
        Assert.Single(activePage.Items);

        Assert.Equal("no-store", controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("37", controller.Response.Headers["X-Total-Count"].ToString());
        Assert.Equal("4", controller.Response.Headers["X-Total-Pages"].ToString());
        Assert.Equal("2", controller.Response.Headers["X-Page"].ToString());
        Assert.Equal("hit", controller.Response.Headers["X-Performance-Cache"].ToString());
        Assert.Equal(
            "45250",
            controller.Response.Headers["X-Performance-Cache-Max-Age-Ms"].ToString());
        Assert.Contains("db;dur=12.5", controller.Response.Headers["Server-Timing"].ToString());
        Assert.Contains("calc;dur=3.25", controller.Response.Headers["Server-Timing"].ToString());
    }

    [Fact]
    public async Task ForceRefresh_RequiresTheDashboardHeaderAndIsNeverAcceptedByRows()
    {
        var service = new RecordingDashboardService();
        var controller = CreateController(service);
        var query = new PerformanceQuery { ForceRefresh = true };

        await controller.Snapshot(query, CancellationToken.None);
        Assert.False(service.LastQuery!.ForceRefresh);

        controller = CreateController(service);
        controller.Request.Headers["X-Performance-Force-Refresh"] = "1";
        query = new PerformanceQuery { ForceRefresh = true };
        await controller.Snapshot(query, CancellationToken.None);
        Assert.True(service.LastQuery!.ForceRefresh);

        controller = CreateController(service);
        query = new PerformanceQuery { ForceRefresh = true };
        await controller.Rows(query, CancellationToken.None);
        Assert.False(service.LastQuery!.ForceRefresh);
    }

    [Theory]
    [InlineData("months", "_MonthlySeries", "branch-months")]
    [InlineData("contributions", "_SubProductContributions", "branch-contributions")]
    public async Task BranchProductSections_UseOnlyTheRequestedScopedQuery(
        string section,
        string expectedPartial,
        string expectedCall)
    {
        var service = new RecordingDashboardService();
        var controller = CreateController(service);

        var result = await controller.BranchProductMonthlyDetail(
            12,
            34,
            section,
            CancellationToken.None);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal(expectedPartial, partial.ViewName);
        Assert.Equal([expectedCall], service.Calls);
        Assert.DoesNotContain("branch-full", service.Calls);
    }

    [Fact]
    public async Task BranchProductDetailShell_ContainsSectionUrlsWithoutLoadingSections()
    {
        var service = new RecordingDashboardService();
        var controller = CreateController(service);

        var result = await controller.BranchProductMonthlyDetail(
            12,
            34,
            section: null,
            CancellationToken.None);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_MonthlyDetail", partial.ViewName);
        Assert.Equal(["branch-header"], service.Calls);
        Assert.Contains("section=months", Assert.IsType<string>(controller.ViewData["MonthsUrl"]));
        Assert.Contains("section=contributions", Assert.IsType<string>(controller.ViewData["ContributionsUrl"]));
    }

    [Theory]
    [InlineData("months", "_MonthlySeries", "main-months")]
    [InlineData("contributions", "_SubProductContributions", "main-contributions")]
    public async Task MainProductSections_UseOnlyTheRequestedScopedQuery(
        string section,
        string expectedPartial,
        string expectedCall)
    {
        var service = new RecordingDashboardService();
        var controller = CreateController(service);

        var result = await controller.MainProductMonthlyDetail(
            34,
            8,
            section,
            CancellationToken.None);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal(expectedPartial, partial.ViewName);
        Assert.Equal([expectedCall], service.Calls);
        Assert.DoesNotContain("main-full", service.Calls);
    }

    [Theory]
    [InlineData("products", "_PortfolioProductBreakdown", "portfolio-products")]
    [InlineData("months", "_PortfolioMonthlySeries", "portfolio-months")]
    [InlineData("contributions", "_PortfolioContributions", "portfolio-contributions")]
    public async Task PortfolioSections_UseOnlyTheRequestedScopedQuery(
        string section,
        string expectedPartial,
        string expectedCall)
    {
        var service = new RecordingDashboardService();
        var controller = CreateController(service);

        var result = await controller.PortfolioDetail(
            55,
            2025,
            2,
            section,
            CancellationToken.None);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal(expectedPartial, partial.ViewName);
        Assert.Equal([expectedCall], service.Calls);
        Assert.DoesNotContain("portfolio-full", service.Calls);
    }

    private static PerformanceController CreateController(RecordingDashboardService service)
    {
        var controller = new PerformanceController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            },
            Url = new ContractUrlHelper()
        };
        return controller;
    }

    private sealed class ContractUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();

        public string? Action(UrlActionContext actionContext)
        {
            var values = actionContext.Values?
                .GetType()
                .GetProperties()
                .Select(property => $"{property.Name}={property.GetValue(actionContext.Values)}")
                ?? [];
            return $"/Performance/{actionContext.Action}?{string.Join("&", values)}";
        }

        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => routeName;
        public string? RouteUrl(UrlRouteContext routeContext) => routeContext.RouteName;
    }

    private sealed class RecordingDashboardService : IDashboardService
    {
        public DashboardIndexViewModel IndexResult { get; } = new()
        {
            SelectedMode = PerformanceMode.BranchProduct,
            SelectedYear = 2025,
            SelectedTerm = 2
        };

        public DashboardSnapshotViewModel SnapshotResult { get; set; } = new();
        public PerformanceQuery? LastQuery { get; private set; }
        public List<string> Calls { get; } = [];

        public Task<DashboardIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
        {
            Calls.Add("index");
            return Task.FromResult(IndexResult);
        }

        public Task<DashboardSnapshotViewModel> GetSnapshotAsync(
            PerformanceQuery query,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            Calls.Add($"snapshot:{query.Mode}:{query.Page}:{query.PageSize}:{query.Search}");
            return Task.FromResult(SnapshotResult);
        }

        public Task<DashboardMonthlyDetailViewModel?> GetBranchProductDetailHeaderAsync(
            int branchId,
            int mainProductInstanceId,
            CancellationToken cancellationToken = default) =>
            Monthly("branch-header");

        public Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthsAsync(
            int branchId,
            int mainProductInstanceId,
            CancellationToken cancellationToken = default) =>
            Monthly("branch-months");

        public Task<DashboardMonthlyDetailViewModel?> GetBranchProductContributionsAsync(
            int branchId,
            int mainProductInstanceId,
            CancellationToken cancellationToken = default) =>
            Monthly("branch-contributions");

        public Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthlyDetailAsync(
            int branchId,
            int mainProductInstanceId,
            CancellationToken cancellationToken = default) =>
            Monthly("branch-full");

        public Task<DashboardMonthlyDetailViewModel?> GetMainProductDetailHeaderAsync(
            int mainProductInstanceId,
            int? groupId,
            CancellationToken cancellationToken = default) =>
            Monthly("main-header");

        public Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthsAsync(
            int mainProductInstanceId,
            int? groupId,
            CancellationToken cancellationToken = default) =>
            Monthly("main-months");

        public Task<DashboardMonthlyDetailViewModel?> GetMainProductContributionsAsync(
            int mainProductInstanceId,
            int? groupId,
            CancellationToken cancellationToken = default) =>
            Monthly("main-contributions");

        public Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthlyDetailAsync(
            int mainProductInstanceId,
            int? groupId,
            CancellationToken cancellationToken = default) =>
            Monthly("main-full");

        public Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailHeaderAsync(
            int portfolioId,
            int year,
            int term,
            CancellationToken cancellationToken = default) =>
            Portfolio("portfolio-header");

        public Task<DashboardPortfolioDetailViewModel?> GetPortfolioProductsAsync(
            int portfolioId,
            int year,
            int term,
            CancellationToken cancellationToken = default) =>
            Portfolio("portfolio-products");

        public Task<DashboardPortfolioDetailViewModel?> GetPortfolioMonthsAsync(
            int portfolioId,
            int year,
            int term,
            CancellationToken cancellationToken = default) =>
            Portfolio("portfolio-months");

        public Task<DashboardPortfolioDetailViewModel?> GetPortfolioContributionsAsync(
            int portfolioId,
            int year,
            int term,
            CancellationToken cancellationToken = default) =>
            Portfolio("portfolio-contributions");

        public Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailAsync(
            int portfolioId,
            int year,
            int term,
            CancellationToken cancellationToken = default) =>
            Portfolio("portfolio-full");

        private Task<DashboardMonthlyDetailViewModel?> Monthly(string call)
        {
            Calls.Add(call);
            return Task.FromResult<DashboardMonthlyDetailViewModel?>(new DashboardMonthlyDetailViewModel());
        }

        private Task<DashboardPortfolioDetailViewModel?> Portfolio(string call)
        {
            Calls.Add(call);
            return Task.FromResult<DashboardPortfolioDetailViewModel?>(new DashboardPortfolioDetailViewModel());
        }
    }
}
