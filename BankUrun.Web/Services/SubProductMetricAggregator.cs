using BankUrun.Web.Models;

namespace BankUrun.Web.Services;

public static class SubProductMetricAggregator
{
    public static MainProductMonthlyValue AggregateMonth(
        int month,
        IReadOnlyCollection<BranchSubProductMonthlyMetric?> metrics)
    {
        var complete = metrics.Count > 0
            && metrics.All(metric => metric?.ActualValue is not null && metric.ActualAsOfDate.HasValue);
        return new MainProductMonthlyValue(
            month,
            Round(metrics.Sum(metric => metric?.TargetValue ?? 0)),
            complete ? Round(metrics.Sum(metric => metric!.ActualValue!.Value)) : null,
            complete ? metrics.Max(metric => metric!.ActualAsOfDate) : null);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
