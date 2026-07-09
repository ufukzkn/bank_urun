using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IOrganizationService
{
    Task<OrganizationIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task CreateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task CreateUnitAsync(UnitInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateUnitAsync(UnitInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteUnitAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task CreateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteBranchAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task AddGroupUnitAsync(GroupUnitInput input, string actor, CancellationToken cancellationToken = default);
    Task RemoveGroupUnitAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task AddBranchUnitAsync(BranchUnitInput input, string actor, CancellationToken cancellationToken = default);
    Task RemoveBranchUnitAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
}
