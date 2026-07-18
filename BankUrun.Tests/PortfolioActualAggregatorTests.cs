using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class PortfolioActualAggregatorTests
{
    [Fact]
    public void Aggregate_SumsAllSubProductActuals()
    {
        var result = PortfolioActualAggregator.Aggregate(
        [
            new(40m, new DateOnly(2025, 1, 10)),
            new(65m, new DateOnly(2025, 1, 12))
        ]);

        Assert.Equal(105m, result.ActualValue);
        Assert.Equal(new DateOnly(2025, 1, 12), result.ActualAsOfDate);
    }

    [Fact]
    public void Aggregate_MarksMonthIncompleteWhenOneSubProductBatchIsMissing()
    {
        var result = PortfolioActualAggregator.Aggregate(
        [
            new(40m, new DateOnly(2025, 1, 10)),
            new(null, null)
        ]);

        Assert.Null(result.ActualValue);
        Assert.Null(result.ActualAsOfDate);
    }

    [Fact]
    public void SharedSubProduct_CanContributeItsFullValueToEachMainProduct()
    {
        var shared = new PortfolioSubProductActualValue(75m, new DateOnly(2025, 1, 10));

        var firstMainProduct = PortfolioActualAggregator.Aggregate([shared]);
        var secondMainProduct = PortfolioActualAggregator.Aggregate([shared]);

        Assert.Equal(75m, firstMainProduct.ActualValue);
        Assert.Equal(75m, secondMainProduct.ActualValue);
    }
}
