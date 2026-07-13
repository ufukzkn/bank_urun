using BankUrun.Web.Models;

namespace BankUrun.Web.Services;

public class MainProductPeriodCalculator : IMainProductPeriodCalculator
{
    public MainProductPeriodCalculationResult Calculate(MainProductPeriodCalculationInput input)
    {
        if (input.CriterionScore < 0)
        {
            throw new InvalidOperationException("Kriter puanı negatif olamaz.");
        }

        if (!Enum.IsDefined(input.CalculationType))
        {
            throw new InvalidOperationException("Hesaplama türü geçersiz.");
        }

        var termMonths = GetTermMonths(input.Term);
        if (input.Months.Any(month => !termMonths.Contains(month.Month))
            || input.Months.GroupBy(month => month.Month).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("Aylık değerler seçili dönemle uyumlu değil.");
        }

        var expectedMonths = GetExpectedMonths(input.Year, input.Term, input.AsOfDate);
        var valuesByMonth = input.Months.ToDictionary(month => month.Month);
        var expectedValues = expectedMonths
            .Select(month => valuesByMonth.GetValueOrDefault(month))
            .ToList();
        var completeBatch = expectedValues.Count > 0
            && expectedValues.All(value => value is not null
                && value.ActualValue.HasValue
                && value.ActualAsOfDate.HasValue
                && value.ActualAsOfDate.Value <= input.AsOfDate);
        var targets = expectedValues.Select(value => value?.TargetValue ?? 0).ToList();
        var target = Aggregate(targets, input.CalculationType);

        if (!completeBatch)
        {
            return new MainProductPeriodCalculationResult(
                Round(target), null, null, null, null, false, expectedMonths);
        }

        var actual = Aggregate(
            expectedValues.Select(value => value!.ActualValue!.Value),
            input.CalculationType);
        var ratio = target == 0 ? 0 : actual / target;
        var score = input.CriterionScore * Math.Min(ratio, 1);

        return new MainProductPeriodCalculationResult(
            Round(target),
            Round(actual),
            Round(ratio * 100),
            Round(score),
            Round(score),
            true,
            expectedMonths);
    }

    public IReadOnlyList<int> GetTermMonths(int term) => term switch
    {
        1 => [1, 2, 3, 4, 5, 6],
        2 => [7, 8, 9, 10, 11, 12],
        _ => throw new InvalidOperationException("Dönem 1 veya 2 olmalıdır.")
    };

    public IReadOnlyList<int> GetExpectedMonths(int year, int term, DateOnly asOfDate)
    {
        var months = GetTermMonths(term);
        var periodStart = new DateOnly(year, months[0], 1);
        var lastMonth = months[^1];
        var periodEnd = new DateOnly(year, lastMonth, DateTime.DaysInMonth(year, lastMonth));
        if (asOfDate < periodStart)
        {
            return [];
        }

        return asOfDate >= periodEnd
            ? months
            : months.Where(month => month <= asOfDate.Month).ToList();
    }

    private static decimal Aggregate(IEnumerable<decimal> values, MainProductCalculationType calculationType)
    {
        var list = values.ToList();
        if (list.Count == 0)
        {
            return 0;
        }

        return calculationType == MainProductCalculationType.Average
            ? list.Average()
            : list.Sum();
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
