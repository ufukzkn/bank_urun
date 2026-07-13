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
}
