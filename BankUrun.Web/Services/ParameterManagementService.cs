using System.Globalization;
using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class ParameterManagementService(
    AppDbContext db,
    TimeProvider timeProvider,
    IMainProductPeriodCalculator calculator) : IParameterManagementService
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CompareInfo TurkishCompare = TurkishCulture.CompareInfo;
    private static readonly string[] TurkishMonthNames =
    [
        "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];
    private static readonly int[] AllowedPageSizes = [5, 10, 25, 50];

    public async Task<ParameterIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var branches = await GetBranchesAsync(cancellationToken);
        var years = await GetYearsAsync(cancellationToken);
        var today = Today;
        var selectedYear = ResolveYear(years, today.Year);
        var selectedTerm = await ResolveTermAsync(selectedYear, today, cancellationToken);
        var selectedBranchId = branches.FirstOrDefault()?.Id ?? 0;

        var page = await GetPageAsync(new ParameterQuery
        {
            BranchId = selectedBranchId,
            Year = selectedYear,
            Term = selectedTerm,
            Page = 1,
            PageSize = 10
        }, cancellationToken);

        return new ParameterIndexViewModel
        {
            Branches = branches,
            Years = years,
            SelectedBranchId = selectedBranchId,
            SelectedYear = selectedYear,
            SelectedTerm = selectedTerm,
            BatchDate = today,
            Page = page
        };
    }

    public async Task<ParameterPageViewModel> GetPageAsync(ParameterQuery query, CancellationToken cancellationToken = default)
    {
        var branches = await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .OrderBy(branch => branch.BranchCode)
            .ToListAsync(cancellationToken);

        if (branches.Count == 0)
        {
            return EmptyPage(query.PageSize);
        }

        var selectedBranch = branches.FirstOrDefault(branch => branch.Id == query.BranchId) ?? branches[0];
        var years = await GetYearsAsync(cancellationToken);
        var year = ResolveYear(years, query.Year);
        var term = query.Term is 1 or 2 ? query.Term.Value : await ResolveTermAsync(year, Today, cancellationToken);

        var instances = await db.MainProductInstances
            .AsNoTracking()
            .Include(instance => instance.MainProduct)
            .Include(instance => instance.Parameter)
            .Where(instance => instance.Year == year && instance.Term == term)
            .OrderBy(instance => instance.MainProduct.Code)
            .ToListAsync(cancellationToken);

        if (instances.Count == 0)
        {
            return EmptyPage(query.PageSize);
        }

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
        var termMonths = calculator.GetTermMonths(term);
        var records = new List<CalculatedRecord>(branches.Count * instances.Count);

        foreach (var branch in branches)
        {
            foreach (var instance in instances)
            {
                records.Add(new CalculatedRecord(
                    branch,
                    instance,
                    BuildRow(branch.Id, instance, termMonths, metricLookup)));
            }
        }

        ApplyProductRanks(records);
        ApplySegmentRanks(records);

        IEnumerable<ParameterRowViewModel> filtered = records
            .Where(record => record.Branch.Id == selectedBranch.Id)
            .Select(record => record.Row);

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(row => ContainsTurkish(
                $"{row.MainProductCode} {row.MainProductName}", search));
        }

        if (query.CalculationType.HasValue)
        {
            filtered = filtered.Where(row => row.CalculationType == query.CalculationType.Value);
        }

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = SortRows(filtered, query.SortKey, descending).ToList();
        var pageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 10;
        var totalCount = ordered.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (decimal)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);

        return new ParameterPageViewModel
        {
            Rows = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task UpsertParameterAsync(MainProductParameterInput input, string actor, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(input.CalculationType) || input.CriterionScore < 0)
        {
            throw new InvalidOperationException("Hesaplama türü ve kriter puanını kontrol edin.");
        }

        var instance = await db.MainProductInstances
            .Include(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.MainProductInstanceId, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün dönem kaydı bulunamadı.");
        _ = calculator.GetTermMonths(instance.Term);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        MainProductParameter parameter;

        if (input.Id == 0)
        {
            if (await db.MainProductParameters.AnyAsync(
                item => item.MainProductInstanceId == input.MainProductInstanceId,
                cancellationToken))
            {
                throw new InvalidOperationException("Bu ana ürün ve dönem için parametre zaten var.");
            }

            parameter = new MainProductParameter
            {
                MainProductInstanceId = input.MainProductInstanceId,
                CalculationType = input.CalculationType,
                CriterionScore = Round(input.CriterionScore),
                IsActive = input.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.MainProductParameters.Add(parameter);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateMainProductParameter", parameter.Id, $"{instance.Year}/{instance.Term} {instance.MainProduct.Code} parametresi oluşturuldu.", actor, now);
        }
        else
        {
            parameter = await db.MainProductParameters
                .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Parametre bulunamadı.");

            if (parameter.MainProductInstanceId != input.MainProductInstanceId)
            {
                throw new InvalidOperationException("Parametre farklı bir ana ürün dönemine taşınamaz.");
            }

            parameter.CalculationType = input.CalculationType;
            parameter.CriterionScore = Round(input.CriterionScore);
            parameter.IsActive = input.IsActive;
            parameter.UpdatedAt = now;
            AddAudit("UpdateMainProductParameter", parameter.Id, $"{instance.Year}/{instance.Term} {instance.MainProduct.Code} parametresi güncellendi.", actor, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateTargetsAsync(MonthlyTargetsInput input, string actor, CancellationToken cancellationToken = default)
    {
        var parameter = await db.MainProductParameters
            .Include(item => item.MainProductInstance)
                .ThenInclude(instance => instance.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.ParameterId, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");
        var branch = await db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");

        var termMonths = calculator.GetTermMonths(parameter.MainProductInstance.Term);
        var suppliedMonths = input.Months.Select(item => item.Month).ToList();
        if (input.Months.Count != termMonths.Count
            || suppliedMonths.Distinct().Count() != termMonths.Count
            || suppliedMonths.Except(termMonths).Any()
            || input.Months.Any(item => item.TargetValue < 0))
        {
            throw new InvalidOperationException("Dönemin altı aylık hedeflerini eksiksiz ve geçerli girin.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var stored = await db.BranchMainProductMonthlyMetrics
            .Where(item => item.BranchId == input.BranchId && item.MainProductParameterId == input.ParameterId)
            .ToDictionaryAsync(item => item.Month, cancellationToken);

        foreach (var monthInput in input.Months)
        {
            if (stored.TryGetValue(monthInput.Month, out var metric))
            {
                metric.TargetValue = Round(monthInput.TargetValue);
                metric.UpdatedAt = now;
                continue;
            }

            db.BranchMainProductMonthlyMetrics.Add(new BranchMainProductMonthlyMetric
            {
                BranchId = input.BranchId,
                MainProductParameterId = input.ParameterId,
                Month = monthInput.Month,
                TargetValue = Round(monthInput.TargetValue),
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        db.AuditLogs.Add(new AuditLog
        {
            Action = "UpdateMainProductTargets",
            EntityName = "BranchMainProductMonthlyMetric",
            EntityKey = $"{input.BranchId}:{input.ParameterId}",
            Description = $"{branch.BranchCode} şubesi için {parameter.MainProductInstance.MainProduct.Code} aylık hedefleri güncellendi.",
            Actor = actor,
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteParameterAsync(ParameterIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        var parameter = await db.MainProductParameters
            .Include(item => item.MainProductInstance)
                .ThenInclude(instance => instance.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        AddAudit(
            "DeleteMainProductParameter",
            parameter.Id,
            $"{parameter.MainProductInstance.Year}/{parameter.MainProductInstance.Term} {parameter.MainProductInstance.MainProduct.Code} parametresi silindi.",
            actor,
            now);
        db.MainProductParameters.Remove(parameter);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private ParameterRowViewModel BuildRow(
        int branchId,
        MainProductInstance instance,
        IReadOnlyCollection<int> termMonths,
        IReadOnlyDictionary<(int BranchId, int ParameterId, int Month), BranchMainProductMonthlyMetric> metricLookup)
    {
        var parameter = instance.Parameter;
        var monthRows = termMonths.Select(month =>
        {
            BranchMainProductMonthlyMetric? metric = null;
            if (parameter is not null)
            {
                metricLookup.TryGetValue((branchId, parameter.Id, month), out metric);
            }

            return new ParameterMonthlyMetricViewModel
            {
                Month = month,
                MonthName = TurkishMonthNames[month],
                TargetValue = metric?.TargetValue ?? 0,
                ActualValue = metric?.ActualValue,
                ActualAsOfDate = metric?.ActualAsOfDate,
                IsBatchExpected = false,
                IsIncludedInCalculation = false
            };
        }).ToList();
        var calculation = parameter is null
            ? null
            : calculator.Calculate(new MainProductPeriodCalculationInput(
                instance.Year,
                instance.Term,
                Today,
                parameter.CalculationType,
                parameter.CriterionScore,
                monthRows.Select(month => new MainProductMonthlyValue(
                    month.Month,
                    month.TargetValue,
                    month.ActualValue,
                    month.ActualAsOfDate)).ToList()));
        var expectedMonths = calculation?.ExpectedMonths
            ?? calculator.GetExpectedMonths(instance.Year, instance.Term, Today);
        foreach (var month in monthRows)
        {
            month.IsBatchExpected = expectedMonths.Contains(month.Month);
            month.IsIncludedInCalculation = month.IsBatchExpected
                && month.ActualValue.HasValue
                && month.ActualAsOfDate.HasValue
                && month.ActualAsOfDate.Value <= Today;
        }

        return new ParameterRowViewModel
        {
            MainProductInstanceId = instance.Id,
            ParameterId = parameter?.Id,
            BranchId = branchId,
            Year = instance.Year,
            Term = instance.Term,
            MainProductCode = instance.MainProduct.Code,
            MainProductName = instance.MainProduct.Name,
            CalculationType = parameter?.CalculationType,
            CriterionScore = parameter?.CriterionScore,
            IsActive = parameter?.IsActive ?? false,
            PeriodTarget = calculation?.TargetValue ?? 0,
            PeriodActual = calculation?.ActualValue,
            HgRatio = calculation?.HgRatioPercent,
            HgoScore = calculation?.HgoScore,
            TotalScore = calculation?.TotalScore,
            HasCompleteBatchData = calculation?.HasCompleteBatchData ?? false,
            Months = monthRows
        };
    }

    private static void ApplyProductRanks(IEnumerable<CalculatedRecord> records)
    {
        foreach (var branchGroup in records.GroupBy(record => record.Branch.Id))
        {
            ApplyDenseRank(
                branchGroup.Where(IsRankable).OrderByDescending(record => record.Row.TotalScore),
                record => record.Row.ProductRank = record.Rank);
        }
    }

    private static void ApplySegmentRanks(IEnumerable<CalculatedRecord> records)
    {
        foreach (var segmentGroup in records.GroupBy(record => new
                 {
                     record.Instance.Id,
                     record.Branch.Group.GroupSegment
                 }))
        {
            ApplyDenseRank(
                segmentGroup.Where(IsRankable).OrderByDescending(record => record.Row.TotalScore),
                record => record.Row.SegmentRank = record.Rank);
        }
    }

    private static void ApplyDenseRank(
        IEnumerable<CalculatedRecord> orderedRecords,
        Action<RankedRecord> applyRank)
    {
        var records = orderedRecords.ToList();
        var ranks = DenseRankCalculator.Calculate(
            records.Select(record => record.Row.TotalScore).ToList());

        for (var index = 0; index < records.Count; index++)
        {
            applyRank(new RankedRecord(records[index], ranks[index]!.Value));
        }
    }

    private static bool IsRankable(CalculatedRecord record) =>
        record.Row.IsActive && record.Row.HasCompleteBatchData && record.Row.TotalScore.HasValue;

    private static IOrderedEnumerable<ParameterRowViewModel> SortRows(
        IEnumerable<ParameterRowViewModel> rows,
        string? sortKey,
        bool descending)
    {
        var key = sortKey?.Trim().ToLowerInvariant();
        return (key, descending) switch
        {
            ("calculation", false) => rows.OrderBy(row => row.CalculationType?.ToString() ?? "", StringComparer.Create(TurkishCulture, true)).ThenBy(row => row.MainProductCode),
            ("calculation", true) => rows.OrderByDescending(row => row.CalculationType?.ToString() ?? "", StringComparer.Create(TurkishCulture, true)).ThenBy(row => row.MainProductCode),
            ("criterion", false) => OrderNullable(rows, row => row.CriterionScore, false),
            ("criterion", true) => OrderNullable(rows, row => row.CriterionScore, true),
            ("target", false) => rows.OrderBy(row => row.PeriodTarget).ThenBy(row => row.MainProductCode),
            ("target", true) => rows.OrderByDescending(row => row.PeriodTarget).ThenBy(row => row.MainProductCode),
            ("actual", false) => OrderNullable(rows, row => row.PeriodActual, false),
            ("actual", true) => OrderNullable(rows, row => row.PeriodActual, true),
            ("ratio", false) => OrderNullable(rows, row => row.HgRatio, false),
            ("ratio", true) => OrderNullable(rows, row => row.HgRatio, true),
            ("hgo", false) => OrderNullable(rows, row => row.HgoScore, false),
            ("hgo", true) => OrderNullable(rows, row => row.HgoScore, true),
            ("total", false) => OrderNullable(rows, row => row.TotalScore, false),
            ("total", true) => OrderNullable(rows, row => row.TotalScore, true),
            ("productrank", false) => OrderNullable(rows, row => row.ProductRank, false),
            ("productrank", true) => OrderNullable(rows, row => row.ProductRank, true),
            ("segmentrank", false) => OrderNullable(rows, row => row.SegmentRank, false),
            ("segmentrank", true) => OrderNullable(rows, row => row.SegmentRank, true),
            ("product", true) => rows.OrderByDescending(row => row.MainProductCode, StringComparer.Create(TurkishCulture, true)).ThenByDescending(row => row.MainProductName, StringComparer.Create(TurkishCulture, true)),
            _ => rows.OrderBy(row => row.MainProductCode, StringComparer.Create(TurkishCulture, true)).ThenBy(row => row.MainProductName, StringComparer.Create(TurkishCulture, true))
        };
    }

    private static IOrderedEnumerable<ParameterRowViewModel> OrderNullable<T>(
        IEnumerable<ParameterRowViewModel> rows,
        Func<ParameterRowViewModel, T?> selector,
        bool descending) where T : struct, IComparable<T>
    {
        return descending
            ? rows.OrderByDescending(row => selector(row).HasValue).ThenByDescending(selector).ThenBy(row => row.MainProductCode)
            : rows.OrderByDescending(row => selector(row).HasValue).ThenBy(selector).ThenBy(row => row.MainProductCode);
    }

    private async Task<IReadOnlyList<ParameterBranchOptionViewModel>> GetBranchesAsync(CancellationToken cancellationToken)
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

    private async Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken cancellationToken)
    {
        return await db.MainProductInstances
            .AsNoTracking()
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .Select(instance => instance.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync(cancellationToken);
    }

    private async Task<int> ResolveTermAsync(int year, DateOnly today, CancellationToken cancellationToken)
    {
        var terms = await db.MainProductInstances
            .AsNoTracking()
            .Where(instance => instance.Year == year && (instance.Term == 1 || instance.Term == 2))
            .Select(instance => instance.Term)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (terms.Count == 0)
        {
            return today.Month <= 6 ? 1 : 2;
        }

        var currentTerm = today.Month <= 6 ? 1 : 2;
        return terms.Contains(currentTerm) ? currentTerm : terms.Max();
    }

    private static int ResolveYear(IReadOnlyList<int> years, int? requestedYear)
    {
        if (requestedYear.HasValue && years.Contains(requestedYear.Value))
        {
            return requestedYear.Value;
        }

        return years.Count > 0 ? years[0] : requestedYear ?? DateTime.Today.Year;
    }

    private static bool ContainsTurkish(string source, string search) =>
        TurkishCompare.IndexOf(source, search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private ParameterPageViewModel EmptyPage(int requestedPageSize)
    {
        return new ParameterPageViewModel
        {
            PageSize = AllowedPageSizes.Contains(requestedPageSize) ? requestedPageSize : 10,
            Page = 1,
            TotalPages = 1
        };
    }

    private void AddAudit(string action, int id, string description, string actor, DateTimeOffset now)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = "MainProductParameter",
            EntityKey = id.ToString(CultureInfo.InvariantCulture),
            Description = description,
            Actor = actor,
            CreatedAt = now
        });
    }

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private sealed record CalculatedRecord(Branch Branch, MainProductInstance Instance, ParameterRowViewModel Row);
    private sealed record RankedRecord(CalculatedRecord Record, int Rank)
    {
        public ParameterRowViewModel Row => Record.Row;
    }
}
