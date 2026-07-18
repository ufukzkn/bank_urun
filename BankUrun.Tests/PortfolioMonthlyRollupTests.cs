using BankUrun.Web.Models;
using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class PortfolioMonthlyRollupTests
{
    [Fact]
    public void Aggregate_SumsPortfoliosBeforeApplyingCriterionOnce()
    {
        var months = Enumerable.Range(1, 6).ToArray();
        var first = months.Select(month => Value(month, 100m, 80m)).ToList();
        var second = months.Select(month => Value(month, 50m, 40m)).ToList();
        var rollup = PortfolioMonthlyRollup.Aggregate(months, [first, second]);

        var result = new MainProductPeriodCalculator().Calculate(new MainProductPeriodCalculationInput(
            2025, 1, new DateOnly(2025, 7, 1), MainProductCalculationType.Cumulative, 10m, rollup));

        Assert.All(rollup, month =>
        {
            Assert.Equal(150m, month.TargetValue);
            Assert.Equal(120m, month.ActualValue);
        });
        Assert.Equal(8m, result.TotalScore);
    }

    [Fact]
    public void Aggregate_MarksBranchMonthIncompleteWhenAnyPortfolioIsIncomplete()
    {
        var months = Enumerable.Range(1, 6).ToArray();
        var first = months.Select(month => Value(month, 100m, 80m)).ToList();
        var second = months.Select(month => Value(month, 50m, month == 3 ? null : 40m)).ToList();

        var result = PortfolioMonthlyRollup.Aggregate(months, [first, second]);

        Assert.Null(result.Single(month => month.Month == 3).ActualValue);
        Assert.Null(result.Single(month => month.Month == 3).ActualAsOfDate);
    }

    private static MainProductMonthlyValue Value(int month, decimal target, decimal? actual) =>
        new(month, target, actual, actual.HasValue ? new DateOnly(2025, month, 15) : null);
}
