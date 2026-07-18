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
                GroupType = group.GroupType
            })
            .ToListAsync(cancellationToken);
        var branches = await db.Branches.AsNoTracking()
            .Where(branch => branch.Group.IsActive)
            .OrderBy(branch => branch.BranchCode)
            .Select(branch => new DashboardBranchOptionViewModel
            {
                Id = branch.Id,
                GroupId = branch.GroupId,
                BranchCode = branch.BranchCode,
                Name = branch.Name,
                GroupNo = branch.Group.GroupNo,
                GroupName = branch.Group.Name,
                GroupType = branch.Group.GroupType
            })
            .ToListAsync(cancellationToken);
        var productDefinitions = await db.ProductDefinitions.AsNoTracking()
            .Where(product => product.Type == ProductType.Main && product.IsActive)
            .OrderBy(product => product.Code)
            .Select(product => new DashboardProductOptionViewModel
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name
            })
            .ToListAsync(cancellationToken);
        var periods = await GetPeriodsAsync(cancellationToken);
        var defaultPeriod = PickDefaultPeriod(periods);
        var gamuts = await db.ProductGamuts.AsNoTracking()
            .Where(gamut => gamut.IsActive && gamut.Group.IsActive)
            .OrderBy(gamut => gamut.Group.GroupNo).ThenBy(gamut => gamut.Code)
            .Select(gamut => new DashboardProductGamutOptionViewModel
            {
                Id = gamut.Id,
                GroupId = gamut.GroupId,
                Code = gamut.Code,
                Name = gamut.Name
            }).ToListAsync(cancellationToken);
        var types = await db.PortfolioTypes.AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.Code)
            .Select(type => new DashboardPortfolioTypeOptionViewModel
            {
                Id = type.Id,
                Code = type.Code,
                Name = type.Name
            }).ToListAsync(cancellationToken);

        return new DashboardIndexViewModel
        {
            Groups = groups,
            Branches = branches,
            Products = productDefinitions,
            ProductGamuts = gamuts,
            PortfolioTypes = types,
            Periods = periods,
            SelectedMode = PerformanceMode.BranchProduct,
            SelectedYear = defaultPeriod.Year,
            SelectedTerm = defaultPeriod.Term,
            BatchDate = Today,
            Snapshot = await GetSnapshotAsync(
                PerformanceMode.BranchProduct, null, null, defaultPeriod.Year, defaultPeriod.Term,
                null, null, null, cancellationToken)
        };
    }

    public async Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        PerformanceMode mode,
        int? groupId,
        int? branchId,
        int? year,
        int? term,
        int? mainProductId,
        int? productGamutId,
        int? portfolioTypeId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(mode)) mode = PerformanceMode.BranchProduct;
        if (mode is not (PerformanceMode.BranchProduct or PerformanceMode.Portfolio)) branchId = null;
        if (mode is not (PerformanceMode.BranchProduct or PerformanceMode.MainProduct)) mainProductId = null;
        if (mode != PerformanceMode.Portfolio)
        {
            productGamutId = null;
            portfolioTypeId = null;
        }
        var availablePeriods = await GetPeriodsAsync(cancellationToken);
        var periodScope = DashboardPeriodScopeSelector.Select(availablePeriods, year, term);

        var datasets = new List<PeriodDataset>();
        foreach (var period in periodScope)
        {
            datasets.Add(await BuildPeriodDatasetAsync(period.Year, period.Term, cancellationToken));
        }

        var selectedBranch = datasets.SelectMany(dataset => dataset.Branches)
            .DistinctBy(branch => branch.Id)
            .FirstOrDefault(branch => branch.Id == branchId
                && (!groupId.HasValue || branch.GroupId == groupId.Value));
        if (selectedBranch is not null) groupId = selectedBranch.GroupId;

        var allPortfolioRecords = datasets.SelectMany(dataset => dataset.Records).ToList();
        var branchProductRecords = BuildBranchProductRecords(allPortfolioRecords);
        var allBranchRows = BuildBranchRows(
            datasets.SelectMany(dataset => dataset.Branches.Select(branch => new PeriodBranch(dataset.Year, dataset.Term, branch))).ToList(),
            branchProductRecords);
        ApplyPartitionedRanks(allBranchRows,
            row => (row.GroupId, row.Year, row.Term), row => row.TotalScore,
            (row, rank, count) => { row.Rank = rank; row.RankCandidateCount = count; });

        var allBranchProductRows = branchProductRecords.Select(BuildBranchProductRow).ToList();
        ApplyPartitionedRanks(allBranchProductRows,
            row => (row.GroupId, row.Year, row.Term, row.MainProductId), row => row.TotalScore,
            (row, rank, count) => { row.SegmentRank = rank; row.SegmentRankCandidateCount = count; });

        var mainPool = branchProductRecords
            .Where(record => !groupId.HasValue || record.Branch.GroupId == groupId.Value)
            .ToList();
        var allMainRows = BuildMainProductRows(mainPool);
        ApplyPartitionedRanks(allMainRows,
            row => (row.Year, row.Term), row => row.TotalScore,
            (row, rank, count) => { row.Rank = rank; row.RankCandidateCount = count; });

        var allPortfolioRows = BuildPortfolioRows(datasets);
        ApplyPartitionedRanks(allPortfolioRows,
            row => (row.GroupId, row.ProductGamutId, row.Year, row.Term), row => row.TotalScore,
            (row, rank, count) => { row.OfficialRank = rank; row.OfficialRankCandidateCount = count; });
        ApplyPartitionedRanks(allPortfolioRows,
            row => (row.BranchId, row.Year, row.Term), row => row.SuccessPercent,
            (row, rank, count) => { row.BranchRank = rank; row.BranchRankCandidateCount = count; });

        var visibleBranches = allBranchRows
            .Where(row => !groupId.HasValue || row.GroupId == groupId.Value)
            .OrderByDescending(row => row.Year).ThenByDescending(row => row.Term).ThenBy(row => row.BranchCode)
            .ToList();
        var visibleBranchProducts = allBranchProductRows
            .Where(row => !groupId.HasValue || row.GroupId == groupId.Value)
            .Where(row => selectedBranch is null || row.BranchId == selectedBranch.Id)
            .Where(row => !mainProductId.HasValue || row.MainProductId == mainProductId.Value)
            .OrderByDescending(row => row.Year).ThenByDescending(row => row.Term)
            .ThenBy(row => row.BranchCode).ThenBy(row => row.ProductCode)
            .ToList();
        var visibleMainProducts = allMainRows
            .Where(row => !mainProductId.HasValue || row.MainProductId == mainProductId.Value)
            .OrderByDescending(row => row.Year).ThenByDescending(row => row.Term).ThenBy(row => row.ProductCode)
            .ToList();
        var visiblePortfolios = allPortfolioRows
            .Where(row => !groupId.HasValue || row.GroupId == groupId.Value)
            .Where(row => selectedBranch is null || row.BranchId == selectedBranch.Id)
            .Where(row => !productGamutId.HasValue || row.ProductGamutId == productGamutId.Value)
            .Where(row => !portfolioTypeId.HasValue || row.PortfolioTypeId == portfolioTypeId.Value)
            .OrderByDescending(row => row.Year).ThenByDescending(row => row.Term)
            .ThenBy(row => row.BranchCode).ThenBy(row => row.PortfolioCode)
            .ToList();

        var summary = selectedBranch is not null && year.HasValue && term.HasValue
            ? allBranchRows.FirstOrDefault(row => row.BranchId == selectedBranch.Id && row.Year == year && row.Term == term)
            : null;
        var selectedGroup = selectedBranch?.Group
            ?? datasets.SelectMany(dataset => dataset.Branches).Select(branch => branch.Group)
                .DistinctBy(group => group.Id).FirstOrDefault(group => group.Id == groupId);

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
            Year = year,
            Term = term,
            AssignedScore = summary?.CriterionScore ?? 0,
            EarnedScore = summary?.TotalScore,
            SuccessPercent = summary?.SuccessPercent,
            HasCompletePeriodData = summary?.HasCompletePeriodData ?? false,
            BranchRank = summary?.Rank,
            RankedBranchCount = summary?.RankCandidateCount ?? 0,
            Branches = visibleBranches,
            BranchProducts = visibleBranchProducts,
            MainProducts = visibleMainProducts,
            Portfolios = visiblePortfolios
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
        var dataset = await BuildPeriodDatasetAsync(instance.Year, instance.Term, cancellationToken);
        var record = BuildBranchProductRecords(dataset.Records)
            .FirstOrDefault(item => item.Branch.Id == branchId && item.Instance.Id == mainProductInstanceId);
        if (record is null) return null;

        return new DashboardMonthlyDetailViewModel
        {
            Mode = PerformanceMode.BranchProduct,
            Title = $"{record.Instance.MainProduct.Code} · {record.Instance.MainProduct.Name}",
            Subtitle = $"{record.Branch.BranchCode} · {record.Branch.Name} · {record.PortfolioCount} portföy",
            CalculationType = record.Parameter.CalculationType,
            Months = BuildMonthRows(record.Months),
            Contributions = BuildBranchContributions(record)
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
        var dataset = await BuildPeriodDatasetAsync(instance.Year, instance.Term, cancellationToken);
        var records = BuildBranchProductRecords(dataset.Records)
            .Where(record => record.Instance.Id == mainProductInstanceId)
            .Where(record => !groupId.HasValue || record.Branch.GroupId == groupId.Value)
            .ToList();
        if (records.Count == 0) return null;
        var months = AggregateMonths(records.Select(record => record.Months).ToList(), instance.Term);
        var first = records[0];
        return new DashboardMonthlyDetailViewModel
        {
            Mode = PerformanceMode.MainProduct,
            Title = $"{instance.MainProduct.Code} · {instance.MainProduct.Name}",
            Subtitle = groupId.HasValue
                ? $"{first.Branch.Group.GroupNo} · {first.Branch.Group.Name} genel toplamı"
                : $"Tüm gruplar · {records.Select(record => record.Branch.Id).Distinct().Count()} şube",
            CalculationType = first.Parameter.CalculationType,
            Months = BuildMonthRows(months),
            Contributions = BuildAggregateContributions(records)
        };
    }

    public async Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default)
    {
        var dataset = await BuildPeriodDatasetAsync(year, term, cancellationToken);
        var records = dataset.Records.Where(record => record.Portfolio.Id == portfolioId).ToList();
        if (records.Count == 0) return null;
        var first = records[0];
        return new DashboardPortfolioDetailViewModel
        {
            PortfolioId = portfolioId,
            Year = year,
            Term = term,
            Title = $"{first.Portfolio.Code} · {first.Portfolio.Name}",
            Subtitle = $"{first.Branch.BranchCode} · {first.Branch.Name} · {first.Gamut.Code} ürün gamı",
            Products = records.OrderBy(record => record.Instance.MainProduct.Code).Select(record => new DashboardPortfolioProductDetailViewModel
            {
                MainProductInstanceId = record.Instance.Id,
                ProductCode = record.Instance.MainProduct.Code,
                ProductName = record.Instance.MainProduct.Name,
                CalculationType = record.Parameter.CalculationType,
                HasParameterConfiguration = record.HasParameterConfiguration,
                HasCompleteTargetData = record.HasCompleteTargetData,
                CriterionScore = record.Parameter.CriterionScore,
                TargetValue = record.Result.TargetValue,
                ActualValue = IsRankable(record) ? record.Result.ActualValue : null,
                HgRatioPercent = IsRankable(record) ? record.Result.HgRatioPercent : null,
                TotalScore = IsRankable(record) ? record.Result.TotalScore : null,
                Months = BuildMonthRows(record.Months),
                Contributions = BuildPortfolioContributions(record)
            }).ToList()
        };
    }

    private async Task<PeriodDataset> BuildPeriodDatasetAsync(int year, int term, CancellationToken cancellationToken)
    {
        var branches = await db.Branches.AsNoTracking().Include(branch => branch.Group)
            .Where(branch => branch.Group.IsActive).OrderBy(branch => branch.BranchCode)
            .ToListAsync(cancellationToken);
        var portfolios = await db.Portfolios.AsNoTracking()
            .Include(portfolio => portfolio.Branch).ThenInclude(branch => branch.Group)
            .Include(portfolio => portfolio.ProductGamut)
            .Include(portfolio => portfolio.PortfolioType)
            .Where(portfolio => portfolio.IsActive && portfolio.Branch.Group.IsActive
                && portfolio.ProductGamut.IsActive && portfolio.PortfolioType.IsActive)
            .ToListAsync(cancellationToken);
        var assignments = await db.ProductGamutMainProductAssignments.AsNoTracking()
            .Where(assignment => year * 10 + term >= assignment.EffectiveFromYear * 10 + assignment.EffectiveFromTerm
                && (!assignment.EffectiveToYear.HasValue || !assignment.EffectiveToTerm.HasValue
                    || year * 10 + term <= assignment.EffectiveToYear.Value * 10 + assignment.EffectiveToTerm.Value))
            .ToListAsync(cancellationToken);
        var exclusions = await db.BranchMainProductExclusions.AsNoTracking()
            .Where(exclusion => year * 10 + term >= exclusion.EffectiveFromYear * 10 + exclusion.EffectiveFromTerm
                && (!exclusion.EffectiveToYear.HasValue || !exclusion.EffectiveToTerm.HasValue
                    || year * 10 + term <= exclusion.EffectiveToYear.Value * 10 + exclusion.EffectiveToTerm.Value))
            .ToListAsync(cancellationToken);
        var instances = await db.MainProductInstances.AsNoTracking()
            .Include(instance => instance.MainProduct)
            .Include(instance => instance.SubProductInstances).ThenInclude(link => link.SubProduct)
            .Where(instance => instance.Year == year && instance.Term == term && instance.MainProduct.IsActive)
            .ToListAsync(cancellationToken);
        var instanceIds = instances.Select(instance => instance.Id).ToList();
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Where(parameter => parameter.IsActive && instanceIds.Contains(parameter.MainProductInstanceId))
            .ToListAsync(cancellationToken);
        var parameterIds = parameters.Select(parameter => parameter.Id).ToList();
        var portfolioIds = portfolios.Select(portfolio => portfolio.Id).ToList();
        var targets = parameterIds.Count == 0 || portfolioIds.Count == 0 ? [] :
            await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
                .Where(target => portfolioIds.Contains(target.PortfolioId)
                    && parameterIds.Contains(target.MainProductParameterId))
                .ToListAsync(cancellationToken);
        var subProductIds = instances.SelectMany(instance => instance.SubProductInstances)
            .Select(link => link.SubProductId).Distinct().ToList();
        var metrics = portfolioIds.Count == 0 || subProductIds.Count == 0 ? [] :
            await db.PortfolioSubProductMonthlyMetrics.AsNoTracking()
                .Where(metric => metric.Year == year && metric.Term == term
                    && portfolioIds.Contains(metric.PortfolioId)
                    && subProductIds.Contains(metric.SubProductId))
                .ToListAsync(cancellationToken);

        var assignmentLookup = assignments.GroupBy(item => item.ProductGamutId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.MainProductId).Distinct().ToList());
        var excluded = exclusions.Select(item => (item.BranchId, item.MainProductId)).ToHashSet();
        var instanceLookup = instances.ToDictionary(instance => instance.MainProductId);
        var parameterLookup = parameters.ToDictionary(parameter => (parameter.GroupId, parameter.MainProductInstanceId));
        var targetLookup = targets.ToDictionary(target => (target.PortfolioId, target.MainProductParameterId, target.Month));
        var metricLookup = metrics.ToDictionary(metric => (metric.PortfolioId, metric.SubProductId, metric.Month));
        var records = new List<PortfolioProductRecord>();

        foreach (var portfolio in portfolios)
        {
            if (!assignmentLookup.TryGetValue(portfolio.ProductGamutId, out var productIds)) continue;
            foreach (var mainProductId in productIds)
            {
                if (excluded.Contains((portfolio.BranchId, mainProductId))
                    || !instanceLookup.TryGetValue(mainProductId, out var instance)) continue;
                var hasParameter = parameterLookup.TryGetValue((portfolio.GroupId, instance.Id), out var parameter);
                parameter ??= new MainProductParameter
                {
                    GroupId = portfolio.GroupId,
                    MainProductInstanceId = instance.Id,
                    CalculationType = MainProductCalculationType.Cumulative,
                    CriterionScore = 0
                };
                var subProducts = instance.SubProductInstances
                    .Where(link => link.SubProduct.IsActive)
                    .DistinctBy(link => link.SubProductId).OrderBy(link => link.SubProduct.Code).ToList();
                var termMonths = calculator.GetTermMonths(term);
                var hasTargets = hasParameter
                    && termMonths.All(month => targetLookup.ContainsKey((portfolio.Id, parameter.Id, month)));
                var months = termMonths.Select(month =>
                {
                    targetLookup.TryGetValue((portfolio.Id, parameter.Id, month), out var target);
                    var actualMetrics = subProducts.Select(link =>
                    {
                        metricLookup.TryGetValue((portfolio.Id, link.SubProductId, month), out var metric);
                        return metric;
                    }).ToList();
                    var actual = PortfolioActualAggregator.Aggregate(actualMetrics.Select(metric => metric is null
                        ? null
                        : new PortfolioSubProductActualValue(metric.ActualValue, metric.ActualAsOfDate)).ToList());
                    return new MainProductMonthlyValue(
                        month,
                        target?.TargetValue ?? 0,
                        actual.ActualValue,
                        actual.ActualAsOfDate);
                }).ToList();
                var result = calculator.Calculate(new MainProductPeriodCalculationInput(
                    year, term, Today, parameter.CalculationType, parameter.CriterionScore, months));
                records.Add(new PortfolioProductRecord(
                    portfolio, portfolio.Branch, portfolio.ProductGamut, portfolio.PortfolioType,
                    instance, parameter, subProducts, hasParameter, hasTargets, result, months, metricLookup));
            }
        }

        return new PeriodDataset(year, term, branches, portfolios, records);
    }

    private List<BranchProductRecord> BuildBranchProductRecords(IReadOnlyCollection<PortfolioProductRecord> records) =>
        records.GroupBy(record => new { BranchId = record.Branch.Id, InstanceId = record.Instance.Id }).Select(group =>
        {
            var items = group.ToList();
            var first = items[0];
            var months = AggregateMonths(items.Select(item => item.Months).ToList(), first.Instance.Term);
            var hasParameter = items.All(item => item.HasParameterConfiguration);
            var hasTargets = hasParameter && items.All(item => item.HasCompleteTargetData);
            var hasSubProducts = items.All(item => item.SubProducts.Count > 0);
            var result = calculator.Calculate(new MainProductPeriodCalculationInput(
                first.Instance.Year, first.Instance.Term, Today, first.Parameter.CalculationType,
                first.Parameter.CriterionScore, months));
            return new BranchProductRecord(first.Branch, first.Instance, first.Parameter, items,
                hasParameter, hasTargets, hasSubProducts, result, months);
        }).ToList();

    private IReadOnlyList<MainProductMonthlyValue> AggregateMonths(
        IReadOnlyCollection<IReadOnlyList<MainProductMonthlyValue>> recordMonths,
        int term) => PortfolioMonthlyRollup.Aggregate(calculator.GetTermMonths(term), recordMonths);

    private static List<DashboardBranchPerformanceViewModel> BuildBranchRows(
        IReadOnlyCollection<PeriodBranch> periodBranches,
        IReadOnlyCollection<BranchProductRecord> records) => periodBranches.Select(item =>
        {
            var branchRecords = records.Where(record => record.Branch.Id == item.Branch.Id
                && record.Instance.Year == item.Year && record.Instance.Term == item.Term).ToList();
            var complete = branchRecords.Count > 0 && branchRecords.All(IsRankable);
            var criterion = Round(branchRecords.Sum(record => record.Parameter.CriterionScore));
            var score = complete ? Round(branchRecords.Sum(record => record.Result.TotalScore!.Value)) : (decimal?)null;
            return new DashboardBranchPerformanceViewModel
            {
                Year = item.Year,
                Term = item.Term,
                GroupId = item.Branch.GroupId,
                GroupNo = item.Branch.Group.GroupNo,
                GroupName = item.Branch.Group.Name,
                BranchId = item.Branch.Id,
                BranchCode = item.Branch.BranchCode,
                BranchName = item.Branch.Name,
                CriterionScore = criterion,
                HgoScore = score,
                TotalScore = score,
                SuccessPercent = score.HasValue && criterion > 0 ? Round(score.Value / criterion * 100) : null,
                HasCompletePeriodData = complete,
                CompleteProductCount = branchRecords.Count(IsRankable),
                ProductCount = branchRecords.Count
            };
        }).ToList();

    private static DashboardProductPerformanceViewModel BuildBranchProductRow(BranchProductRecord record) => new()
    {
        Year = record.Instance.Year,
        Term = record.Instance.Term,
        GroupId = record.Branch.GroupId,
        GroupNo = record.Branch.Group.GroupNo,
        GroupName = record.Branch.Group.Name,
        BranchId = record.Branch.Id,
        BranchCode = record.Branch.BranchCode,
        BranchName = record.Branch.Name,
        MainProductInstanceId = record.Instance.Id,
        MainProductId = record.Instance.MainProductId,
        ProductCode = record.Instance.MainProduct.Code,
        ProductName = record.Instance.MainProduct.Name,
        CalculationType = record.Parameter.CalculationType,
        CriterionScore = record.Parameter.CriterionScore,
        TargetValue = record.Result.TargetValue,
        ActualValue = IsRankable(record) ? record.Result.ActualValue : null,
        HgRatioPercent = IsRankable(record) ? record.Result.HgRatioPercent : null,
        HgoScore = IsRankable(record) ? record.Result.HgoScore : null,
        TotalScore = IsRankable(record) ? record.Result.TotalScore : null,
        HasCompleteBatchData = IsRankable(record),
        HasSubProductConfiguration = record.HasSubProductConfiguration,
        HasCompleteTargetData = record.HasCompleteTargetData,
        HasParameterConfiguration = record.HasParameterConfiguration,
        SubProductCount = record.PortfolioRecords.SelectMany(item => item.SubProducts)
            .Select(link => link.SubProductId).Distinct().Count()
    };

    private static List<DashboardMainProductPerformanceViewModel> BuildMainProductRows(
        IReadOnlyCollection<BranchProductRecord> records) => records
        .GroupBy(record => new { record.Instance.Year, record.Instance.Term, record.Instance.MainProductId })
        .Select(group =>
        {
            var items = group.ToList();
            var first = items[0];
            var configured = items.All(item => item.HasSubProductConfiguration);
            var parameters = items.All(item => item.HasParameterConfiguration);
            var targets = parameters && items.All(item => item.HasCompleteTargetData);
            var complete = parameters && configured && targets && items.All(IsRankable);
            var target = Round(items.Sum(item => item.Result.TargetValue));
            var actual = complete ? Round(items.Sum(item => item.Result.ActualValue!.Value)) : (decimal?)null;
            return new DashboardMainProductPerformanceViewModel
            {
                Year = first.Instance.Year,
                Term = first.Instance.Term,
                MainProductInstanceId = first.Instance.Id,
                MainProductId = first.Instance.MainProductId,
                ProductCode = first.Instance.MainProduct.Code,
                ProductName = first.Instance.MainProduct.Name,
                SubProductCount = items.SelectMany(item => item.PortfolioRecords)
                    .SelectMany(item => item.SubProducts).Select(link => link.SubProductId).Distinct().Count(),
                BranchCount = items.Select(item => item.Branch.Id).Distinct().Count(),
                CriterionScore = Round(items.Sum(item => item.Parameter.CriterionScore)),
                TargetValue = target,
                ActualValue = actual,
                HgRatioPercent = actual.HasValue && target > 0 ? Round(actual.Value / target * 100) : null,
                HgoScore = complete ? Round(items.Sum(item => item.Result.HgoScore!.Value)) : null,
                TotalScore = complete ? Round(items.Sum(item => item.Result.TotalScore!.Value)) : null,
                HasCompleteBatchData = complete,
                HasSubProductConfiguration = configured,
                HasCompleteTargetData = targets,
                HasParameterConfiguration = parameters
            };
        }).ToList();

    private static List<DashboardPortfolioPerformanceViewModel> BuildPortfolioRows(
        IReadOnlyCollection<PeriodDataset> datasets) => datasets.SelectMany(dataset => dataset.Portfolios.Select(portfolio =>
    {
        var records = dataset.Records.Where(record => record.Portfolio.Id == portfolio.Id).ToList();
        var complete = records.Count > 0 && records.All(IsRankable);
        var criterion = Round(records.Sum(record => record.Parameter.CriterionScore));
        var score = complete ? Round(records.Sum(record => record.Result.TotalScore!.Value)) : (decimal?)null;
        return new DashboardPortfolioPerformanceViewModel
        {
            Year = dataset.Year,
            Term = dataset.Term,
            GroupId = portfolio.GroupId,
            GroupNo = portfolio.Branch.Group.GroupNo,
            GroupName = portfolio.Branch.Group.Name,
            BranchId = portfolio.BranchId,
            BranchCode = portfolio.Branch.BranchCode,
            BranchName = portfolio.Branch.Name,
            PortfolioId = portfolio.Id,
            PortfolioCode = portfolio.Code,
            PortfolioName = portfolio.Name,
            ProductGamutId = portfolio.ProductGamutId,
            ProductGamutCode = portfolio.ProductGamut.Code,
            ProductGamutName = portfolio.ProductGamut.Name,
            PortfolioTypeId = portfolio.PortfolioTypeId,
            PortfolioTypeCode = portfolio.PortfolioType.Code,
            PortfolioTypeName = portfolio.PortfolioType.Name,
            CriterionScore = criterion,
            HgoScore = score,
            TotalScore = score,
            SuccessPercent = score.HasValue && criterion > 0 ? Round(score.Value / criterion * 100) : null,
            HasCompletePeriodData = complete,
            CompleteProductCount = records.Count(IsRankable),
            ProductCount = records.Count
        };
    })).ToList();

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildPortfolioContributions(
        PortfolioProductRecord record) => record.SubProducts.Select(link =>
    {
        var metrics = calculator.GetTermMonths(record.Instance.Term).Select(month =>
        {
            record.MetricLookup.TryGetValue((record.Portfolio.Id, link.SubProductId, month), out var metric);
            return metric;
        }).ToList();
        var complete = metrics.All(metric => metric?.ActualValue.HasValue == true);
        return new DashboardSubProductContributionViewModel
        {
            SubProductId = link.SubProductId,
            Code = link.SubProduct.Code,
            Name = link.SubProduct.Name,
            ActualValue = complete ? Round(Aggregate(metrics.Select(metric => metric!.ActualValue!.Value),
                record.Parameter.CalculationType)) : null
        };
    }).ToList();

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildBranchContributions(BranchProductRecord record) =>
        record.PortfolioRecords.SelectMany(BuildPortfolioContributions)
            .GroupBy(item => new { item.SubProductId, item.Code, item.Name })
            .Select(group => new DashboardSubProductContributionViewModel
            {
                SubProductId = group.Key.SubProductId,
                Code = group.Key.Code,
                Name = group.Key.Name,
                ActualValue = group.All(item => item.ActualValue.HasValue)
                    ? Round(group.Sum(item => item.ActualValue!.Value)) : null
            }).ToList();

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildAggregateContributions(
        IReadOnlyCollection<BranchProductRecord> records) => records.SelectMany(BuildBranchContributions)
            .GroupBy(item => new { item.SubProductId, item.Code, item.Name })
            .Select(group => new DashboardSubProductContributionViewModel
            {
                SubProductId = group.Key.SubProductId,
                Code = group.Key.Code,
                Name = group.Key.Name,
                ActualValue = group.All(item => item.ActualValue.HasValue)
                    ? Round(group.Sum(item => item.ActualValue!.Value)) : null
            }).ToList();

    private static IReadOnlyList<DashboardProductMonthViewModel> BuildMonthRows(
        IEnumerable<MainProductMonthlyValue> months) => months.Select(month => new DashboardProductMonthViewModel
    {
        Month = month.Month,
        MonthName = TurkishMonthNames[month.Month],
        TargetValue = month.TargetValue,
        ActualValue = month.ActualValue,
        ActualAsOfDate = month.ActualAsOfDate,
        HgRatioPercent = month.ActualValue.HasValue && month.TargetValue > 0
            ? Round(month.ActualValue.Value / month.TargetValue * 100) : null
    }).ToList();

    private static void ApplyPartitionedRanks<TRow, TKey>(
        IReadOnlyCollection<TRow> rows,
        Func<TRow, TKey> partition,
        Func<TRow, decimal?> score,
        Action<TRow, int?, int> assign) where TKey : notnull
    {
        var items = rows.ToList();
        var ranks = PartitionedDenseRankCalculator.Calculate(items, partition, score);
        for (var index = 0; index < items.Count; index++)
        {
            assign(items[index], ranks[index].Rank, ranks[index].CandidateCount);
        }
    }

    private async Task<IReadOnlyList<DashboardPeriodOptionViewModel>> GetPeriodsAsync(CancellationToken cancellationToken) =>
        (await db.MainProductInstances.AsNoTracking()
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .Select(instance => new { instance.Year, instance.Term }).Distinct()
            .ToListAsync(cancellationToken))
        .Select(period => new DashboardPeriodOptionViewModel { Year = period.Year, Term = period.Term })
        .OrderByDescending(period => period.Year).ThenByDescending(period => period.Term).ToList();

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

    private static bool IsRankable(PortfolioProductRecord record) => record.HasParameterConfiguration
        && record.HasCompleteTargetData
        && record.SubProducts.Count > 0 && record.Result.HasCompleteBatchData && record.Result.TotalScore.HasValue;
    private static bool IsRankable(BranchProductRecord record) => record.HasParameterConfiguration
        && record.HasCompleteTargetData
        && record.HasSubProductConfiguration && record.Result.HasCompleteBatchData && record.Result.TotalScore.HasValue;
    private static decimal Aggregate(IEnumerable<decimal> values, MainProductCalculationType type)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : type == MainProductCalculationType.Average ? list.Average() : list.Sum();
    }
    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record PeriodBranch(int Year, int Term, Branch Branch);
    private sealed record PeriodDataset(
        int Year,
        int Term,
        IReadOnlyList<Branch> Branches,
        IReadOnlyList<Portfolio> Portfolios,
        IReadOnlyList<PortfolioProductRecord> Records);
    private sealed record PortfolioProductRecord(
        Portfolio Portfolio,
        Branch Branch,
        ProductGamut Gamut,
        PortfolioType PortfolioType,
        MainProductInstance Instance,
        MainProductParameter Parameter,
        IReadOnlyList<SubProductInstance> SubProducts,
        bool HasParameterConfiguration,
        bool HasCompleteTargetData,
        MainProductPeriodCalculationResult Result,
        IReadOnlyList<MainProductMonthlyValue> Months,
        IReadOnlyDictionary<(int PortfolioId, int SubProductId, int Month), PortfolioSubProductMonthlyMetric> MetricLookup);
    private sealed record BranchProductRecord(
        Branch Branch,
        MainProductInstance Instance,
        MainProductParameter Parameter,
        IReadOnlyList<PortfolioProductRecord> PortfolioRecords,
        bool HasParameterConfiguration,
        bool HasCompleteTargetData,
        bool HasSubProductConfiguration,
        MainProductPeriodCalculationResult Result,
        IReadOnlyList<MainProductMonthlyValue> Months)
    {
        public int PortfolioCount => PortfolioRecords.Select(record => record.Portfolio.Id).Distinct().Count();
    }
}
