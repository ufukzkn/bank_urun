using BankUrun.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class PerformanceController(IDashboardService dashboardService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboardService.GetIndexAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Snapshot(
        int? groupId,
        int? branchId,
        int? year,
        int? term,
        int? mainProductInstanceId,
        CancellationToken cancellationToken) =>
        PartialView("_Snapshot", await dashboardService.GetSnapshotAsync(
            groupId, branchId, year, term, mainProductInstanceId, cancellationToken));
}
