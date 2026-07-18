namespace BankUrun.Web.Services;

public sealed record PortfolioSubProductActualValue(decimal? ActualValue, DateOnly? ActualAsOfDate);

public static class PortfolioActualAggregator
{
    public static (decimal? ActualValue, DateOnly? ActualAsOfDate) Aggregate(
        IReadOnlyCollection<PortfolioSubProductActualValue?> values)
    {
        if (values.Count == 0 || values.Any(value => value?.ActualValue.HasValue != true
            || value.ActualAsOfDate.HasValue != true))
        {
            return (null, null);
        }

        return (
            Math.Round(values.Sum(value => value!.ActualValue!.Value), 2, MidpointRounding.AwayFromZero),
            values.Max(value => value!.ActualAsOfDate));
    }
}
