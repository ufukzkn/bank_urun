using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class PerformanceController(IPerformanceWorkspaceService performanceService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await performanceService.GetIndexAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateParameter(PerformanceParameterInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => performanceService.CreateParameterAsync(input, Actor, cancellationToken), "Performans parametresi oluşturuldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateParameter(PerformanceParameterUpdateInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => performanceService.UpdateParameterAsync(input, Actor, cancellationToken), "Parametre ve segment dağılımı güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteParameter(PerformanceParameterIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => performanceService.DeleteParameterAsync(input, Actor, cancellationToken), "Performans parametresi silindi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMetricResult(MetricResultInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => performanceService.UpsertMetricResultAsync(input, Actor, cancellationToken), "Şube gerçekleşmesi güncellendi.");
    }

    private string Actor => User.Identity?.Name ?? "local-user";

    private async Task<IActionResult> ExecuteAndRedirectAsync(bool isValid, Func<Task> action, string successMessage)
    {
        if (!isValid)
        {
            TempData["Error"] = "Bilgileri kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
