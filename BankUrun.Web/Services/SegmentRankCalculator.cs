namespace BankUrun.Web.Services;

public static class SegmentRankCalculator
{
    public static SegmentRankResult Calculate(
        int selectedBranchId,
        IReadOnlyCollection<SegmentRankCandidate> candidates)
    {
        var eligible = candidates
            .Where(candidate => candidate.TotalScore.HasValue)
            .OrderBy(candidate => candidate.BranchId)
            .ToList();
        var ranks = DenseRankCalculator.Calculate(eligible.Select(candidate => candidate.TotalScore).ToList());
        var selectedIndex = eligible.FindIndex(candidate =>
            candidate.BranchId == selectedBranchId);

        return new SegmentRankResult(
            selectedIndex < 0 ? null : ranks[selectedIndex],
            eligible.Count);
    }
}

public sealed record SegmentRankCandidate(
    int BranchId,
    decimal? TotalScore);

public sealed record SegmentRankResult(
    int? Rank,
    int CandidateCount);
