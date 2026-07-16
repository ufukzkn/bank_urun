using BankUrun.Web.Models;
using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class SubProductMetricAggregatorTests
{
    [Fact]
    public void AggregateMonth_SumsAllLinkedSubProducts()
    {
        var result = SubProductMetricAggregator.AggregateMonth(1,
        [
            Metric(100, 90, new DateOnly(2025, 1, 31)),
            Metric(250, 275, new DateOnly(2025, 1, 31))
        ]);

        Assert.Equal(350, result.TargetValue);
        Assert.Equal(365, result.ActualValue);
        Assert.Equal(new DateOnly(2025, 1, 31), result.ActualAsOfDate);
    }

    [Fact]
    public void AggregateMonth_WhenOneSubProductIsMissing_MarksWholeMonthPending()
    {
        var result = SubProductMetricAggregator.AggregateMonth(2,
        [
            Metric(100, 90, new DateOnly(2025, 2, 28)),
            Metric(250, null, null)
        ]);

        Assert.Equal(350, result.TargetValue);
        Assert.Null(result.ActualValue);
        Assert.Null(result.ActualAsOfDate);
    }

    [Fact]
    public void AggregateMonth_SharedSubProductContributesItsFullValueToEachParent()
    {
        IReadOnlyCollection<BranchSubProductMonthlyMetric?> shared =
        [
            Metric(120, 132, new DateOnly(2025, 3, 31))
        ];

        var firstParent = SubProductMetricAggregator.AggregateMonth(3, shared);
        var secondParent = SubProductMetricAggregator.AggregateMonth(3, shared);

        Assert.Equal(120, firstParent.TargetValue);
        Assert.Equal(120, secondParent.TargetValue);
        Assert.Equal(132, firstParent.ActualValue);
        Assert.Equal(132, secondParent.ActualValue);
    }

    private static BranchSubProductMonthlyMetric Metric(decimal target, decimal? actual, DateOnly? asOfDate) => new()
    {
        TargetValue = target,
        ActualValue = actual,
        ActualAsOfDate = asOfDate
    };
}
