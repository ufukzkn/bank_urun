namespace BankUrun.Web.Services;

public static class PortfolioMonthlyRollup
{
    public static IReadOnlyList<MainProductMonthlyValue> Aggregate(
        IReadOnlyList<int> termMonths,
        IReadOnlyCollection<IReadOnlyList<MainProductMonthlyValue>> portfolioMonths) =>
        termMonths.Select(month =>
        {
            var values = portfolioMonths.Select(months => months.Single(value => value.Month == month)).ToList();
            var complete = values.Count > 0
                && values.All(value => value.ActualValue.HasValue && value.ActualAsOfDate.HasValue);
            return new MainProductMonthlyValue(
                month,
                Round(values.Sum(value => value.TargetValue)),
                complete ? Round(values.Sum(value => value.ActualValue!.Value)) : null,
                complete ? values.Max(value => value.ActualAsOfDate) : null);
        }).ToList();

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
