using BankUrun.Web.Models;
using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class MainProductPeriodCalculatorTests
{
    private readonly MainProductPeriodCalculator calculator = new();

    [Fact]
    public void Average_UsesSixMonthAveragesForCompletedTerm()
    {
        var result = calculator.Calculate(Input(
            2025,
            2,
            new DateOnly(2026, 1, 5),
            MainProductCalculationType.Average,
            20m,
            Enumerable.Range(7, 6).Select(month => Month(month, 100m, 80m))));

        Assert.True(result.HasCompleteBatchData);
        Assert.Equal(100m, result.TargetValue);
        Assert.Equal(80m, result.ActualValue);
        Assert.Equal(80m, result.HgRatioPercent);
        Assert.Equal(16m, result.HgoScore);
        Assert.Equal(result.HgoScore, result.TotalScore);
    }

    [Fact]
    public void Cumulative_SumsExpectedMonths()
    {
        var result = calculator.Calculate(Input(
            2026,
            1,
            new DateOnly(2026, 7, 1),
            MainProductCalculationType.Cumulative,
            12m,
            Enumerable.Range(1, 6).Select(month => Month(month, month * 10m, month * 9m))));

        Assert.Equal(210m, result.TargetValue);
        Assert.Equal(189m, result.ActualValue);
        Assert.Equal(90m, result.HgRatioPercent);
        Assert.Equal(10.8m, result.TotalScore);
    }

    [Fact]
    public void OpenTerm_ShowsFullTargetButDoesNotProducePeriodScore()
    {
        var result = calculator.Calculate(Input(
            2026,
            2,
            new DateOnly(2026, 7, 13),
            MainProductCalculationType.Cumulative,
            10m,
            Enumerable.Range(7, 6).Select(month => Month(month, month * 10m, month == 7 ? 45m : null))));

        Assert.Equal([7, 8, 9, 10, 11, 12], result.ExpectedMonths);
        Assert.False(result.HasCompleteBatchData);
        Assert.Equal(570m, result.TargetValue);
        Assert.Null(result.ActualValue);
        Assert.Null(result.TotalScore);
    }

    [Fact]
    public void MissingExpectedBatchMonth_DoesNotProduceScore()
    {
        var values = Enumerable.Range(1, 6)
            .Select(month => Month(month, 100m, month == 4 ? null : 90m));

        var result = calculator.Calculate(Input(
            2026,
            1,
            new DateOnly(2026, 7, 1),
            MainProductCalculationType.Average,
            15m,
            values));

        Assert.False(result.HasCompleteBatchData);
        Assert.Null(result.ActualValue);
        Assert.Null(result.HgRatioPercent);
        Assert.Null(result.TotalScore);
    }

    [Fact]
    public void ZeroTarget_ProducesZeroRatioAndScore()
    {
        var result = calculator.Calculate(Input(
            2026,
            1,
            new DateOnly(2026, 7, 1),
            MainProductCalculationType.Cumulative,
            25m,
            Enumerable.Range(1, 6).Select(month => Month(month, 0m, 10m))));

        Assert.Equal(0m, result.HgRatioPercent);
        Assert.Equal(0m, result.TotalScore);
    }

    [Fact]
    public void Score_IsCappedAtCriterionScore()
    {
        var result = calculator.Calculate(Input(
            2026,
            1,
            new DateOnly(2026, 7, 1),
            MainProductCalculationType.Average,
            17.35m,
            Enumerable.Range(1, 6).Select(month => Month(month, 100m, 180m))));

        Assert.Equal(180m, result.HgRatioPercent);
        Assert.Equal(17.35m, result.TotalScore);
    }

    [Fact]
    public void Results_AreRoundedToTwoDecimalsAwayFromZero()
    {
        var result = calculator.Calculate(Input(
            2026,
            1,
            new DateOnly(2026, 7, 1),
            MainProductCalculationType.Cumulative,
            10m,
            Enumerable.Range(1, 6).Select(month => Month(month, 3m, 2m))));

        Assert.Equal(66.67m, result.HgRatioPercent);
        Assert.Equal(6.67m, result.TotalScore);
    }

    [Fact]
    public void FutureTerm_ShowsConfiguredTargetAndNoScore()
    {
        var result = calculator.Calculate(Input(
            2027,
            1,
            new DateOnly(2026, 7, 13),
            MainProductCalculationType.Average,
            10m,
            Enumerable.Range(1, 6).Select(month => Month(month, 100m, null))));

        Assert.Equal([1, 2, 3, 4, 5, 6], result.ExpectedMonths);
        Assert.Equal(100m, result.TargetValue);
        Assert.False(result.HasCompleteBatchData);
        Assert.Null(result.TotalScore);
    }

    [Fact]
    public void PeriodEndDay_IsNotClosedYet()
    {
        var result = calculator.Calculate(Input(
            2026,
            1,
            new DateOnly(2026, 6, 30),
            MainProductCalculationType.Cumulative,
            10m,
            Enumerable.Range(1, 6).Select(month => Month(month, 100m, 90m))));

        Assert.False(result.HasCompleteBatchData);
        Assert.Null(result.TotalScore);
    }

    private static MainProductPeriodCalculationInput Input(
        int year,
        int term,
        DateOnly asOfDate,
        MainProductCalculationType calculationType,
        decimal criterionScore,
        IEnumerable<MainProductMonthlyValue> months) =>
        new(year, term, asOfDate, calculationType, criterionScore, months.ToList());

    private static MainProductMonthlyValue Month(int month, decimal target, decimal? actual) =>
        new(month, target, actual, actual.HasValue ? new DateOnly(2025, month, 13) : null);
}
