using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IParameterManagementService
{
    Task<ParameterIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task<ParameterPageViewModel> GetPageAsync(ParameterQuery query, CancellationToken cancellationToken = default);
    Task UpsertParameterAsync(MainProductParameterInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateTargetsAsync(MonthlyTargetsInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteParameterAsync(ParameterIdInput input, string actor, CancellationToken cancellationToken = default);
}
