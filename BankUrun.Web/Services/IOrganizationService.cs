using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IOrganizationService
{
    Task<OrganizationIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task CreateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task CreateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteBranchAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
}
