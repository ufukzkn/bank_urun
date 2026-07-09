using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankUrun.Web.Controllers;

public class OrganizationController(IOrganizationService organizationService) : Controller
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUnit(UnitInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.CreateUnitAsync(input, Actor, cancellationToken), "Birim oluşturuldu.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUnit(UnitInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.UpdateUnitAsync(input, Actor, cancellationToken), "Birim güncellendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnit(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.DeleteUnitAsync(input, Actor, cancellationToken), "Birim silindi.");
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGroupUnit(GroupUnitInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.AddGroupUnitAsync(input, Actor, cancellationToken), "Grup-birim bağlantısı eklendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveGroupUnit(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.RemoveGroupUnitAsync(input, Actor, cancellationToken), "Grup-birim bağlantısı kaldırıldı.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBranchUnit(BranchUnitInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.AddBranchUnitAsync(input, Actor, cancellationToken), "Şube-birim bağlantısı eklendi.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBranchUnit(LinkIdInput input, CancellationToken cancellationToken)
    {
        return await ExecuteAndRedirectAsync(ModelState.IsValid, () => organizationService.RemoveBranchUnitAsync(input, Actor, cancellationToken), "Şube-birim bağlantısı kaldırıldı.");
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
