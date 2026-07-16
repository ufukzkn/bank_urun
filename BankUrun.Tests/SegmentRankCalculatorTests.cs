using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class SegmentRankCalculatorTests
{
    [Fact]
    public void Calculate_RanksBranchesByTotalScoreOnly()
    {
        var result = SegmentRankCalculator.Calculate(20,
        [
            new(10, 18m),
            new(20, 21m),
            new(30, 19m)
        ]);

        Assert.Equal(1, result.Rank);
        Assert.Equal(3, result.CandidateCount);
    }

    [Fact]
    public void Calculate_KeepsEqualTotalsOnSameDenseRank()
    {
        var first = SegmentRankCalculator.Calculate(10,
        [
            new(10, 21m),
            new(20, 21m),
            new(30, 18m)
        ]);
        var second = SegmentRankCalculator.Calculate(20,
        [
            new(10, 21m),
            new(20, 21m),
            new(30, 18m)
        ]);
        var third = SegmentRankCalculator.Calculate(30,
        [
            new(10, 21m),
            new(20, 21m),
            new(30, 18m)
        ]);

        Assert.Equal(1, first.Rank);
        Assert.Equal(1, second.Rank);
        Assert.Equal(2, third.Rank);
    }

    [Fact]
    public void Calculate_ExcludesIncompleteBranches()
    {
        var result = SegmentRankCalculator.Calculate(20,
        [
            new(10, 12m),
            new(20, null),
            new(30, 10m)
        ]);

        Assert.Null(result.Rank);
        Assert.Equal(2, result.CandidateCount);
    }

    [Fact]
    public void Calculate_UsesBranchIdentityForSelectedCandidate()
    {
        var result = SegmentRankCalculator.Calculate(202,
        [
            new(101, 9m),
            new(202, 7m),
            new(303, 8m)
        ]);

        Assert.Equal(3, result.Rank);
    }
}
