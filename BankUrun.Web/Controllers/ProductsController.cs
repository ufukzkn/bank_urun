using BankUrun.Web.Models;
using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ProductsController(IProductManagementService productService, IProductCodeService codeService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductFilterInput filter, CancellationToken cancellationToken)
    {
        var model = await productService.GetIndexAsync(filter, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateProductInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ürün bilgilerini kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecuteAndRedirectAsync(
            () => productService.CreateProductAsync(input, Actor, cancellationToken),
            "Ürün oluşturuldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMainToPeriod(AddMainToPeriodInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Yıl, dönem ve ana ürün seçimi zorunlu.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecuteAndRedirectAsync(
            () => productService.AddMainToPeriodAsync(input, Actor, cancellationToken),
            "Ana ürün döneme eklendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignSubProduct(AssignSubProductInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ana ürün dönemi ve alt ürün seçimi zorunlu.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecuteAndRedirectAsync(
            () => productService.AssignSubProductAsync(input, Actor, cancellationToken),
            "Alt ürün bağlandı.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameProduct(RenameProductInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ürün adı en az 2 karakter olmalı.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecuteAndRedirectAsync(
            () => productService.RenameProductAsync(input, Actor, cancellationToken),
            "Ürün adı güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateProduct(ProductIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            () => productService.DeactivateProductAsync(input, Actor, cancellationToken),
            "Ürün pasifleştirildi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(ProductIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            () => productService.DeleteProductAsync(input, Actor, cancellationToken),
            "Ürün kalıcı silindi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMainProductPeriod(EntityIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            () => productService.RemoveMainProductPeriodAsync(input, Actor, cancellationToken),
            "Ana ürün dönemden kaldırıldı.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSubProductAssignment(EntityIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(
            () => productService.RemoveSubProductAssignmentAsync(input, Actor, cancellationToken),
            "Alt ürün bağlantısı kaldırıldı.");
    }

    [HttpGet]
    public async Task<IActionResult> SuggestCode(ProductType type, string code, CancellationToken cancellationToken)
    {
        if (!codeService.IsValidCode(code))
        {
            return Json(new { valid = false, message = "Kod 2 karakter alfanumerik olmalı." });
        }

        var normalized = codeService.NormalizeCode(code);
        var suggestion = await codeService.SuggestCodeAsync(type, normalized, cancellationToken);
        return Json(new
        {
            valid = suggestion is not null,
            requested = normalized,
            suggestion,
            available = suggestion == normalized,
            message = suggestion is null ? "Boş 2 karakterli kod kalmadı." : null
        });
    }

    private string Actor => User.Identity?.Name ?? "local-user";

    private async Task<IActionResult> ExecuteAndRedirectAsync(Func<Task> action, string successMessage)
    {
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
