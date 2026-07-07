using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IProductManagementService
{
    Task<ProductIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task CreateProductAsync(CreateProductInput input, string actor, CancellationToken cancellationToken = default);
    Task RenameProductAsync(RenameProductInput input, string actor, CancellationToken cancellationToken = default);
    Task DeactivateProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default);
}
