using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class OrganizationService(AppDbContext db) : IOrganizationService
{
    public async Task<OrganizationIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.GroupDefinitions
            .AsNoTracking()
            .Include(group => group.Branches)
            .Include(group => group.ProductGamuts)
            .OrderBy(group => group.GroupNo)
            .Select(group => new GroupRowViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name,
                GroupType = group.GroupType,
                IsActive = group.IsActive,
                BranchPerformanceEnabled = group.BranchPerformanceEnabled,
                MiyPerformanceEnabled = group.MiyPerformanceEnabled,
                ScaleEnabled = group.ScaleEnabled,
                BranchCount = group.Branches.Count,
                ProductGamutCount = group.ProductGamuts.Count,
                PortfolioCount = group.Branches.SelectMany(branch => branch.Portfolios).Count()
            })
            .ToListAsync(cancellationToken);

        var branches = await db.Branches
            .AsNoTracking()
            .Include(branch => branch.Group)
            .Include(branch => branch.Portfolios)
            .Include(branch => branch.MainProductExclusions)
            .OrderBy(branch => branch.Group.GroupNo)
            .ThenBy(branch => branch.BranchCode)
            .Select(branch => new BranchRowViewModel
            {
                Id = branch.Id,
                GroupId = branch.GroupId,
                GroupNo = branch.Group.GroupNo,
                GroupName = branch.Group.Name,
                GroupType = branch.Group.GroupType,
                BranchCode = branch.BranchCode,
                Name = branch.Name,
                PortfolioCount = branch.Portfolios.Count,
                ProductExclusionCount = branch.MainProductExclusions.Count
            })
            .ToListAsync(cancellationToken);

        var portfolioTypes = await db.Set<PortfolioType>().AsNoTracking()
            .Include(type => type.Portfolios)
            .OrderBy(type => type.Code)
            .Select(type => new PortfolioTypeRowViewModel
            {
                Id = type.Id,
                Code = type.Code,
                Name = type.Name,
                IsActive = type.IsActive,
                PortfolioCount = type.Portfolios.Count
            })
            .ToListAsync(cancellationToken);
        var portfolios = await db.Set<Portfolio>().AsNoTracking()
            .Include(portfolio => portfolio.Branch).ThenInclude(branch => branch.Group)
            .Include(portfolio => portfolio.ProductGamut)
            .Include(portfolio => portfolio.PortfolioType)
            .OrderBy(portfolio => portfolio.Branch.BranchCode)
            .ThenBy(portfolio => portfolio.Code)
            .Select(portfolio => new PortfolioRowViewModel
            {
                Id = portfolio.Id,
                BranchId = portfolio.BranchId,
                GroupId = portfolio.GroupId,
                ProductGamutId = portfolio.ProductGamutId,
                PortfolioTypeId = portfolio.PortfolioTypeId,
                Code = portfolio.Code,
                Name = portfolio.Name,
                IsActive = portfolio.IsActive,
                BranchCode = portfolio.Branch.BranchCode,
                BranchName = portfolio.Branch.Name,
                GroupNo = portfolio.Branch.Group.GroupNo,
                GroupName = portfolio.Branch.Group.Name,
                ProductGamutCode = portfolio.ProductGamut.Code,
                ProductGamutName = portfolio.ProductGamut.Name,
                PortfolioTypeCode = portfolio.PortfolioType.Code,
                PortfolioTypeName = portfolio.PortfolioType.Name
            })
            .ToListAsync(cancellationToken);
        var productExclusions = await db.Set<BranchMainProductExclusion>().AsNoTracking()
            .Include(exclusion => exclusion.Branch)
            .Include(exclusion => exclusion.MainProduct)
            .OrderBy(exclusion => exclusion.Branch.BranchCode)
            .ThenBy(exclusion => exclusion.MainProduct.Code)
            .Select(exclusion => new BranchMainProductExclusionRowViewModel
            {
                Id = exclusion.Id,
                BranchId = exclusion.BranchId,
                MainProductId = exclusion.MainProductId,
                BranchCode = exclusion.Branch.BranchCode,
                BranchName = exclusion.Branch.Name,
                MainProductCode = exclusion.MainProduct.Code,
                MainProductName = exclusion.MainProduct.Name,
                EffectiveFromYear = exclusion.EffectiveFromYear,
                EffectiveFromTerm = exclusion.EffectiveFromTerm,
                EffectiveToYear = exclusion.EffectiveToYear,
                EffectiveToTerm = exclusion.EffectiveToTerm
            })
            .ToListAsync(cancellationToken);
        var productGamuts = await db.Set<ProductGamut>().AsNoTracking()
            .OrderBy(gamut => gamut.Code)
            .Select(gamut => new OrganizationProductGamutOptionViewModel
            {
                Id = gamut.Id,
                GroupId = gamut.GroupId,
                Code = gamut.Code,
                Name = gamut.Name,
                IsActive = gamut.IsActive
            })
            .ToListAsync(cancellationToken);
        var mainProducts = await db.ProductDefinitions.AsNoTracking()
            .Where(product => product.Type == ProductType.Main)
            .OrderBy(product => product.Code)
            .Select(product => new OrganizationMainProductOptionViewModel
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                IsActive = product.IsActive
            })
            .ToListAsync(cancellationToken);

        return new OrganizationIndexViewModel
        {
            Groups = groups,
            Branches = branches,
            PortfolioTypes = portfolioTypes,
            Portfolios = portfolios,
            ProductExclusions = productExclusions,
            ProductGamuts = productGamuts,
            MainProducts = mainProducts,
            NextGroupNo = NextNumber(groups.Select(group => group.GroupNo)),
            NextBranchCode = NextNumber(branches.Select(branch => branch.BranchCode))
        };
    }

    public async Task CreateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var groupNo = input.GenerateNumberAutomatically
            ? await NextGroupNoAsync(cancellationToken)
            : NormalizeCode(input.GroupNo);
        await EnsureGroupNoAvailableAsync(groupNo, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var group = new GroupDefinition
        {
            GroupNo = groupNo,
            Name = input.Name.Trim(),
            GroupType = input.GroupType,
            IsActive = input.IsActive,
            BranchPerformanceEnabled = input.BranchPerformanceEnabled,
            MiyPerformanceEnabled = input.MiyPerformanceEnabled,
            ScaleEnabled = input.ScaleEnabled,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.GroupDefinitions.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{group.GroupNo} grubu oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateGroupAsync(GroupInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var groupNo = NormalizeCode(input.GroupNo);
        await EnsureGroupNoAvailableAsync(groupNo, group.Id, cancellationToken);
        var old = $"{group.GroupNo} {group.Name} {group.GroupType}";

        group.GroupNo = groupNo;
        group.Name = input.Name.Trim();
        group.GroupType = input.GroupType;
        group.IsActive = input.IsActive;
        group.BranchPerformanceEnabled = input.BranchPerformanceEnabled;
        group.MiyPerformanceEnabled = input.MiyPerformanceEnabled;
        group.ScaleEnabled = input.ScaleEnabled;
        group.UpdatedAt = DateTimeOffset.UtcNow;

        AddAudit("UpdateGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{old} -> {group.GroupNo} {group.Name} {group.GroupType}", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteGroupAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions
            .Include(item => item.Branches)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");

        if (group.Branches.Count > 0)
        {
            throw new InvalidOperationException($"{group.GroupNo} grubuna bağlı şubeler var. Önce şubeleri başka gruba taşıyın veya silin.");
        }
        if (await db.Set<ProductGamut>().AnyAsync(gamut => gamut.GroupId == group.Id, cancellationToken))
            throw new InvalidOperationException($"{group.GroupNo} grubuna bağlı ürün gamları var. Önce ürün gamlarını taşıyın veya silin.");
        if (await db.MainProductParameters.AnyAsync(parameter => parameter.GroupId == group.Id, cancellationToken))
            throw new InvalidOperationException($"{group.GroupNo} grubuna bağlı ana ürün parametreleri var. Önce grup ürün kurallarını kaldırın.");

        AddAudit("DeleteGroupDefinition", "GroupDefinition", group.Id.ToString(), $"{group.GroupNo} grubu silindi.", actor);
        db.GroupDefinitions.Remove(group);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetGroupDeleteImpactAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var group = await db.GroupDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var branchCount = await db.Branches.AsNoTracking().CountAsync(branch => branch.GroupId == id, cancellationToken);
        var gamutCount = await db.Set<ProductGamut>().AsNoTracking().CountAsync(gamut => gamut.GroupId == id, cancellationToken);
        var parameterCount = await db.MainProductParameters.AsNoTracking().CountAsync(parameter => parameter.GroupId == id, cancellationToken);
        var blockers = new List<string>();
        if (branchCount > 0) blockers.Add("Gruba bağlı şubeler vardır.");
        if (gamutCount > 0) blockers.Add("Gruba bağlı ürün gamları vardır.");
        if (parameterCount > 0) blockers.Add("Gruba bağlı ana ürün parametreleri vardır.");
        return new ManagementImpactViewModel
        {
            Operation = "Organizasyon grubunu sil",
            Subject = $"{group.GroupNo} - {group.Name}",
            Summary = blockers.Count == 0
                ? "Grup kalıcı olarak silinir."
                : "Bağlı kayıtlar kaldırılmadan grup silinemez.",
            Allowed = blockers.Count == 0,
            Counts =
            [
                Impact("Bağlı şube", branchCount),
                Impact("Ürün gamı", gamutCount),
                Impact("Ana ürün parametresi", parameterCount)
            ],
            Blockers = blockers
        };
    }

    public async Task CreateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Bağlı grup bulunamadı.");
        var branchCode = input.GenerateNumberAutomatically
            ? await NextBranchCodeAsync(cancellationToken)
            : NormalizeCode(input.BranchCode);
        await EnsureBranchCodeAvailableAsync(branchCode, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var branch = new Branch
        {
            GroupId = input.GroupId,
            BranchCode = branchCode,
            Name = input.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Branches.Add(branch);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateBranch", "Branch", branch.Id.ToString(), $"{branch.BranchCode} şubesi {group.GroupNo} grubuna bağlandı.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateBranchAsync(BranchInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var branch = await db.Branches
            .Include(item => item.Group)
            .Include(item => item.Portfolios).ThenInclude(portfolio => portfolio.ProductGamut)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Bağlı grup bulunamadı.");
        if (branch.GroupId != input.GroupId)
            throw new InvalidOperationException("Şube doğrudan başka gruba taşınamaz. Grup aktarımı ayrı ve etkileri önizlenen bir işlem olarak yapılmalıdır.");
        var branchCode = NormalizeCode(input.BranchCode);
        await EnsureBranchCodeAvailableAsync(branchCode, branch.Id, cancellationToken);
        var old = $"{branch.BranchCode} {branch.Name} ({branch.Group.GroupNo})";
        if (!string.Equals(branch.BranchCode, branchCode, StringComparison.Ordinal))
        {
            var portfolioIds = branch.Portfolios.Select(portfolio => portfolio.Id).ToList();
            var rewrittenCodes = branch.Portfolios.ToDictionary(
                portfolio => portfolio.Id,
                portfolio => RewritePortfolioBranchCode(
                    portfolio.Code, branch.BranchCode, branchCode, portfolio.ProductGamut.Code));
            var rewrittenCodeValues = rewrittenCodes.Values.ToList();
            var duplicateCode = rewrittenCodeValues
                .GroupBy(value => value, StringComparer.Ordinal).FirstOrDefault(grouping => grouping.Count() > 1)?.Key;
            if (duplicateCode is not null || await db.Set<Portfolio>().AsNoTracking()
                    .AnyAsync(portfolio => !portfolioIds.Contains(portfolio.Id)
                        && rewrittenCodeValues.Contains(portfolio.Code), cancellationToken))
                throw new InvalidOperationException("Şube kodu değişikliği bağlı portföy kodlarıyla çakışıyor.");
            foreach (var portfolio in branch.Portfolios)
                portfolio.Code = rewrittenCodes[portfolio.Id];
        }

        branch.GroupId = input.GroupId;
        branch.BranchCode = branchCode;
        branch.Name = input.Name.Trim();
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        AddAudit("UpdateBranch", "Branch", branch.Id.ToString(),
            $"{old} -> {branch.BranchCode} {branch.Name} ({group.GroupNo}); {branch.Portfolios.Count} bağlı portföy kodu güncellendi.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteBranchAsync(LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");

        var portfolioIds = await db.Set<Portfolio>().AsNoTracking()
            .Where(item => item.BranchId == branch.Id).Select(item => item.Id).ToListAsync(cancellationToken);
        var targetCount = await db.Set<PortfolioMainProductMonthlyTarget>().AsNoTracking()
            .CountAsync(item => portfolioIds.Contains(item.PortfolioId), cancellationToken);
        var actualCount = await db.Set<PortfolioSubProductMonthlyMetric>().AsNoTracking()
            .CountAsync(item => portfolioIds.Contains(item.PortfolioId), cancellationToken);
        var exclusionCount = await db.Set<BranchMainProductExclusion>().AsNoTracking()
            .CountAsync(item => item.BranchId == branch.Id, cancellationToken);
        AddAudit("DeleteBranch", "Branch", branch.Id.ToString(),
            $"{branch.BranchCode} şubesi silindi; kapsam=Tüm dönemler, portföy={portfolioIds.Count}, istisna={exclusionCount}, hedef={targetCount}, gerçekleşme={actualCount}.", actor);
        db.Branches.Remove(branch);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetBranchDeleteImpactAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var branch = await db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var portfolioIds = await db.Set<Portfolio>().AsNoTracking()
            .Where(portfolio => portfolio.BranchId == id)
            .Select(portfolio => portfolio.Id)
            .ToListAsync(cancellationToken);
        var targetCount = await db.Set<PortfolioMainProductMonthlyTarget>().AsNoTracking()
            .CountAsync(target => portfolioIds.Contains(target.PortfolioId), cancellationToken);
        var actualCount = await db.Set<PortfolioSubProductMonthlyMetric>().AsNoTracking()
            .CountAsync(metric => portfolioIds.Contains(metric.PortfolioId), cancellationToken);
        var exclusionCount = await db.Set<BranchMainProductExclusion>().AsNoTracking()
            .CountAsync(exclusion => exclusion.BranchId == id, cancellationToken);
        return new ManagementImpactViewModel
        {
            Operation = "Şubeyi kalıcı sil",
            Subject = $"{branch.BranchCode} - {branch.Name}",
            Summary = "Şube; portföyleri, ürün istisnaları ve bütün portföy performans verileriyle birlikte silinir.",
            Counts =
            [
                Impact("Portföy", portfolioIds.Count),
                Impact("Şube ürün istisnası", exclusionCount),
                Impact("Ana ürün aylık hedefi", targetCount),
                Impact("Alt ürün aylık gerçekleşmesi", actualCount)
            ],
            Warnings = portfolioIds.Count + exclusionCount + targetCount + actualCount > 0
                ? ["Bu işlem geçmiş hedef ve gerçekleşme verilerini de kalıcı olarak siler."]
                : []
        };
    }

    public async Task UpsertPortfolioTypeAsync(
        PortfolioTypeInput input, string actor, CancellationToken cancellationToken = default)
    {
        var code = NormalizeTwoCharacterCode(input.Code, "Portföy tipi kodu");
        var name = NormalizeName(input.Name, "Portföy tipi adı", 120);
        if (await db.Set<PortfolioType>().AnyAsync(item => item.Code == code && item.Id != input.Id, cancellationToken))
            throw new InvalidOperationException($"{code} kodlu portföy tipi zaten var.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        PortfolioType type;
        if (input.Id == 0)
        {
            type = new PortfolioType
            {
                Code = code,
                Name = name,
                IsActive = input.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Set<PortfolioType>().Add(type);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreatePortfolioType", "PortfolioType", type.Id.ToString(), $"{code} portföy tipi oluşturuldu.", actor);
        }
        else
        {
            type = await db.Set<PortfolioType>().FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Portföy tipi bulunamadı.");
            type.Code = code;
            type.Name = name;
            type.IsActive = input.IsActive;
            type.UpdatedAt = now;
            AddAudit("UpdatePortfolioType", "PortfolioType", type.Id.ToString(), $"{code} portföy tipi güncellendi.", actor);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetPortfolioTypeDeleteImpactAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var type = await db.Set<PortfolioType>().AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Portföy tipi bulunamadı.");
        var portfolioCount = await db.Set<Portfolio>().AsNoTracking()
            .CountAsync(portfolio => portfolio.PortfolioTypeId == id, cancellationToken);
        return new ManagementImpactViewModel
        {
            Operation = "Portföy tipini sil",
            Subject = $"{type.Code} - {type.Name}",
            Summary = portfolioCount == 0
                ? "Portföy tipi kalıcı olarak silinir."
                : "Bağlı portföyler kaldırılmadan portföy tipi silinemez.",
            Allowed = portfolioCount == 0,
            Counts = [Impact("Bağlı portföy", portfolioCount)],
            Blockers = portfolioCount > 0 ? ["Bu tipi kullanan portföyler vardır."] : []
        };
    }

    public async Task DeletePortfolioTypeAsync(
        LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        var type = await db.Set<PortfolioType>().FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Portföy tipi bulunamadı.");
        if (await db.Set<Portfolio>().AnyAsync(portfolio => portfolio.PortfolioTypeId == type.Id, cancellationToken))
            throw new InvalidOperationException("Bağlı portföyleri olan portföy tipi silinemez.");
        AddAudit("DeletePortfolioType", "PortfolioType", type.Id.ToString(), $"{type.Code} portföy tipi silindi.", actor);
        db.Set<PortfolioType>().Remove(type);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertPortfolioAsync(
        PortfolioInput input, string actor, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(input.Name, "Portföy adı");
        var branch = await db.Branches.Include(item => item.Group)
            .FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var gamut = await db.Set<ProductGamut>().FirstOrDefaultAsync(item => item.Id == input.ProductGamutId, cancellationToken)
            ?? throw new InvalidOperationException("Ürün gamı bulunamadı.");
        var type = await db.Set<PortfolioType>().FirstOrDefaultAsync(item => item.Id == input.PortfolioTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Portföy tipi bulunamadı.");
        var code = input.Id == 0 && input.GenerateCodeAutomatically
            ? await NextPortfolioCodeAsync(branch.BranchCode, gamut.Code, cancellationToken)
            : NormalizeManagementCode(input.Code, 40);
        ValidatePortfolioCode(code, branch.BranchCode, gamut.Code);
        if (gamut.GroupId != branch.GroupId)
            throw new InvalidOperationException("Portföyün ürün gamı şubenin grubuna ait olmalıdır.");
        if (input.Id == 0 && (!gamut.IsActive || !type.IsActive))
            throw new InvalidOperationException("Yeni portföy yalnız aktif ürün gamı ve portföy tipiyle oluşturulabilir.");
        if (await db.Set<Portfolio>().AnyAsync(item => item.Code == code && item.Id != input.Id, cancellationToken))
            throw new InvalidOperationException($"{code} kodlu portföy zaten var.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        Portfolio portfolio;
        if (input.Id == 0)
        {
            portfolio = new Portfolio
            {
                BranchId = branch.Id,
                GroupId = branch.GroupId,
                ProductGamutId = gamut.Id,
                PortfolioTypeId = type.Id,
                Code = code,
                Name = name,
                IsActive = input.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Set<Portfolio>().Add(portfolio);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreatePortfolio", "Portfolio", portfolio.Id.ToString(),
                $"{branch.BranchCode} şubesine {code} portföyü eklendi.", actor);
        }
        else
        {
            portfolio = await db.Set<Portfolio>().FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Portföy bulunamadı.");
            if (portfolio.GroupId != branch.GroupId)
                throw new InvalidOperationException("Portföy başka bir organizasyon grubuna taşınamaz. Hedef grupta yeni portföy oluşturun.");
            portfolio.BranchId = branch.Id;
            portfolio.GroupId = branch.GroupId;
            portfolio.ProductGamutId = gamut.Id;
            portfolio.PortfolioTypeId = type.Id;
            portfolio.Code = code;
            portfolio.Name = name;
            portfolio.IsActive = input.IsActive;
            portfolio.UpdatedAt = now;
            AddAudit("UpdatePortfolio", "Portfolio", portfolio.Id.ToString(),
                $"{branch.BranchCode} · {code} portföyü güncellendi.", actor);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetPortfolioDeleteImpactAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var portfolio = await db.Set<Portfolio>().AsNoTracking()
            .Include(item => item.Branch)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Portföy bulunamadı.");
        var targetCount = await db.Set<PortfolioMainProductMonthlyTarget>().AsNoTracking()
            .CountAsync(target => target.PortfolioId == id, cancellationToken);
        var actualCount = await db.Set<PortfolioSubProductMonthlyMetric>().AsNoTracking()
            .CountAsync(metric => metric.PortfolioId == id, cancellationToken);
        return new ManagementImpactViewModel
        {
            Operation = "Portföyü kalıcı sil",
            Subject = $"{portfolio.Code} - {portfolio.Name}",
            Summary = "Portföy ve ona bağlı bütün hedef/gerçekleşme kayıtları kalıcı olarak silinir.",
            Counts =
            [
                Impact("Ana ürün aylık hedefi", targetCount),
                Impact("Alt ürün aylık gerçekleşmesi", actualCount)
            ],
            Warnings = targetCount + actualCount > 0
                ? ["Bu işlem geçmiş portföy performans verilerini de siler."]
                : []
        };
    }

    public async Task DeletePortfolioAsync(
        LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        var portfolio = await db.Set<Portfolio>().FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Portföy bulunamadı.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var targets = await db.Set<PortfolioMainProductMonthlyTarget>()
            .Where(item => item.PortfolioId == portfolio.Id).ToListAsync(cancellationToken);
        var metrics = await db.Set<PortfolioSubProductMonthlyMetric>()
            .Where(item => item.PortfolioId == portfolio.Id).ToListAsync(cancellationToken);
        AddAudit("DeletePortfolio", "Portfolio", portfolio.Id.ToString(),
            $"{portfolio.Code} portföyü silindi; kapsam=Tüm dönemler, hedef={targets.Count}, gerçekleşme={metrics.Count}.", actor);
        db.Set<PortfolioMainProductMonthlyTarget>().RemoveRange(targets);
        db.Set<PortfolioSubProductMonthlyMetric>().RemoveRange(metrics);
        db.Set<Portfolio>().Remove(portfolio);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpsertBranchMainProductExclusionAsync(
        BranchMainProductExclusionInput input, string actor, CancellationToken cancellationToken = default)
    {
        if (input.Id == 0)
        {
            var impact = await GetBranchMainProductExclusionImpactAsync(input, cancellationToken);
            if (!impact.Allowed)
                throw new InvalidOperationException(impact.Blockers.FirstOrDefault() ?? "Şube ürün istisnası oluşturulamaz.");
        }
        ValidateEffectivePeriod(input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm);
        var branch = await db.Branches.FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.MainProductId
            && item.Type == ProductType.Main, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün bulunamadı.");
        var siblings = await db.Set<BranchMainProductExclusion>().AsNoTracking()
            .Where(item => item.BranchId == input.BranchId && item.MainProductId == input.MainProductId
                && item.Id != input.Id)
            .ToListAsync(cancellationToken);
        if (siblings.Any(item => ProductRelationshipLifecycle.PeriodsOverlap(
            input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm,
            item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm)))
            throw new InvalidOperationException("Aynı şube ve ana ürün için çakışan bir istisna aralığı vardır.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        BranchMainProductExclusion exclusion;
        if (input.Id == 0)
        {
            exclusion = new BranchMainProductExclusion
            {
                BranchId = branch.Id,
                MainProductId = product.Id,
                EffectiveFromYear = input.EffectiveFromYear,
                EffectiveFromTerm = input.EffectiveFromTerm,
                EffectiveToYear = input.EffectiveToYear,
                EffectiveToTerm = input.EffectiveToTerm,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Set<BranchMainProductExclusion>().Add(exclusion);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateBranchMainProductExclusion", "BranchMainProductExclusion", exclusion.Id.ToString(),
                $"{product.Code} ana ürünü {branch.BranchCode} şubesinin kapsamından çıkarıldı; kapsam={FormatPeriodRange(input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm)}, etkilenen istisna=1.", actor);
        }
        else
        {
            exclusion = await db.Set<BranchMainProductExclusion>().FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Şube ürün istisnası bulunamadı.");
            var previousRange = FormatPeriodRange(
                exclusion.EffectiveFromYear, exclusion.EffectiveFromTerm,
                exclusion.EffectiveToYear, exclusion.EffectiveToTerm);
            exclusion.BranchId = branch.Id;
            exclusion.MainProductId = product.Id;
            exclusion.EffectiveFromYear = input.EffectiveFromYear;
            exclusion.EffectiveFromTerm = input.EffectiveFromTerm;
            exclusion.EffectiveToYear = input.EffectiveToYear;
            exclusion.EffectiveToTerm = input.EffectiveToTerm;
            exclusion.UpdatedAt = now;
            AddAudit("UpdateBranchMainProductExclusion", "BranchMainProductExclusion", exclusion.Id.ToString(),
                $"{branch.BranchCode} · {product.Code} istisnası güncellendi; kapsam={previousRange} -> {FormatPeriodRange(input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm)}, etkilenen istisna=1.", actor);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetBranchMainProductExclusionImpactAsync(
        BranchMainProductExclusionInput input, CancellationToken cancellationToken = default)
    {
        ValidateEffectivePeriod(input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm);
        var branch = await db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == input.BranchId, cancellationToken)
            ?? throw new InvalidOperationException("Şube bulunamadı.");
        var product = await db.ProductDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == input.MainProductId && item.Type == ProductType.Main, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün bulunamadı.");
        var gamutIds = await db.Set<ProductGamut>().AsNoTracking()
            .Where(gamut => gamut.GroupId == branch.GroupId)
            .Select(gamut => gamut.Id)
            .ToListAsync(cancellationToken);
        var assignments = await db.Set<ProductGamutMainProductAssignment>().AsNoTracking()
            .Where(assignment => gamutIds.Contains(assignment.ProductGamutId)
                && assignment.MainProductId == product.Id)
            .ToListAsync(cancellationToken);
        var affectedGamutIds = assignments.Where(assignment => ProductRelationshipLifecycle.PeriodsOverlap(
                assignment.EffectiveFromYear, assignment.EffectiveFromTerm,
                assignment.EffectiveToYear, assignment.EffectiveToTerm,
                input.EffectiveFromYear, input.EffectiveFromTerm,
                input.EffectiveToYear, input.EffectiveToTerm))
            .Select(assignment => assignment.ProductGamutId)
            .Distinct()
            .ToList();
        var portfolioCount = await db.Set<Portfolio>().AsNoTracking()
            .CountAsync(portfolio => portfolio.BranchId == branch.Id
                && affectedGamutIds.Contains(portfolio.ProductGamutId), cancellationToken);
        var existing = await db.Set<BranchMainProductExclusion>().AsNoTracking()
            .Where(item => item.BranchId == input.BranchId && item.MainProductId == input.MainProductId
                && item.Id != input.Id)
            .ToListAsync(cancellationToken);
        var hasOverlap = existing.Any(item => ProductRelationshipLifecycle.PeriodsOverlap(
            item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm,
            input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm));
        var blockers = new List<string>();
        if (affectedGamutIds.Count == 0)
            blockers.Add("Ana ürün seçili dönemde şubenin grubundaki hiçbir ürün gamına atanmış değildir.");
        if (hasOverlap)
            blockers.Add("Aynı şube ve ana ürün için çakışan bir istisna aralığı vardır.");
        return new ManagementImpactViewModel
        {
            Operation = "Ana ürünü şube kapsamından çıkar",
            Subject = $"{branch.BranchCode} · {product.Code} - {product.Name}",
            Summary = $"Ana ürün {input.EffectiveFromYear}/{input.EffectiveFromTerm}. dönemden itibaren yalnız bu şubenin performans kapsamından çıkarılır; geçmiş veriler korunur.",
            Allowed = blockers.Count == 0,
            Counts =
            [
                Impact("Etkilenen ürün gamı", affectedGamutIds.Count),
                Impact("Etkilenen portföy", portfolioCount)
            ],
            Warnings = portfolioCount > 0
                ? ["Ürün seçili dönemde bu portföylerin yeni performans hesaplarına katılmaz."]
                : [],
            Blockers = blockers
        };
    }

    public async Task<ManagementImpactViewModel> GetBranchMainProductExclusionRemovalImpactAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var exclusion = await db.Set<BranchMainProductExclusion>().AsNoTracking()
            .Include(item => item.Branch).Include(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Şube ürün istisnası bulunamadı.");
        var periodRange = FormatPeriodRange(
            exclusion.EffectiveFromYear, exclusion.EffectiveFromTerm,
            exclusion.EffectiveToYear, exclusion.EffectiveToTerm);
        return new ManagementImpactViewModel
        {
            Operation = "Şube ürün istisnasını kaldır",
            Subject = $"{exclusion.Branch.BranchCode} · {exclusion.MainProduct.Code} - {exclusion.MainProduct.Name} · {periodRange}",
            Summary = $"{periodRange} aralığındaki istisna kaldırılır ve ana ürün geçerli ürün gamlarına göre yeniden şube performansına katılır.",
            Warnings = ["Saklanan hedef ve gerçekleşme kayıtları değiştirilmez."]
        };
    }

    public async Task DeleteBranchMainProductExclusionAsync(
        LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        var exclusion = await db.Set<BranchMainProductExclusion>()
            .Include(item => item.Branch).Include(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Şube ürün istisnası bulunamadı.");
        AddAudit("DeleteBranchMainProductExclusion", "BranchMainProductExclusion", exclusion.Id.ToString(),
            $"{exclusion.Branch.BranchCode} · {exclusion.MainProduct.Code} istisnası kaldırıldı; kapsam={FormatPeriodRange(exclusion.EffectiveFromYear, exclusion.EffectiveFromTerm, exclusion.EffectiveToYear, exclusion.EffectiveToTerm)}, silinen istisna=1, hedef/gerçekleşme=0 (korundu).", actor);
        db.Set<BranchMainProductExclusion>().Remove(exclusion);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureGroupNoAvailableAsync(string groupNo, int? currentId, CancellationToken cancellationToken)
    {
        if (await db.GroupDefinitions.AnyAsync(item => item.GroupNo == groupNo && item.Id != currentId, cancellationToken))
        {
            throw new InvalidOperationException($"{groupNo} numaralı grup zaten var.");
        }
    }

    private async Task EnsureBranchCodeAvailableAsync(string branchCode, int? currentId, CancellationToken cancellationToken)
    {
        if (await db.Branches.AnyAsync(item => item.BranchCode == branchCode && item.Id != currentId, cancellationToken))
        {
            throw new InvalidOperationException($"{branchCode} kodlu şube zaten var.");
        }
    }

    private async Task<string> NextGroupNoAsync(CancellationToken cancellationToken)
    {
        return NextNumber(await db.GroupDefinitions.AsNoTracking().Select(item => item.GroupNo).ToListAsync(cancellationToken));
    }

    private async Task<string> NextBranchCodeAsync(CancellationToken cancellationToken)
    {
        return NextNumber(await db.Branches.AsNoTracking().Select(item => item.BranchCode).ToListAsync(cancellationToken));
    }

    private static string NextNumber(IEnumerable<string> values)
    {
        var numericValues = values
            .Select(value => long.TryParse(value, out var number) ? (long?)number : null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        var next = numericValues.Count == 0 ? 1 : numericValues.Max() + 1;
        var width = Math.Max(4, values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Length).DefaultIfEmpty(4).Max());
        return next.ToString($"D{width}");
    }

    private static string NormalizeCode(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Kod/no boş olamaz.");
        }

        return normalized;
    }

    private async Task<string> NextPortfolioCodeAsync(
        string branchCode, string productGamutCode, CancellationToken cancellationToken)
    {
        var prefix = $"P{branchCode.ToUpperInvariant()}-{productGamutCode.ToUpperInvariant()}";
        var codes = await db.Set<Portfolio>().AsNoTracking()
            .Where(item => item.Code.StartsWith(prefix))
            .Select(item => item.Code)
            .ToListAsync(cancellationToken);
        var sequence = codes.Select(code => code.Length == prefix.Length + 2
                && int.TryParse(code[^2..], out var value) ? value : 0)
            .DefaultIfEmpty(0).Max() + 1;
        if (sequence > 99) throw new InvalidOperationException($"{prefix} için kullanılabilir portföy sıra numarası kalmadı.");
        return $"{prefix}{sequence:00}";
    }

    private static string NormalizeManagementCode(string value, int maxLength)
    {
        var code = value.Trim().ToUpperInvariant();
        if (code.Length < 1 || code.Length > maxLength || code.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            throw new InvalidOperationException($"Kod 1-{maxLength} karakter olmalı; yalnız harf, rakam, tire ve alt çizgi içerebilir.");
        return code;
    }

    private static string NormalizeTwoCharacterCode(string value, string fieldName)
    {
        var code = value.Trim().ToUpperInvariant();
        if (code.Length != 2 || code.Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new InvalidOperationException($"{fieldName} 2 karakter alfanumerik olmalı.");
        return code;
    }

    private static string NormalizeName(string value, string fieldName, int maxLength = 180)
    {
        var name = value.Trim();
        if (name.Length < 2 || name.Length > maxLength)
            throw new InvalidOperationException($"{fieldName} 2-{maxLength} karakter olmalı.");
        return name;
    }

    private static void ValidatePortfolioCode(string code, string branchCode, string productGamutCode)
    {
        var expectedPrefix = $"P{branchCode.Trim().ToUpperInvariant()}-{productGamutCode.Trim().ToUpperInvariant()}";
        if (!code.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || code.Length != expectedPrefix.Length + 2
            || !code[^2..].All(char.IsAsciiDigit))
            throw new InvalidOperationException($"Portföy kodu {expectedPrefix}01 biçiminde olmalıdır.");
    }

    private static string RewritePortfolioBranchCode(
        string currentCode, string oldBranchCode, string newBranchCode, string productGamutCode)
    {
        var expectedPrefix = $"P{oldBranchCode.Trim().ToUpperInvariant()}-{productGamutCode.Trim().ToUpperInvariant()}";
        if (!currentCode.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || currentCode.Length != expectedPrefix.Length + 2
            || !currentCode[^2..].All(char.IsAsciiDigit))
            throw new InvalidOperationException($"{currentCode} portföy kodu beklenen {expectedPrefix}01 biçiminde değil; şube kodu otomatik güncellenemedi.");
        return $"P{newBranchCode.Trim().ToUpperInvariant()}-{productGamutCode.Trim().ToUpperInvariant()}{currentCode[^2..]}";
    }

    private static void ValidateEffectivePeriod(
        int fromYear, int fromTerm, int? toYear, int? toTerm)
    {
        if (fromYear is < 2000 or > 2100 || fromTerm is < 1 or > 2)
            throw new InvalidOperationException("Başlangıç yıl/dönem bilgisini kontrol edin.");
        if (toYear.HasValue != toTerm.HasValue)
            throw new InvalidOperationException("Bitiş yılı ve dönemi birlikte girilmelidir.");
        if (toYear is < 2000 or > 2100 || toTerm is < 1 or > 2)
            throw new InvalidOperationException("Bitiş yıl/dönem bilgisini kontrol edin.");
        if (toYear.HasValue && PeriodOrdinal(toYear.Value, toTerm!.Value) < PeriodOrdinal(fromYear, fromTerm))
            throw new InvalidOperationException("Geçerlilik bitişi başlangıçtan önce olamaz.");
    }

    private static int PeriodOrdinal(int year, int term) => checked(year * 2 + term - 1);

    private static string FormatPeriodRange(int fromYear, int fromTerm, int? toYear, int? toTerm) =>
        $"{fromYear}/{fromTerm}–{(toYear.HasValue && toTerm.HasValue ? $"{toYear}/{toTerm}" : "devam")}";

    private static ManagementImpactCountViewModel Impact(string label, int count) => new()
    {
        Label = label,
        Count = count
    };

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
