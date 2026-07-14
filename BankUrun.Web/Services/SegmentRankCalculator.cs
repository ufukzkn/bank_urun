namespace BankUrun.Web.Services;

public static class SegmentRankCalculator
{
    public static SegmentRankResult Calculate(
        int selectedMainProductInstanceId,
        IReadOnlyCollection<SegmentRankCandidate> candidates)
    {
        var eligible = candidates
            .Where(candidate => candidate.TotalScore.HasValue && candidate.HgRatioPercent.HasValue)
            .OrderBy(candidate => candidate.MainProductInstanceId)
            .ToList();
        var ranks = DenseRankCalculator.Calculate(
            eligible.Select(candidate => candidate.TotalScore).ToList(),
            eligible.Select(candidate => candidate.HgRatioPercent).ToList());
        var selectedIndex = eligible.FindIndex(candidate =>
            candidate.MainProductInstanceId == selectedMainProductInstanceId);

        return new SegmentRankResult(
            selectedIndex < 0 ? null : ranks[selectedIndex],
            eligible.Count);
    }
}

public sealed record SegmentRankCandidate(
    int MainProductInstanceId,
    decimal? TotalScore,
    decimal? HgRatioPercent);

public sealed record SegmentRankResult(
    int? Rank,
    int CandidateCount);
