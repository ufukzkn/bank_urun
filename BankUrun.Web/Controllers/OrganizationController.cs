using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class OrganizationController(
    IOrganizationService organizationService,
    IPerformanceCacheInvalidator performanceCacheInvalidator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await organizationService.GetIndexAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(GroupInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.CreateGroupAsync(input, Actor, cancellationToken), "Grup oluşturuldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGroup(GroupInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.UpdateGroupAsync(input, Actor, cancellationToken), "Grup güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroup(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.DeleteGroupAsync(input, Actor, cancellationToken), "Grup silindi.");
    }

    [HttpGet]
    public Task<IActionResult> GroupDeleteImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => organizationService.GetGroupDeleteImpactAsync(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBranch(BranchInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.CreateBranchAsync(input, Actor, cancellationToken), "Şube oluşturuldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBranch(BranchInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.UpdateBranchAsync(input, Actor, cancellationToken), "Şube güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBranch(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.DeleteBranchAsync(input, Actor, cancellationToken), "Şube silindi.");
    }

    [HttpGet]
    public Task<IActionResult> BranchDeleteImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => organizationService.GetBranchDeleteImpactAsync(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePortfolioType(PortfolioTypeInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid,
            () => organizationService.UpsertPortfolioTypeAsync(input, Actor, cancellationToken),
            input.Id == 0 ? "Portföy tipi oluşturuldu." : "Portföy tipi güncellendi.");
    }

    [HttpGet]
    public Task<IActionResult> PortfolioTypeDeleteImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => organizationService.GetPortfolioTypeDeleteImpactAsync(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePortfolioType(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid,
            () => organizationService.DeletePortfolioTypeAsync(input, Actor, cancellationToken),
            "Portföy tipi silindi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePortfolio(PortfolioInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid,
            () => organizationService.UpsertPortfolioAsync(input, Actor, cancellationToken),
            input.Id == 0 ? "Portföy oluşturuldu." : "Portföy güncellendi.");
    }

    [HttpGet]
    public Task<IActionResult> PortfolioDeleteImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => organizationService.GetPortfolioDeleteImpactAsync(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePortfolio(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid,
            () => organizationService.DeletePortfolioAsync(input, Actor, cancellationToken),
            "Portföy silindi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBranchMainProductExclusion(BranchMainProductExclusionInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid,
            () => organizationService.UpsertBranchMainProductExclusionAsync(input, Actor, cancellationToken),
            input.Id == 0 ? "Ana ürün şube kapsamından çıkarıldı." : "Şube ürün istisnası güncellendi.");
    }

    [HttpGet]
    public Task<IActionResult> BranchMainProductExclusionImpact(
        int branchId,
        int mainProductId,
        int effectiveFromYear,
        int effectiveFromTerm,
        CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => organizationService.GetBranchMainProductExclusionImpactAsync(
            new BranchMainProductExclusionInput
            {
                BranchId = branchId,
                MainProductId = mainProductId,
                EffectiveFromYear = effectiveFromYear,
                EffectiveFromTerm = effectiveFromTerm
            }, cancellationToken));

    [HttpGet]
    public Task<IActionResult> BranchMainProductExclusionRemovalImpact(int id, CancellationToken cancellationToken) =>
        ExecuteImpactAsync(() => organizationService.GetBranchMainProductExclusionRemovalImpactAsync(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBranchMainProductExclusion(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid,
            () => organizationService.DeleteBranchMainProductExclusionAsync(input, Actor, cancellationToken),
            "Şube ürün istisnası kaldırıldı; ürün yeniden kapsama alındı.");
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
