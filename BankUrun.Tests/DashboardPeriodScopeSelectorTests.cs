using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;

namespace BankUrun.Tests;

public class DashboardPeriodScopeSelectorTests
{
    private static readonly DashboardPeriodOptionViewModel[] Periods =
    [
        new() { Year = 2024, Term = 1 }, new() { Year = 2024, Term = 2 },
        new() { Year = 2025, Term = 1 }, new() { Year = 2025, Term = 2 },
        new() { Year = 2026, Term = 1 }, new() { Year = 2026, Term = 2 }
    ];

    [Fact]
    public void Select_WithNoYearOrTerm_ReturnsEveryExactPeriodNewestFirst()
    {
        var result = DashboardPeriodScopeSelector.Select(Periods, null, null);

        Assert.Equal(
            [(2026, 2), (2026, 1), (2025, 2), (2025, 1), (2024, 2), (2024, 1)],
            result.Select(period => (period.Year, period.Term)));
    }

    [Theory]
    [InlineData(2025, null, 2)]
    [InlineData(null, 1, 3)]
    [InlineData(2024, 2, 1)]
    public void Select_AppliesOnlySpecifiedDimensions(int? year, int? term, int expectedCount)
    {
        var result = DashboardPeriodScopeSelector.Select(Periods, year, term);

        Assert.Equal(expectedCount, result.Count);
        Assert.All(result, period =>
        {
            if (year.HasValue) Assert.Equal(year.Value, period.Year);
            if (term.HasValue) Assert.Equal(term.Value, period.Term);
        });
    }

    [Fact]
    public void Select_DoesNotInventUnavailablePeriod()
    {
        var result = DashboardPeriodScopeSelector.Select(Periods, 2023, 2);

        Assert.Empty(result);
    }
}
