using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class DenseRankCalculatorTests
{
    [Fact]
    public void Calculate_GivesEqualScoresSameRankWithoutGaps()
    {
        var result = DenseRankCalculator.Calculate([10m, 8m, 8m, null, 4m, 10m]);

        Assert.Equal([1, 2, 2, null, 3, 1], result);
    }

    [Fact]
    public void Calculate_UsesTieBreakerOnlyWhenScoresAreEqual()
    {
        var result = DenseRankCalculator.Calculate(
            [10m, 10m, 8m, null],
            [95m, 112m, 180m, 200m]);

        Assert.Equal([2, 1, 3, null], result);
    }

    [Fact]
    public void Calculate_GivesSameRankWhenScoreAndTieBreakerAreEqual()
    {
        var result = DenseRankCalculator.Calculate(
            [10m, 10m, 8m],
            [112m, 112m, 180m]);

        Assert.Equal([1, 1, 2], result);
    }

    [Fact]
    public void Calculate_RejectsMismatchedRankInputs()
    {
        Assert.Throws<ArgumentException>(() => DenseRankCalculator.Calculate([10m], [90m, 80m]));
    }
}
