using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class SegmentRankCalculatorTests
{
    [Fact]
    public void Calculate_RanksByScoreThenHgRatioAndReportsEligiblePool()
    {
        var result = SegmentRankCalculator.Calculate(20,
        [
            new(10, 21m, 105m),
            new(20, 21m, 118m),
            new(30, 18m, 140m),
            new(40, null, null)
        ]);

        Assert.Equal(1, result.Rank);
        Assert.Equal(3, result.CandidateCount);
    }

    [Fact]
    public void Calculate_RanksProductsWithinSelectedBranchByTotalScore()
    {
        var first = SegmentRankCalculator.Calculate(101,
        [
            new(101, 6.12m, 76.51m),
            new(102, 3.95m, 92m),
            new(103, 2m, 110m)
        ]);
        var second = SegmentRankCalculator.Calculate(102,
        [
            new(101, 6.12m, 76.51m),
            new(102, 3.95m, 92m),
            new(103, 2m, 110m)
        ]);
        var third = SegmentRankCalculator.Calculate(103,
        [
            new(101, 6.12m, 76.51m),
            new(102, 3.95m, 92m),
            new(103, 2m, 110m)
        ]);

        Assert.Equal(1, first.Rank);
        Assert.Equal(2, second.Rank);
        Assert.Equal(3, third.Rank);
    }

    [Fact]
    public void Calculate_KeepsEqualScoreAndRatioOnSameRank()
    {
        var first = SegmentRankCalculator.Calculate(10,
        [
            new(10, 21m, 118m),
            new(20, 21m, 118m),
            new(30, 18m, 140m)
        ]);
        var second = SegmentRankCalculator.Calculate(20,
        [
            new(10, 21m, 118m),
            new(20, 21m, 118m),
            new(30, 18m, 140m)
        ]);

        Assert.Equal(1, first.Rank);
        Assert.Equal(1, second.Rank);
    }

    [Fact]
    public void Calculate_ExcludesMissingBatchAndLeavesItUnranked()
    {
        var result = SegmentRankCalculator.Calculate(20,
        [
            new(10, 12m, 80m),
            new(20, null, null),
            new(30, 10m, 70m)
        ]);

        Assert.Null(result.Rank);
        Assert.Equal(2, result.CandidateCount);
    }
}
