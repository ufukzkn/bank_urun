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
    private static readonly PerformanceSegment[] Segments = Enum.GetValues<PerformanceSegment>();

    public async Task<ParameterIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.GroupDefinitions
            .AsNoTracking()
            .OrderBy(group => group.GroupNo)
            .Select(group => new ParameterGroupOptionViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name,
                GroupSegment = group.GroupSegment
            })
            .ToListAsync(cancellationToken);
        var products = await db.MainProductInstances
            .AsNoTracking()
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

        return new ParameterIndexViewModel
        {
            Groups = groups,
            Products = products,
            Years = products.Select(product => product.Year).Distinct().OrderByDescending(year => year).ToList(),
            Page = await GetPageAsync(new ParameterQuery(), cancellationToken),
            SubProductPage = await GetSubProductPageAsync(new SubProductTargetQuery(), cancellationToken)
        };
    }

    public async Task<ParameterPageViewModel> GetPageAsync(ParameterQuery query, CancellationToken cancellationToken = default)
    {
        var parameters = await db.MainProductParameters
            .AsNoTracking()
            .Include(parameter => parameter.Group)
            .Include(parameter => parameter.MainProductInstance)
                .ThenInclude(instance => instance.MainProduct)
            .Include(parameter => parameter.SegmentRules)
            .ToListAsync(cancellationToken);

        IEnumerable<MainProductParameter> filtered = parameters;
        if (query.GroupId.HasValue)
        {
            filtered = filtered.Where(parameter => parameter.GroupId == query.GroupId.Value);
        }
        if (query.Year.HasValue)
        {
            filtered = filtered.Where(parameter => parameter.MainProductInstance.Year == query.Year.Value);
        }
        if (query.Term is 1 or 2)
        {
            filtered = filtered.Where(parameter => parameter.MainProductInstance.Term == query.Term.Value);
        }
        if (query.CalculationType.HasValue)
        {
            filtered = filtered.Where(parameter => parameter.CalculationType == query.CalculationType.Value);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(parameter => ContainsTurkish(
                $"{parameter.Group.GroupNo} {parameter.Group.Name} {parameter.MainProductInstance.MainProduct.Code} {parameter.MainProductInstance.MainProduct.Name}",
                search));
        }

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = SortParameters(filtered, query.SortKey, descending).ToList();
        var pageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 10;
        var totalCount = ordered.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (decimal)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);
        var selected = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var groupIds = selected.Select(parameter => parameter.GroupId).Distinct().ToList();
        var branches = await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .Where(branch => groupIds.Contains(branch.GroupId))
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

        var rows = new List<ParameterRowViewModel>(selected.Count);
        foreach (var parameter in selected)
        {
            var parameterBranches = branches.Where(branch => branch.GroupId == parameter.GroupId).ToList();
            rows.Add(new ParameterRowViewModel
            {
                ParameterId = parameter.Id,
                GroupId = parameter.GroupId,
                MainProductInstanceId = parameter.MainProductInstanceId,
                Year = parameter.MainProductInstance.Year,
                Term = parameter.MainProductInstance.Term,
                GroupNo = parameter.Group.GroupNo,
                GroupName = parameter.Group.Name,
                GroupSegment = parameter.Group.GroupSegment,
                MainProductCode = parameter.MainProductInstance.MainProduct.Code,
                MainProductName = parameter.MainProductInstance.MainProduct.Name,
                CalculationType = parameter.CalculationType,
                CriterionScore = parameter.CriterionScore,
                IsActive = parameter.IsActive,
                Rules = parameter.SegmentRules.OrderBy(rule => rule.SortOrder).Select(ToRuleViewModel).ToList(),
                Branches = parameterBranches
            });
        }

        return new ParameterPageViewModel
        {
            Rows = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<ParameterTargetEditorViewModel> GetTargetEditorAsync(
        int parameterId,
        int branchId,
        CancellationToken cancellationToken = default)
    {
        var parameter = await db.MainProductParameters
            .AsNoTracking()
            .Include(item => item.MainProductInstance)
            .FirstOrDefaultAsync(item => item.Id == parameterId, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");
        return await BuildTargetEditorAsync(parameter, branchId, cancellationToken);
    }

    public async Task<SubProductTargetPageViewModel> GetSubProductPageAsync(
        SubProductTargetQuery query,
        CancellationToken cancellationToken = default)
    {
        var links = await db.SubProductInstances.AsNoTracking()
            .Include(link => link.SubProduct)
            .Include(link => link.MainProductInstance).ThenInclude(instance => instance.MainProduct)
            .Where(link => link.SubProduct.IsActive)
            .ToListAsync(cancellationToken);
        if (query.GroupId.HasValue)
        {
            var allowedInstances = await db.MainProductParameters.AsNoTracking()
                .Where(parameter => parameter.GroupId == query.GroupId.Value && parameter.IsActive)
                .Select(parameter => parameter.MainProductInstanceId)
                .ToListAsync(cancellationToken);
            links = links.Where(link => allowedInstances.Contains(link.MainProductInstanceId)).ToList();
        }
        if (query.MainProductInstanceId.HasValue)
            links = links.Where(link => link.MainProductInstanceId == query.MainProductInstanceId.Value).ToList();
        if (query.Year.HasValue)
            links = links.Where(link => link.MainProductInstance.Year == query.Year.Value).ToList();
        if (query.Term is 1 or 2)
            links = links.Where(link => link.MainProductInstance.Term == query.Term.Value).ToList();
        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            links = links.Where(link => ContainsTurkish(
                $"{link.SubProduct.Code} {link.SubProduct.Name} {link.MainProductInstance.MainProduct.Code} {link.MainProductInstance.MainProduct.Name}", search)).ToList();
        }

        var grouped = links.GroupBy(link => new
            {
                link.SubProductId,
                link.SubProduct.Code,
                link.SubProduct.Name,
                link.MainProductInstance.Year,
                link.MainProductInstance.Term
            })
            .Select(group => new SubProductTargetRowViewModel
            {
                SubProductId = group.Key.SubProductId,
                SubProductCode = group.Key.Code,
                SubProductName = group.Key.Name,
                Year = group.Key.Year,
                Term = group.Key.Term,
                ParentProducts = group.Select(link => new SubProductParentViewModel
                {
                    MainProductInstanceId = link.MainProductInstanceId,
                    Code = link.MainProductInstance.MainProduct.Code,
                    Name = link.MainProductInstance.MainProduct.Name
                }).DistinctBy(parent => parent.MainProductInstanceId).OrderBy(parent => parent.Code).ToList()
            }).ToList();
        var comparer = StringComparer.Create(TurkishCulture, true);
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        grouped = (query.SortKey?.Trim().ToLowerInvariant(), descending) switch
        {
            ("term", false) => grouped.OrderBy(row => row.Term).ThenByDescending(row => row.Year).ToList(),
            ("term", true) => grouped.OrderByDescending(row => row.Term).ThenByDescending(row => row.Year).ToList(),
            ("subproduct", false) => grouped.OrderBy(row => row.SubProductCode, comparer).ToList(),
            ("subproduct", true) => grouped.OrderByDescending(row => row.SubProductCode, comparer).ToList(),
            ("parent", false) => grouped.OrderBy(row => string.Join(' ', row.ParentProducts.Select(parent => parent.Code)), comparer).ToList(),
            ("parent", true) => grouped.OrderByDescending(row => string.Join(' ', row.ParentProducts.Select(parent => parent.Code)), comparer).ToList(),
            (_, true) => grouped.OrderByDescending(row => row.Year).ThenByDescending(row => row.Term).ThenBy(row => row.SubProductCode, comparer).ToList(),
            _ => grouped.OrderBy(row => row.Year).ThenBy(row => row.Term).ThenBy(row => row.SubProductCode, comparer).ToList()
        };
        var pageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 10;
        var totalCount = grouped.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (decimal)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);
        var selected = grouped.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var branches = await db.Branches.AsNoTracking().Include(branch => branch.Group)
            .Where(branch => !query.GroupId.HasValue || branch.GroupId == query.GroupId.Value)
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
            }).ToListAsync(cancellationToken);
        selected.ForEach(row => row.Branches = branches);
        return new SubProductTargetPageViewModel
        {
            Rows = selected,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<SubProductTargetEditorViewModel> GetSubProductTargetEditorAsync(
        int subProductId, int branchId, int year, int term, CancellationToken cancellationToken = default)
    {
        _ = calculator.GetTermMonths(term);
        var product = await db.ProductDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == subProductId && item.Type == ProductType.Sub, cancellationToken)
            ?? throw new InvalidOperationException("Alt ürün bulunamadı.");
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(item => item.Id == branchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var mapped = await db.SubProductInstances.AsNoTracking()
            .AnyAsync(link => link.SubProductId == subProductId
                && link.MainProductInstance.Year == year && link.MainProductInstance.Term == term, cancellationToken);
        if (!mapped) throw new InvalidOperationException("Alt ürün seçili dönemde bir ana ürüne bağlı değil.");
        var metrics = await db.BranchSubProductMonthlyMetrics.AsNoTracking()
            .Where(item => item.BranchId == branchId && item.SubProductId == subProductId && item.Year == year && item.Term == term)
            .ToDictionaryAsync(item => item.Month, cancellationToken);
        return new SubProductTargetEditorViewModel
        {
            SubProductId = product.Id,
            BranchId = branch.Id,
            Year = year,
            Term = term,
            SubProductCode = product.Code,
            SubProductName = product.Name,
            BranchLabel = $"{branch.BranchCode} - {branch.Name}",
            Months = calculator.GetTermMonths(term).Select(month =>
            {
                metrics.TryGetValue(month, out var metric);
                return new ParameterMonthlyMetricViewModel
                {
                    Month = month,
                    MonthName = TurkishMonthNames[month],
                    TargetValue = metric?.TargetValue ?? 0,
                    ActualValue = metric?.ActualValue,
                    ActualAsOfDate = metric?.ActualAsOfDate
                };
            }).ToList()
        };
    }

    public async Task UpdateSubProductTargetsAsync(
        SubProductMonthlyTargetsInput input, string actor, CancellationToken cancellationToken = default)
    {
        var termMonths = calculator.GetTermMonths(input.Term);
        if (input.Months.Count != termMonths.Count
            || input.Months.Select(month => month.Month).Distinct().Count() != termMonths.Count
            || input.Months.Select(month => month.Month).Except(termMonths).Any()
            || input.Months.Any(month => month.TargetValue < 0))
            throw new InvalidOperationException("Dönemin altı aylık hedeflerini eksiksiz ve geçerli girin.");
        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.SubProductId && item.Type == ProductType.Sub, cancellationToken)
            ?? throw new InvalidOperationException("Alt ürün bulunamadı.");
        var mapped = await db.SubProductInstances.AsNoTracking().AnyAsync(link =>
            link.SubProductId == input.SubProductId && link.MainProductInstance.Year == input.Year
            && link.MainProductInstance.Term == input.Term, cancellationToken);
        if (!mapped) throw new InvalidOperationException("Alt ürün seçili dönemde bir ana ürüne bağlı değil.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var stored = await db.BranchSubProductMonthlyMetrics
            .Where(item => item.BranchId == input.BranchId && item.SubProductId == input.SubProductId
                && item.Year == input.Year && item.Term == input.Term)
            .ToDictionaryAsync(item => item.Month, cancellationToken);
        foreach (var monthInput in input.Months)
        {
            if (stored.TryGetValue(monthInput.Month, out var metric))
            {
                metric.TargetValue = Round(monthInput.TargetValue);
                metric.UpdatedAt = now;
            }
            else
            {
                db.BranchSubProductMonthlyMetrics.Add(new BranchSubProductMonthlyMetric
                {
                    BranchId = input.BranchId,
                    SubProductId = input.SubProductId,
                    Year = input.Year,
                    Term = input.Term,
                    Month = monthInput.Month,
                    TargetValue = Round(monthInput.TargetValue),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
        db.AuditLogs.Add(new AuditLog
        {
            Action = "UpdateSubProductTargets",
            EntityName = "BranchSubProductMonthlyMetric",
            EntityKey = $"{input.BranchId}:{input.SubProductId}:{input.Year}:{input.Term}",
            Description = $"{branch.BranchCode} şubesi için {product.Code} alt ürün hedefleri güncellendi.",
            Actor = actor,
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpsertParameterAsync(MainProductParameterInput input, string actor, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(input.CalculationType) || input.CriterionScore < 0)
        {
            throw new InvalidOperationException("Hesaplama türü ve kriter puanını kontrol edin.");
        }

        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
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
            if (await db.MainProductParameters.AnyAsync(item =>
                    item.GroupId == input.GroupId && item.MainProductInstanceId == input.MainProductInstanceId,
                cancellationToken))
            {
                throw new InvalidOperationException("Bu grup, ana ürün ve dönem için parametre zaten var.");
            }

            parameter = new MainProductParameter
            {
                GroupId = input.GroupId,
                MainProductInstanceId = input.MainProductInstanceId,
                CalculationType = input.CalculationType,
                CriterionScore = Round(input.CriterionScore),
                IsActive = input.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            parameter.SegmentRules = CreateDefaultRules(parameter.CriterionScore, now);
            db.MainProductParameters.Add(parameter);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateMainProductParameter", parameter.Id, $"{group.GroupNo} · {instance.Year}/{instance.Term} {instance.MainProduct.Code} parametresi oluşturuldu.", actor, now);
        }
        else
        {
            parameter = await db.MainProductParameters.Include(item => item.SegmentRules)
                .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Parametre bulunamadı.");
            if (parameter.GroupId != input.GroupId || parameter.MainProductInstanceId != input.MainProductInstanceId)
            {
                throw new InvalidOperationException("Parametre farklı grup veya ana ürün dönemine taşınamaz.");
            }
            ValidateRules(input.CriterionScore, input.Rules);
            var existingRuleIds = parameter.SegmentRules.Select(rule => rule.Id).Order().ToArray();
            var submittedRuleIds = input.Rules.Select(rule => rule.Id).Order().ToArray();
            if (!existingRuleIds.SequenceEqual(submittedRuleIds))
            {
                throw new InvalidOperationException("Segment kural seti değiştirilemez.");
            }

            parameter.CalculationType = input.CalculationType;
            parameter.CriterionScore = Round(input.CriterionScore);
            parameter.IsActive = input.IsActive;
            parameter.UpdatedAt = now;
            foreach (var ruleInput in input.Rules)
            {
                var rule = parameter.SegmentRules.Single(item => item.Id == ruleInput.Id);
                rule.SortOrder = ruleInput.SortOrder;
                rule.TargetShare = ToRatio(ruleInput.TargetShare);
                rule.SizeShare = ToRatio(ruleInput.SizeShare);
                rule.ScaleShare = ToRatio(ruleInput.ScaleShare);
                rule.AllocatedScore = Round(ruleInput.AllocatedScore);
                rule.HgoWeight = ToRatio(ruleInput.HgoWeight);
                rule.DevelopmentWeight = ToRatio(ruleInput.DevelopmentWeight);
                rule.SizeWeight = ToRatio(ruleInput.SizeWeight);
                rule.UpdatedAt = now;
            }
            AddAudit("UpdateMainProductParameter", parameter.Id, $"{group.GroupNo} · {instance.Year}/{instance.Term} {instance.MainProduct.Code} parametresi güncellendi.", actor, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateTargetsAsync(MonthlyTargetsInput input, string actor, CancellationToken cancellationToken = default)
    {
        var parameter = await db.MainProductParameters
            .Include(item => item.MainProductInstance).ThenInclude(instance => instance.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.ParameterId, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");
        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        if (branch.GroupId != parameter.GroupId)
        {
            throw new InvalidOperationException("Şube ile parametre aynı gruba ait olmalıdır.");
        }

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
            }
            else
            {
                db.BranchMainProductMonthlyMetrics.Add(new BranchMainProductMonthlyMetric
                {
                    GroupId = parameter.GroupId,
                    BranchId = input.BranchId,
                    MainProductParameterId = input.ParameterId,
                    Month = monthInput.Month,
                    TargetValue = Round(monthInput.TargetValue),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
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
            .Include(item => item.Group)
            .Include(item => item.MainProductInstance).ThenInclude(instance => instance.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");
        var now = timeProvider.GetUtcNow();
        AddAudit("DeleteMainProductParameter", parameter.Id,
            $"{parameter.Group.GroupNo} · {parameter.MainProductInstance.Year}/{parameter.MainProductInstance.Term} {parameter.MainProductInstance.MainProduct.Code} parametresi silindi.", actor, now);
        db.MainProductParameters.Remove(parameter);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ParameterTargetEditorViewModel> BuildTargetEditorAsync(
        MainProductParameter parameter,
        int branchId,
        CancellationToken cancellationToken)
    {
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(item => item.Id == branchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        if (branch.GroupId != parameter.GroupId)
        {
            throw new InvalidOperationException("Şube ile parametre aynı gruba ait olmalıdır.");
        }
        var metrics = await db.BranchMainProductMonthlyMetrics
            .AsNoTracking()
            .Where(item => item.BranchId == branchId && item.MainProductParameterId == parameter.Id)
            .ToDictionaryAsync(item => item.Month, cancellationToken);
        var months = calculator.GetTermMonths(parameter.MainProductInstance.Term)
            .Select(month =>
            {
                metrics.TryGetValue(month, out var metric);
                return new ParameterMonthlyMetricViewModel
                {
                    Month = month,
                    MonthName = TurkishMonthNames[month],
                    TargetValue = metric?.TargetValue ?? 0,
                    ActualValue = metric?.ActualValue,
                    ActualAsOfDate = metric?.ActualAsOfDate
                };
            }).ToList();
        return new ParameterTargetEditorViewModel
        {
            ParameterId = parameter.Id,
            BranchId = branch.Id,
            BranchLabel = $"{branch.BranchCode} - {branch.Name}",
            Months = months
        };
    }

    private static IOrderedEnumerable<MainProductParameter> SortParameters(
        IEnumerable<MainProductParameter> parameters,
        string? sortKey,
        bool descending)
    {
        var comparer = StringComparer.Create(TurkishCulture, true);
        Func<MainProductParameter, object> fallback = item => item.MainProductInstance.Year;
        return (sortKey?.Trim().ToLowerInvariant(), descending) switch
        {
            ("year", false) => parameters.OrderBy(item => item.MainProductInstance.Year).ThenBy(item => item.MainProductInstance.Term),
            ("term", false) => parameters.OrderBy(item => item.MainProductInstance.Term).ThenByDescending(item => item.MainProductInstance.Year),
            ("term", true) => parameters.OrderByDescending(item => item.MainProductInstance.Term).ThenByDescending(item => item.MainProductInstance.Year),
            ("group", false) => parameters.OrderBy(item => item.Group.GroupNo, comparer),
            ("group", true) => parameters.OrderByDescending(item => item.Group.GroupNo, comparer),
            ("product", false) => parameters.OrderBy(item => item.MainProductInstance.MainProduct.Code, comparer),
            ("product", true) => parameters.OrderByDescending(item => item.MainProductInstance.MainProduct.Code, comparer),
            ("calculation", false) => parameters.OrderBy(item => item.CalculationType.ToString(), comparer),
            ("calculation", true) => parameters.OrderByDescending(item => item.CalculationType.ToString(), comparer),
            ("criterion", false) => parameters.OrderBy(item => item.CriterionScore),
            ("criterion", true) => parameters.OrderByDescending(item => item.CriterionScore),
            ("active", false) => parameters.OrderBy(item => item.IsActive),
            ("active", true) => parameters.OrderByDescending(item => item.IsActive),
            (_, true) => parameters.OrderByDescending(fallback).ThenByDescending(item => item.MainProductInstance.Term),
            _ => parameters.OrderBy(fallback).ThenBy(item => item.MainProductInstance.Term)
        };
    }

    private static bool ContainsTurkish(string source, string search) =>
        TurkishCompare.IndexOf(source, search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal ToRatio(decimal percent) => percent / 100;

    private static List<MainProductSegmentRule> CreateDefaultRules(decimal totalScore, DateTimeOffset now)
    {
        decimal[] allocationShares = [0.25m, 0.25m, 0.20m, 0.20m, 0.10m];
        var rules = new List<MainProductSegmentRule>(Segments.Length);
        decimal allocated = 0;
        for (var index = 0; index < Segments.Length; index++)
        {
            var score = index == Segments.Length - 1
                ? totalScore - allocated
                : Round(totalScore * allocationShares[index]);
            allocated += score;
            rules.Add(new MainProductSegmentRule
            {
                PerformanceSegment = Segments[index],
                SortOrder = index + 1,
                TargetShare = allocationShares[index],
                SizeShare = 0.20m,
                ScaleShare = 0,
                AllocatedScore = score,
                HgoWeight = 0.70m,
                DevelopmentWeight = 0.15m,
                SizeWeight = 0.15m,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        return rules;
    }

    private static void ValidateRules(decimal totalScore, IReadOnlyCollection<MainProductSegmentRuleInput> rules)
    {
        if (rules.Count != Segments.Length
            || rules.Select(rule => rule.PerformanceSegment).Distinct().Count() != Segments.Length
            || rules.Any(rule => !Segments.Contains(rule.PerformanceSegment)))
        {
            throw new InvalidOperationException("Her performans segmenti için tek bir kural bulunmalı.");
        }
        if (rules.Any(rule => rule.TargetShare is < 0 or > 100
            || rule.SizeShare is < 0 or > 100
            || rule.ScaleShare is < 0 or > 100
            || rule.HgoWeight is < 0 or > 100
            || rule.DevelopmentWeight is < 0 or > 100
            || rule.SizeWeight is < 0 or > 100
            || rule.AllocatedScore < 0))
        {
            throw new InvalidOperationException("Segment payları geçerli aralıkta olmalı.");
        }
        if (rules.Any(rule => decimal.Abs(rule.HgoWeight + rule.DevelopmentWeight + rule.SizeWeight - 100) > 0.01m))
        {
            throw new InvalidOperationException("Her segmentte HGO, gelişim ve büyüklük payları toplamı %100 olmalı.");
        }
        if (decimal.Abs(rules.Sum(rule => rule.AllocatedScore) - totalScore) > 0.01m)
        {
            throw new InvalidOperationException("Segment puanları toplam puana eşit olmalı.");
        }
    }

    private static MainProductSegmentRuleViewModel ToRuleViewModel(MainProductSegmentRule rule) => new()
    {
        Id = rule.Id,
        PerformanceSegment = rule.PerformanceSegment,
        SortOrder = rule.SortOrder,
        TargetShare = rule.TargetShare * 100,
        SizeShare = rule.SizeShare * 100,
        ScaleShare = rule.ScaleShare * 100,
        AllocatedScore = rule.AllocatedScore,
        HgoWeight = rule.HgoWeight * 100,
        DevelopmentWeight = rule.DevelopmentWeight * 100,
        SizeWeight = rule.SizeWeight * 100
    };

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
}
