using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IOrganizationService
{
    Task<OrganizationIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task CreateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetGroupDeleteImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task CreateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetBranchDeleteImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteBranchAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task UpsertPortfolioTypeAsync(PortfolioTypeInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetPortfolioTypeDeleteImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeletePortfolioTypeAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task UpsertPortfolioAsync(PortfolioInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetPortfolioDeleteImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeletePortfolioAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetBranchMainProductExclusionImpactAsync(BranchMainProductExclusionInput input, CancellationToken cancellationToken = default);
    Task UpsertBranchMainProductExclusionAsync(BranchMainProductExclusionInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetBranchMainProductExclusionRemovalImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteBranchMainProductExclusionAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
}
