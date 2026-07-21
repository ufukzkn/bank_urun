using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IProductManagementService
{
    Task<ProductIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task CreateProductAsync(CreateProductInput input, string actor, CancellationToken cancellationToken = default);
    Task RenameProductAsync(RenameProductInput input, string actor, CancellationToken cancellationToken = default);
    Task DeactivateProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetProductDeleteImpactAsync(ProductIdInput input, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default);
    Task UpsertProductGamutAsync(ProductGamutInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetProductGamutDeleteImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteProductGamutAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task UpsertProductGamutAssignmentAsync(ProductGamutAssignmentInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetProductGamutAssignmentRemovalImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteProductGamutAssignmentAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetGroupMainProductRemovalImpactAsync(GroupMainProductRemovalInput input, CancellationToken cancellationToken = default);
    Task RemoveMainProductFromGroupAsync(GroupMainProductRemovalInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetProductGamutMainProductRemovalImpactAsync(ProductGamutMainProductRemovalInput input, CancellationToken cancellationToken = default);
    Task RemoveMainProductFromProductGamutAsync(ProductGamutMainProductRemovalInput input, string actor, CancellationToken cancellationToken = default);
}
