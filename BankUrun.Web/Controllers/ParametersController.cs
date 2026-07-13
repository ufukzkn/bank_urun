using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ParametersController(IParameterManagementService parameterService) : Controller
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
    public async Task<IActionResult> UpdateTargets(MonthlyTargetsInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            ModelState.IsValid,
            () => parameterService.UpdateTargetsAsync(input, Actor, cancellationToken),
            "Aylık hedefler güncellendi.");
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
