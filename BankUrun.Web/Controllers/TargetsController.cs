using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class TargetsController(
    ITargetManagementService targetService,
    IPerformanceCacheInvalidator performanceCacheInvalidator) : Controller
{
    private const long MaximumImportLength = 5 * 1024 * 1024;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await targetService.GetIndexAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Rows(
        [FromQuery] TargetQuery query,
        CancellationToken cancellationToken)
    {
        var page = await targetService.GetPageAsync(query, cancellationToken);
        Response.Headers.Append("X-Total-Count", page.TotalCount.ToString());
        Response.Headers.Append("X-Total-Pages", page.TotalPages.ToString());
        Response.Headers.Append("X-Page", page.Page.ToString());
        return PartialView("_TargetRows", page);
    }

    [HttpGet]
    public async Task<IActionResult> Editor(
        int parameterId,
        int portfolioId,
        CancellationToken cancellationToken)
    {
        try
        {
            return PartialView("_TargetEditor",
                await targetService.GetEditorAsync(
                    parameterId, portfolioId, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        TargetPeriodInput input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Hedef bilgilerini kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await targetService.UpdateTargetsAsync(input, Actor, cancellationToken);
            performanceCacheInvalidator.Invalidate();
            TempData["Success"] = "Portföy ana ürün hedefi güncellendi.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromQuery] TargetQuery query,
        TargetEntryMode entryMode = TargetEntryMode.SixMonth,
        CancellationToken cancellationToken = default)
    {
        var workbook = await targetService.ExportAsync(
            query, entryMode, false, cancellationToken);
        return File(
            workbook.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            workbook.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportSelected(
        [FromForm] TargetSelectedExportInput input,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Seçili dışa aktarma bilgileri geçersiz.");
        }
        if (input.ContextKeys is not { Count: > 0 })
        {
            return BadRequest("Dışa aktarılacak en az bir hedef satırı seçin.");
        }
        if (input.ContextKeys.Count > TargetSelectedExportInput.MaximumContextCount)
        {
            return BadRequest(
                $"Tek işlemde en fazla {TargetSelectedExportInput.MaximumContextCount} hedef satırı dışa aktarılabilir.");
        }

        var contextKeys = new HashSet<TargetContextKey>();
        foreach (var value in input.ContextKeys)
        {
            var parts = value?.Split(
                ':',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts is not { Length: 2 }
                || !int.TryParse(parts[0], out var portfolioId)
                || !int.TryParse(parts[1], out var parameterId)
                || portfolioId <= 0
                || parameterId <= 0)
            {
                return BadRequest("Seçili hedef satırlarından biri geçersiz.");
            }

            contextKeys.Add(new TargetContextKey(portfolioId, parameterId));
        }

        try
        {
            var workbook = await targetService.ExportSelectedAsync(
                contextKeys, input.EntryMode, cancellationToken);
            return File(
                workbook.Content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                workbook.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Template(
        TargetEntryMode entryMode = TargetEntryMode.SixMonth,
        CancellationToken cancellationToken = default)
    {
        var workbook = await targetService.ExportAsync(
            new TargetQuery(), entryMode, true, cancellationToken);
        return File(
            workbook.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            workbook.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> MissingTemplate(
        [FromQuery] TargetQuery query,
        TargetEntryMode entryMode = TargetEntryMode.SixMonth,
        CancellationToken cancellationToken = default)
    {
        var workbook = await targetService.ExportMissingTemplateAsync(
            query, entryMode, cancellationToken);
        return File(
            workbook.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            workbook.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportPreview(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return PartialView("_TargetImportPreview",
                new TargetImportPreviewViewModel
                {
                    Errors = ["Bir Excel dosyası seçin."]
                });
        }
        if (file.Length > MaximumImportLength)
        {
            return PartialView("_TargetImportPreview",
                new TargetImportPreviewViewModel
                {
                    FileName = file.FileName,
                    Errors = ["Excel dosyası en fazla 5 MB olabilir."]
                });
        }

        await using var stream = file.OpenReadStream();
        var preview = await targetService.PreviewImportAsync(
            stream, file.FileName, cancellationToken);
        return PartialView("_TargetImportPreview", preview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportConfirm(
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            await targetService.ConfirmImportAsync(token, Actor, cancellationToken);
            performanceCacheInvalidator.Invalidate();
            TempData["Success"] = "Excel hedefleri başarıyla içe aktarıldı.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private string Actor => User.Identity?.Name ?? "local-user";
}
