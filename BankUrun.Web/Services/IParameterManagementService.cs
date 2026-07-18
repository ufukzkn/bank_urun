using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IParameterManagementService
{
    Task<ParameterIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<ParameterPageViewModel> GetPageAsync(ParameterQuery query, CancellationToken cancellationToken = default);
    Task<MainProductTargetPageViewModel> GetMainProductTargetPageAsync(MainProductTargetQuery query, CancellationToken cancellationToken = default);
    Task<MainProductTargetEditorViewModel> GetMainProductTargetEditorAsync(int parameterId, int portfolioId, CancellationToken cancellationToken = default);
    Task UpsertParameterAsync(MainProductParameterInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateMainProductTargetsAsync(PortfolioMainProductTargetsInput input, string actor, CancellationToken cancellationToken = default);
    Task<ManagementImpactViewModel> GetParameterDeleteImpactAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteParameterAsync(ParameterIdInput input, string actor, CancellationToken cancellationToken = default);
}
