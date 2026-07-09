using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ScoresController(IScoreService scoreService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await scoreService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateScore(ScoreInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => scoreService.CreateScoreAsync(input, Actor, cancellationToken), "Puan satırı oluşturuldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScore(ScoreInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => scoreService.UpdateScoreAsync(input, Actor, cancellationToken), "Puan satırı güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScore(ScoreIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => scoreService.DeleteScoreAsync(input, Actor, cancellationToken), "Puan satırı silindi.");
    }

    private string Actor => User.Identity?.Name ?? "local-user";

    private async Task<IActionResult> ExecuteAndRedirectAsync(bool isValid, Func<Task> action, string successMessage)
    {
        if (!isValid)
        {
            TempData["Error"] = "Puan bilgilerini kontrol edin.";
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
