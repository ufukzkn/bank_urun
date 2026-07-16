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
        int? mainProductInstanceId,
        CancellationToken cancellationToken) =>
        PartialView("_Snapshot", await dashboardService.GetSnapshotAsync(
            mode, groupId, branchId, year, term, mainProductInstanceId, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> BranchProductMonthlyDetail(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken)
    {
        var model = await dashboardService.GetBranchProductMonthlyDetailAsync(
            branchId, mainProductInstanceId, cancellationToken);
        return model is null ? NotFound() : PartialView("_MonthlyDetail", model);
    }

    [HttpGet]
    public async Task<IActionResult> MainProductMonthlyDetail(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken)
    {
        var model = await dashboardService.GetMainProductMonthlyDetailAsync(
            mainProductInstanceId, groupId, cancellationToken);
        return model is null ? NotFound() : PartialView("_MonthlyDetail", model);
    }
}
