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
        return new ParameterIndexViewModel
        {
            Groups = groups,
            Products = products,
            Years = products.Select(product => product.Year).Distinct().OrderByDescending(year => year).ToList(),
            Page = await GetPageAsync(new ParameterQuery(), cancellationToken)
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
