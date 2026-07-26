using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public static class TargetPeriodValueConverter
{
    public static IReadOnlyList<MonthlyTargetValue> Expand(
        int term,
        MainProductCalculationType calculationType,
        TargetEntryMode entryMode,
        decimal? sixMonthValue,
        decimal? firstThreeMonthValue,
        decimal? secondThreeMonthValue,
        IReadOnlyCollection<MonthlyTargetValue> monthlyValues)
    {
        var months = GetTermMonths(term);
        return entryMode switch
        {
            TargetEntryMode.SixMonth => ExpandBlock(
                months,
                RequireValue(sixMonthValue, "Altı aylık hedefi girin."),
                calculationType),
            TargetEntryMode.ThreeMonth =>
            [
                .. ExpandBlock(
                    months.Take(3).ToArray(),
                    RequireValue(firstThreeMonthValue, "İlk üç aylık hedefi girin."),
                    calculationType),
                .. ExpandBlock(
                    months.Skip(3).Take(3).ToArray(),
                    RequireValue(secondThreeMonthValue, "İkinci üç aylık hedefi girin."),
                    calculationType)
            ],
            TargetEntryMode.Monthly => ValidateMonthly(months, monthlyValues),
            _ => throw new InvalidOperationException("Hedef giriş biçimi geçersiz.")
        };
    }

    public static decimal Aggregate(
        IEnumerable<decimal> values,
        MainProductCalculationType calculationType)
    {
        var list = values.ToList();
        if (list.Count == 0)
        {
            return 0;
        }

        return Round(calculationType == MainProductCalculationType.Average
            ? list.Average()
            : list.Sum());
    }

    public static IReadOnlyList<int> GetTermMonths(int term) => term switch
    {
        1 => [1, 2, 3, 4, 5, 6],
        2 => [7, 8, 9, 10, 11, 12],
        _ => throw new InvalidOperationException("Dönem 1 veya 2 olmalıdır.")
    };

    private static IReadOnlyList<MonthlyTargetValue> ExpandBlock(
        IReadOnlyList<int> months,
        decimal value,
        MainProductCalculationType calculationType)
    {
        ValidateValue(value);
        if (calculationType == MainProductCalculationType.Average)
        {
            return months.Select(month => new MonthlyTargetValue(month, Round(value))).ToList();
        }

        var perMonth = Round(value / months.Count);
        var result = months.Take(months.Count - 1)
            .Select(month => new MonthlyTargetValue(month, perMonth))
            .ToList();
        var allocated = perMonth * (months.Count - 1);
        result.Add(new MonthlyTargetValue(months[^1], Round(value - allocated)));
        return result;
    }

    private static IReadOnlyList<MonthlyTargetValue> ValidateMonthly(
        IReadOnlyList<int> termMonths,
        IReadOnlyCollection<MonthlyTargetValue> values)
    {
        if (values.Count != termMonths.Count
            || values.Select(item => item.Month).Distinct().Count() != termMonths.Count
            || values.Select(item => item.Month).Except(termMonths).Any())
        {
            throw new InvalidOperationException("Dönemin altı aylık hedeflerini eksiksiz girin.");
        }

        foreach (var value in values)
        {
            ValidateValue(value.TargetValue);
        }

        return termMonths
            .Select(month => new MonthlyTargetValue(
                month,
                Round(values.Single(item => item.Month == month).TargetValue)))
            .ToList();
    }

    private static decimal RequireValue(decimal? value, string message)
    {
        if (!value.HasValue)
        {
            throw new InvalidOperationException(message);
        }

        ValidateValue(value.Value);
        return value.Value;
    }

    private static void ValidateValue(decimal value)
    {
        if (value < 0)
        {
            throw new InvalidOperationException("Hedef tutarı negatif olamaz.");
        }
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record MonthlyTargetValue(int Month, decimal TargetValue);
