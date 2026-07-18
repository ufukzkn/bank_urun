using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class PartitionedDenseRankCalculatorTests
{
    [Fact]
    public void Calculate_RanksEachGroupGamutAndPeriodIndependently()
    {
        var rows = new[]
        {
            new Row(1, 10, 2025, 1, 90m),
            new Row(1, 10, 2025, 1, 70m),
            new Row(1, 20, 2025, 1, 65m),
            new Row(1, 10, 2025, 2, 60m),
            new Row(1, 10, 2025, 1, 90m),
            new Row(1, 10, 2025, 1, null)
        };

        var result = PartitionedDenseRankCalculator.Calculate(
            rows,
            row => (row.GroupId, row.GamutId, row.Year, row.Term),
            row => row.Score);

        Assert.Equal([1, 2, 1, 1, 1, null], result.Select(item => item.Rank));
        Assert.Equal([3, 3, 1, 1, 3, 3], result.Select(item => item.CandidateCount));
    }

    [Fact]
    public void Calculate_DoesNotChangeOfficialRanksWhenRowsAreLaterFiltered()
    {
        var rows = new[] { new Row(1, 10, 2025, 1, 100m), new Row(1, 10, 2025, 1, 80m) };
        var ranked = PartitionedDenseRankCalculator.Calculate(
            rows, row => (row.GroupId, row.GamutId, row.Year, row.Term), row => row.Score);

        var visibleSecondRowRank = ranked[1];

        Assert.Equal(2, visibleSecondRowRank.Rank);
        Assert.Equal(2, visibleSecondRowRank.CandidateCount);
    }

    private sealed record Row(int GroupId, int GamutId, int Year, int Term, decimal? Score);
}
