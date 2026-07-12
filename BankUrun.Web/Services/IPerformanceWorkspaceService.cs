using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IPerformanceWorkspaceService
{
    Task<PerformanceIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task CreateParameterAsync(PerformanceParameterInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateParameterAsync(PerformanceParameterUpdateInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteParameterAsync(PerformanceParameterIdInput input, string actor, CancellationToken cancellationToken = default);
    Task UpsertMetricResultAsync(MetricResultInput input, string actor, CancellationToken cancellationToken = default);
}
