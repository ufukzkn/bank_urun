using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public static class DashboardPeriodScopeSelector
{
    public static IReadOnlyList<DashboardPeriodOptionViewModel> Select(
        IEnumerable<DashboardPeriodOptionViewModel> availablePeriods,
        int? year,
        int? term) => availablePeriods
        .Where(period => !year.HasValue || period.Year == year.Value)
        .Where(period => !term.HasValue || period.Term == term.Value)
        .OrderByDescending(period => period.Year)
        .ThenByDescending(period => period.Term)
        .ToList();
}
