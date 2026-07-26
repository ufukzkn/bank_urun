using BankUrun.Web.Models;
using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;

namespace BankUrun.Tests;

public class TargetPeriodValueConverterTests
{
    [Fact]
    public void SixMonthCumulative_DistributesAndPreservesRoundedTotal()
    {
        var values = TargetPeriodValueConverter.Expand(
            1,
            MainProductCalculationType.Cumulative,
            TargetEntryMode.SixMonth,
            100,
            null,
            null,
            []);

        Assert.Equal(6, values.Count);
        Assert.Equal([16.67m, 16.67m, 16.67m, 16.67m, 16.67m, 16.65m],
            values.Select(item => item.TargetValue));
        Assert.Equal(100m, TargetPeriodValueConverter.Aggregate(
            values.Select(item => item.TargetValue),
            MainProductCalculationType.Cumulative));
    }

    [Fact]
    public void ThreeMonthCumulative_DistributesEachBlockIndependently()
    {
        var values = TargetPeriodValueConverter.Expand(
            2,
            MainProductCalculationType.Cumulative,
            TargetEntryMode.ThreeMonth,
            null,
            60,
            90,
            []);

        Assert.Equal([7, 8, 9, 10, 11, 12], values.Select(item => item.Month));
        Assert.Equal([20m, 20m, 20m, 30m, 30m, 30m],
            values.Select(item => item.TargetValue));
    }

    [Fact]
    public void AverageEntry_CopiesTheEnteredValueToEveryMonth()
    {
        var values = TargetPeriodValueConverter.Expand(
            1,
            MainProductCalculationType.Average,
            TargetEntryMode.SixMonth,
            275.50m,
            null,
            null,
            []);

        Assert.All(values, item => Assert.Equal(275.50m, item.TargetValue));
        Assert.Equal(275.50m, TargetPeriodValueConverter.Aggregate(
            values.Select(item => item.TargetValue),
            MainProductCalculationType.Average));
    }

    [Fact]
    public void MonthlyEntry_RequiresExactlyTheSelectedTermsSixMonths()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            TargetPeriodValueConverter.Expand(
                1,
                MainProductCalculationType.Cumulative,
                TargetEntryMode.Monthly,
                null,
                null,
                null,
                [new MonthlyTargetValue(1, 10)]));

        Assert.Contains("altı aylık", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
