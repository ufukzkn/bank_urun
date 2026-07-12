using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class PerformanceWorkspaceService(AppDbContext db) : IPerformanceWorkspaceService
{
    private static readonly PerformanceSegment[] Segments =
    [
        PerformanceSegment.Kurumsal,
        PerformanceSegment.Ticari,
        PerformanceSegment.Kobi,
        PerformanceSegment.Bireysel,
        PerformanceSegment.Diger
    ];

    public async Task<PerformanceIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.GroupDefinitions
            .AsNoTracking()
            .OrderBy(group => group.GroupNo)
            .Select(group => new PerformanceGroupOptionViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name
            })
            .ToListAsync(cancellationToken);

        var branches = await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .OrderBy(branch => branch.Group.GroupNo)
            .ThenBy(branch => branch.BranchCode)
            .Select(branch => new PerformanceBranchOptionViewModel
            {
                Id = branch.Id,
                GroupId = branch.GroupId,
                BranchCode = branch.BranchCode,
                Name = branch.Name,
                GroupLabel = branch.Group.GroupNo + " - " + branch.Group.Name
            })
            .ToListAsync(cancellationToken);

        var products = await db.SubProductInstances
            .AsNoTracking()
            .Include(item => item.SubProduct)
            .Include(item => item.MainProductInstance)
                .ThenInclude(item => item.MainProduct)
            .Where(item => item.SubProduct.IsActive && item.MainProductInstance.MainProduct.IsActive)
            .OrderByDescending(item => item.MainProductInstance.Year)
            .ThenByDescending(item => item.MainProductInstance.Term)
            .ThenBy(item => item.MainProductInstance.MainProduct.Code)
            .ThenBy(item => item.SubProduct.Code)
            .Select(item => new PerformanceProductOptionViewModel
            {
                Id = item.Id,
                Year = item.MainProductInstance.Year,
                Term = item.MainProductInstance.Term,
                MainProductCode = item.MainProductInstance.MainProduct.Code,
                MainProductName = item.MainProductInstance.MainProduct.Name,
                SubProductCode = item.SubProduct.Code,
                SubProductName = item.SubProduct.Name
            })
            .ToListAsync(cancellationToken);

        var parameters = await db.GroupProductParameters
            .AsNoTracking()
            .Include(item => item.Group)
            .Include(item => item.SubProductInstance)
                .ThenInclude(item => item.SubProduct)
            .Include(item => item.SubProductInstance)
                .ThenInclude(item => item.MainProductInstance)
                    .ThenInclude(item => item.MainProduct)
            .Include(item => item.SegmentRules)
            .OrderByDescending(item => item.SubProductInstance.MainProductInstance.Year)
            .ThenByDescending(item => item.SubProductInstance.MainProductInstance.Term)
            .ThenBy(item => item.Group.GroupNo)
            .ThenBy(item => item.SubProductInstance.MainProductInstance.MainProduct.Code)
            .ThenBy(item => item.SubProductInstance.SubProduct.Code)
            .ToListAsync(cancellationToken);

        var storedResults = await db.BranchProductMetricResults
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var resultLookup = storedResults.ToDictionary(item => (item.BranchId, item.GroupProductSegmentRuleId));
        var branchesByGroup = branches.GroupBy(branch => branch.GroupId).ToDictionary(group => group.Key, group => group.ToList());

        var parameterRows = parameters.Select(parameter => new PerformanceParameterRowViewModel
        {
            Id = parameter.Id,
            GroupId = parameter.GroupId,
            SubProductInstanceId = parameter.SubProductInstanceId,
            Year = parameter.SubProductInstance.MainProductInstance.Year,
            Term = parameter.SubProductInstance.MainProductInstance.Term,
            GroupNo = parameter.Group.GroupNo,
            GroupName = parameter.Group.Name,
            MainProductCode = parameter.SubProductInstance.MainProductInstance.MainProduct.Code,
            MainProductName = parameter.SubProductInstance.MainProductInstance.MainProduct.Name,
            SubProductCode = parameter.SubProductInstance.SubProduct.Code,
            SubProductName = parameter.SubProductInstance.SubProduct.Name,
            TotalScore = parameter.TotalScore,
            IsActive = parameter.IsActive,
            Rules = parameter.SegmentRules
                .OrderBy(rule => rule.SortOrder)
                .Select(ToRuleViewModel)
                .ToList()
        }).ToList();

        var resultRows = new List<PerformanceResultRowViewModel>();
        foreach (var parameter in parameters.Where(parameter => parameter.IsActive))
        {
            if (!branchesByGroup.TryGetValue(parameter.GroupId, out var groupBranches))
            {
                continue;
            }

            foreach (var rule in parameter.SegmentRules)
            {
                foreach (var branch in groupBranches)
                {
                    resultLookup.TryGetValue((branch.Id, rule.Id), out var stored);
                    var hgo = stored?.HgoAchievement ?? 0;
                    var development = stored?.DevelopmentAchievement ?? 0;
                    var size = stored?.SizeAchievement ?? 0;
                    var weighted = CalculateWeightedAchievement(rule, hgo, development, size);
                    resultRows.Add(new PerformanceResultRowViewModel
                    {
                        BranchId = branch.Id,
                        GroupId = parameter.GroupId,
                        ParameterId = parameter.Id,
                        RuleId = rule.Id,
                        Year = parameter.SubProductInstance.MainProductInstance.Year,
                        Term = parameter.SubProductInstance.MainProductInstance.Term,
                        GroupNo = parameter.Group.GroupNo,
                        GroupName = parameter.Group.Name,
                        BranchCode = branch.BranchCode,
                        BranchName = branch.Name,
                        MainProductCode = parameter.SubProductInstance.MainProductInstance.MainProduct.Code,
                        MainProductName = parameter.SubProductInstance.MainProductInstance.MainProduct.Name,
                        SubProductCode = parameter.SubProductInstance.SubProduct.Code,
                        SubProductName = parameter.SubProductInstance.SubProduct.Name,
                        PerformanceSegment = rule.PerformanceSegment,
                        AllocatedScore = rule.AllocatedScore,
                        HgoAchievement = hgo * 100,
                        DevelopmentAchievement = development * 100,
                        SizeAchievement = size * 100,
                        HgoWeight = rule.HgoWeight * 100,
                        DevelopmentWeight = rule.DevelopmentWeight * 100,
                        SizeWeight = rule.SizeWeight * 100,
                        WeightedAchievement = weighted * 100,
                        EarnedScore = decimal.Round(rule.AllocatedScore * decimal.Min(1, weighted), 2),
                        IsMissing = stored is null
                    });
                }
            }
        }

        return new PerformanceIndexViewModel
        {
            Groups = groups,
            Branches = branches,
            Products = products,
            Parameters = parameterRows,
            Results = resultRows
        };
    }

    public async Task CreateParameterAsync(PerformanceParameterInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var product = await db.SubProductInstances
            .Include(item => item.SubProduct)
            .Include(item => item.MainProductInstance)
                .ThenInclude(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.SubProductInstanceId, cancellationToken)
            ?? throw new InvalidOperationException("Alt ürün instance kaydı bulunamadı.");

        if (await db.GroupProductParameters.AnyAsync(item => item.GroupId == input.GroupId && item.SubProductInstanceId == input.SubProductInstanceId, cancellationToken))
        {
            throw new InvalidOperationException("Bu grup ve ürün dönemi için performans parametresi zaten var.");
        }

        var now = DateTimeOffset.UtcNow;
        var parameter = new GroupProductParameter
        {
            GroupId = input.GroupId,
            SubProductInstanceId = input.SubProductInstanceId,
            TotalScore = input.TotalScore,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.GroupProductParameters.Add(parameter);
        parameter.SegmentRules = CreateDefaultRules(input.TotalScore, now);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateGroupProductParameter", "GroupProductParameter", parameter.Id.ToString(), $"{group.GroupNo} - {product.SubProduct.Code} için performans parametresi oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateParameterAsync(PerformanceParameterUpdateInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var parameter = await db.GroupProductParameters
            .Include(item => item.SegmentRules)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Performans parametresi bulunamadı.");

        ValidateRules(input.TotalScore, input.Rules);
        var existingRuleIds = parameter.SegmentRules.Select(rule => rule.Id).Order().ToArray();
        var submittedRuleIds = input.Rules.Select(rule => rule.Id).Order().ToArray();
        if (!existingRuleIds.SequenceEqual(submittedRuleIds))
        {
            throw new InvalidOperationException("Segment kural seti değiştirilemez.");
        }

        parameter.TotalScore = input.TotalScore;
        parameter.IsActive = input.IsActive;
        parameter.UpdatedAt = DateTimeOffset.UtcNow;
        foreach (var ruleInput in input.Rules)
        {
            var rule = parameter.SegmentRules.Single(item => item.Id == ruleInput.Id);
            rule.SortOrder = ruleInput.SortOrder;
            rule.TargetShare = ToRatio(ruleInput.TargetShare);
            rule.SizeShare = ToRatio(ruleInput.SizeShare);
            rule.ScaleShare = ToRatio(ruleInput.ScaleShare);
            rule.AllocatedScore = ruleInput.AllocatedScore;
            rule.HgoWeight = ToRatio(ruleInput.HgoWeight);
            rule.DevelopmentWeight = ToRatio(ruleInput.DevelopmentWeight);
            rule.SizeWeight = ToRatio(ruleInput.SizeWeight);
            rule.UpdatedAt = parameter.UpdatedAt;
        }

        AddAudit("UpdateGroupProductParameter", "GroupProductParameter", parameter.Id.ToString(), "Performans parametresi ve segment dağılımı güncellendi.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteParameterAsync(PerformanceParameterIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var parameter = await db.GroupProductParameters
            .Include(item => item.Group)
            .Include(item => item.SubProductInstance)
                .ThenInclude(item => item.SubProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Performans parametresi bulunamadı.");

        AddAudit("DeleteGroupProductParameter", "GroupProductParameter", parameter.Id.ToString(), $"{parameter.Group.GroupNo} - {parameter.SubProductInstance.SubProduct.Code} performans parametresi silindi.", actor);
        db.GroupProductParameters.Remove(parameter);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpsertMetricResultAsync(MetricResultInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        ValidateAchievement(input.HgoAchievement);
        ValidateAchievement(input.DevelopmentAchievement);
        ValidateAchievement(input.SizeAchievement);

        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var rule = await db.GroupProductSegmentRules
            .Include(item => item.GroupProductParameter)
            .FirstOrDefaultAsync(item => item.Id == input.RuleId, cancellationToken)
            ?? throw new InvalidOperationException("Segment kuralı bulunamadı.");
        if (branch.GroupId != rule.GroupProductParameter.GroupId)
        {
            throw new InvalidOperationException("Şube, seçilen grup ürün parametresine bağlı değil.");
        }

        var result = await db.BranchProductMetricResults
            .FirstOrDefaultAsync(item => item.BranchId == input.BranchId && item.GroupProductSegmentRuleId == input.RuleId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (result is null)
        {
            result = new BranchProductMetricResult
            {
                BranchId = input.BranchId,
                GroupProductSegmentRuleId = input.RuleId,
                CreatedAt = now
            };
            db.BranchProductMetricResults.Add(result);
        }

        result.HgoAchievement = ToRatio(input.HgoAchievement);
        result.DevelopmentAchievement = ToRatio(input.DevelopmentAchievement);
        result.SizeAchievement = ToRatio(input.SizeAchievement);
        result.UpdatedAt = now;
        AddAudit("UpsertBranchProductMetricResult", "BranchProductMetricResult", $"{input.BranchId}:{input.RuleId}", "Şube performans gerçekleşmeleri güncellendi.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static List<GroupProductSegmentRule> CreateDefaultRules(decimal totalScore, DateTimeOffset now)
    {
        var allocationShares = new[] { 0.25m, 0.25m, 0.20m, 0.20m, 0.10m };
        var rules = new List<GroupProductSegmentRule>();
        decimal allocated = 0;
        for (var index = 0; index < Segments.Length; index++)
        {
            var score = index == Segments.Length - 1
                ? totalScore - allocated
                : decimal.Round(totalScore * allocationShares[index], 2);
            allocated += score;
            rules.Add(new GroupProductSegmentRule
            {
                PerformanceSegment = Segments[index],
                SortOrder = index + 1,
                TargetShare = allocationShares[index],
                SizeShare = 0.20m,
                ScaleShare = 0m,
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

    private static void ValidateRules(decimal totalScore, IReadOnlyCollection<PerformanceSegmentRuleInput> rules)
    {
        if (rules.Count != Segments.Length || rules.Select(rule => rule.PerformanceSegment).Distinct().Count() != Segments.Length)
        {
            throw new InvalidOperationException("Her performans segmenti için tek bir kural bulunmalı.");
        }

        if (rules.Any(rule => rule.TargetShare is < 0 or > 100 || rule.SizeShare is < 0 or > 100 || rule.ScaleShare is < 0 or > 100 || rule.AllocatedScore < 0))
        {
            throw new InvalidOperationException("Segment payları geçerli aralıkta olmalı.");
        }

        if (rules.Any(rule => decimal.Abs(rule.HgoWeight + rule.DevelopmentWeight + rule.SizeWeight - 100) > 0.01m))
        {
            throw new InvalidOperationException("Her segmentte HGO, gelişim ve büyüklük ağırlıkları toplamı %100 olmalı.");
        }

        if (decimal.Abs(rules.Sum(rule => rule.AllocatedScore) - totalScore) > 0.01m)
        {
            throw new InvalidOperationException("Segment puan tahsisleri toplam puana eşit olmalı.");
        }
    }

    private static void ValidateAchievement(decimal value)
    {
        if (value is < 0 or > 200)
        {
            throw new InvalidOperationException("Gerçekleşme oranı %0 ile %200 arasında olmalı.");
        }
    }

    private static decimal CalculateWeightedAchievement(GroupProductSegmentRule rule, decimal hgo, decimal development, decimal size)
    {
        return rule.HgoWeight * hgo + rule.DevelopmentWeight * development + rule.SizeWeight * size;
    }

    private static PerformanceSegmentRuleViewModel ToRuleViewModel(GroupProductSegmentRule rule) => new()
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

    private static decimal ToRatio(decimal percent) => percent / 100;

    private void AddAudit(string action, string entityName, string entityKey, string description, string actor)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityKey = entityKey,
            Description = description,
            Actor = string.IsNullOrWhiteSpace(actor) ? "local-user" : actor,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
