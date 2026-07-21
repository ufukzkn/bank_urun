using System.Globalization;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;

namespace BankUrun.Web.Services;

public static class PerformanceQueryProcessor
{
    private static readonly HashSet<int> AllowedPageSizes = [10, 25, 50];
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CompareInfo TurkishCompare = TurkishCulture.CompareInfo;

    public static StringComparer TurkishTextComparer { get; } =
        StringComparer.Create(TurkishCulture, ignoreCase: true);

    public static PerformanceQuery Normalize(PerformanceQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var mode = Enum.IsDefined(query.Mode)
            ? query.Mode
            : PerformanceMode.BranchProduct;
        var pageSize = AllowedPageSizes.Contains(query.PageSize)
            ? query.PageSize
            : mode == PerformanceMode.MainProduct ? 10 : 25;
        var sortDirection = query.SortDirection?.Trim().ToLowerInvariant();
        return new PerformanceQuery
        {
            Mode = mode,
            GroupId = query.GroupId,
            BranchId = mode is PerformanceMode.BranchProduct or PerformanceMode.Portfolio
                ? query.BranchId
                : null,
            Year = query.Year,
            Term = query.Term is 1 or 2 ? query.Term : null,
            MainProductId = mode is PerformanceMode.BranchProduct or PerformanceMode.MainProduct
                ? query.MainProductId
                : null,
            ProductGamutId = mode == PerformanceMode.Portfolio
                ? query.ProductGamutId
                : null,
            PortfolioTypeId = mode == PerformanceMode.Portfolio
                ? query.PortfolioTypeId
                : null,
            Search = string.IsNullOrWhiteSpace(query.Search)
                ? null
                : query.Search.Trim()[..Math.Min(query.Search.Trim().Length, 100)],
            SortKey = string.IsNullOrWhiteSpace(query.SortKey)
                ? null
                : query.SortKey.Trim(),
            SortDirection = sortDirection is "asc" or "desc"
                ? sortDirection
                : null,
            Page = Math.Max(1, query.Page),
            PageSize = pageSize,
            ForceRefresh = query.ForceRefresh
        };
    }

    public static PerformancePage<T> Page<T>(
        IEnumerable<T> source,
        int requestedPage,
        int pageSize)
    {
        var items = source as IReadOnlyCollection<T> ?? source.ToList();
        var totalCount = items.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var page = Math.Clamp(requestedPage, 1, totalPages);
        return new PerformancePage<T>
        {
            Items = items.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public static string BuildFactScopeKey(
        int? groupId,
        int? year,
        int? term,
        IEnumerable<DashboardPeriodOptionViewModel> periods)
    {
        ArgumentNullException.ThrowIfNull(periods);
        var periodSignature = string.Join(
            ',',
            periods
                .OrderBy(period => period.Year)
                .ThenBy(period => period.Term)
                .Select(period =>
                $"{period.Year.ToString(CultureInfo.InvariantCulture)}-{period.Term.ToString(CultureInfo.InvariantCulture)}"));
        return
            $"g:{groupId?.ToString(CultureInfo.InvariantCulture) ?? "*"};" +
            $"y:{year?.ToString(CultureInfo.InvariantCulture) ?? "*"};" +
            $"t:{term?.ToString(CultureInfo.InvariantCulture) ?? "*"};" +
            $"p:{periodSignature}";
    }

    public static bool MatchesTurkish(string? search, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        return values.Any(value => !string.IsNullOrEmpty(value)
            && TurkishCompare.IndexOf(
                value,
                term,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);
    }
}
