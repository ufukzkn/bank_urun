using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ParametersController(
    IParameterManagementService parameterService,
    IPerformanceCacheInvalidator performanceCacheInvalidator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await parameterService.GetIndexAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Rows([FromQuery] ParameterQuery query, CancellationToken cancellationToken)
    {
        var page = await parameterService.GetPageAsync(query, cancellationToken);
        Response.Headers.Append("X-Total-Count", page.TotalCount.ToString());
        Response.Headers.Append("X-Total-Pages", page.TotalPages.ToString());
        Response.Headers.Append("X-Page", page.Page.ToString());
        return PartialView("_ParameterRows", page);
    }

    [HttpGet]
    public async Task<IActionResult> MainProductTargetRows([FromQuery] MainProductTargetQuery query, CancellationToken cancellationToken)
    {
        var page = await parameterService.GetMainProductTargetPageAsync(query, cancellationToken);
        Response.Headers.Append("X-Total-Count", page.TotalCount.ToString());
        Response.Headers.Append("X-Total-Pages", page.TotalPages.ToString());
        Response.Headers.Append("X-Page", page.Page.ToString());
        return PartialView("_MainProductTargetRows", page);
    }

    [HttpGet]
    public async Task<IActionResult> MainProductTargetEditor(int parameterId, int portfolioId, CancellationToken cancellationToken)
    {
        try { return PartialView("_MainProductTargetEditor", await parameterService.GetMainProductTargetEditorAsync(parameterId, portfolioId, cancellationToken)); }
        catch (InvalidOperationException) { return NotFound(); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveParameter(MainProductParameterInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            ModelState.IsValid,
            () => parameterService.UpsertParameterAsync(input, Actor, cancellationToken),
            input.Id == 0 ? "Ana ürün parametresi oluşturuldu." : "Ana ürün parametresi güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMainProductTargets(PortfolioMainProductTargetsInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            ModelState.IsValid,
            () => parameterService.UpdateMainProductTargetsAsync(input, Actor, cancellationToken),
            "Portföy ana ürün hedefleri güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteParameter(ParameterIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            ModelState.IsValid,
            () => parameterService.DeleteParameterAsync(input, Actor, cancellationToken),
            "Ana ürün parametresi silindi.");
    }

    [HttpGet]
    public Task<IActionResult> ParameterDeleteImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => parameterService.GetParameterDeleteImpactAsync(id, cancellationToken));

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
            performanceCacheInvalidator.Invalidate();
            TempData["Success"] = successMessage;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static async Task<IActionResult> ExecuteImpactAsync(Func<Task<ManagementImpactViewModel>> action)
    {
        try
        {
            return new JsonResult(await action());
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }
}
