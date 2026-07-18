using BankUrun.Web.Services;
using BankUrun.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class PerformanceController(IDashboardService dashboardService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboardService.GetIndexAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Snapshot(
        PerformanceMode mode,
        int? groupId,
        int? branchId,
        int? year,
        int? term,
        int? mainProductId,
        int? productGamutId,
        int? portfolioTypeId,
        CancellationToken cancellationToken) =>
        PartialView("_Snapshot", await dashboardService.GetSnapshotAsync(
            mode, groupId, branchId, year, term, mainProductId,
            productGamutId, portfolioTypeId, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> BranchProductMonthlyDetail(
        int branchId,
        int mainProductInstanceId,
        string? section,
        CancellationToken cancellationToken)
    {
        var model = await dashboardService.GetBranchProductMonthlyDetailAsync(
            branchId, mainProductInstanceId, cancellationToken);
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
        var model = await dashboardService.GetMainProductMonthlyDetailAsync(
            mainProductInstanceId, groupId, cancellationToken);
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
        var model = await dashboardService.GetPortfolioDetailAsync(portfolioId, year, term, cancellationToken);
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
}
