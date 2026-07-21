using BankUrun.Web.Services;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class PerformanceController(IDashboardService dashboardService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboardService.GetIndexAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Snapshot(
        [FromQuery] PerformanceQuery query,
        CancellationToken cancellationToken)
    {
        query.ForceRefresh = query.ForceRefresh
            && string.Equals(
                Request.Headers["X-Performance-Force-Refresh"],
                "1",
                StringComparison.Ordinal);
        var model = await dashboardService.GetSnapshotAsync(query, cancellationToken);
        SetPerformanceHeaders(model);
        return PartialView("_Snapshot", model);
    }

    [HttpGet]
    public async Task<IActionResult> Rows(
        [FromQuery] PerformanceQuery query,
        CancellationToken cancellationToken)
    {
        query.ForceRefresh = false;
        var model = await dashboardService.GetSnapshotAsync(query, cancellationToken);
        SetPerformanceHeaders(model);
        return PartialView("_PerformanceRows", model);
    }

    [HttpGet]
    public async Task<IActionResult> BranchProductMonthlyDetail(
        int branchId,
        int mainProductInstanceId,
        string? section,
        CancellationToken cancellationToken)
    {
        var model = section switch
        {
            "months" => await dashboardService.GetBranchProductMonthsAsync(
                branchId, mainProductInstanceId, cancellationToken),
            "contributions" => await dashboardService.GetBranchProductContributionsAsync(
                branchId, mainProductInstanceId, cancellationToken),
            _ => await dashboardService.GetBranchProductDetailHeaderAsync(
                branchId, mainProductInstanceId, cancellationToken)
        };
        if (model is null)
        {
            return NotFound();
        }

        if (section == "months")
        {
            return PartialView("_MonthlySeries", model);
        }

        if (section == "contributions")
        {
            return PartialView("_SubProductContributions", model);
        }

        ViewData["MonthsUrl"] = Url.Action(nameof(BranchProductMonthlyDetail), new
        {
            branchId,
            mainProductInstanceId,
            section = "months"
        });
        ViewData["ContributionsUrl"] = Url.Action(nameof(BranchProductMonthlyDetail), new
        {
            branchId,
            mainProductInstanceId,
            section = "contributions"
        });
        return PartialView("_MonthlyDetail", model);
    }

    [HttpGet]
    public async Task<IActionResult> MainProductMonthlyDetail(
        int mainProductInstanceId,
        int? groupId,
        string? section,
        CancellationToken cancellationToken)
    {
        var model = section switch
        {
            "months" => await dashboardService.GetMainProductMonthsAsync(
                mainProductInstanceId, groupId, cancellationToken),
            "contributions" => await dashboardService.GetMainProductContributionsAsync(
                mainProductInstanceId, groupId, cancellationToken),
            _ => await dashboardService.GetMainProductDetailHeaderAsync(
                mainProductInstanceId, groupId, cancellationToken)
        };
        if (model is null)
        {
            return NotFound();
        }

        if (section == "months")
        {
            return PartialView("_MonthlySeries", model);
        }

        if (section == "contributions")
        {
            return PartialView("_SubProductContributions", model);
        }

        ViewData["MonthsUrl"] = Url.Action(nameof(MainProductMonthlyDetail), new
        {
            mainProductInstanceId,
            groupId,
            section = "months"
        });
        ViewData["ContributionsUrl"] = Url.Action(nameof(MainProductMonthlyDetail), new
        {
            mainProductInstanceId,
            groupId,
            section = "contributions"
        });
        return PartialView("_MonthlyDetail", model);
    }

    [HttpGet]
    public async Task<IActionResult> PortfolioDetail(
        int portfolioId,
        int year,
        int term,
        string? section,
        CancellationToken cancellationToken)
    {
        var model = section switch
        {
            "products" => await dashboardService.GetPortfolioProductsAsync(
                portfolioId, year, term, cancellationToken),
            "months" => await dashboardService.GetPortfolioMonthsAsync(
                portfolioId, year, term, cancellationToken),
            "contributions" => await dashboardService.GetPortfolioContributionsAsync(
                portfolioId, year, term, cancellationToken),
            _ => await dashboardService.GetPortfolioDetailHeaderAsync(
                portfolioId, year, term, cancellationToken)
        };
        if (model is null)
        {
            return NotFound();
        }

        if (section == "products")
        {
            return PartialView("_PortfolioProductBreakdown", model);
        }

        if (section == "months")
        {
            return PartialView("_PortfolioMonthlySeries", model);
        }

        if (section == "contributions")
        {
            return PartialView("_PortfolioContributions", model);
        }

        ViewData["ProductsUrl"] = Url.Action(nameof(PortfolioDetail), new
        {
            portfolioId,
            year,
            term,
            section = "products"
        });
        ViewData["MonthsUrl"] = Url.Action(nameof(PortfolioDetail), new
        {
            portfolioId,
            year,
            term,
            section = "months"
        });
        ViewData["ContributionsUrl"] = Url.Action(nameof(PortfolioDetail), new
        {
            portfolioId,
            year,
            term,
            section = "contributions"
        });
        return PartialView("_PortfolioDetail", model);
    }

    private void SetPerformanceHeaders(DashboardSnapshotViewModel model)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Server-Timing"] = model.Timing.ServerTiming;
        Response.Headers["X-Total-Count"] = model.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = model.TotalPages.ToString();
        Response.Headers["X-Page"] = model.Page.ToString();
        Response.Headers["X-Performance-Cache"] = model.Timing.CacheHit ? "hit" : "miss";
        Response.Headers["X-Performance-Cache-Max-Age-Ms"] =
            Math.Floor(model.Timing.CacheRemainingMilliseconds).ToString(
                System.Globalization.CultureInfo.InvariantCulture);
    }
}
