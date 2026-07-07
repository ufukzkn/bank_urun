using BankUrun.Web.Models;
using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ProductsController(IProductManagementService productService, IProductCodeService codeService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (Request.Path.Value?.Equals("/Products", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Redirect("~/");
        }

        var model = await productService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateProductInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ürün bilgilerini kontrol edin.";
            return Redirect("~/");
        }

        return await ExecuteAndRedirectAsync(
            () => productService.CreateProductAsync(input, Actor, cancellationToken),
            "Ürün oluşturuldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameProduct(RenameProductInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ürün adı en az 2 karakter olmalı.";
            return Redirect("~/");
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

    [HttpGet("/code-suggestion")]
    public async Task<IActionResult> SuggestCode(ProductType type, string code, int? mainProductId, CancellationToken cancellationToken)
    {
        if (!codeService.IsValidCode(code))
        {
            return Json(new { valid = false, message = "Kod 2 karakter alfanumerik olmalı." });
        }

        var normalized = codeService.NormalizeCode(code);
        var suggestion = await codeService.SuggestCodeAsync(type, normalized, mainProductId, cancellationToken);
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

        return Redirect("~/");
    }
}
