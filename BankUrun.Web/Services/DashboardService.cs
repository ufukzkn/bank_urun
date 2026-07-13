using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class DashboardService(
    AppDbContext db,
    TimeProvider timeProvider,
    IMainProductPeriodCalculator calculator) : IDashboardService
{
    private static readonly string[] TurkishMonthNames =
    [
        "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];

    public async Task<DashboardIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var branches = await GetBranchOptionsAsync(cancellationToken);
        var years = await db.MainProductInstances
            .AsNoTracking()
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .Select(instance => instance.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync(cancellationToken);
        var today = Today;
        var selectedYear = years.Contains(today.Year) ? today.Year : years.FirstOrDefault(today.Year);
        var currentTerm = today.Month <= 6 ? 1 : 2;
        var selectedTerm = await db.MainProductInstances
            .AsNoTracking()
            .AnyAsync(instance => instance.Year == selectedYear && instance.Term == currentTerm, cancellationToken)
                ? currentTerm
                : await db.MainProductInstances
                    .AsNoTracking()
                    .Where(instance => instance.Year == selectedYear && (instance.Term == 1 || instance.Term == 2))
                    .Select(instance => instance.Term)
                    .OrderByDescending(term => term)
                    .FirstOrDefaultAsync(cancellationToken);
        if (selectedTerm is not (1 or 2))
        {
            selectedTerm = 1;
        }

        var selectedBranchId = branches.FirstOrDefault()?.Id ?? 0;
        return new DashboardIndexViewModel
        {
            Branches = branches,
            Years = years,
            SelectedBranchId = selectedBranchId,
            SelectedYear = selectedYear,
            SelectedTerm = selectedTerm,
            BatchDate = today,
            Snapshot = await GetSnapshotAsync(selectedBranchId, selectedYear, selectedTerm, cancellationToken)
        };
    }

    public async Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        int? branchId,
        int? year,
        int? term,
        CancellationToken cancellationToken = default)
    {
        var branches = await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .OrderBy(branch => branch.BranchCode)
            .ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return new DashboardSnapshotViewModel();
        }

        var selectedBranch = branches.FirstOrDefault(branch => branch.Id == branchId) ?? branches[0];
        var selectedYear = year ?? Today.Year;
        var selectedTerm = term is 1 or 2 ? term.Value : (Today.Month <= 6 ? 1 : 2);
        var instances = await db.MainProductInstances
            .AsNoTracking()
            .Include(instance => instance.MainProduct)
            .Include(instance => instance.Parameter)
            .Where(instance => instance.Year == selectedYear && instance.Term == selectedTerm)
            .OrderBy(instance => instance.MainProduct.Code)
            .ToListAsync(cancellationToken);
        var parameterIds = instances
            .Where(instance => instance.Parameter is not null)
            .Select(instance => instance.Parameter!.Id)
            .ToList();
        var metrics = parameterIds.Count == 0
            ? []
            : await db.BranchMainProductMonthlyMetrics
                .AsNoTracking()
                .Where(metric => parameterIds.Contains(metric.MainProductParameterId))
                .ToListAsync(cancellationToken);
        var metricLookup = metrics.ToDictionary(
            metric => (metric.BranchId, metric.MainProductParameterId, metric.Month));
        var termMonths = calculator.GetTermMonths(selectedTerm);
        var records = new List<CalculatedDashboardRecord>(branches.Count * Math.Max(1, instances.Count));

        foreach (var branch in branches)
        {
            foreach (var instance in instances)
            {
                var parameter = instance.Parameter;
                MainProductPeriodCalculationResult? result = null;
                if (parameter is not null)
                {
                    var monthValues = termMonths.Select(month =>
                    {
                        metricLookup.TryGetValue((branch.Id, parameter.Id, month), out var metric);
                        return new MainProductMonthlyValue(
                            month,
                            metric?.TargetValue ?? 0,
                            metric?.ActualValue,
                            metric?.ActualAsOfDate);
                    }).ToList();
                    result = calculator.Calculate(new MainProductPeriodCalculationInput(
                        instance.Year,
                        instance.Term,
                        Today,
                        parameter.CalculationType,
                        parameter.CriterionScore,
                        monthValues));
                }

                records.Add(new CalculatedDashboardRecord(branch, instance, parameter, result));
            }
        }

        var branchSummaries = branches
            .Select(branch => BuildBranchSummary(
                branch,
                records.Where(record => record.Branch.Id == branch.Id).ToList(),
                branch.Id == selectedBranch.Id))
            .ToList();
        ApplyBranchRanks(branchSummaries);

        var selectedRecords = records
            .Where(record => record.Branch.Id == selectedBranch.Id)
            .OrderBy(record => record.Instance.MainProduct.Code)
            .ToList();
        var productRanks = DenseRankCalculator.Calculate(selectedRecords
            .Select(record => IsRankable(record) ? record.Result!.TotalScore : null)
            .ToList());
        var products = selectedRecords.Select((record, index) => new DashboardProductPerformanceViewModel
        {
            ProductCode = record.Instance.MainProduct.Code,
            ProductName = record.Instance.MainProduct.Name,
            CalculationType = record.Parameter?.CalculationType,
            CriterionScore = record.Parameter?.CriterionScore,
            HgRatioPercent = record.Result?.HgRatioPercent,
            TotalScore = record.Result?.TotalScore,
            ProductRank = productRanks[index],
            SegmentRank = GetSegmentRank(record, records),
            IsActive = record.Parameter?.IsActive ?? false,
            HasParameter = record.Parameter is not null,
            HasCompleteBatchData = record.Result?.HasCompleteBatchData ?? false
        }).ToList();
        var selectedSummary = branchSummaries.First(summary => summary.BranchId == selectedBranch.Id);
        var ranking = branchSummaries
            .Where(summary => summary.Rank.HasValue)
            .OrderBy(summary => summary.Rank)
            .ThenBy(summary => summary.BranchCode)
            .Take(10)
            .ToList();
        if (selectedSummary.Rank.HasValue && ranking.All(summary => summary.BranchId != selectedBranch.Id))
        {
            ranking.Add(selectedSummary);
        }

        return new DashboardSnapshotViewModel
        {
            BranchId = selectedBranch.Id,
            BranchCode = selectedBranch.BranchCode,
            BranchName = selectedBranch.Name,
            GroupNo = selectedBranch.Group.GroupNo,
            GroupName = selectedBranch.Group.Name,
            GroupSegment = selectedBranch.Group.GroupSegment,
            Year = selectedYear,
            Term = selectedTerm,
            AssignedScore = selectedSummary.AssignedScore,
            EligibleScore = selectedSummary.EligibleScore,
            EarnedScore = selectedSummary.EarnedScore,
            SuccessPercent = selectedSummary.SuccessPercent,
            ActiveProductCount = selectedSummary.ActiveProductCount,
            CompleteProductCount = selectedSummary.CompleteProductCount,
            PendingProductCount = selectedSummary.PendingProductCount,
            BranchRank = selectedSummary.Rank,
            RankedBranchCount = branchSummaries.Count(summary => summary.Rank.HasValue),
            BranchRanking = ranking,
            Products = products,
            Months = BuildMonthlyTrend(selectedRecords, termMonths, metricLookup)
        };
    }

    private static DashboardBranchPerformanceViewModel BuildBranchSummary(
        Branch branch,
        IReadOnlyCollection<CalculatedDashboardRecord> records,
        bool isSelected)
    {
        var active = records.Where(record => record.Parameter?.IsActive == true).ToList();
        var complete = active.Where(IsRankable).ToList();
        var eligible = complete.Sum(record => record.Parameter!.CriterionScore);
        var earned = complete.Sum(record => record.Result!.TotalScore ?? 0);
        return new DashboardBranchPerformanceViewModel
        {
            BranchId = branch.Id,
            BranchCode = branch.BranchCode,
            BranchName = branch.Name,
            GroupSegment = branch.Group.GroupSegment,
            AssignedScore = Round(active.Sum(record => record.Parameter!.CriterionScore)),
            EarnedScore = Round(earned),
            EligibleScore = Round(eligible),
            SuccessPercent = eligible == 0 ? 0 : Round(earned / eligible * 100),
            ActiveProductCount = active.Count,
            CompleteProductCount = complete.Count,
            PendingProductCount = active.Count - complete.Count,
            IsSelected = isSelected
        };
    }

    private static void ApplyBranchRanks(IReadOnlyList<DashboardBranchPerformanceViewModel> summaries)
    {
        var ranks = DenseRankCalculator.Calculate(summaries
            .Select(summary => summary.EligibleScore > 0 ? summary.SuccessPercent : (decimal?)null)
            .ToList());
        for (var index = 0; index < summaries.Count; index++)
        {
            summaries[index].Rank = ranks[index];
        }
    }

    private static int? GetSegmentRank(
        CalculatedDashboardRecord selected,
        IReadOnlyCollection<CalculatedDashboardRecord> allRecords)
    {
        var candidates = allRecords
            .Where(record => record.Instance.Id == selected.Instance.Id
                && record.Branch.Group.GroupSegment == selected.Branch.Group.GroupSegment)
            .OrderBy(record => record.Branch.Id)
            .ToList();
        var ranks = DenseRankCalculator.Calculate(candidates
            .Select(record => IsRankable(record) ? record.Result!.TotalScore : null)
            .ToList());
        var index = candidates.FindIndex(record => record.Branch.Id == selected.Branch.Id);
        return index >= 0 ? ranks[index] : null;
    }

    private static bool IsRankable(CalculatedDashboardRecord record) =>
        record.Parameter?.IsActive == true
        && record.Result?.HasCompleteBatchData == true
        && record.Result.TotalScore.HasValue;

    private IReadOnlyList<DashboardMonthPerformanceViewModel> BuildMonthlyTrend(
        IReadOnlyCollection<CalculatedDashboardRecord> selectedRecords,
        IReadOnlyCollection<int> termMonths,
        IReadOnlyDictionary<(int BranchId, int ParameterId, int Month), BranchMainProductMonthlyMetric> metricLookup)
    {
        return termMonths.Select(month =>
        {
            var ratios = selectedRecords
                .Where(record => record.Parameter?.IsActive == true)
                .Select(record =>
                {
                    metricLookup.TryGetValue((record.Branch.Id, record.Parameter!.Id, month), out var metric);
                    if (metric?.ActualValue is null
                        || metric.ActualAsOfDate is null
                        || metric.ActualAsOfDate > Today
                        || metric.TargetValue <= 0)
                    {
                        return (decimal?)null;
                    }

                    return metric.ActualValue.Value / metric.TargetValue * 100;
                })
                .Where(ratio => ratio.HasValue)
                .Select(ratio => ratio!.Value)
                .ToList();
            return new DashboardMonthPerformanceViewModel
            {
                Month = month,
                MonthName = TurkishMonthNames[month],
                AverageHgPercent = ratios.Count == 0 ? null : Round(ratios.Average()),
                CompleteProductCount = ratios.Count
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<ParameterBranchOptionViewModel>> GetBranchOptionsAsync(CancellationToken cancellationToken)
    {
        return await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .OrderBy(branch => branch.BranchCode)
            .Select(branch => new ParameterBranchOptionViewModel
            {
                Id = branch.Id,
                BranchCode = branch.BranchCode,
                Name = branch.Name,
                GroupNo = branch.Group.GroupNo,
                GroupName = branch.Group.Name,
                GroupSegment = branch.Group.GroupSegment
            })
            .ToListAsync(cancellationToken);
    }

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record CalculatedDashboardRecord(
        Branch Branch,
        MainProductInstance Instance,
        MainProductParameter? Parameter,
        MainProductPeriodCalculationResult? Result);
}
