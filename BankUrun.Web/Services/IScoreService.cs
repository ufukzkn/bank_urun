using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public interface IScoreService
{
    Task<ScoreIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);
    Task CreateScoreAsync(ScoreInput input, string actor, CancellationToken cancellationToken = default);
    Task UpdateScoreAsync(ScoreInput input, string actor, CancellationToken cancellationToken = default);
    Task DeleteScoreAsync(ScoreIdInput input, string actor, CancellationToken cancellationToken = default);
}
