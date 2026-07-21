using BankUrun.Web.Models;
using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class ProductsController(
    IProductManagementService productService,
    IOrganizationService organizationService,
    IProductCodeService codeService,
    IPerformanceCacheInvalidator performanceCacheInvalidator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
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
            return RedirectToAction(nameof(Index));
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
            TempData["Error"] = "Ürün kodu ve adını kontrol edin.";
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

    [HttpGet]
    public Task<IActionResult> ProductDeleteImpact(
        int productId,
        ProductType type,
        int? mainProductInstanceId,
        int? subProductInstanceId,
        string deleteScope = "Single",
        CancellationToken cancellationToken = default) =>
        ExecuteImpactAsync(() => productService.GetProductDeleteImpactAsync(new ProductIdInput
        {
            ProductId = productId,
            Type = type,
            MainProductInstanceId = mainProductInstanceId,
            SubProductInstanceId = subProductInstanceId,
            DeleteScope = deleteScope
        }, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProductGamut(ProductGamutInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ürün gamı bilgilerini kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecuteAndRedirectAsync(
            () => productService.UpsertProductGamutAsync(input, Actor, cancellationToken),
            input.Id == 0 ? "Ürün gamı oluşturuldu." : "Ürün gamı güncellendi.");
    }

    [HttpGet]
    public Task<IActionResult> ProductGamutDeleteImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => productService.GetProductGamutDeleteImpactAsync(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductGamut(LinkIdInput input, CancellationToken cancellationToken) =>
        await ExecuteAndRedirectAsync(
            () => productService.DeleteProductGamutAsync(input, Actor, cancellationToken),
            "Ürün gamı silindi.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProductGamutAssignment(ProductGamutAssignmentInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ürün gamı atamasını kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecuteAndRedirectAsync(
            () => productService.UpsertProductGamutAssignmentAsync(input, Actor, cancellationToken),
            input.Id == 0 ? "Ana ürün ürün gamına eklendi." : "Ürün gamı ataması güncellendi.");
    }

    [HttpGet]
    public Task<IActionResult> ProductGamutAssignmentRemovalImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => productService.GetProductGamutAssignmentRemovalImpactAsync(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductGamutAssignment(LinkIdInput input, CancellationToken cancellationToken) =>
        await ExecuteAndRedirectAsync(
            () => productService.DeleteProductGamutAssignmentAsync(input, Actor, cancellationToken),
            "Ana ürün ürün gamından çıkarıldı.");

    [HttpGet]
    public Task<IActionResult> GroupMainProductRemovalImpact(
        int groupId, int mainProductId, int effectiveFromYear, int effectiveFromTerm,
        CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => productService.GetGroupMainProductRemovalImpactAsync(new GroupMainProductRemovalInput
        {
            GroupId = groupId,
            MainProductId = mainProductId,
            EffectiveFromYear = effectiveFromYear,
            EffectiveFromTerm = effectiveFromTerm
        }, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMainProductFromGroup(
        GroupMainProductRemovalInput input, CancellationToken cancellationToken) =>
        await ExecuteAndRedirectAsync(
            () => productService.RemoveMainProductFromGroupAsync(input, Actor, cancellationToken),
            "Ana ürün seçili dönemden itibaren grubun ürün gamlarından çıkarıldı; geçmiş korundu.");

    [HttpGet]
    public Task<IActionResult> MainProductScopeRemovalImpact(
        MainProductScopeRemovalInput input, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => GetScopeRemovalImpactAsync(input, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMainProductFromScope(
        MainProductScopeRemovalInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Ana ürün çıkarma kapsamını ve dönemini kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        return await ExecuteAndRedirectAsync(
            () => RemoveFromScopeAsync(input, cancellationToken),
            "Ana ürün seçili dönemden itibaren seçilen kapsamdan çıkarıldı; geçmiş korundu.");
    }

    [HttpGet("/code-suggestion")]
    public async Task<IActionResult> SuggestCode(ProductType type, string code, int? mainProductInstanceId, CancellationToken cancellationToken)
    {
        if (!codeService.IsValidCode(code))
        {
            return Json(new { valid = false, message = "Kod 2 karakter alfanumerik olmalı." });
        }

        var normalized = codeService.NormalizeCode(code);
        var suggestion = await codeService.SuggestCodeAsync(type, normalized, mainProductInstanceId, cancellationToken);
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

    private Task<ManagementImpactViewModel> GetScopeRemovalImpactAsync(
        MainProductScopeRemovalInput input, CancellationToken cancellationToken) => input.Scope switch
    {
        MainProductRemovalScope.Group => productService.GetGroupMainProductRemovalImpactAsync(new GroupMainProductRemovalInput
        {
            GroupId = RequireScopeId(input.GroupId, "Grup"),
            MainProductId = input.MainProductId,
            EffectiveFromYear = input.EffectiveFromYear,
            EffectiveFromTerm = input.EffectiveFromTerm
        }, cancellationToken),
        MainProductRemovalScope.ProductGamut => productService.GetProductGamutMainProductRemovalImpactAsync(new ProductGamutMainProductRemovalInput
        {
            ProductGamutId = RequireScopeId(input.ProductGamutId, "Ürün gamı"),
            MainProductId = input.MainProductId,
            EffectiveFromYear = input.EffectiveFromYear,
            EffectiveFromTerm = input.EffectiveFromTerm
        }, cancellationToken),
        MainProductRemovalScope.Branch => organizationService.GetBranchMainProductExclusionImpactAsync(new BranchMainProductExclusionInput
        {
            BranchId = RequireScopeId(input.BranchId, "Şube"),
            MainProductId = input.MainProductId,
            EffectiveFromYear = input.EffectiveFromYear,
            EffectiveFromTerm = input.EffectiveFromTerm
        }, cancellationToken),
        _ => throw new InvalidOperationException("Geçerli bir çıkarma kapsamı seçin.")
    };

    private Task RemoveFromScopeAsync(MainProductScopeRemovalInput input, CancellationToken cancellationToken) => input.Scope switch
    {
        MainProductRemovalScope.Group => productService.RemoveMainProductFromGroupAsync(new GroupMainProductRemovalInput
        {
            GroupId = RequireScopeId(input.GroupId, "Grup"),
            MainProductId = input.MainProductId,
            EffectiveFromYear = input.EffectiveFromYear,
            EffectiveFromTerm = input.EffectiveFromTerm
        }, Actor, cancellationToken),
        MainProductRemovalScope.ProductGamut => productService.RemoveMainProductFromProductGamutAsync(new ProductGamutMainProductRemovalInput
        {
            ProductGamutId = RequireScopeId(input.ProductGamutId, "Ürün gamı"),
            MainProductId = input.MainProductId,
            EffectiveFromYear = input.EffectiveFromYear,
            EffectiveFromTerm = input.EffectiveFromTerm
        }, Actor, cancellationToken),
        MainProductRemovalScope.Branch => organizationService.UpsertBranchMainProductExclusionAsync(new BranchMainProductExclusionInput
        {
            BranchId = RequireScopeId(input.BranchId, "Şube"),
            MainProductId = input.MainProductId,
            EffectiveFromYear = input.EffectiveFromYear,
            EffectiveFromTerm = input.EffectiveFromTerm
        }, Actor, cancellationToken),
        _ => throw new InvalidOperationException("Geçerli bir çıkarma kapsamı seçin.")
    };

    private static int RequireScopeId(int? value, string label) =>
        value is > 0 ? value.Value : throw new InvalidOperationException($"{label} seçmelisiniz.");

    private async Task<IActionResult> ExecuteAndRedirectAsync(Func<Task> action, string successMessage)
    {
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
