using BankUrun.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class DashboardController(IDashboardService dashboardService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await dashboardService.GetIndexAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Snapshot(
        int? branchId,
        int? year,
        int? term,
        CancellationToken cancellationToken)
    {
        var snapshot = await dashboardService.GetSnapshotAsync(branchId, year, term, cancellationToken);
        return PartialView("_Snapshot", snapshot);
    }
}
