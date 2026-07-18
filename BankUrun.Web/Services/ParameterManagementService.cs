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
    private static readonly int[] AllowedPageSizes = [5, 10, 25, 50];
    private static readonly string[] TurkishMonthNames =
    [
        "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];

    public async Task<ParameterIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.GroupDefinitions.AsNoTracking()
            .OrderBy(group => group.GroupNo)
            .Select(group => new ParameterGroupOptionViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name,
                GroupType = group.GroupType
            }).ToListAsync(cancellationToken);
        var products = await db.MainProductInstances.AsNoTracking()
            .Include(instance => instance.MainProduct)
            .Where(instance => instance.Term == 1 || instance.Term == 2)
            .OrderByDescending(instance => instance.Year)
            .ThenByDescending(instance => instance.Term)
            .ThenBy(instance => instance.MainProduct.Code)
            .Select(instance => new ParameterProductOptionViewModel
            {
                Id = instance.Id,
                MainProductId = instance.MainProductId,
                Year = instance.Year,
                Term = instance.Term,
                Code = instance.MainProduct.Code,
                Name = instance.MainProduct.Name
            }).ToListAsync(cancellationToken);
        var gamuts = await db.ProductGamuts.AsNoTracking()
            .Include(gamut => gamut.Group)
            .OrderBy(gamut => gamut.Group.GroupNo).ThenBy(gamut => gamut.Code)
            .Select(gamut => new ParameterGamutOptionViewModel
            {
                Id = gamut.Id,
                GroupId = gamut.GroupId,
                GroupNo = gamut.Group.GroupNo,
                Code = gamut.Code,
                Name = gamut.Name
            }).ToListAsync(cancellationToken);
        var portfolios = await db.Portfolios.AsNoTracking()
            .Include(portfolio => portfolio.Branch)
            .OrderBy(portfolio => portfolio.Branch.BranchCode).ThenBy(portfolio => portfolio.Code)
            .Select(portfolio => new ParameterPortfolioOptionViewModel
            {
                Id = portfolio.Id,
                GroupId = portfolio.Branch.GroupId,
                ProductGamutId = portfolio.ProductGamutId,
                Code = portfolio.Code,
                Name = portfolio.Name,
                BranchCode = portfolio.Branch.BranchCode
            }).ToListAsync(cancellationToken);

        return new ParameterIndexViewModel
        {
            Groups = groups,
            Products = products,
            ProductGamuts = gamuts,
            Portfolios = portfolios,
            Years = products.Select(product => product.Year).Distinct().OrderByDescending(year => year).ToList(),
            Page = await GetPageAsync(new ParameterQuery(), cancellationToken),
            TargetPage = await GetMainProductTargetPageAsync(new MainProductTargetQuery(), cancellationToken)
        };
    }

    public async Task<ParameterPageViewModel> GetPageAsync(ParameterQuery query, CancellationToken cancellationToken = default)
    {
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Include(parameter => parameter.Group)
            .Include(parameter => parameter.MainProductInstance).ThenInclude(instance => instance.MainProduct)
            .ToListAsync(cancellationToken);
        IEnumerable<MainProductParameter> filtered = parameters;
        if (query.GroupId.HasValue) filtered = filtered.Where(item => item.GroupId == query.GroupId.Value);
        if (query.Year.HasValue) filtered = filtered.Where(item => item.MainProductInstance.Year == query.Year.Value);
        if (query.Term is 1 or 2) filtered = filtered.Where(item => item.MainProductInstance.Term == query.Term.Value);
        if (query.CalculationType.HasValue) filtered = filtered.Where(item => item.CalculationType == query.CalculationType.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            filtered = filtered.Where(item => ContainsTurkish(
                $"{item.Group.GroupNo} {item.Group.Name} {item.MainProductInstance.MainProduct.Code} {item.MainProductInstance.MainProduct.Name}", search));
        }

        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = SortParameters(filtered, query.SortKey, descending).ToList();
        var pageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (decimal)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);
        var rows = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(parameter => new ParameterRowViewModel
        {
            ParameterId = parameter.Id,
            GroupId = parameter.GroupId,
            MainProductInstanceId = parameter.MainProductInstanceId,
            MainProductId = parameter.MainProductInstance.MainProductId,
            Year = parameter.MainProductInstance.Year,
            Term = parameter.MainProductInstance.Term,
            GroupNo = parameter.Group.GroupNo,
            GroupName = parameter.Group.Name,
            GroupType = parameter.Group.GroupType,
            MainProductCode = parameter.MainProductInstance.MainProduct.Code,
            MainProductName = parameter.MainProductInstance.MainProduct.Name,
            CalculationType = parameter.CalculationType,
            CriterionScore = parameter.CriterionScore,
            IsActive = parameter.IsActive
        }).ToList();
        return new ParameterPageViewModel
        {
            Rows = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = ordered.Count,
            TotalPages = totalPages
        };
    }

    public async Task<MainProductTargetPageViewModel> GetMainProductTargetPageAsync(
        MainProductTargetQuery query,
        CancellationToken cancellationToken = default)
    {
        var parameters = await db.MainProductParameters.AsNoTracking()
            .Include(item => item.Group)
            .Include(item => item.MainProductInstance).ThenInclude(item => item.MainProduct)
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var portfolios = await db.Portfolios.AsNoTracking()
            .Include(item => item.Branch)
            .Include(item => item.ProductGamut)
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var assignments = await db.ProductGamutMainProductAssignments.AsNoTracking().ToListAsync(cancellationToken);
        var exclusions = await db.BranchMainProductExclusions.AsNoTracking().ToListAsync(cancellationToken);
        var targets = await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
            .GroupBy(item => new { item.PortfolioId, item.MainProductParameterId })
            .Select(group => new
            {
                group.Key.PortfolioId,
                group.Key.MainProductParameterId,
                Target = group.Sum(item => item.TargetValue),
                Count = group.Count()
            }).ToListAsync(cancellationToken);
        var targetMap = targets.ToDictionary(item => (item.PortfolioId, item.MainProductParameterId));

        var rows = new List<MainProductTargetRowViewModel>();
        foreach (var portfolio in portfolios)
        {
            foreach (var parameter in parameters.Where(item => item.GroupId == portfolio.Branch.GroupId))
            {
                var year = parameter.MainProductInstance.Year;
                var term = parameter.MainProductInstance.Term;
                var mainProductId = parameter.MainProductInstance.MainProductId;
                if (!assignments.Any(item => item.ProductGamutId == portfolio.ProductGamutId
                    && item.MainProductId == mainProductId && IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm, year, term)))
                    continue;
                if (exclusions.Any(item => item.BranchId == portfolio.BranchId && item.MainProductId == mainProductId
                    && IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm, year, term)))
                    continue;

                targetMap.TryGetValue((portfolio.Id, parameter.Id), out var target);
                rows.Add(new MainProductTargetRowViewModel
                {
                    PortfolioId = portfolio.Id,
                    ParameterId = parameter.Id,
                    MainProductId = mainProductId,
                    ProductGamutId = portfolio.ProductGamutId,
                    Year = year,
                    Term = term,
                    GroupNo = parameter.Group.GroupNo,
                    GroupName = parameter.Group.Name,
                    BranchCode = portfolio.Branch.BranchCode,
                    BranchName = portfolio.Branch.Name,
                    PortfolioCode = portfolio.Code,
                    PortfolioName = portfolio.Name,
                    ProductGamutCode = portfolio.ProductGamut.Code,
                    ProductGamutName = portfolio.ProductGamut.Name,
                    MainProductCode = parameter.MainProductInstance.MainProduct.Code,
                    MainProductName = parameter.MainProductInstance.MainProduct.Name,
                    PeriodTarget = target?.Target ?? 0,
                    EnteredMonthCount = target?.Count ?? 0
                });
            }
        }

        if (query.GroupId.HasValue) rows = rows.Where(row => parameters.Any(item => item.Id == row.ParameterId && item.GroupId == query.GroupId.Value)).ToList();
        if (query.ProductGamutId.HasValue) rows = rows.Where(row => row.ProductGamutId == query.ProductGamutId.Value).ToList();
        if (query.PortfolioId.HasValue) rows = rows.Where(row => row.PortfolioId == query.PortfolioId.Value).ToList();
        if (query.MainProductId.HasValue) rows = rows.Where(row => row.MainProductId == query.MainProductId.Value).ToList();
        if (query.Year.HasValue) rows = rows.Where(row => row.Year == query.Year.Value).ToList();
        if (query.Term is 1 or 2) rows = rows.Where(row => row.Term == query.Term.Value).ToList();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rows = rows.Where(row => ContainsTurkish(
                $"{row.GroupNo} {row.GroupName} {row.BranchCode} {row.BranchName} {row.PortfolioCode} {row.PortfolioName} {row.ProductGamutCode} {row.ProductGamutName} {row.MainProductCode} {row.MainProductName}", search)).ToList();
        }

        var comparer = StringComparer.Create(TurkishCulture, true);
        var desc = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        rows = (query.SortKey?.Trim().ToLowerInvariant(), desc) switch
        {
            ("term", false) => rows.OrderBy(row => row.Term).ThenBy(row => row.Year).ToList(),
            ("term", true) => rows.OrderByDescending(row => row.Term).ThenByDescending(row => row.Year).ToList(),
            ("group", false) => rows.OrderBy(row => row.GroupNo, comparer).ToList(),
            ("group", true) => rows.OrderByDescending(row => row.GroupNo, comparer).ToList(),
            ("branch", false) => rows.OrderBy(row => row.BranchCode, comparer).ToList(),
            ("branch", true) => rows.OrderByDescending(row => row.BranchCode, comparer).ToList(),
            ("portfolio", false) => rows.OrderBy(row => row.PortfolioCode, comparer).ToList(),
            ("portfolio", true) => rows.OrderByDescending(row => row.PortfolioCode, comparer).ToList(),
            ("gamut", false) => rows.OrderBy(row => row.ProductGamutCode, comparer).ToList(),
            ("gamut", true) => rows.OrderByDescending(row => row.ProductGamutCode, comparer).ToList(),
            ("product", false) => rows.OrderBy(row => row.MainProductCode, comparer).ToList(),
            ("product", true) => rows.OrderByDescending(row => row.MainProductCode, comparer).ToList(),
            ("target", false) => rows.OrderBy(row => row.PeriodTarget).ToList(),
            ("target", true) => rows.OrderByDescending(row => row.PeriodTarget).ToList(),
            (_, false) => rows.OrderBy(row => row.Year).ThenBy(row => row.Term).ThenBy(row => row.PortfolioCode, comparer).ToList(),
            _ => rows.OrderByDescending(row => row.Year).ThenByDescending(row => row.Term).ThenBy(row => row.PortfolioCode, comparer).ToList()
        };
        var pageSize = AllowedPageSizes.Contains(query.PageSize) ? query.PageSize : 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(rows.Count / (decimal)pageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);
        return new MainProductTargetPageViewModel
        {
            Rows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = rows.Count,
            TotalPages = totalPages
        };
    }

    public async Task<MainProductTargetEditorViewModel> GetMainProductTargetEditorAsync(
        int parameterId,
        int portfolioId,
        CancellationToken cancellationToken = default)
    {
        var (parameter, portfolio) = await ValidatePortfolioParameterAsync(parameterId, portfolioId, cancellationToken);
        var stored = await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
            .Where(item => item.PortfolioId == portfolioId && item.MainProductParameterId == parameterId)
            .ToDictionaryAsync(item => item.Month, cancellationToken);
        return new MainProductTargetEditorViewModel
        {
            ParameterId = parameterId,
            PortfolioId = portfolioId,
            Year = parameter.MainProductInstance.Year,
            Term = parameter.MainProductInstance.Term,
            PortfolioLabel = $"{portfolio.Code} - {portfolio.Name}",
            MainProductLabel = $"{parameter.MainProductInstance.MainProduct.Code} - {parameter.MainProductInstance.MainProduct.Name}",
            Months = calculator.GetTermMonths(parameter.MainProductInstance.Term).Select(month =>
            {
                stored.TryGetValue(month, out var target);
                return new ParameterMonthlyTargetViewModel
                {
                    Month = month,
                    MonthName = TurkishMonthNames[month],
                    TargetValue = target?.TargetValue ?? 0,
                    HasStoredTarget = target is not null
                };
            }).ToList()
        };
    }

    public async Task UpsertParameterAsync(MainProductParameterInput input, string actor, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(input.CalculationType) || input.CriterionScore < 0)
            throw new InvalidOperationException("Hesaplama türü ve kriter puanını kontrol edin.");
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var instance = await db.MainProductInstances.Include(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.MainProductInstanceId, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün dönem kaydı bulunamadı.");
        _ = calculator.GetTermMonths(instance.Term);
        var now = timeProvider.GetUtcNow();
        MainProductParameter parameter;
        if (input.Id == 0)
        {
            if (await db.MainProductParameters.AnyAsync(item => item.GroupId == input.GroupId && item.MainProductInstanceId == input.MainProductInstanceId, cancellationToken))
                throw new InvalidOperationException("Bu grup, ana ürün ve dönem için parametre zaten var.");
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
            db.MainProductParameters.Add(parameter);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateMainProductParameter", parameter.Id, $"{group.GroupNo} · {instance.Year}/{instance.Term} {instance.MainProduct.Code} parametresi oluşturuldu.", actor, now);
        }
        else
        {
            parameter = await db.MainProductParameters.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Parametre bulunamadı.");
            if (parameter.GroupId != input.GroupId || parameter.MainProductInstanceId != input.MainProductInstanceId)
                throw new InvalidOperationException("Parametre farklı grup veya ana ürün dönemine taşınamaz.");
            parameter.CalculationType = input.CalculationType;
            parameter.CriterionScore = Round(input.CriterionScore);
            parameter.IsActive = input.IsActive;
            parameter.UpdatedAt = now;
            AddAudit("UpdateMainProductParameter", parameter.Id, $"{group.GroupNo} · {instance.Year}/{instance.Term} {instance.MainProduct.Code} parametresi güncellendi.", actor, now);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMainProductTargetsAsync(
        PortfolioMainProductTargetsInput input,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var (parameter, portfolio) = await ValidatePortfolioParameterAsync(input.ParameterId, input.PortfolioId, cancellationToken);
        var termMonths = calculator.GetTermMonths(parameter.MainProductInstance.Term);
        if (input.Months.Count != termMonths.Count
            || input.Months.Select(item => item.Month).Distinct().Count() != termMonths.Count
            || input.Months.Select(item => item.Month).Except(termMonths).Any()
            || input.Months.Any(item => item.TargetValue < 0))
            throw new InvalidOperationException("Dönemin altı aylık hedeflerini eksiksiz ve geçerli girin.");
        var now = timeProvider.GetUtcNow();
        var stored = await db.PortfolioMainProductMonthlyTargets
            .Where(item => item.PortfolioId == input.PortfolioId && item.MainProductParameterId == input.ParameterId)
            .ToDictionaryAsync(item => item.Month, cancellationToken);
        var previousTotal = Round(stored.Values.Sum(item => item.TargetValue));
        var newTotal = Round(input.Months.Sum(item => item.TargetValue));
        var createdCount = 0;
        var updatedCount = 0;
        foreach (var monthInput in input.Months)
        {
            if (stored.TryGetValue(monthInput.Month, out var target))
            {
                target.TargetValue = Round(monthInput.TargetValue);
                target.UpdatedAt = now;
                updatedCount++;
            }
            else
            {
                db.PortfolioMainProductMonthlyTargets.Add(new PortfolioMainProductMonthlyTarget
                {
                    PortfolioId = input.PortfolioId,
                    GroupId = parameter.GroupId,
                    MainProductParameterId = input.ParameterId,
                    Month = monthInput.Month,
                    TargetValue = Round(monthInput.TargetValue),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                createdCount++;
            }
        }
        db.AuditLogs.Add(new AuditLog
        {
            Action = "UpdatePortfolioMainProductTargets",
            EntityName = nameof(PortfolioMainProductMonthlyTarget),
            EntityKey = $"{input.PortfolioId}:{input.ParameterId}",
            Description = $"{portfolio.Code} · {parameter.MainProductInstance.MainProduct.Code} hedefleri güncellendi; kapsam={parameter.MainProductInstance.Year}/{parameter.MainProductInstance.Term}, ay={input.Months.Count}, eklenen={createdCount}, güncellenen={updatedCount}, önceki toplam={previousTotal:N2}, yeni toplam={newTotal:N2}.",
            Actor = actor,
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetParameterDeleteImpactAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var parameter = await db.MainProductParameters.AsNoTracking()
            .Include(item => item.Group)
            .Include(item => item.MainProductInstance).ThenInclude(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");
        var targetPortfolioIds = await db.PortfolioMainProductMonthlyTargets.AsNoTracking()
            .Where(item => item.MainProductParameterId == id)
            .Select(item => item.PortfolioId)
            .ToListAsync(cancellationToken);

        return new ManagementImpactViewModel
        {
            Operation = "Ana ürün parametresini sil",
            Subject = $"{parameter.Group.GroupNo} · {parameter.MainProductInstance.MainProduct.Code} · {parameter.MainProductInstance.Year}/{parameter.MainProductInstance.Term}",
            Summary = "Seçili grup, ana ürün, yıl ve döneme ait parametre ile ona bağlı aylık portföy hedefleri kalıcı olarak silinir.",
            Counts =
            [
                Impact("Ana ürün parametresi", 1),
                Impact("Etkilenen grup", 1),
                Impact("Etkilenen ana ürün", 1),
                Impact("Etkilenen yıl", 1),
                Impact("Etkilenen dönem", 1),
                Impact("Hedefi silinecek portföy", targetPortfolioIds.Distinct().Count()),
                Impact("Silinecek aylık hedef", targetPortfolioIds.Count)
            ],
            Warnings =
            [
                $"{parameter.Group.GroupNo} grubunda {parameter.MainProductInstance.MainProduct.Code} ürünü için {parameter.MainProductInstance.Year}/{parameter.MainProductInstance.Term} performansı 'Parametre bekleniyor' durumuna geçer.",
                "Alt ürün gerçekleşmeleri silinmez; yalnız parametreye bağlı portföy ana ürün hedefleri kaldırılır."
            ]
        };
    }

    public async Task DeleteParameterAsync(ParameterIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        var parameter = await db.MainProductParameters
            .Include(item => item.Group)
            .Include(item => item.MainProductInstance).ThenInclude(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");
        var targets = await db.PortfolioMainProductMonthlyTargets
            .Where(item => item.MainProductParameterId == parameter.Id).ToListAsync(cancellationToken);
        db.PortfolioMainProductMonthlyTargets.RemoveRange(targets);
        db.MainProductParameters.Remove(parameter);
        AddAudit("DeleteMainProductParameter", parameter.Id,
            $"{parameter.Group.GroupNo} · {parameter.MainProductInstance.Year}/{parameter.MainProductInstance.Term} {parameter.MainProductInstance.MainProduct.Code} parametresi ve {targets.Count} portföy hedefi silindi.",
            actor, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<(MainProductParameter Parameter, Portfolio Portfolio)> ValidatePortfolioParameterAsync(
        int parameterId, int portfolioId, CancellationToken cancellationToken)
    {
        var parameter = await db.MainProductParameters.AsNoTracking()
            .Include(item => item.MainProductInstance).ThenInclude(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == parameterId, cancellationToken)
            ?? throw new InvalidOperationException("Parametre bulunamadı.");
        var portfolio = await db.Portfolios.AsNoTracking().Include(item => item.Branch).Include(item => item.ProductGamut)
            .FirstOrDefaultAsync(item => item.Id == portfolioId, cancellationToken)
            ?? throw new InvalidOperationException("Portföy bulunamadı.");
        if (portfolio.Branch.GroupId != parameter.GroupId)
            throw new InvalidOperationException("Portföy ve ana ürün parametresi aynı gruba ait olmalıdır.");
        var year = parameter.MainProductInstance.Year;
        var term = parameter.MainProductInstance.Term;
        var assigned = (await db.ProductGamutMainProductAssignments.AsNoTracking()
            .Where(item => item.ProductGamutId == portfolio.ProductGamutId && item.MainProductId == parameter.MainProductInstance.MainProductId)
            .ToListAsync(cancellationToken))
            .Any(item => IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm, year, term));
        if (!assigned) throw new InvalidOperationException("Ana ürün seçili dönemde portföyün ürün gamına bağlı değil.");
        var excluded = (await db.BranchMainProductExclusions.AsNoTracking()
            .Where(item => item.BranchId == portfolio.BranchId && item.MainProductId == parameter.MainProductInstance.MainProductId)
            .ToListAsync(cancellationToken))
            .Any(item => IsEffective(item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm, year, term));
        if (excluded) throw new InvalidOperationException("Ana ürün seçili dönemde bu şubeden çıkarılmış.");
        return (parameter, portfolio);
    }

    private static bool IsEffective(int fromYear, int fromTerm, int? toYear, int? toTerm, int year, int term)
    {
        var key = year * 10 + term;
        var from = fromYear * 10 + fromTerm;
        var to = toYear.HasValue && toTerm.HasValue ? toYear.Value * 10 + toTerm.Value : int.MaxValue;
        return key >= from && key <= to;
    }

    private static IOrderedEnumerable<MainProductParameter> SortParameters(
        IEnumerable<MainProductParameter> parameters, string? sortKey, bool descending)
    {
        var comparer = StringComparer.Create(TurkishCulture, true);
        return (sortKey?.Trim().ToLowerInvariant(), descending) switch
        {
            ("term", false) => parameters.OrderBy(item => item.MainProductInstance.Term).ThenBy(item => item.MainProductInstance.Year),
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
            (_, false) => parameters.OrderBy(item => item.MainProductInstance.Year).ThenBy(item => item.MainProductInstance.Term),
            _ => parameters.OrderByDescending(item => item.MainProductInstance.Year).ThenByDescending(item => item.MainProductInstance.Term)
        };
    }

    private static bool ContainsTurkish(string source, string search) =>
        TurkishCompare.IndexOf(source, search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static ManagementImpactCountViewModel Impact(string label, int count) => new()
    {
        Label = label,
        Count = count
    };

    private void AddAudit(string action, int id, string description, string actor, DateTimeOffset now) =>
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = nameof(MainProductParameter),
            EntityKey = id.ToString(CultureInfo.InvariantCulture),
            Description = description,
            Actor = actor,
            CreatedAt = now
        });
}
