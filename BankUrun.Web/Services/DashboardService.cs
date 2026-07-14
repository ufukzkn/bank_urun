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
        var groups = await db.GroupDefinitions.AsNoTracking().Where(group => group.IsActive)
            .OrderBy(group => group.GroupNo)
            .Select(group => new ParameterGroupOptionViewModel { Id = group.Id, GroupNo = group.GroupNo, Name = group.Name, GroupSegment = group.GroupSegment })
            .ToListAsync(cancellationToken);
        var branches = await GetBranchOptionsAsync(cancellationToken);
        var products = await db.MainProductInstances.AsNoTracking().Include(instance => instance.MainProduct)
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .OrderByDescending(instance => instance.Year).ThenByDescending(instance => instance.Term).ThenBy(instance => instance.MainProduct.Code)
            .Select(instance => new ParameterProductOptionViewModel { Id = instance.Id, Year = instance.Year, Term = instance.Term, Code = instance.MainProduct.Code, Name = instance.MainProduct.Name })
            .ToListAsync(cancellationToken);
        var years = products.Select(product => product.Year).Distinct().OrderByDescending(year => year).ToList();
        var year = years.Contains(Today.Year) ? Today.Year : years.FirstOrDefault(Today.Year);
        var term = products.Any(product => product.Year == year && product.Term == (Today.Month <= 6 ? 1 : 2))
            ? (Today.Month <= 6 ? 1 : 2)
            : products.Where(product => product.Year == year).Select(product => product.Term).DefaultIfEmpty(1).Max();
        var groupId = groups.FirstOrDefault()?.Id ?? 0;
        var branchId = branches.FirstOrDefault(branch => branch.GroupId == groupId)?.Id ?? 0;
        return new DashboardIndexViewModel
        {
            Groups = groups,
            Branches = branches,
            Products = products,
            Years = years,
            SelectedGroupId = groupId,
            SelectedBranchId = branchId,
            SelectedYear = year,
            SelectedTerm = term,
            BatchDate = Today,
            Snapshot = await GetSnapshotAsync(groupId, branchId, year, term, null, cancellationToken)
        };
    }

    public async Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        int? groupId,
        int? branchId,
        int? year,
        int? term,
        int? mainProductInstanceId,
        CancellationToken cancellationToken = default)
    {
        var allBranches = await db.Branches.AsNoTracking().Include(branch => branch.Group)
            .OrderBy(branch => branch.BranchCode).ToListAsync(cancellationToken);
        if (allBranches.Count == 0) return new DashboardSnapshotViewModel();
        var selectedGroup = allBranches.Select(branch => branch.Group).DistinctBy(group => group.Id)
            .FirstOrDefault(group => group.Id == groupId) ?? allBranches[0].Group;
        var groupBranches = allBranches.Where(branch => branch.GroupId == selectedGroup.Id).ToList();
        var selectedBranch = groupBranches.FirstOrDefault(branch => branch.Id == branchId) ?? groupBranches.FirstOrDefault();
        if (selectedBranch is null) return new DashboardSnapshotViewModel { GroupId = selectedGroup.Id };

        var selectedYear = year ?? Today.Year;
        var selectedTerm = term is 1 or 2 ? term.Value : (Today.Month <= 6 ? 1 : 2);
        var instanceQuery = db.MainProductInstances.AsNoTracking().Include(instance => instance.MainProduct)
            .Where(instance => instance.Year == selectedYear && instance.Term == selectedTerm);
        var instances = await instanceQuery.OrderBy(instance => instance.MainProduct.Code).ToListAsync(cancellationToken);
        var instanceIds = instances.Select(instance => instance.Id).ToList();
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Where(parameter => instanceIds.Contains(parameter.MainProductInstanceId) && parameter.IsActive)
            .ToListAsync(cancellationToken);
        var parameterIds = parameters.Select(parameter => parameter.Id).ToList();
        var metrics = parameterIds.Count == 0 ? [] : await db.BranchMainProductMonthlyMetrics.AsNoTracking()
            .Where(metric => parameterIds.Contains(metric.MainProductParameterId))
            .ToListAsync(cancellationToken);
        var parameterLookup = parameters.ToDictionary(parameter => (parameter.GroupId, parameter.MainProductInstanceId));
        var metricLookup = metrics.ToDictionary(metric => (metric.BranchId, metric.MainProductParameterId, metric.Month));
        var termMonths = calculator.GetTermMonths(selectedTerm);
        var records = new List<CalculatedDashboardRecord>(allBranches.Count * Math.Max(1, instances.Count));

        foreach (var branch in allBranches)
        {
            foreach (var instance in instances)
            {
                parameterLookup.TryGetValue((branch.GroupId, instance.Id), out var parameter);
                if (parameter is null) continue;
                var months = termMonths.Select(month =>
                {
                    metricLookup.TryGetValue((branch.Id, parameter.Id, month), out var metric);
                    return new MainProductMonthlyValue(month, metric?.TargetValue ?? 0, metric?.ActualValue, metric?.ActualAsOfDate);
                }).ToList();
                var result = calculator.Calculate(new MainProductPeriodCalculationInput(
                    instance.Year, instance.Term, Today, parameter.CalculationType, parameter.CriterionScore, months));
                records.Add(new CalculatedDashboardRecord(branch, instance, parameter, result, months));
            }
        }

        var branchSummaries = groupBranches.Select(branch => BuildBranchSummary(
            branch,
            records.Where(record => record.Branch.Id == branch.Id).ToList(),
            branch.Id == selectedBranch.Id)).ToList();
        ApplyBranchRanks(branchSummaries);
        var selectedRecords = records.Where(record => record.Branch.Id == selectedBranch.Id)
            .OrderBy(record => record.Instance.MainProduct.Code).ToList();
        var segmentCandidates = selectedRecords.Select(record => new SegmentRankCandidate(
            record.Instance.Id,
            IsRankable(record) ? record.Result.TotalScore : null,
            IsRankable(record) ? record.Result.HgRatioPercent : null)).ToList();
        var products = selectedRecords.Select(record =>
        {
            var segmentRank = SegmentRankCalculator.Calculate(record.Instance.Id, segmentCandidates);
            return BuildProduct(record, segmentRank.Rank, segmentRank.CandidateCount);
        }).ToList();
        if (mainProductInstanceId.HasValue)
        {
            products = products.Where(product => product.MainProductInstanceId == mainProductInstanceId.Value).ToList();
        }
        var selectedSummary = branchSummaries.First(summary => summary.BranchId == selectedBranch.Id);
        var ranking = branchSummaries.Where(summary => summary.Rank.HasValue)
            .OrderBy(summary => summary.Rank).ThenBy(summary => summary.BranchCode).Take(12).ToList();
        if (selectedSummary.Rank.HasValue && ranking.All(summary => summary.BranchId != selectedBranch.Id)) ranking.Add(selectedSummary);

        return new DashboardSnapshotViewModel
        {
            GroupId = selectedGroup.Id,
            BranchId = selectedBranch.Id,
            BranchCode = selectedBranch.BranchCode,
            BranchName = selectedBranch.Name,
            GroupNo = selectedGroup.GroupNo,
            GroupName = selectedGroup.Name,
            GroupSegment = selectedGroup.GroupSegment,
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
            Products = products
        };
    }

    private DashboardProductPerformanceViewModel BuildProduct(
        CalculatedDashboardRecord record,
        int? segmentRank,
        int segmentRankCandidateCount)
    {
        var months = record.Months.Select(month => new DashboardProductMonthViewModel
        {
            Month = month.Month,
            MonthName = TurkishMonthNames[month.Month],
            TargetValue = month.TargetValue,
            ActualValue = month.ActualValue,
            ActualAsOfDate = month.ActualAsOfDate,
            HgRatioPercent = month.ActualValue.HasValue && month.TargetValue > 0 ? Round(month.ActualValue.Value / month.TargetValue * 100) : null
        }).ToList();
        return new DashboardProductPerformanceViewModel
        {
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
            SegmentRank = segmentRank,
            SegmentRankCandidateCount = segmentRankCandidateCount,
            HasCompleteBatchData = record.Result.HasCompleteBatchData,
            Months = months
        };
    }

    private static DashboardBranchPerformanceViewModel BuildBranchSummary(Branch branch, IReadOnlyCollection<CalculatedDashboardRecord> records, bool selected)
    {
        var complete = records.Where(IsRankable).ToList();
        var eligible = complete.Sum(record => record.Parameter.CriterionScore);
        var earned = complete.Sum(record => record.Result.TotalScore ?? 0);
        return new DashboardBranchPerformanceViewModel
        {
            BranchId = branch.Id,
            BranchCode = branch.BranchCode,
            BranchName = branch.Name,
            GroupSegment = branch.Group.GroupSegment,
            AssignedScore = Round(records.Sum(record => record.Parameter.CriterionScore)),
            EligibleScore = Round(eligible),
            EarnedScore = Round(earned),
            SuccessPercent = eligible == 0 ? 0 : Round(earned / eligible * 100),
            ActiveProductCount = records.Count,
            CompleteProductCount = complete.Count,
            PendingProductCount = records.Count - complete.Count,
            IsSelected = selected
        };
    }

    private static void ApplyBranchRanks(IReadOnlyList<DashboardBranchPerformanceViewModel> rows)
    {
        var ranks = DenseRankCalculator.Calculate(rows.Select(row => row.EligibleScore > 0 ? (decimal?)row.SuccessPercent : null).ToList());
        for (var index = 0; index < rows.Count; index++) rows[index].Rank = ranks[index];
    }

    private async Task<IReadOnlyList<ParameterBranchOptionViewModel>> GetBranchOptionsAsync(CancellationToken cancellationToken) =>
        await db.Branches.AsNoTracking().Include(branch => branch.Group).OrderBy(branch => branch.BranchCode)
            .Select(branch => new ParameterBranchOptionViewModel
            {
                Id = branch.Id, GroupId = branch.GroupId, BranchCode = branch.BranchCode, Name = branch.Name,
                GroupNo = branch.Group.GroupNo, GroupName = branch.Group.Name, GroupSegment = branch.Group.GroupSegment
            }).ToListAsync(cancellationToken);

    private static bool IsRankable(CalculatedDashboardRecord record) => record.Result.HasCompleteBatchData && record.Result.TotalScore.HasValue;
    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private sealed record CalculatedDashboardRecord(Branch Branch, MainProductInstance Instance, MainProductParameter Parameter, MainProductPeriodCalculationResult Result, IReadOnlyList<MainProductMonthlyValue> Months);
}
