using System.Diagnostics;
using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class DashboardService(
    AppDbContext db,
    TimeProvider timeProvider,
    IMainProductPeriodCalculator calculator,
    IPerformanceFactCache factCache,
    ILogger<DashboardService> logger) : IDashboardService
{
    private static readonly StringComparer TurkishTextComparer =
        PerformanceQueryProcessor.TurkishTextComparer;
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
        var periods = (await GetPeriodsCachedAsync(
            forceRefresh: false, cancellationToken)).Value;
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
            })
            .ToListAsync(cancellationToken);
        var types = await db.PortfolioTypes.AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.Code)
            .Select(type => new DashboardPortfolioTypeOptionViewModel
            {
                Id = type.Id,
                Code = type.Code,
                Name = type.Name
            })
            .ToListAsync(cancellationToken);

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
            BatchDate = Today
        };
    }

    public async Task<DashboardSnapshotViewModel> GetSnapshotAsync(
        PerformanceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var totalTimer = Stopwatch.StartNew();
        query = PerformanceQueryProcessor.Normalize(query);
        var periodCatalogTimer = Stopwatch.StartNew();
        var periodCatalog = await GetPeriodsCachedAsync(
            query.ForceRefresh, cancellationToken);
        periodCatalogTimer.Stop();
        var availablePeriods = periodCatalog.Value;
        var periodScope = DashboardPeriodScopeSelector.Select(
            availablePeriods, query.Year, query.Term);
        var periodKeys = periodScope.Select(period => new PeriodKey(period.Year, period.Term)).ToList();
        var scopeKey = PerformanceQueryProcessor.BuildFactScopeKey(
            query.GroupId, query.Year, query.Term, periodScope);

        var cached = await factCache.GetOrCreateAsync(
            scopeKey,
            query.ForceRefresh,
            token => LoadFactSetAsync(
                periodKeys,
                query.GroupId,
                branchId: null,
                mainProductId: null,
                portfolioId: null,
                FactSections.All,
                token),
            cancellationToken);
        var facts = cached.Value;
        var calculationTimer = Stopwatch.StartNew();

        var selectedBranch = facts.Branches.FirstOrDefault(branch =>
            branch.Id == query.BranchId
            && (!query.GroupId.HasValue || branch.GroupId == query.GroupId.Value));
        var hasInvalidBranchSelection =
            query.BranchId.HasValue && selectedBranch is null;
        if (selectedBranch is not null)
        {
            query.GroupId = selectedBranch.GroupId;
        }

        var snapshot = CreateSnapshotShell(query, selectedBranch, facts.Branches);
        var records = BuildPortfolioProductRecords(facts, periodKeys);
        var factCount = facts.FactCount;
        var candidateCount = 0;

        switch (query.Mode)
        {
            case PerformanceMode.Branch:
            {
                var branchProductRecords = BuildBranchProductRecords(records);
                var rows = BuildBranchRows(periodKeys, facts.Branches, branchProductRecords);
                ApplyPartitionedRanks(rows,
                    row => (row.GroupId, row.Year, row.Term),
                    row => row.TotalScore,
                    (row, rank, count) =>
                    {
                        row.Rank = rank;
                        row.RankCandidateCount = count;
                    });
                candidateCount = rows.Count;
                var visible = rows
                    .Where(row => !query.GroupId.HasValue || row.GroupId == query.GroupId.Value)
                    .Where(row => MatchesSearch(query.Search,
                        row.GroupNo, row.GroupName, row.BranchCode, row.BranchName));
                var page = PerformanceQueryProcessor.Page(
                    SortBranchRows(visible, query.SortKey, query.SortDirection),
                    query.Page,
                    query.PageSize);
                ApplyPage(snapshot, page);
                break;
            }
            case PerformanceMode.BranchProduct:
            {
                var branchProductRecords = BuildBranchProductRecords(records);
                var rows = branchProductRecords.Select(BuildBranchProductRow).ToList();
                ApplyPartitionedRanks(rows,
                    row => (row.GroupId, row.Year, row.Term, row.MainProductId),
                    row => row.TotalScore,
                    (row, rank, count) =>
                    {
                        row.SegmentRank = rank;
                        row.SegmentRankCandidateCount = count;
                    });
                candidateCount = rows.Count;
                var visible = rows
                    .Where(_ => !hasInvalidBranchSelection)
                    .Where(row => !query.GroupId.HasValue || row.GroupId == query.GroupId.Value)
                    .Where(row => selectedBranch is null || row.BranchId == selectedBranch.Id)
                    .Where(row => !query.MainProductId.HasValue
                        || row.MainProductId == query.MainProductId.Value)
                    .Where(row => MatchesSearch(query.Search,
                        row.GroupNo, row.GroupName, row.BranchCode, row.BranchName,
                        row.ProductCode, row.ProductName));
                var page = PerformanceQueryProcessor.Page(
                    SortBranchProductRows(visible, query.SortKey, query.SortDirection),
                    query.Page,
                    query.PageSize);
                ApplyPage(snapshot, page);

                if (selectedBranch is not null && query.Year.HasValue && query.Term.HasValue)
                {
                    var branchRows = BuildBranchRows(periodKeys, facts.Branches, branchProductRecords);
                    ApplyPartitionedRanks(branchRows,
                        row => (row.GroupId, row.Year, row.Term),
                        row => row.TotalScore,
                        (row, rank, count) =>
                        {
                            row.Rank = rank;
                            row.RankCandidateCount = count;
                        });
                    ApplyBranchSummary(snapshot, branchRows.FirstOrDefault(row =>
                        row.BranchId == selectedBranch.Id
                        && row.Year == query.Year.Value
                        && row.Term == query.Term.Value));
                }
                break;
            }
            case PerformanceMode.MainProduct:
            {
                var branchProductRecords = BuildBranchProductRecords(records)
                    .Where(record => !query.GroupId.HasValue
                        || record.Branch.GroupId == query.GroupId.Value)
                    .ToList();
                var rows = BuildMainProductRows(branchProductRecords);
                ApplyPartitionedRanks(rows,
                    row => (row.Year, row.Term),
                    row => row.TotalScore,
                    (row, rank, count) =>
                    {
                        row.Rank = rank;
                        row.RankCandidateCount = count;
                    });
                candidateCount = rows.Count;
                var visible = rows
                    .Where(row => !query.MainProductId.HasValue
                        || row.MainProductId == query.MainProductId.Value)
                    .Where(row => MatchesSearch(query.Search, row.ProductCode, row.ProductName));
                var page = PerformanceQueryProcessor.Page(
                    SortMainProductRows(visible, query.SortKey, query.SortDirection),
                    query.Page,
                    query.PageSize);
                ApplyPage(snapshot, page);
                break;
            }
            case PerformanceMode.Portfolio:
            {
                var rows = BuildPortfolioRows(
                    periodKeys, facts.Branches, facts.Portfolios, records);
                ApplyPartitionedRanks(rows,
                    row => (row.GroupId, row.ProductGamutId, row.Year, row.Term),
                    row => row.TotalScore,
                    (row, rank, count) =>
                    {
                        row.OfficialRank = rank;
                        row.OfficialRankCandidateCount = count;
                    });
                ApplyPartitionedRanks(rows,
                    row => (row.BranchId, row.Year, row.Term),
                    row => row.SuccessPercent,
                    (row, rank, count) =>
                    {
                        row.BranchRank = rank;
                        row.BranchRankCandidateCount = count;
                    });
                candidateCount = rows.Count;
                var visible = rows
                    .Where(_ => !hasInvalidBranchSelection)
                    .Where(row => !query.GroupId.HasValue || row.GroupId == query.GroupId.Value)
                    .Where(row => selectedBranch is null || row.BranchId == selectedBranch.Id)
                    .Where(row => !query.ProductGamutId.HasValue
                        || row.ProductGamutId == query.ProductGamutId.Value)
                    .Where(row => !query.PortfolioTypeId.HasValue
                        || row.PortfolioTypeId == query.PortfolioTypeId.Value)
                    .Where(row => MatchesSearch(query.Search,
                        row.GroupNo, row.GroupName, row.BranchCode, row.BranchName,
                        row.PortfolioCode, row.PortfolioName,
                        row.PortfolioTypeCode, row.PortfolioTypeName,
                        row.ProductGamutCode, row.ProductGamutName));
                var page = PerformanceQueryProcessor.Page(
                    SortPortfolioRows(visible, query.SortKey, query.SortDirection),
                    query.Page,
                    query.PageSize);
                ApplyPage(snapshot, page);
                break;
            }
        }

        calculationTimer.Stop();
        totalTimer.Stop();
        var cacheExpiresAt = periodCatalog.ExpiresAt <= cached.ExpiresAt
            ? periodCatalog.ExpiresAt
            : cached.ExpiresAt;
        snapshot.Timing = new DashboardPerformanceTimingViewModel
        {
            CacheHit = periodCatalog.CacheHit && cached.CacheHit,
            PeriodCount = periodKeys.Count,
            FactCount = factCount,
            CandidateCount = candidateCount,
            ReturnedCount = snapshot.Results.ItemCount,
            DatabaseMilliseconds =
                (periodCatalog.CacheHit ? 0 : periodCatalogTimer.Elapsed.TotalMilliseconds)
                + (cached.CacheHit ? 0 : facts.DatabaseMilliseconds),
            CalculationMilliseconds = calculationTimer.Elapsed.TotalMilliseconds,
            TotalMilliseconds = totalTimer.Elapsed.TotalMilliseconds,
            CacheRemainingMilliseconds = Math.Max(
                0,
                (cacheExpiresAt - timeProvider.GetUtcNow()).TotalMilliseconds)
        };
        logger.LogInformation(
            "Performance snapshot mode={Mode} periods={PeriodCount} cacheHit={CacheHit} " +
            "facts={FactCount} candidates={CandidateCount} returned={ReturnedCount} " +
            "dbMs={DatabaseMilliseconds:F2} calcMs={CalculationMilliseconds:F2} totalMs={TotalMilliseconds:F2}",
            query.Mode,
            snapshot.Timing.PeriodCount,
            snapshot.Timing.CacheHit,
            snapshot.Timing.FactCount,
            snapshot.Timing.CandidateCount,
            snapshot.Timing.ReturnedCount,
            snapshot.Timing.DatabaseMilliseconds,
            snapshot.Timing.CalculationMilliseconds,
            snapshot.Timing.TotalMilliseconds);
        return snapshot;
    }

    public Task<DashboardMonthlyDetailViewModel?> GetBranchProductDetailHeaderAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default) =>
        GetBranchProductDetailSectionAsync(
            branchId, mainProductInstanceId, DetailSection.Header, cancellationToken);

    public Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthsAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default) =>
        GetBranchProductDetailSectionAsync(
            branchId, mainProductInstanceId, DetailSection.Months, cancellationToken);

    public Task<DashboardMonthlyDetailViewModel?> GetBranchProductContributionsAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default) =>
        GetBranchProductDetailSectionAsync(
            branchId, mainProductInstanceId, DetailSection.Contributions, cancellationToken);

    public Task<DashboardMonthlyDetailViewModel?> GetBranchProductMonthlyDetailAsync(
        int branchId,
        int mainProductInstanceId,
        CancellationToken cancellationToken = default) =>
        GetBranchProductDetailSectionAsync(
            branchId, mainProductInstanceId, DetailSection.All, cancellationToken);

    public Task<DashboardMonthlyDetailViewModel?> GetMainProductDetailHeaderAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default) =>
        GetMainProductDetailSectionAsync(
            mainProductInstanceId, groupId, DetailSection.Header, cancellationToken);

    public Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthsAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default) =>
        GetMainProductDetailSectionAsync(
            mainProductInstanceId, groupId, DetailSection.Months, cancellationToken);

    public Task<DashboardMonthlyDetailViewModel?> GetMainProductContributionsAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default) =>
        GetMainProductDetailSectionAsync(
            mainProductInstanceId, groupId, DetailSection.Contributions, cancellationToken);

    public Task<DashboardMonthlyDetailViewModel?> GetMainProductMonthlyDetailAsync(
        int mainProductInstanceId,
        int? groupId,
        CancellationToken cancellationToken = default) =>
        GetMainProductDetailSectionAsync(
            mainProductInstanceId, groupId, DetailSection.All, cancellationToken);

    public Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailHeaderAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default) =>
        GetPortfolioDetailSectionAsync(
            portfolioId, year, term, DetailSection.Header, cancellationToken);

    public Task<DashboardPortfolioDetailViewModel?> GetPortfolioProductsAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default) =>
        GetPortfolioDetailSectionAsync(
            portfolioId, year, term, DetailSection.Products, cancellationToken);

    public Task<DashboardPortfolioDetailViewModel?> GetPortfolioMonthsAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default) =>
        GetPortfolioDetailSectionAsync(
            portfolioId, year, term, DetailSection.Months, cancellationToken);

    public Task<DashboardPortfolioDetailViewModel?> GetPortfolioContributionsAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default) =>
        GetPortfolioDetailSectionAsync(
            portfolioId, year, term, DetailSection.Contributions, cancellationToken);

    public Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailAsync(
        int portfolioId,
        int year,
        int term,
        CancellationToken cancellationToken = default) =>
        GetPortfolioDetailSectionAsync(
            portfolioId, year, term, DetailSection.All, cancellationToken);

    private async Task<DashboardMonthlyDetailViewModel?> GetBranchProductDetailSectionAsync(
        int branchId,
        int mainProductInstanceId,
        DetailSection section,
        CancellationToken cancellationToken)
    {
        var descriptor = await GetInstanceDescriptorAsync(mainProductInstanceId, cancellationToken);
        if (descriptor is null)
        {
            return null;
        }

        var sections = SectionsFor(section);
        var facts = await LoadFactSetAsync(
            [new PeriodKey(descriptor.Year, descriptor.Term)],
            groupId: null,
            branchId,
            descriptor.MainProductId,
            portfolioId: null,
            sections,
            cancellationToken);
        var records = BuildBranchProductRecords(BuildPortfolioProductRecords(
            facts, [new PeriodKey(descriptor.Year, descriptor.Term)]));
        var record = records.FirstOrDefault(item =>
            item.Branch.Id == branchId && item.Instance.Id == mainProductInstanceId);
        if (record is null)
        {
            return null;
        }

        return new DashboardMonthlyDetailViewModel
        {
            Mode = PerformanceMode.BranchProduct,
            Title = $"{record.Instance.ProductCode} · {record.Instance.ProductName}",
            Subtitle = $"{record.Branch.BranchCode} · {record.Branch.BranchName} · " +
                $"{record.PortfolioCount} portföy",
            CalculationType = record.Parameter.CalculationType,
            Months = section is DetailSection.Months or DetailSection.All
                ? BuildMonthRows(record.Months)
                : [],
            Contributions = section is DetailSection.Contributions or DetailSection.All
                ? BuildBranchContributions(record)
                : []
        };
    }

    private async Task<DashboardMonthlyDetailViewModel?> GetMainProductDetailSectionAsync(
        int mainProductInstanceId,
        int? groupId,
        DetailSection section,
        CancellationToken cancellationToken)
    {
        var descriptor = await GetInstanceDescriptorAsync(mainProductInstanceId, cancellationToken);
        if (descriptor is null)
        {
            return null;
        }

        var sections = SectionsFor(section);
        var facts = await LoadFactSetAsync(
            [new PeriodKey(descriptor.Year, descriptor.Term)],
            groupId,
            branchId: null,
            descriptor.MainProductId,
            portfolioId: null,
            sections,
            cancellationToken);
        var records = BuildBranchProductRecords(BuildPortfolioProductRecords(
                facts, [new PeriodKey(descriptor.Year, descriptor.Term)]))
            .Where(record => record.Instance.Id == mainProductInstanceId)
            .Where(record => !groupId.HasValue || record.Branch.GroupId == groupId.Value)
            .ToList();
        if (records.Count == 0)
        {
            return null;
        }

        var first = records[0];
        return new DashboardMonthlyDetailViewModel
        {
            Mode = PerformanceMode.MainProduct,
            Title = $"{descriptor.ProductCode} · {descriptor.ProductName}",
            Subtitle = groupId.HasValue
                ? $"{first.Branch.GroupNo} · {first.Branch.GroupName} genel toplamı"
                : $"Tüm gruplar · {records.Select(record => record.Branch.Id).Distinct().Count()} şube",
            CalculationType = first.Parameter.CalculationType,
            Months = section is DetailSection.Months or DetailSection.All
                ? BuildMonthRows(AggregateMonths(
                    records.Select(record => record.Months).ToList(), descriptor.Term))
                : [],
            Contributions = section is DetailSection.Contributions or DetailSection.All
                ? BuildAggregateContributions(records)
                : []
        };
    }

    private async Task<DashboardPortfolioDetailViewModel?> GetPortfolioDetailSectionAsync(
        int portfolioId,
        int year,
        int term,
        DetailSection section,
        CancellationToken cancellationToken)
    {
        if (term is not (1 or 2))
        {
            return null;
        }

        var portfolioDescriptor = await db.Portfolios.AsNoTracking()
            .Where(portfolio => portfolio.Id == portfolioId)
            .Select(portfolio => new
            {
                portfolio.BranchId,
                portfolio.GroupId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (portfolioDescriptor is null)
        {
            return null;
        }

        var sections = SectionsFor(section);
        var facts = await LoadFactSetAsync(
            [new PeriodKey(year, term)],
            portfolioDescriptor.GroupId,
            portfolioDescriptor.BranchId,
            mainProductId: null,
            portfolioId,
            sections,
            cancellationToken);
        var records = BuildPortfolioProductRecords(facts, [new PeriodKey(year, term)])
            .Where(record => record.Portfolio.Id == portfolioId)
            .OrderBy(record => record.Instance.ProductCode, TurkishTextComparer)
            .ToList();
        if (records.Count == 0)
        {
            return null;
        }

        var first = records[0];
        return new DashboardPortfolioDetailViewModel
        {
            PortfolioId = portfolioId,
            Year = year,
            Term = term,
            Title = $"{first.Portfolio.Code} · {first.Portfolio.Name}",
            Subtitle = $"{first.Branch.BranchCode} · {first.Branch.BranchName} · " +
                $"{first.Gamut.Code} ürün gamı",
            Products = section == DetailSection.Header
                ? []
                : records.Select(record => new DashboardPortfolioProductDetailViewModel
                {
                    MainProductInstanceId = record.Instance.Id,
                    ProductCode = record.Instance.ProductCode,
                    ProductName = record.Instance.ProductName,
                    CalculationType = record.Parameter.CalculationType,
                    HasParameterConfiguration = record.HasParameterConfiguration,
                    HasCompleteTargetData = record.HasCompleteTargetData,
                    CriterionScore = record.Parameter.CriterionScore,
                    TargetValue = record.Result.TargetValue,
                    ActualValue = IsRankable(record) ? record.Result.ActualValue : null,
                    HgRatioPercent = IsRankable(record) ? record.Result.HgRatioPercent : null,
                    TotalScore = IsRankable(record) ? record.Result.TotalScore : null,
                    Months = section is DetailSection.Months or DetailSection.All
                        ? BuildMonthRows(record.Months)
                        : [],
                    Contributions = section is DetailSection.Contributions or DetailSection.All
                        ? BuildPortfolioContributions(record)
                        : []
                }).ToList()
        };
    }

    private async Task<PerformanceFactSet> LoadFactSetAsync(
        IReadOnlyCollection<PeriodKey> periods,
        int? groupId,
        int? branchId,
        int? mainProductId,
        int? portfolioId,
        FactSections sections,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        if (periods.Count == 0)
        {
            return PerformanceFactSet.Empty;
        }

        var years = periods.Select(period => period.Year).Distinct().ToList();
        var terms = periods.Select(period => period.Term).Distinct().ToList();
        var periodCodes = periods
            .Select(period => period.Year * 10 + period.Term)
            .Distinct()
            .ToList();
        var firstPeriodCode = periodCodes.Min();
        var lastPeriodCode = periodCodes.Max();

        var branchesQuery = db.Branches.AsNoTracking()
            .Where(branch => branch.Group.IsActive);
        if (groupId.HasValue)
        {
            branchesQuery = branchesQuery.Where(branch => branch.GroupId == groupId.Value);
        }
        if (branchId.HasValue)
        {
            branchesQuery = branchesQuery.Where(branch => branch.Id == branchId.Value);
        }
        var branches = await branchesQuery
            .Select(branch => new BranchFact(
                branch.Id,
                branch.GroupId,
                branch.BranchCode,
                branch.Name,
                branch.Group.GroupNo,
                branch.Group.Name,
                branch.Group.GroupType))
            .ToListAsync(cancellationToken);
        var branchIds = branches.Select(branch => branch.Id).ToList();

        var portfoliosQuery = db.Portfolios.AsNoTracking()
            .Where(portfolio => portfolio.IsActive
                && portfolio.Branch.Group.IsActive
                && portfolio.ProductGamut.IsActive
                && portfolio.PortfolioType.IsActive);
        if (groupId.HasValue)
        {
            portfoliosQuery = portfoliosQuery.Where(portfolio => portfolio.GroupId == groupId.Value);
        }
        if (branchId.HasValue)
        {
            portfoliosQuery = portfoliosQuery.Where(portfolio => portfolio.BranchId == branchId.Value);
        }
        if (portfolioId.HasValue)
        {
            portfoliosQuery = portfoliosQuery.Where(portfolio => portfolio.Id == portfolioId.Value);
        }
        if (branchIds.Count == 0)
        {
            portfoliosQuery = portfoliosQuery.Where(_ => false);
        }
        else
        {
            portfoliosQuery = portfoliosQuery.Where(portfolio => branchIds.Contains(portfolio.BranchId));
        }
        var portfolios = await portfoliosQuery
            .Select(portfolio => new PortfolioFact(
                portfolio.Id,
                portfolio.BranchId,
                portfolio.GroupId,
                portfolio.ProductGamutId,
                portfolio.PortfolioTypeId,
                portfolio.Code,
                portfolio.Name,
                portfolio.ProductGamut.Code,
                portfolio.ProductGamut.Name,
                portfolio.PortfolioType.Code,
                portfolio.PortfolioType.Name))
            .ToListAsync(cancellationToken);
        var portfolioIds = portfolios.Select(portfolio => portfolio.Id).ToList();
        var gamutIds = portfolios.Select(portfolio => portfolio.ProductGamutId).Distinct().ToList();

        var assignmentsQuery = db.ProductGamutMainProductAssignments.AsNoTracking()
            .Where(assignment => gamutIds.Contains(assignment.ProductGamutId)
                && assignment.EffectiveFromYear * 10 + assignment.EffectiveFromTerm <= lastPeriodCode
                && (!assignment.EffectiveToYear.HasValue
                    || assignment.EffectiveToYear.Value * 10
                        + assignment.EffectiveToTerm!.Value >= firstPeriodCode));
        if (mainProductId.HasValue)
        {
            assignmentsQuery = assignmentsQuery.Where(
                assignment => assignment.MainProductId == mainProductId.Value);
        }
        var assignments = await assignmentsQuery
            .Select(assignment => new AssignmentFact(
                assignment.ProductGamutId,
                assignment.MainProductId,
                assignment.EffectiveFromYear,
                assignment.EffectiveFromTerm,
                assignment.EffectiveToYear,
                assignment.EffectiveToTerm))
            .ToListAsync(cancellationToken);

        var exclusionsQuery = db.BranchMainProductExclusions.AsNoTracking()
            .Where(exclusion => branchIds.Contains(exclusion.BranchId)
                && exclusion.EffectiveFromYear * 10 + exclusion.EffectiveFromTerm <= lastPeriodCode
                && (!exclusion.EffectiveToYear.HasValue
                    || exclusion.EffectiveToYear.Value * 10
                        + exclusion.EffectiveToTerm!.Value >= firstPeriodCode));
        if (mainProductId.HasValue)
        {
            exclusionsQuery = exclusionsQuery.Where(
                exclusion => exclusion.MainProductId == mainProductId.Value);
        }
        var exclusions = await exclusionsQuery
            .Select(exclusion => new ExclusionFact(
                exclusion.BranchId,
                exclusion.MainProductId,
                exclusion.EffectiveFromYear,
                exclusion.EffectiveFromTerm,
                exclusion.EffectiveToYear,
                exclusion.EffectiveToTerm))
            .ToListAsync(cancellationToken);

        var instancesQuery = db.MainProductInstances.AsNoTracking()
            .Where(instance => years.Contains(instance.Year)
                && terms.Contains(instance.Term)
                && periodCodes.Contains(instance.Year * 10 + instance.Term)
                && instance.MainProduct.IsActive);
        if (mainProductId.HasValue)
        {
            instancesQuery = instancesQuery.Where(
                instance => instance.MainProductId == mainProductId.Value);
        }
        var instances = await instancesQuery
            .Select(instance => new MainProductInstanceFact(
                instance.Id,
                instance.MainProductId,
                instance.Year,
                instance.Term,
                instance.MainProduct.Code,
                instance.MainProduct.Name))
            .ToListAsync(cancellationToken);
        var instanceIds = instances.Select(instance => instance.Id).ToList();

        var subProducts = new List<SubProductFact>();
        if (sections.HasFlag(FactSections.SubProducts) && instanceIds.Count > 0)
        {
            subProducts = await db.SubProductInstances.AsNoTracking()
                .Where(link => instanceIds.Contains(link.MainProductInstanceId)
                    && link.SubProduct.IsActive)
                .Select(link => new SubProductFact(
                    link.MainProductInstanceId,
                    link.SubProductId,
                    link.SubProduct.Code,
                    link.SubProduct.Name))
                .ToListAsync(cancellationToken);
        }

        var parametersQuery = db.MainProductParameters.AsNoTracking()
            .Where(parameter => parameter.IsActive
                && instanceIds.Contains(parameter.MainProductInstanceId));
        if (groupId.HasValue)
        {
            parametersQuery = parametersQuery.Where(
                parameter => parameter.GroupId == groupId.Value);
        }
        else if (branchIds.Count > 0)
        {
            var groupIds = branches.Select(branch => branch.GroupId).Distinct().ToList();
            parametersQuery = parametersQuery.Where(parameter => groupIds.Contains(parameter.GroupId));
        }
        var parameters = await parametersQuery
            .Select(parameter => new ParameterFact(
                parameter.Id,
                parameter.GroupId,
                parameter.MainProductInstanceId,
                parameter.CalculationType,
                parameter.CriterionScore))
            .ToListAsync(cancellationToken);
        var parameterIds = parameters.Select(parameter => parameter.Id).ToList();

        var targets = new List<TargetFact>();
        if (sections.HasFlag(FactSections.Targets)
            && portfolioIds.Count > 0
            && parameterIds.Count > 0)
        {
            targets = await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
                .Where(target => portfolioIds.Contains(target.PortfolioId)
                    && parameterIds.Contains(target.MainProductParameterId))
                .Select(target => new TargetFact(
                    target.PortfolioId,
                    target.MainProductParameterId,
                    target.Month,
                    target.TargetValue))
                .ToListAsync(cancellationToken);
        }

        var metrics = new List<MetricFact>();
        var subProductIds = subProducts.Select(product => product.SubProductId).Distinct().ToList();
        if (sections.HasFlag(FactSections.Metrics)
            && portfolioIds.Count > 0
            && subProductIds.Count > 0)
        {
            metrics = await db.PortfolioSubProductMonthlyMetrics.AsNoTracking()
                .Where(metric => portfolioIds.Contains(metric.PortfolioId)
                    && subProductIds.Contains(metric.SubProductId)
                    && years.Contains(metric.Year)
                    && terms.Contains(metric.Term)
                    && periodCodes.Contains(metric.Year * 10 + metric.Term))
                .Select(metric => new MetricFact(
                    metric.PortfolioId,
                    metric.SubProductId,
                    metric.Year,
                    metric.Term,
                    metric.Month,
                    metric.ActualValue,
                    metric.ActualAsOfDate))
                .ToListAsync(cancellationToken);
        }

        timer.Stop();
        return new PerformanceFactSet(
            branches,
            portfolios,
            assignments,
            exclusions,
            instances,
            subProducts,
            parameters,
            targets,
            metrics,
            timer.Elapsed.TotalMilliseconds);
    }

    private List<PortfolioProductRecord> BuildPortfolioProductRecords(
        PerformanceFactSet facts,
        IReadOnlyCollection<PeriodKey> periods)
    {
        var branchLookup = facts.Branches.ToDictionary(branch => branch.Id);
        var assignments = facts.Assignments
            .GroupBy(assignment => assignment.ProductGamutId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var exclusions = facts.Exclusions
            .GroupBy(exclusion => (exclusion.BranchId, exclusion.MainProductId))
            .ToDictionary(group => group.Key, group => group.ToList());
        var instances = facts.Instances.ToDictionary(
            instance => (instance.MainProductId, instance.Year, instance.Term));
        var parameters = facts.Parameters.ToDictionary(
            parameter => (parameter.GroupId, parameter.MainProductInstanceId));
        var subProducts = facts.SubProducts
            .GroupBy(product => product.MainProductInstanceId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SubProductFact>)group
                    .DistinctBy(item => item.SubProductId)
                    .OrderBy(item => item.Code, TurkishTextComparer)
                    .ToList());
        var targets = facts.Targets.ToDictionary(
            target => (target.PortfolioId, target.MainProductParameterId, target.Month));
        var metrics = facts.Metrics.ToDictionary(metric =>
            (metric.PortfolioId, metric.SubProductId, metric.Year, metric.Term, metric.Month));
        var records = new List<PortfolioProductRecord>();

        foreach (var period in periods)
        {
            foreach (var portfolio in facts.Portfolios)
            {
                if (!branchLookup.TryGetValue(portfolio.BranchId, out var branch)
                    || !assignments.TryGetValue(portfolio.ProductGamutId, out var gamutAssignments))
                {
                    continue;
                }

                foreach (var assignment in gamutAssignments
                             .Where(item => IsEffective(item, period))
                             .DistinctBy(item => item.MainProductId))
                {
                    if (exclusions.TryGetValue(
                            (portfolio.BranchId, assignment.MainProductId),
                            out var productExclusions)
                        && productExclusions.Any(item => IsEffective(item, period)))
                    {
                        continue;
                    }
                    if (!instances.TryGetValue(
                            (assignment.MainProductId, period.Year, period.Term),
                            out var instance))
                    {
                        continue;
                    }

                    var hasParameter = parameters.TryGetValue(
                        (portfolio.GroupId, instance.Id), out var parameter);
                    parameter ??= ParameterFact.Missing(
                        portfolio.GroupId, instance.Id);
                    var linkedProducts = subProducts.GetValueOrDefault(instance.Id) ?? [];
                    var termMonths = calculator.GetTermMonths(period.Term);
                    var hasTargets = hasParameter
                        && termMonths.All(month => targets.ContainsKey(
                            (portfolio.Id, parameter.Id, month)));
                    var months = termMonths.Select(month =>
                    {
                        targets.TryGetValue(
                            (portfolio.Id, parameter.Id, month), out var target);
                        var actualValues = linkedProducts.Select(product =>
                        {
                            metrics.TryGetValue(
                                (portfolio.Id, product.SubProductId,
                                    period.Year, period.Term, month),
                                out var metric);
                            return metric is null
                                ? null
                                : new PortfolioSubProductActualValue(
                                    metric.ActualValue, metric.ActualAsOfDate);
                        }).ToList();
                        var actual = PortfolioActualAggregator.Aggregate(actualValues);
                        return new MainProductMonthlyValue(
                            month,
                            target?.TargetValue ?? 0,
                            actual.ActualValue,
                            actual.ActualAsOfDate);
                    }).ToList();
                    var result = calculator.Calculate(new MainProductPeriodCalculationInput(
                        period.Year,
                        period.Term,
                        Today,
                        parameter.CalculationType,
                        parameter.CriterionScore,
                        months));
                    records.Add(new PortfolioProductRecord(
                        portfolio,
                        branch,
                        new ProductGamutFact(
                            portfolio.ProductGamutId,
                            portfolio.ProductGamutCode,
                            portfolio.ProductGamutName),
                        new PortfolioTypeFact(
                            portfolio.PortfolioTypeId,
                            portfolio.PortfolioTypeCode,
                            portfolio.PortfolioTypeName),
                        instance,
                        parameter,
                        linkedProducts,
                        hasParameter,
                        hasTargets,
                        result,
                        months,
                        metrics));
                }
            }
        }

        return records;
    }

    private List<BranchProductRecord> BuildBranchProductRecords(
        IReadOnlyCollection<PortfolioProductRecord> records) =>
        records.GroupBy(record => new
        {
            BranchId = record.Branch.Id,
            InstanceId = record.Instance.Id
        }).Select(group =>
        {
            var items = group.ToList();
            var first = items[0];
            var months = AggregateMonths(
                items.Select(item => item.Months).ToList(),
                first.Instance.Term);
            var hasParameter = items.All(item => item.HasParameterConfiguration);
            var hasTargets = hasParameter
                && items.All(item => item.HasCompleteTargetData);
            var hasSubProducts = items.All(item => item.SubProducts.Count > 0);
            var result = calculator.Calculate(new MainProductPeriodCalculationInput(
                first.Instance.Year,
                first.Instance.Term,
                Today,
                first.Parameter.CalculationType,
                first.Parameter.CriterionScore,
                months));
            return new BranchProductRecord(
                first.Branch,
                first.Instance,
                first.Parameter,
                items,
                hasParameter,
                hasTargets,
                hasSubProducts,
                result,
                months);
        }).ToList();

    private IReadOnlyList<MainProductMonthlyValue> AggregateMonths(
        IReadOnlyCollection<IReadOnlyList<MainProductMonthlyValue>> recordMonths,
        int term) =>
        PortfolioMonthlyRollup.Aggregate(calculator.GetTermMonths(term), recordMonths);

    private static List<DashboardBranchPerformanceViewModel> BuildBranchRows(
        IReadOnlyCollection<PeriodKey> periods,
        IReadOnlyCollection<BranchFact> branches,
        IReadOnlyCollection<BranchProductRecord> records)
    {
        var recordLookup = records
            .GroupBy(record => (
                record.Branch.Id,
                record.Instance.Year,
                record.Instance.Term))
            .ToDictionary(group => group.Key, group => group.ToList());
        return periods.SelectMany(period => branches.Select(branch =>
        {
            var branchRecords = recordLookup.GetValueOrDefault(
                (branch.Id, period.Year, period.Term)) ?? [];
            var complete = branchRecords.Count > 0
                && branchRecords.All(IsRankable);
            var criterion = Round(branchRecords.Sum(
                record => record.Parameter.CriterionScore));
            var score = complete
                ? Round(branchRecords.Sum(record => record.Result.TotalScore!.Value))
                : (decimal?)null;
            return new DashboardBranchPerformanceViewModel
            {
                Year = period.Year,
                Term = period.Term,
                GroupId = branch.GroupId,
                GroupNo = branch.GroupNo,
                GroupName = branch.GroupName,
                BranchId = branch.Id,
                BranchCode = branch.BranchCode,
                BranchName = branch.BranchName,
                CriterionScore = criterion,
                HgoScore = score,
                TotalScore = score,
                SuccessPercent = score.HasValue && criterion > 0
                    ? Round(score.Value / criterion * 100)
                    : null,
                HasCompletePeriodData = complete,
                CompleteProductCount = branchRecords.Count(IsRankable),
                ProductCount = branchRecords.Count
            };
        })).ToList();
    }

    private static DashboardProductPerformanceViewModel BuildBranchProductRow(
        BranchProductRecord record) => new()
    {
        Year = record.Instance.Year,
        Term = record.Instance.Term,
        GroupId = record.Branch.GroupId,
        GroupNo = record.Branch.GroupNo,
        GroupName = record.Branch.GroupName,
        BranchId = record.Branch.Id,
        BranchCode = record.Branch.BranchCode,
        BranchName = record.Branch.BranchName,
        MainProductInstanceId = record.Instance.Id,
        MainProductId = record.Instance.MainProductId,
        ProductCode = record.Instance.ProductCode,
        ProductName = record.Instance.ProductName,
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
        SubProductCount = record.PortfolioRecords
            .SelectMany(item => item.SubProducts)
            .Select(item => item.SubProductId)
            .Distinct()
            .Count()
    };

    private static List<DashboardMainProductPerformanceViewModel> BuildMainProductRows(
        IReadOnlyCollection<BranchProductRecord> records) =>
        records.GroupBy(record => new
        {
            record.Instance.Year,
            record.Instance.Term,
            record.Instance.MainProductId
        }).Select(group =>
        {
            var items = group.ToList();
            var first = items[0];
            var configured = items.All(item => item.HasSubProductConfiguration);
            var parameters = items.All(item => item.HasParameterConfiguration);
            var targets = parameters && items.All(item => item.HasCompleteTargetData);
            var complete = parameters
                && configured
                && targets
                && items.All(IsRankable);
            var target = Round(items.Sum(item => item.Result.TargetValue));
            var actual = complete
                ? Round(items.Sum(item => item.Result.ActualValue!.Value))
                : (decimal?)null;
            return new DashboardMainProductPerformanceViewModel
            {
                Year = first.Instance.Year,
                Term = first.Instance.Term,
                MainProductInstanceId = first.Instance.Id,
                MainProductId = first.Instance.MainProductId,
                ProductCode = first.Instance.ProductCode,
                ProductName = first.Instance.ProductName,
                SubProductCount = items.SelectMany(item => item.PortfolioRecords)
                    .SelectMany(item => item.SubProducts)
                    .Select(item => item.SubProductId)
                    .Distinct()
                    .Count(),
                BranchCount = items.Select(item => item.Branch.Id).Distinct().Count(),
                CriterionScore = Round(items.Sum(item => item.Parameter.CriterionScore)),
                TargetValue = target,
                ActualValue = actual,
                HgRatioPercent = actual.HasValue && target > 0
                    ? Round(actual.Value / target * 100)
                    : null,
                HgoScore = complete
                    ? Round(items.Sum(item => item.Result.HgoScore!.Value))
                    : null,
                TotalScore = complete
                    ? Round(items.Sum(item => item.Result.TotalScore!.Value))
                    : null,
                HasCompleteBatchData = complete,
                HasSubProductConfiguration = configured,
                HasCompleteTargetData = targets,
                HasParameterConfiguration = parameters
            };
        }).ToList();

    private static List<DashboardPortfolioPerformanceViewModel> BuildPortfolioRows(
        IReadOnlyCollection<PeriodKey> periods,
        IReadOnlyCollection<BranchFact> branches,
        IReadOnlyCollection<PortfolioFact> portfolios,
        IReadOnlyCollection<PortfolioProductRecord> records)
    {
        var recordLookup = records
            .GroupBy(record => (
                record.Portfolio.Id,
                record.Instance.Year,
                record.Instance.Term))
            .ToDictionary(group => group.Key, group => group.ToList());
        var branchLookup = branches.ToDictionary(branch => branch.Id);
        return periods.SelectMany(period => portfolios.Select(portfolio =>
        {
            var portfolioRecords = recordLookup.GetValueOrDefault(
                (portfolio.Id, period.Year, period.Term)) ?? [];
            branchLookup.TryGetValue(portfolio.BranchId, out var branch);
            var complete = portfolioRecords.Count > 0
                && portfolioRecords.All(IsRankable);
            var criterion = Round(portfolioRecords.Sum(
                record => record.Parameter.CriterionScore));
            var score = complete
                ? Round(portfolioRecords.Sum(record => record.Result.TotalScore!.Value))
                : (decimal?)null;
            return new DashboardPortfolioPerformanceViewModel
            {
                Year = period.Year,
                Term = period.Term,
                GroupId = portfolio.GroupId,
                GroupNo = branch?.GroupNo ?? string.Empty,
                GroupName = branch?.GroupName ?? string.Empty,
                BranchId = portfolio.BranchId,
                BranchCode = branch?.BranchCode ?? string.Empty,
                BranchName = branch?.BranchName ?? string.Empty,
                PortfolioId = portfolio.Id,
                PortfolioCode = portfolio.Code,
                PortfolioName = portfolio.Name,
                ProductGamutId = portfolio.ProductGamutId,
                ProductGamutCode = portfolio.ProductGamutCode,
                ProductGamutName = portfolio.ProductGamutName,
                PortfolioTypeId = portfolio.PortfolioTypeId,
                PortfolioTypeCode = portfolio.PortfolioTypeCode,
                PortfolioTypeName = portfolio.PortfolioTypeName,
                CriterionScore = criterion,
                HgoScore = score,
                TotalScore = score,
                SuccessPercent = score.HasValue && criterion > 0
                    ? Round(score.Value / criterion * 100)
                    : null,
                HasCompletePeriodData = complete,
                CompleteProductCount = portfolioRecords.Count(IsRankable),
                ProductCount = portfolioRecords.Count
            };
        })).ToList();
    }

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildPortfolioContributions(
        PortfolioProductRecord record) =>
        record.SubProducts.Select(product =>
        {
            var metrics = calculator.GetTermMonths(record.Instance.Term)
                .Select(month =>
                {
                    record.MetricLookup.TryGetValue(
                        (record.Portfolio.Id, product.SubProductId,
                            record.Instance.Year, record.Instance.Term, month),
                        out var metric);
                    return metric;
                })
                .ToList();
            var complete = metrics.All(metric => metric?.ActualValue.HasValue == true);
            return new DashboardSubProductContributionViewModel
            {
                SubProductId = product.SubProductId,
                Code = product.Code,
                Name = product.Name,
                ActualValue = complete
                    ? Round(Aggregate(
                        metrics.Select(metric => metric!.ActualValue!.Value),
                        record.Parameter.CalculationType))
                    : null
            };
        }).ToList();

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildBranchContributions(
        BranchProductRecord record) =>
        record.PortfolioRecords
            .SelectMany(BuildPortfolioContributions)
            .GroupBy(item => new { item.SubProductId, item.Code, item.Name })
            .Select(group => new DashboardSubProductContributionViewModel
            {
                SubProductId = group.Key.SubProductId,
                Code = group.Key.Code,
                Name = group.Key.Name,
                ActualValue = group.All(item => item.ActualValue.HasValue)
                    ? Round(group.Sum(item => item.ActualValue!.Value))
                    : null
            })
            .ToList();

    private IReadOnlyList<DashboardSubProductContributionViewModel> BuildAggregateContributions(
        IReadOnlyCollection<BranchProductRecord> records) =>
        records.SelectMany(BuildBranchContributions)
            .GroupBy(item => new { item.SubProductId, item.Code, item.Name })
            .Select(group => new DashboardSubProductContributionViewModel
            {
                SubProductId = group.Key.SubProductId,
                Code = group.Key.Code,
                Name = group.Key.Name,
                ActualValue = group.All(item => item.ActualValue.HasValue)
                    ? Round(group.Sum(item => item.ActualValue!.Value))
                    : null
            })
            .ToList();

    private static IReadOnlyList<DashboardProductMonthViewModel> BuildMonthRows(
        IEnumerable<MainProductMonthlyValue> months) =>
        months.Select(month => new DashboardProductMonthViewModel
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

    private static DashboardSnapshotViewModel CreateSnapshotShell(
        PerformanceQuery query,
        BranchFact? selectedBranch,
        IReadOnlyCollection<BranchFact> branches)
    {
        var selectedGroup = selectedBranch is not null
            ? new GroupFact(
                selectedBranch.GroupId,
                selectedBranch.GroupNo,
                selectedBranch.GroupName)
            : branches.Where(branch => branch.GroupId == query.GroupId)
                .Select(branch => new GroupFact(
                    branch.GroupId,
                    branch.GroupNo,
                    branch.GroupName))
                .FirstOrDefault();
        return new DashboardSnapshotViewModel
        {
            Mode = query.Mode,
            HasSelectedBranch = selectedBranch is not null,
            GroupId = selectedGroup?.Id,
            BranchId = selectedBranch?.Id,
            BranchCode = selectedBranch?.BranchCode ?? string.Empty,
            BranchName = selectedBranch?.BranchName ?? string.Empty,
            GroupNo = selectedGroup?.GroupNo ?? string.Empty,
            GroupName = selectedGroup?.Name ?? "Tüm gruplar",
            Year = query.Year,
            Term = query.Term
        };
    }

    private static void ApplyBranchSummary(
        DashboardSnapshotViewModel snapshot,
        DashboardBranchPerformanceViewModel? summary)
    {
        snapshot.AssignedScore = summary?.CriterionScore ?? 0;
        snapshot.EarnedScore = summary?.TotalScore;
        snapshot.SuccessPercent = summary?.SuccessPercent;
        snapshot.HasCompletePeriodData = summary?.HasCompletePeriodData ?? false;
        snapshot.BranchRank = summary?.Rank;
        snapshot.RankedBranchCount = summary?.RankCandidateCount ?? 0;
    }

    private static void ApplyPage<T>(
        DashboardSnapshotViewModel snapshot,
        PerformancePage<T> page)
    {
        snapshot.Results = page;
    }

    private static IOrderedEnumerable<DashboardBranchPerformanceViewModel> SortBranchRows(
        IEnumerable<DashboardBranchPerformanceViewModel> rows,
        string? sortKey,
        string? direction)
    {
        var descending = IsDescending(direction);
        var ordered = sortKey?.ToLowerInvariant() switch
        {
            "year" => Order(rows, row => row.Year, descending),
            "term" => Order(rows, row => row.Term, descending),
            "group" => OrderText(rows, row => $"{row.GroupNo} {row.GroupName}", descending),
            "branch" => OrderText(rows, row => $"{row.BranchCode} {row.BranchName}", descending),
            "criterion" => Order(rows, row => row.CriterionScore, descending),
            "hgo" => OrderNullable(rows, row => row.HgoScore, descending),
            "total" => OrderNullable(rows, row => row.TotalScore, descending),
            "ratio" => OrderNullable(rows, row => row.SuccessPercent, descending),
            "rank" => OrderNullable(rows, row => row.Rank, descending),
            _ => rows.OrderByDescending(row => row.Year)
                .ThenByDescending(row => row.Term)
                .ThenBy(row => row.BranchCode, TurkishTextComparer)
        };
        return ordered
            .ThenByDescending(row => row.Year)
            .ThenByDescending(row => row.Term)
            .ThenBy(row => row.BranchCode, TurkishTextComparer)
            .ThenBy(row => row.BranchId);
    }

    private static IOrderedEnumerable<DashboardProductPerformanceViewModel> SortBranchProductRows(
        IEnumerable<DashboardProductPerformanceViewModel> rows,
        string? sortKey,
        string? direction)
    {
        var descending = IsDescending(direction);
        var ordered = sortKey?.ToLowerInvariant() switch
        {
            "year" => Order(rows, row => row.Year, descending),
            "term" => Order(rows, row => row.Term, descending),
            "group" => OrderText(rows, row => $"{row.GroupNo} {row.GroupName}", descending),
            "branch" => OrderText(rows, row => $"{row.BranchCode} {row.BranchName}", descending),
            "product" => OrderText(rows, row => $"{row.ProductCode} {row.ProductName}", descending),
            "criterion" => Order(rows, row => row.CriterionScore, descending),
            "target" => Order(rows, row => row.TargetValue, descending),
            "actual" => OrderNullable(rows, row => row.ActualValue, descending),
            "ratio" => OrderNullable(rows, row => row.HgRatioPercent, descending),
            "hgo" => OrderNullable(rows, row => row.HgoScore, descending),
            "total" => OrderNullable(rows, row => row.TotalScore, descending),
            "rank" => OrderNullable(rows, row => row.SegmentRank, descending),
            _ => rows.OrderByDescending(row => row.Year)
                .ThenByDescending(row => row.Term)
                .ThenBy(row => row.BranchCode, TurkishTextComparer)
                .ThenBy(row => row.ProductCode, TurkishTextComparer)
        };
        return ordered
            .ThenByDescending(row => row.Year)
            .ThenByDescending(row => row.Term)
            .ThenBy(row => row.BranchCode, TurkishTextComparer)
            .ThenBy(row => row.ProductCode, TurkishTextComparer)
            .ThenBy(row => row.BranchId)
            .ThenBy(row => row.MainProductId);
    }

    private static IOrderedEnumerable<DashboardMainProductPerformanceViewModel> SortMainProductRows(
        IEnumerable<DashboardMainProductPerformanceViewModel> rows,
        string? sortKey,
        string? direction)
    {
        var descending = IsDescending(direction);
        var ordered = sortKey?.ToLowerInvariant() switch
        {
            "year" => Order(rows, row => row.Year, descending),
            "term" => Order(rows, row => row.Term, descending),
            "product" => OrderText(rows, row => $"{row.ProductCode} {row.ProductName}", descending),
            "subcount" or "sub-count" => Order(rows, row => row.SubProductCount, descending),
            "branchcount" or "branch-count" => Order(rows, row => row.BranchCount, descending),
            "criterion" => Order(rows, row => row.CriterionScore, descending),
            "target" => Order(rows, row => row.TargetValue, descending),
            "actual" => OrderNullable(rows, row => row.ActualValue, descending),
            "ratio" => OrderNullable(rows, row => row.HgRatioPercent, descending),
            "hgo" => OrderNullable(rows, row => row.HgoScore, descending),
            "total" => OrderNullable(rows, row => row.TotalScore, descending),
            "rank" => OrderNullable(rows, row => row.Rank, descending),
            _ => rows.OrderByDescending(row => row.Year)
                .ThenByDescending(row => row.Term)
                .ThenBy(row => row.ProductCode, TurkishTextComparer)
        };
        return ordered
            .ThenByDescending(row => row.Year)
            .ThenByDescending(row => row.Term)
            .ThenBy(row => row.ProductCode, TurkishTextComparer)
            .ThenBy(row => row.MainProductId);
    }

    private static IOrderedEnumerable<DashboardPortfolioPerformanceViewModel> SortPortfolioRows(
        IEnumerable<DashboardPortfolioPerformanceViewModel> rows,
        string? sortKey,
        string? direction)
    {
        var descending = IsDescending(direction);
        var ordered = sortKey?.ToLowerInvariant() switch
        {
            "year" => Order(rows, row => row.Year, descending),
            "term" => Order(rows, row => row.Term, descending),
            "group" => OrderText(rows, row => $"{row.GroupNo} {row.GroupName}", descending),
            "branch" => OrderText(rows, row => $"{row.BranchCode} {row.BranchName}", descending),
            "portfolio" => OrderText(rows, row => $"{row.PortfolioCode} {row.PortfolioName}", descending),
            "type" => OrderText(rows, row => $"{row.PortfolioTypeCode} {row.PortfolioTypeName}", descending),
            "gamut" => OrderText(rows, row => $"{row.ProductGamutCode} {row.ProductGamutName}", descending),
            "criterion" => Order(rows, row => row.CriterionScore, descending),
            "hgo" => OrderNullable(rows, row => row.HgoScore, descending),
            "total" => OrderNullable(rows, row => row.TotalScore, descending),
            "ratio" => OrderNullable(rows, row => row.SuccessPercent, descending),
            "officialrank" or "official-rank" =>
                OrderNullable(rows, row => row.OfficialRank, descending),
            "branchrank" or "branch-rank" =>
                OrderNullable(rows, row => row.BranchRank, descending),
            _ => rows.OrderByDescending(row => row.Year)
                .ThenByDescending(row => row.Term)
                .ThenBy(row => row.BranchCode, TurkishTextComparer)
                .ThenBy(row => row.PortfolioCode, TurkishTextComparer)
        };
        return ordered
            .ThenByDescending(row => row.Year)
            .ThenByDescending(row => row.Term)
            .ThenBy(row => row.BranchCode, TurkishTextComparer)
            .ThenBy(row => row.PortfolioCode, TurkishTextComparer)
            .ThenBy(row => row.PortfolioId);
    }

    private static IOrderedEnumerable<T> Order<T, TKey>(
        IEnumerable<T> rows,
        Func<T, TKey> key,
        bool descending) =>
        descending ? rows.OrderByDescending(key) : rows.OrderBy(key);

    private static IOrderedEnumerable<T> OrderText<T>(
        IEnumerable<T> rows,
        Func<T, string> key,
        bool descending) =>
        descending
            ? rows.OrderByDescending(key, TurkishTextComparer)
            : rows.OrderBy(key, TurkishTextComparer);

    private static IOrderedEnumerable<T> OrderNullable<T, TKey>(
        IEnumerable<T> rows,
        Func<T, TKey?> key,
        bool descending) where TKey : struct =>
        descending
            ? rows.OrderBy(row => !key(row).HasValue).ThenByDescending(key)
            : rows.OrderBy(row => !key(row).HasValue).ThenBy(key);

    private static bool IsDescending(string? direction) =>
        string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesSearch(string? search, params string?[] values)
    {
        return PerformanceQueryProcessor.MatchesTurkish(search, values);
    }

    private async Task<InstanceDescriptor?> GetInstanceDescriptorAsync(
        int instanceId,
        CancellationToken cancellationToken) =>
        await db.MainProductInstances.AsNoTracking()
            .Where(instance => instance.Id == instanceId && instance.MainProduct.IsActive)
            .Select(instance => new InstanceDescriptor(
                instance.MainProductId,
                instance.Year,
                instance.Term,
                instance.MainProduct.Code,
                instance.MainProduct.Name))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<IReadOnlyList<DashboardPeriodOptionViewModel>> GetPeriodsAsync(
        CancellationToken cancellationToken) =>
        (await db.MainProductInstances.AsNoTracking()
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .Select(instance => new { instance.Year, instance.Term })
            .Distinct()
            .ToListAsync(cancellationToken))
        .Select(period => new DashboardPeriodOptionViewModel
        {
            Year = period.Year,
            Term = period.Term
        })
        .OrderByDescending(period => period.Year)
        .ThenByDescending(period => period.Term)
        .ToList();

    private Task<PerformanceFactCacheResult<IReadOnlyList<DashboardPeriodOptionViewModel>>>
        GetPeriodsCachedAsync(
        bool forceRefresh,
        CancellationToken cancellationToken) =>
        factCache.GetOrCreateAsync(
            "period-catalog",
            forceRefresh,
            async token => (IReadOnlyList<DashboardPeriodOptionViewModel>)
                await GetPeriodsAsync(token),
            cancellationToken);

    private DashboardPeriodOptionViewModel PickDefaultPeriod(
        IReadOnlyCollection<DashboardPeriodOptionViewModel> periods) =>
        periods.Where(period => IsPeriodClosed(period.Year, period.Term))
            .OrderByDescending(period => period.Year)
            .ThenByDescending(period => period.Term)
            .FirstOrDefault()
        ?? periods.OrderByDescending(period => period.Year)
            .ThenByDescending(period => period.Term)
            .FirstOrDefault()
        ?? new DashboardPeriodOptionViewModel
        {
            Year = Today.Year,
            Term = Today.Month <= 6 ? 1 : 2
        };

    private bool IsPeriodClosed(int year, int term)
    {
        var lastMonth = calculator.GetTermMonths(term)[^1];
        return Today > new DateOnly(
            year, lastMonth, DateTime.DaysInMonth(year, lastMonth));
    }

    private static bool IsEffective(IEffectivePeriod item, PeriodKey period)
    {
        var value = period.Year * 10 + period.Term;
        var start = item.EffectiveFromYear * 10 + item.EffectiveFromTerm;
        var end = item.EffectiveToYear.HasValue && item.EffectiveToTerm.HasValue
            ? item.EffectiveToYear.Value * 10 + item.EffectiveToTerm.Value
            : int.MaxValue;
        return value >= start && value <= end;
    }

    private static FactSections SectionsFor(DetailSection section) => section switch
    {
        DetailSection.Header => FactSections.Metadata,
        DetailSection.Contributions =>
            FactSections.Metadata | FactSections.SubProducts | FactSections.Metrics,
        _ => FactSections.All
    };

    private static bool IsRankable(PortfolioProductRecord record) =>
        record.HasParameterConfiguration
        && record.HasCompleteTargetData
        && record.SubProducts.Count > 0
        && record.Result.HasCompleteBatchData
        && record.Result.TotalScore.HasValue;

    private static bool IsRankable(BranchProductRecord record) =>
        record.HasParameterConfiguration
        && record.HasCompleteTargetData
        && record.HasSubProductConfiguration
        && record.Result.HasCompleteBatchData
        && record.Result.TotalScore.HasValue;

    private static decimal Aggregate(
        IEnumerable<decimal> values,
        MainProductCalculationType type)
    {
        var list = values.ToList();
        return list.Count == 0
            ? 0
            : type == MainProductCalculationType.Average
                ? list.Average()
                : list.Sum();
    }

    private DateOnly Today =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private readonly record struct PeriodKey(int Year, int Term);
    private sealed record GroupFact(int Id, string GroupNo, string Name);
    private sealed record BranchFact(
        int Id,
        int GroupId,
        string BranchCode,
        string BranchName,
        string GroupNo,
        string GroupName,
        GroupType GroupType);
    private sealed record PortfolioFact(
        int Id,
        int BranchId,
        int GroupId,
        int ProductGamutId,
        int PortfolioTypeId,
        string Code,
        string Name,
        string ProductGamutCode,
        string ProductGamutName,
        string PortfolioTypeCode,
        string PortfolioTypeName);
    private interface IEffectivePeriod
    {
        int EffectiveFromYear { get; }
        int EffectiveFromTerm { get; }
        int? EffectiveToYear { get; }
        int? EffectiveToTerm { get; }
    }
    private sealed record AssignmentFact(
        int ProductGamutId,
        int MainProductId,
        int EffectiveFromYear,
        int EffectiveFromTerm,
        int? EffectiveToYear,
        int? EffectiveToTerm) : IEffectivePeriod;
    private sealed record ExclusionFact(
        int BranchId,
        int MainProductId,
        int EffectiveFromYear,
        int EffectiveFromTerm,
        int? EffectiveToYear,
        int? EffectiveToTerm) : IEffectivePeriod;
    private sealed record MainProductInstanceFact(
        int Id,
        int MainProductId,
        int Year,
        int Term,
        string ProductCode,
        string ProductName);
    private sealed record SubProductFact(
        int MainProductInstanceId,
        int SubProductId,
        string Code,
        string Name);
    private sealed record ParameterFact(
        int Id,
        int GroupId,
        int MainProductInstanceId,
        MainProductCalculationType CalculationType,
        decimal CriterionScore)
    {
        public static ParameterFact Missing(int groupId, int instanceId) =>
            new(0, groupId, instanceId, MainProductCalculationType.Cumulative, 0);
    }
    private sealed record TargetFact(
        int PortfolioId,
        int MainProductParameterId,
        int Month,
        decimal TargetValue);
    private sealed record MetricFact(
        int PortfolioId,
        int SubProductId,
        int Year,
        int Term,
        int Month,
        decimal? ActualValue,
        DateOnly? ActualAsOfDate);
    private sealed record ProductGamutFact(int Id, string Code, string Name);
    private sealed record PortfolioTypeFact(int Id, string Code, string Name);
    private sealed record InstanceDescriptor(
        int MainProductId,
        int Year,
        int Term,
        string ProductCode,
        string ProductName);
    private sealed record PerformanceFactSet(
        IReadOnlyList<BranchFact> Branches,
        IReadOnlyList<PortfolioFact> Portfolios,
        IReadOnlyList<AssignmentFact> Assignments,
        IReadOnlyList<ExclusionFact> Exclusions,
        IReadOnlyList<MainProductInstanceFact> Instances,
        IReadOnlyList<SubProductFact> SubProducts,
        IReadOnlyList<ParameterFact> Parameters,
        IReadOnlyList<TargetFact> Targets,
        IReadOnlyList<MetricFact> Metrics,
        double DatabaseMilliseconds)
    {
        public static PerformanceFactSet Empty { get; } =
            new([], [], [], [], [], [], [], [], [], 0);
        public int FactCount => Branches.Count
            + Portfolios.Count
            + Assignments.Count
            + Exclusions.Count
            + Instances.Count
            + SubProducts.Count
            + Parameters.Count
            + Targets.Count
            + Metrics.Count;
    }
    private sealed record PortfolioProductRecord(
        PortfolioFact Portfolio,
        BranchFact Branch,
        ProductGamutFact Gamut,
        PortfolioTypeFact PortfolioType,
        MainProductInstanceFact Instance,
        ParameterFact Parameter,
        IReadOnlyList<SubProductFact> SubProducts,
        bool HasParameterConfiguration,
        bool HasCompleteTargetData,
        MainProductPeriodCalculationResult Result,
        IReadOnlyList<MainProductMonthlyValue> Months,
        IReadOnlyDictionary<
            (int PortfolioId, int SubProductId, int Year, int Term, int Month),
            MetricFact> MetricLookup);
    private sealed record BranchProductRecord(
        BranchFact Branch,
        MainProductInstanceFact Instance,
        ParameterFact Parameter,
        IReadOnlyList<PortfolioProductRecord> PortfolioRecords,
        bool HasParameterConfiguration,
        bool HasCompleteTargetData,
        bool HasSubProductConfiguration,
        MainProductPeriodCalculationResult Result,
        IReadOnlyList<MainProductMonthlyValue> Months)
    {
        public int PortfolioCount =>
            PortfolioRecords.Select(record => record.Portfolio.Id).Distinct().Count();
    }

    [Flags]
    private enum FactSections
    {
        Metadata = 0,
        SubProducts = 1,
        Targets = 2,
        Metrics = 4,
        All = SubProducts | Targets | Metrics
    }

    private enum DetailSection
    {
        Header,
        Months,
        Contributions,
        Products,
        All
    }
}
