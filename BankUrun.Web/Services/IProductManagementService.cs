using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IProductManagementService
{
    Task<ProductIndexViewModel> GetIndexAsync(ProductFilterInput filter, CancellationToken cancellationToken = default);
    Task CreateProductAsync(CreateProductInput input, string actor, CancellationToken cancellationToken = default);
    Task AddMainToPeriodAsync(AddMainToPeriodInput input, string actor, CancellationToken cancellationToken = default);
    Task AssignSubProductAsync(AssignSubProductInput input, string actor, CancellationToken cancellationToken = default);
    Task RenameProductAsync(RenameProductInput input, string actor, CancellationToken cancellationToken = default);
    Task DeactivateProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default);
    Task RemoveMainProductPeriodAsync(EntityIdInput input, string actor, CancellationToken cancellationToken = default);
    Task RemoveSubProductAssignmentAsync(EntityIdInput input, string actor, CancellationToken cancellationToken = default);
}
