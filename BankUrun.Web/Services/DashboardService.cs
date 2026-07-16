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
        var groups = await db.GroupDefinitions.AsNoTracking()
            .Where(group => group.IsActive)
            .OrderBy(group => group.GroupNo)
            .Select(group => new ParameterGroupOptionViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name,
                GroupSegment = group.GroupSegment
            })
            .ToListAsync(cancellationToken);
        var branches = await db.Branches.AsNoTracking()
            .Include(branch => branch.Group)
            .Where(branch => branch.Group.IsActive)
            .OrderBy(branch => branch.BranchCode)
            .Select(branch => new ParameterBranchOptionViewModel
            {
                Id = branch.Id,
                GroupId = branch.GroupId,
                BranchCode = branch.BranchCode,
                Name = branch.Name,
                GroupNo = branch.Group.GroupNo,
                GroupName = branch.Group.Name,
                GroupSegment = branch.Group.GroupSegment
            })
            .ToListAsync(cancellationToken);
        var products = await db.MainProductInstances.AsNoTracking()
            .Include(instance => instance.MainProduct)
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .OrderByDescending(instance => instance.Year)
            .ThenByDescending(instance => instance.Term)
            .ThenBy(instance => instance.MainProduct.Code)
            .Select(instance => new ParameterProductOptionViewModel
            {
                Id = instance.Id,
                Year = instance.Year,
                Term = instance.Term,
                Code = instance.MainProduct.Code,
                Name = instance.MainProduct.Name
            })
            .ToListAsync(cancellationToken);
        var periods = products.Select(product => (product.Year, product.Term))
            .Distinct()
            .OrderByDescending(period => period.Year)
            .ThenByDescending(period => period.Term)
            .Select(period => new DashboardPeriodOptionViewModel { Year = period.Year, Term = period.Term })
            .ToList();
        var selectedPeriod = PickDefaultPeriod(periods);

        return new DashboardIndexViewModel
        {
            Groups = groups,
            Branches = branches,
            Products = products,
            Periods = periods,
            SelectedMode = PerformanceMode.BranchProduct,
            SelectedYear = selectedPeriod.Year,
            SelectedTerm = selectedPeriod.Term,
            BatchDate = Today,
            Snapshot = await GetSnapshotAsync(
                PerformanceMode.BranchProduct, null, null, selectedPeriod.Year, selectedPeriod.Term, null, cancellationToken)
        };
    }

    public async Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        PerformanceMode mode,
        int? groupId,
        int? branchId,
        int? year,
        int? term,
        int? mainProductInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(mode)) mode = PerformanceMode.BranchProduct;
        var periods = await GetPeriodsAsync(cancellationToken);
        var selectedPeriod = periods.FirstOrDefault(period => period.Year == year && period.Term == term)
            ?? PickDefaultPeriod(periods);
        var dataset = await BuildDatasetAsync(selectedPeriod.Year, selectedPeriod.Term, cancellationToken);
        var selectedGroup = groupId.HasValue
            ? dataset.Branches.Select(branch => branch.Group).DistinctBy(group => group.Id)
                .FirstOrDefault(group => group.Id == groupId.Value)
            : null;
        var selectedBranch = branchId.HasValue
            ? dataset.Branches.FirstOrDefault(branch => branch.Id == branchId.Value
                && (selectedGroup is null || branch.GroupId == selectedGroup.Id))
            : null;
        if (selectedBranch is not null) selectedGroup = selectedBranch.Group;

        var branchRows = BuildBranchRows(dataset.Branches, dataset.Records);
        var visibleBranchRows = branchRows
            .Where(row => selectedGroup is null || row.GroupId == selectedGroup.Id)
            .OrderBy(row => row.BranchCode)
            .ToList();
        ApplyBranchRanks(visibleBranchRows);
        var branchProductRows = dataset.Records
            .Where(record => selectedGroup is null || record.Branch.GroupId == selectedGroup.Id)
            .Where(record => selectedBranch is null || record.Branch.Id == selectedBranch.Id)
            .Where(record => !mainProductInstanceId.HasValue || record.Instance.Id == mainProductInstanceId.Value)
            .OrderBy(record => record.Branch.BranchCode)
            .ThenBy(record => record.Instance.MainProduct.Code)
            .Select(BuildBranchProduct)
            .ToList();
        ApplyBranchProductRanks(branchProductRows);
        var mainRows = BuildMainProductRows(
            dataset.Records.Where(record => selectedGroup is null || record.Branch.GroupId == selectedGroup.Id).ToList());
        if (mainProductInstanceId.HasValue)
        {
            mainRows = mainRows.Where(row => row.MainProductInstanceId == mainProductInstanceId.Value).ToList();
        }
        ApplyMainProductRanks(mainRows);
        var selectedSummary = selectedBranch is null
            ? null
            : visibleBranchRows.FirstOrDefault(row => row.BranchId == selectedBranch.Id);

        return new DashboardSnapshotViewModel
        {
            Mode = mode,
            HasSelectedBranch = selectedBranch is not null,
            GroupId = selectedGroup?.Id,
            BranchId = selectedBranch?.Id,
            BranchCode = selectedBranch?.BranchCode ?? string.Empty,
            BranchName = selectedBranch?.Name ?? string.Empty,
            GroupNo = selectedGroup?.GroupNo ?? string.Empty,
            GroupName = selectedGroup?.Name ?? "Tüm gruplar",
            Year = selectedPeriod.Year,
            Term = selectedPeriod.Term,
            AssignedScore = selectedSummary?.CriterionScore ?? 0,
            EarnedScore = selectedSummary?.TotalScore,
            SuccessPercent = selectedSummary?.SuccessPercent,
            HasCompletePeriodData = selectedSummary?.HasCompletePeriodData ?? false,
            BranchRank = selectedSummary?.Rank,
            RankedBranchCount = selectedSummary?.RankCandidateCount ?? 0,
            Branches = visibleBranchRows,
            BranchProducts = branchProductRows,
            MainProducts = mainRows
        };
    }

    public async Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthlyDetailAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default)
    {
        var instance = await db.MainProductInstances.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == mainProductInstanceId, cancellationToken);
        if (instance is null) return null;
        var dataset = await BuildDatasetAsync(instance.Year, instance.Term, cancellationToken);
        var record = dataset.Records.FirstOrDefault(item => item.Branch.Id == branchId && item.Instance.Id == mainProductInstanceId);
        if (record is null) return null;

        return new DashboardMonthlyDetailViewModel
        {
            Mode = PerformanceMode.BranchProduct,
            Title = $"{record.Instance.MainProduct.Code} · {record.Instance.MainProduct.Name}",
            Subtitle = $"{record.Branch.BranchCode} · {record.Branch.Name}",
            CalculationType = record.Parameter.CalculationType,
            Months = BuildMonthRows(record.Months),
            Contributions = BuildBranchContributions(record, dataset.MetricLookup)
        };
    }

    public async Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthlyDetailAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default)
    {
        var instance = await db.MainProductInstances.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == mainProductInstanceId, cancellationToken);
        if (instance is null) return null;
        var dataset = await BuildDatasetAsync(instance.Year, instance.Term, cancellationToken);
        var records = dataset.Records
            .Where(record => record.Instance.Id == mainProductInstanceId)
            .Where(record => !groupId.HasValue || record.Branch.GroupId == groupId.Value)
            .ToList();
        if (records.Count == 0) return null;
        var calculationType = records[0].Parameter.CalculationType;
        var months = calculator.GetTermMonths(instance.Term).Select(month =>
        {
            var values = records.Select(record => record.Months.First(value => value.Month == month)).ToList();
            var complete = values.All(value => value.ActualValue.HasValue && value.ActualAsOfDate.HasValue);
            return new MainProductMonthlyValue(
                month,
                Round(values.Sum(value => value.TargetValue)),
                complete ? Round(values.Sum(value => value.ActualValue!.Value)) : null,
                complete ? values.Max(value => value.ActualAsOfDate) : null);
        }).ToList();
        var first = records[0];
        return new DashboardMonthlyDetailViewModel
        {
            Mode = PerformanceMode.MainProduct,
            Title = $"{first.Instance.MainProduct.Code} · {first.Instance.MainProduct.Name}",
            Subtitle = groupId.HasValue
                ? $"{first.Branch.Group.GroupNo} · {first.Branch.Group.Name} genel toplamı"
                : $"Tüm gruplar · {records.Select(record => record.Branch.Id).Distinct().Count()} şube",
            CalculationType = calculationType,
            Months = BuildMonthRows(months),
            Contributions = BuildAggregateContributions(records, dataset.MetricLookup)
        };
    }

    private async Task<DashboardDataset> BuildDatasetAsync(int year, int term, CancellationToken cancellationToken)
    {
        var branches = await db.Branches.AsNoTracking().Include(branch => branch.Group)
            .Where(branch => branch.Group.IsActive)
            .OrderBy(branch => branch.BranchCode)
            .ToListAsync(cancellationToken);
        var instances = await db.MainProductInstances.AsNoTracking()
            .Include(instance => instance.MainProduct)
            .Include(instance => instance.SubProductInstances).ThenInclude(link => link.SubProduct)
            .Where(instance => instance.Year == year && instance.Term == term)
            .OrderBy(instance => instance.MainProduct.Code)
            .ToListAsync(cancellationToken);
        var instanceIds = instances.Select(instance => instance.Id).ToList();
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Where(parameter => instanceIds.Contains(parameter.MainProductInstanceId) && parameter.IsActive)
            .ToListAsync(cancellationToken);
        var subProductIds = instances.SelectMany(instance => instance.SubProductInstances)
            .Where(link => link.SubProduct.IsActive)
            .Select(link => link.SubProductId)
            .Distinct()
            .ToList();
        var metrics = subProductIds.Count == 0
            ? []
            : await db.BranchSubProductMonthlyMetrics.AsNoTracking()
                .Where(metric => metric.Year == year && metric.Term == term && subProductIds.Contains(metric.SubProductId))
                .ToListAsync(cancellationToken);
        var parameterLookup = parameters.ToDictionary(parameter => (parameter.GroupId, parameter.MainProductInstanceId));
        var metricLookup = metrics.ToDictionary(metric => (metric.BranchId, metric.SubProductId, metric.Month));
        var termMonths = calculator.GetTermMonths(term);
        var records = new List<CalculatedDashboardRecord>();

        foreach (var branch in branches)
        {
            foreach (var instance in instances)
            {
                if (!parameterLookup.TryGetValue((branch.GroupId, instance.Id), out var parameter)) continue;
                var subProducts = instance.SubProductInstances
                    .Where(link => link.SubProduct.IsActive)
                    .Select(link => link.SubProduct)
                    .DistinctBy(product => product.Id)
                    .OrderBy(product => product.Code)
                    .ToList();
                var months = termMonths.Select(month => AggregateMonth(
                    branch.Id, subProducts, month, metricLookup)).ToList();
                var result = calculator.Calculate(new MainProductPeriodCalculationInput(
                    year, term, Today, parameter.CalculationType, parameter.CriterionScore, months));
                records.Add(new CalculatedDashboardRecord(branch, instance, parameter, subProducts, result, months));
            }
        }

        return new DashboardDataset(branches, instances, records, metricLookup);
    }

    private static MainProductMonthlyValue AggregateMonth(
        int branchId,
        IReadOnlyCollection<ProductDefinition> subProducts,
        int month,
        IReadOnlyDictionary<(int BranchId, int SubProductId, int Month), BranchSubProductMonthlyMetric> metricLookup)
    {
        var metrics = subProducts.Select(product =>
        {
            metricLookup.TryGetValue((branchId, product.Id, month), out var metric);
            return metric;
        }).ToList();
        return SubProductMetricAggregator.AggregateMonth(month, metrics);
    }

    private static List<DashboardBranchPerformanceViewModel> BuildBranchRows(
        IReadOnlyCollection<Branch> branches,
        IReadOnlyCollection<CalculatedDashboardRecord> records) =>
        branches.Select(branch =>
        {
            var branchRecords = records.Where(record => record.Branch.Id == branch.Id).ToList();
            var complete = branchRecords.Count > 0 && branchRecords.All(IsRankable);
            var criterion = Round(branchRecords.Sum(record => record.Parameter.CriterionScore));
            var score = complete ? Round(branchRecords.Sum(record => record.Result.TotalScore!.Value)) : (decimal?)null;
            return new DashboardBranchPerformanceViewModel
            {
                GroupId = branch.GroupId,
                GroupNo = branch.Group.GroupNo,
                GroupName = branch.Group.Name,
                BranchId = branch.Id,
                BranchCode = branch.BranchCode,
                BranchName = branch.Name,
                CriterionScore = criterion,
                HgoScore = score,
                TotalScore = score,
                SuccessPercent = score.HasValue && criterion > 0 ? Round(score.Value / criterion * 100) : null,
                HasCompletePeriodData = complete,
                CompleteProductCount = branchRecords.Count(IsRankable),
                ProductCount = branchRecords.Count
            };
        }).ToList();

    private static void ApplyBranchRanks(IReadOnlyList<DashboardBranchPerformanceViewModel> rows)
    {
        var ranks = DenseRankCalculator.Calculate(rows.Select(row => row.TotalScore).ToList());
        var candidates = rows.Count(row => row.TotalScore.HasValue);
        for (var index = 0; index < rows.Count; index++)
        {
            rows[index].Rank = ranks[index];
            rows[index].RankCandidateCount = candidates;
        }
    }

    private static DashboardProductPerformanceViewModel BuildBranchProduct(CalculatedDashboardRecord record) => new()
    {
        GroupId = record.Branch.GroupId,
        GroupNo = record.Branch.Group.GroupNo,
        GroupName = record.Branch.Group.Name,
        BranchId = record.Branch.Id,
        BranchCode = record.Branch.BranchCode,
        BranchName = record.Branch.Name,
        MainProductInstanceId = record.Instance.Id,
        ProductCode = record.Instance.MainProduct.Code,
        ProductName = record.Instance.MainProduct.Name,
        CalculationType = record.Parameter.CalculationType,
        CriterionScore = record.Parameter.CriterionScore,
        TargetValue = record.Result.TargetValue,
        ActualValue = record.Result.ActualValue,
        HgRatioPercent = record.Result.HgRatioPercent,
        HgoScore = record.Result.HgoScore,
        TotalScore = record.Result.TotalScore,
        HasCompleteBatchData = record.Result.HasCompleteBatchData,
        HasSubProductConfiguration = record.SubProducts.Count > 0,
        SubProductCount = record.SubProducts.Count
    };

    private static List<DashboardMainProductPerformanceViewModel> BuildMainProductRows(
        IReadOnlyCollection<CalculatedDashboardRecord> records)
    {
        var rows = records.GroupBy(record => record.Instance.Id).Select(group =>
        {
            var groupRecords = group.ToList();
            var first = groupRecords[0];
            var configured = groupRecords.All(record => record.SubProducts.Count > 0);
            var complete = configured && groupRecords.All(IsRankable);
            var target = Round(groupRecords.Sum(record => record.Result.TargetValue));
            var actual = complete ? Round(groupRecords.Sum(record => record.Result.ActualValue!.Value)) : (decimal?)null;
            return new DashboardMainProductPerformanceViewModel
            {
                MainProductInstanceId = first.Instance.Id,
                ProductCode = first.Instance.MainProduct.Code,
                ProductName = first.Instance.MainProduct.Name,
                SubProductCount = first.SubProducts.Count,
                BranchCount = groupRecords.Select(record => record.Branch.Id).Distinct().Count(),
                CriterionScore = Round(groupRecords.Sum(record => record.Parameter.CriterionScore)),
                TargetValue = target,
                ActualValue = actual,
                HgRatioPercent = actual.HasValue && target > 0 ? Round(actual.Value / target * 100) : null,
                HgoScore = complete ? Round(groupRecords.Sum(record => record.Result.HgoScore!.Value)) : null,
                TotalScore = complete ? Round(groupRecords.Sum(record => record.Result.TotalScore!.Value)) : null,
                HasCompleteBatchData = complete,
                HasSubProductConfiguration = configured
            };
        }).OrderBy(row => row.ProductCode).ToList();
        return rows;
    }

    private static void ApplyBranchProductRanks(IReadOnlyList<DashboardProductPerformanceViewModel> rows)
    {
        var ranks = DenseRankCalculator.Calculate(rows.Select(row => row.TotalScore).ToList());
        var candidates = rows.Count(row => row.TotalScore.HasValue);
        for (var index = 0; index < rows.Count; index++)
        {
            rows[index].SegmentRank = ranks[index];
            rows[index].SegmentRankCandidateCount = candidates;
        }
    }

    private static void ApplyMainProductRanks(IReadOnlyList<DashboardMainProductPerformanceViewModel> rows)
    {
        var ranks = DenseRankCalculator.Calculate(rows.Select(row => row.TotalScore).ToList());
        var candidates = rows.Count(row => row.TotalScore.HasValue);
        for (var index = 0; index < rows.Count; index++)
        {
            rows[index].Rank = ranks[index];
            rows[index].RankCandidateCount = candidates;
        }
    }

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildBranchContributions(
        CalculatedDashboardRecord record,
        IReadOnlyDictionary<(int BranchId, int SubProductId, int Month), BranchSubProductMonthlyMetric> metricLookup) =>
        record.SubProducts.Select(product => BuildContribution(
            product,
            record.Parameter.CalculationType,
            calculator.GetTermMonths(record.Instance.Term)
                .Select(month => GetMetric(metricLookup, record.Branch.Id, product.Id, month)).ToList()))
            .ToList();

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildAggregateContributions(
        IReadOnlyCollection<CalculatedDashboardRecord> records,
        IReadOnlyDictionary<(int BranchId, int SubProductId, int Month), BranchSubProductMonthlyMetric> metricLookup)
    {
        var first = records.First();
        return first.SubProducts.Select(product =>
        {
            var branchContributions = records.Select(record => BuildContribution(
                product,
                record.Parameter.CalculationType,
                calculator.GetTermMonths(record.Instance.Term)
                    .Select(month => GetMetric(metricLookup, record.Branch.Id, product.Id, month)).ToList())).ToList();
            return new DashboardSubProductContributionViewModel
            {
                SubProductId = product.Id,
                Code = product.Code,
                Name = product.Name,
                TargetValue = Round(branchContributions.Sum(item => item.TargetValue)),
                ActualValue = branchContributions.All(item => item.ActualValue.HasValue)
                    ? Round(branchContributions.Sum(item => item.ActualValue!.Value))
                    : null
            };
        }).ToList();
    }

    private static BranchSubProductMonthlyMetric? GetMetric(
        IReadOnlyDictionary<(int BranchId, int SubProductId, int Month), BranchSubProductMonthlyMetric> lookup,
        int branchId, int subProductId, int month)
    {
        lookup.TryGetValue((branchId, subProductId, month), out var metric);
        return metric;
    }

    private static DashboardSubProductContributionViewModel BuildContribution(
        ProductDefinition product,
        MainProductCalculationType calculationType,
        IReadOnlyCollection<BranchSubProductMonthlyMetric?> metrics)
    {
        var targetValues = metrics.Select(metric => metric?.TargetValue ?? 0).ToList();
        var complete = metrics.Count > 0 && metrics.All(metric => metric?.ActualValue.HasValue == true);
        return new DashboardSubProductContributionViewModel
        {
            SubProductId = product.Id,
            Code = product.Code,
            Name = product.Name,
            TargetValue = Round(Aggregate(targetValues, calculationType)),
            ActualValue = complete
                ? Round(Aggregate(metrics.Select(metric => metric!.ActualValue!.Value), calculationType))
                : null
        };
    }

    private static IReadOnlyList<DashboardProductMonthViewModel> BuildMonthRows(
        IEnumerable<MainProductMonthlyValue> months) => months.Select(month => new DashboardProductMonthViewModel
        {
            Month = month.Month,
            MonthName = TurkishMonthNames[month.Month],
            TargetValue = month.TargetValue,
            ActualValue = month.ActualValue,
            ActualAsOfDate = month.ActualAsOfDate,
            HgRatioPercent = month.ActualValue.HasValue && month.TargetValue > 0
                ? Round(month.ActualValue.Value / month.TargetValue * 100)
                : null
        }).ToList();

    private async Task<IReadOnlyList<DashboardPeriodOptionViewModel>> GetPeriodsAsync(CancellationToken cancellationToken) =>
        (await db.MainProductInstances.AsNoTracking()
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .Select(instance => new { instance.Year, instance.Term })
            .Distinct()
            .ToListAsync(cancellationToken))
        .Select(period => new DashboardPeriodOptionViewModel { Year = period.Year, Term = period.Term })
        .ToList();

    private DashboardPeriodOptionViewModel PickDefaultPeriod(IReadOnlyCollection<DashboardPeriodOptionViewModel> periods) =>
        periods.Where(period => IsPeriodClosed(period.Year, period.Term))
            .OrderByDescending(period => period.Year).ThenByDescending(period => period.Term).FirstOrDefault()
        ?? periods.OrderByDescending(period => period.Year).ThenByDescending(period => period.Term).FirstOrDefault()
        ?? new DashboardPeriodOptionViewModel { Year = Today.Year, Term = Today.Month <= 6 ? 1 : 2 };

    private bool IsPeriodClosed(int year, int term)
    {
        var lastMonth = calculator.GetTermMonths(term)[^1];
        return Today > new DateOnly(year, lastMonth, DateTime.DaysInMonth(year, lastMonth));
    }

    private static bool IsRankable(CalculatedDashboardRecord record) =>
        record.SubProducts.Count > 0 && record.Result.HasCompleteBatchData && record.Result.TotalScore.HasValue;
    private static decimal Aggregate(IEnumerable<decimal> values, MainProductCalculationType type)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : type == MainProductCalculationType.Average ? list.Average() : list.Sum();
    }
    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record CalculatedDashboardRecord(
        Branch Branch,
        MainProductInstance Instance,
        MainProductParameter Parameter,
        IReadOnlyList<ProductDefinition> SubProducts,
        MainProductPeriodCalculationResult Result,
        IReadOnlyList<MainProductMonthlyValue> Months);
    private sealed record DashboardDataset(
        IReadOnlyList<Branch> Branches,
        IReadOnlyList<MainProductInstance> Instances,
        IReadOnlyList<CalculatedDashboardRecord> Records,
        IReadOnlyDictionary<(int BranchId, int SubProductId, int Month), BranchSubProductMonthlyMetric> MetricLookup);
}
