using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class ProductManagementService(AppDbContext db, IProductCodeService codeService) : IProductManagementService
{
    public async Task<ProductIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var instances = await db.MainProductInstances
            .AsNoTracking()
            .Include(instance => instance.MainProduct)
            .Include(instance => instance.SubProductInstances)
                .ThenInclude(instance => instance.SubProduct)
            .OrderByDescending(instance => instance.Year)
            .ThenByDescending(instance => instance.Term)
            .ThenBy(instance => instance.MainProduct.Code)
            .ToListAsync(cancellationToken);

        var rows = new List<ProductRowViewModel>();
        foreach (var instance in instances)
        {
            var subInstances = instance.SubProductInstances
                .OrderBy(subInstance => subInstance.SubProduct.Code)
                .ToList();

            if (subInstances.Count == 0)
            {
                rows.Add(ToRow(instance, null));
                continue;
            }

            rows.AddRange(subInstances.Select(subInstance => ToRow(instance, subInstance)));
        }

        var groups = await db.GroupDefinitions.AsNoTracking()
            .OrderBy(group => group.GroupNo)
            .Select(group => new ProductGroupOptionViewModel
            {
                Id = group.Id,
                GroupNo = group.GroupNo,
                Name = group.Name
            })
            .ToListAsync(cancellationToken);
        var mainProductDefinitions = await db.ProductDefinitions.AsNoTracking()
            .Where(product => product.Type == ProductType.Main)
            .OrderBy(product => product.Code)
            .Select(product => new ProductDefinitionOptionViewModel
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                IsActive = product.IsActive
            })
            .ToListAsync(cancellationToken);
        var branches = await db.Branches.AsNoTracking()
            .Include(branch => branch.Group)
            .OrderBy(branch => branch.Group.GroupNo)
            .ThenBy(branch => branch.BranchCode)
            .Select(branch => new ProductBranchOptionViewModel
            {
                Id = branch.Id,
                GroupId = branch.GroupId,
                GroupNo = branch.Group.GroupNo,
                BranchCode = branch.BranchCode,
                Name = branch.Name
            })
            .ToListAsync(cancellationToken);
        var productGamuts = await db.Set<ProductGamut>().AsNoTracking()
            .Include(gamut => gamut.Group)
            .Include(gamut => gamut.Portfolios)
            .Include(gamut => gamut.MainProductAssignments)
                .ThenInclude(assignment => assignment.MainProduct)
            .OrderBy(gamut => gamut.Group.GroupNo)
            .ThenBy(gamut => gamut.Code)
            .Select(gamut => new ProductGamutRowViewModel
            {
                Id = gamut.Id,
                GroupId = gamut.GroupId,
                GroupNo = gamut.Group.GroupNo,
                GroupName = gamut.Group.Name,
                Code = gamut.Code,
                Name = gamut.Name,
                IsActive = gamut.IsActive,
                PortfolioCount = gamut.Portfolios.Count,
                Assignments = gamut.MainProductAssignments
                    .OrderBy(assignment => assignment.MainProduct.Code)
                    .ThenBy(assignment => assignment.EffectiveFromYear)
                    .ThenBy(assignment => assignment.EffectiveFromTerm)
                    .Select(assignment => new ProductGamutAssignmentRowViewModel
                    {
                        Id = assignment.Id,
                        MainProductId = assignment.MainProductId,
                        MainProductCode = assignment.MainProduct.Code,
                        MainProductName = assignment.MainProduct.Name,
                        EffectiveFromYear = assignment.EffectiveFromYear,
                        EffectiveFromTerm = assignment.EffectiveFromTerm,
                        EffectiveToYear = assignment.EffectiveToYear,
                        EffectiveToTerm = assignment.EffectiveToTerm
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new ProductIndexViewModel
        {
            Rows = rows,
            Groups = groups,
            Branches = branches,
            MainProductDefinitions = mainProductDefinitions,
            ProductGamuts = productGamuts,
            MainProducts = instances
                .Where(instance => instance.MainProduct.IsActive)
                .Select(instance => new MainProductOptionViewModel
                {
                    Id = instance.Id,
                    MainProductId = instance.MainProductId,
                    Code = instance.MainProduct.Code,
                    Name = instance.MainProduct.Name,
                    Year = instance.Year,
                    Term = instance.Term,
                    IsActive = instance.MainProduct.IsActive
                })
                .ToList()
        };
    }

    public async Task CreateProductAsync(CreateProductInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (input.Type == ProductType.Main)
        {
            var code = await ResolveCodeAsync(input.Type, input.CodeMode, input.ManualCode, null, cancellationToken);
            var mainProduct = await GetOrCreateProductDefinitionAsync(ProductType.Main, code, input.Name, now, cancellationToken);
            var year = input.Year ?? throw new InvalidOperationException("Ana ürün için yıl zorunlu.");
            var term = input.Term ?? throw new InvalidOperationException("Ana ürün için dönem zorunlu.");

            var exists = await db.MainProductInstances.AnyAsync(
                instance => instance.MainProductId == mainProduct.Id && instance.Year == year && instance.Term == term,
                cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"{mainProduct.Code} ana ürünü {year}/{term} döneminde zaten var.");
            }

            var instanceRecord = new MainProductInstance
            {
                MainProductId = mainProduct.Id,
                ProductDefinitionType = ProductType.Main,
                Year = year,
                Term = term,
                CreatedAt = now
            };

            db.MainProductInstances.Add(instanceRecord);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateMainProductInstance", "MainProductInstance", instanceRecord.Id.ToString(), $"{mainProduct.Code} ana ürünü {year}/{term} dönemine eklendi.", actor);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (!input.MainProductInstanceId.HasValue)
        {
            throw new InvalidOperationException("Alt ürün için bağlı ana ürün seçilmeli.");
        }

        var mainInstance = await db.MainProductInstances
            .Include(instance => instance.MainProduct)
            .FirstOrDefaultAsync(instance => instance.Id == input.MainProductInstanceId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Bağlı ana ürün instance kaydı bulunamadı.");

        if (!mainInstance.MainProduct.IsActive)
        {
            throw new InvalidOperationException("Pasif ana ürüne alt ürün eklenemez.");
        }

        var subCode = await ResolveCodeAsync(input.Type, input.CodeMode, input.ManualCode, mainInstance.Id, cancellationToken);
        var subProduct = await GetOrCreateProductDefinitionAsync(ProductType.Sub, subCode, input.Name, now, cancellationToken);

        if (!subProduct.IsActive)
        {
            throw new InvalidOperationException("Pasif alt ürün bağlanamaz.");
        }

        var subInstanceExists = await db.SubProductInstances.AnyAsync(
            instance => instance.MainProductInstanceId == mainInstance.Id && instance.SubProductId == subProduct.Id,
            cancellationToken);

        if (subInstanceExists)
        {
            var suggestion = await codeService.SuggestCodeAsync(ProductType.Sub, subCode, mainInstance.Id, cancellationToken);
            throw DuplicateCodeException(subCode, suggestion);
        }

        var subInstanceRecord = new SubProductInstance
        {
            MainProductInstanceId = mainInstance.Id,
            SubProductId = subProduct.Id,
            ProductDefinitionType = ProductType.Sub,
            CreatedAt = now
        };

        db.SubProductInstances.Add(subInstanceRecord);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateSubProductInstance", "SubProductInstance", subInstanceRecord.Id.ToString(), $"{subProduct.Code} alt ürünü {mainInstance.MainProduct.Code} {mainInstance.Year}/{mainInstance.Term} kaydına bağlandı.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RenameProductAsync(RenameProductInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.ProductId && item.Type == input.Type, cancellationToken)
            ?? throw new InvalidOperationException("Ürün tanımı bulunamadı.");

        var oldCode = product.Code;
        var oldName = product.Name;
        var newCode = ResolveOptionalCode(input.Code, product.Code);

        if (newCode != product.Code && await db.ProductDefinitions.AnyAsync(item => item.Type == input.Type && item.Code == newCode, cancellationToken))
        {
            throw new InvalidOperationException($"{newCode} kodlu {ProductTypeName(input.Type)} zaten var.");
        }

        product.Code = newCode;
        product.Name = input.Name.Trim();
        product.UpdatedAt = now;
        AddAudit("UpdateProductDefinition", "ProductDefinition", product.Id.ToString(), $"{oldCode} '{oldName}' -> {product.Code} '{product.Name}' güncellendi.", actor);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeactivateProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.ProductId && item.Type == input.Type, cancellationToken)
            ?? throw new InvalidOperationException("Ürün tanımı bulunamadı.");

        product.IsActive = false;
        product.UpdatedAt = now;
        AddAudit("DeactivateProductDefinition", "ProductDefinition", product.Id.ToString(), $"{product.Code} {ProductTypeName(input.Type)} pasifleştirildi.", actor);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var deleteScope = NormalizeDeleteScope(input.DeleteScope);
        var deleteAll = deleteScope == "All";

        if (input.Type == ProductType.Main)
        {
            if (deleteAll)
            {
                var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.ProductId && item.Type == ProductType.Main, cancellationToken)
                    ?? throw new InvalidOperationException("Ana ürün tanımı bulunamadı.");
                if (!ProductRelationshipLifecycle.MatchesConfirmationCode(product.Code, input.ConfirmationCode))
                    throw new InvalidOperationException($"Kalıcı silme için {product.Code} ürün kodunu doğrulayın.");

                var instanceIds = await db.MainProductInstances
                    .Where(instance => instance.MainProductId == product.Id)
                    .Select(instance => instance.Id)
                    .ToListAsync(cancellationToken);
                var targetSubLinks = await db.SubProductInstances
                    .Where(link => instanceIds.Contains(link.MainProductInstanceId))
                    .ToListAsync(cancellationToken);
                var linkedSubProductIds = targetSubLinks.Select(link => link.SubProductId).Distinct().ToList();
                var sharedSubProductIds = await db.SubProductInstances.AsNoTracking()
                    .Where(link => linkedSubProductIds.Contains(link.SubProductId)
                        && !instanceIds.Contains(link.MainProductInstanceId))
                    .Select(link => link.SubProductId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var orphanSubProductIds = ProductRelationshipLifecycle.FindOrphanSubProductIds(
                    linkedSubProductIds, sharedSubProductIds).ToList();
                var orphanSubProducts = await db.ProductDefinitions
                    .Where(item => orphanSubProductIds.Contains(item.Id) && item.Type == ProductType.Sub)
                    .ToListAsync(cancellationToken);
                var parameterIds = await db.MainProductParameters.AsNoTracking()
                    .Where(parameter => instanceIds.Contains(parameter.MainProductInstanceId))
                    .Select(parameter => parameter.Id)
                    .ToListAsync(cancellationToken);
                var targetCount = await db.Set<PortfolioMainProductMonthlyTarget>().AsNoTracking()
                    .CountAsync(target => parameterIds.Contains(target.MainProductParameterId), cancellationToken);
                var portfolioMetrics = await db.Set<PortfolioSubProductMonthlyMetric>()
                    .Where(metric => orphanSubProductIds.Contains(metric.SubProductId))
                    .ToListAsync(cancellationToken);
                var gamutAssignments = await db.Set<ProductGamutMainProductAssignment>()
                    .Where(assignment => assignment.MainProductId == product.Id)
                    .ToListAsync(cancellationToken);
                var branchExclusions = await db.Set<BranchMainProductExclusion>()
                    .Where(exclusion => exclusion.MainProductId == product.Id)
                    .ToListAsync(cancellationToken);
                db.Set<PortfolioSubProductMonthlyMetric>().RemoveRange(portfolioMetrics);
                db.Set<ProductGamutMainProductAssignment>().RemoveRange(gamutAssignments);
                db.Set<BranchMainProductExclusion>().RemoveRange(branchExclusions);
                db.SubProductInstances.RemoveRange(targetSubLinks);
                db.ProductDefinitions.RemoveRange(orphanSubProducts);
                AddAudit("DeleteMainProductDefinition", "ProductDefinition", product.Id.ToString(),
                    $"{product.Code} ana ürünü silindi; kapsam=Tüm dönemler, dönem={instanceIds.Count}, parametre={parameterIds.Count}, hedef={targetCount}, gam ataması={gamutAssignments.Count}, şube istisnası={branchExclusions.Count}, gerçekleşme={portfolioMetrics.Count}, yetim alt ürün={orphanSubProducts.Count}, korunan ortak alt ürün={sharedSubProductIds.Count}.", actor);
                db.ProductDefinitions.Remove(product);
            }
            else
            {
                var instance = await db.MainProductInstances
                    .Include(item => item.MainProduct)
                    .FirstOrDefaultAsync(item => item.Id == input.MainProductInstanceId, cancellationToken)
                    ?? throw new InvalidOperationException("Ana ürün instance kaydı bulunamadı.");

                var hasLinkedSubProducts = await db.SubProductInstances
                    .AnyAsync(subInstance => subInstance.MainProductInstanceId == instance.Id, cancellationToken);

                if (hasLinkedSubProducts)
                {
                    throw new InvalidOperationException($"{instance.MainProduct.Code} {instance.Year}/{instance.Term} kaydına bağlı alt ürünler var. Önce alt ürün instance'larını silmelisiniz.");
                }

                AddAudit("DeleteMainProductInstance", "MainProductInstance", instance.Id.ToString(), $"{instance.MainProduct.Code} {instance.Year}/{instance.Term} instance kaydı silindi.", actor);
                db.MainProductInstances.Remove(instance);
            }
        }
        else
        {
            if (deleteAll)
            {
                var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.ProductId && item.Type == ProductType.Sub, cancellationToken)
                    ?? throw new InvalidOperationException("Alt ürün tanımı bulunamadı.");

                var linkCount = await db.SubProductInstances.AsNoTracking()
                    .CountAsync(item => item.SubProductId == product.Id, cancellationToken);
                var metricCount = await db.Set<PortfolioSubProductMonthlyMetric>().AsNoTracking()
                    .CountAsync(item => item.SubProductId == product.Id, cancellationToken);
                AddAudit("DeleteSubProductDefinition", "ProductDefinition", product.Id.ToString(),
                    $"{product.Code} alt ürünü silindi; kapsam=Tüm dönemler, ana ürün bağlantısı={linkCount}, gerçekleşme={metricCount}.", actor);
                db.ProductDefinitions.Remove(product);
            }
            else
            {
                var instance = await db.SubProductInstances
                    .Include(item => item.SubProduct)
                    .FirstOrDefaultAsync(item => item.Id == input.SubProductInstanceId, cancellationToken)
                    ?? throw new InvalidOperationException("Alt ürün instance kaydı bulunamadı.");

                AddAudit("DeleteSubProductInstance", "SubProductInstance", instance.Id.ToString(),
                    $"{instance.SubProduct.Code} alt ürün bağlantısı kaldırıldı; kapsam=Seçili dönem, gerçekleşme=0 (stabil ham kayıtlar korundu).", actor);
                db.SubProductInstances.Remove(instance);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetProductDeleteImpactAsync(
        ProductIdInput input, CancellationToken cancellationToken = default)
    {
        var deleteAll = NormalizeDeleteScope(input.DeleteScope) == "All";
        var product = await db.ProductDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == input.ProductId && item.Type == input.Type, cancellationToken)
            ?? throw new InvalidOperationException("Ürün tanımı bulunamadı.");

        if (input.Type == ProductType.Main)
        {
            var instanceQuery = db.MainProductInstances.AsNoTracking()
                .Where(instance => instance.MainProductId == product.Id);
            if (!deleteAll)
            {
                if (!input.MainProductInstanceId.HasValue)
                    throw new InvalidOperationException("Silinecek ana ürün dönem kaydı belirtilmedi.");
                instanceQuery = instanceQuery.Where(instance => instance.Id == input.MainProductInstanceId.Value);
            }
            var instanceIds = await instanceQuery.Select(instance => instance.Id).ToListAsync(cancellationToken);
            if (!deleteAll && instanceIds.Count == 0)
                throw new InvalidOperationException("Ana ürün dönem kaydı bulunamadı.");
            var subLinks = await db.SubProductInstances.AsNoTracking()
                .Where(link => instanceIds.Contains(link.MainProductInstanceId))
                .Select(link => new { link.Id, link.SubProductId })
                .ToListAsync(cancellationToken);
            var subProductIds = subLinks.Select(link => link.SubProductId).Distinct().ToList();
            var sharedSubProductIds = deleteAll
                ? await db.SubProductInstances.AsNoTracking()
                    .Where(link => subProductIds.Contains(link.SubProductId)
                        && !instanceIds.Contains(link.MainProductInstanceId))
                    .Select(link => link.SubProductId).Distinct().ToListAsync(cancellationToken)
                : [];
            var orphanSubProductIds = deleteAll
                ? ProductRelationshipLifecycle.FindOrphanSubProductIds(subProductIds, sharedSubProductIds).ToList()
                : [];
            var parameterIds = await db.MainProductParameters.AsNoTracking()
                .Where(parameter => instanceIds.Contains(parameter.MainProductInstanceId))
                .Select(parameter => parameter.Id)
                .ToListAsync(cancellationToken);
            var targetCount = await db.Set<PortfolioMainProductMonthlyTarget>().AsNoTracking()
                .CountAsync(target => parameterIds.Contains(target.MainProductParameterId), cancellationToken);
            var actualCount = deleteAll
                ? await db.Set<PortfolioSubProductMonthlyMetric>().AsNoTracking()
                    .CountAsync(metric => orphanSubProductIds.Contains(metric.SubProductId), cancellationToken)
                : 0;
            var assignmentCount = deleteAll
                ? await db.Set<ProductGamutMainProductAssignment>().AsNoTracking()
                    .CountAsync(assignment => assignment.MainProductId == product.Id, cancellationToken)
                : 0;
            var exclusionCount = deleteAll
                ? await db.Set<BranchMainProductExclusion>().AsNoTracking()
                    .CountAsync(exclusion => exclusion.MainProductId == product.Id, cancellationToken)
                : 0;
            var blockers = !deleteAll && subLinks.Count > 0
                ? new[] { "Dönem kaydına bağlı alt ürünler vardır. Önce bu bağlantıları kaldırın veya tüm dönemlerden güvenli silmeyi kullanın." }
                : [];

            return new ManagementImpactViewModel
            {
                Operation = deleteAll ? "Ana ürünü kalıcı sil" : "Ana ürün dönem kaydını sil",
                Subject = $"{product.Code} - {product.Name}",
                Summary = deleteAll
                    ? "Ana ürün tüm dönemlerden kaldırılır; yalnız bu ana ürünü besleyen alt ürünler silinir, ortak alt ürünler korunur."
                    : "Yalnız seçili dönem kaydı ve ona bağlı parametre/hedefler kaldırılır.",
                Allowed = blockers.Length == 0,
                Counts =
                [
                    Impact("Dönem kaydı", instanceIds.Count),
                    Impact("Grup parametresi", parameterIds.Count),
                    Impact("Portföy ana ürün hedefi", targetCount),
                    Impact("Alt ürün bağlantısı", subLinks.Count),
                    Impact("Portföy gerçekleşme kaydı", actualCount),
                    Impact("Silinecek yetim alt ürün", orphanSubProductIds.Count),
                    Impact("Korunacak ortak alt ürün", sharedSubProductIds.Count),
                    Impact("Ürün gamı ataması", assignmentCount),
                    Impact("Şube istisnası", exclusionCount)
                ],
                Warnings = deleteAll
                    ? ["Kalıcı silme geçmiş dönem hedeflerini ve yalnız yetim alt ürünlere ait gerçekleşmeleri siler; ortak alt ürün ham kayıtları korunur."]
                    : ["Dönem silme geri alınamaz."],
                Blockers = blockers
            };
        }

        var subInstanceQuery = db.SubProductInstances.AsNoTracking().Where(link => link.SubProductId == product.Id);
        if (!deleteAll)
        {
            if (!input.SubProductInstanceId.HasValue)
                throw new InvalidOperationException("Silinecek alt ürün bağlantısı belirtilmedi.");
            subInstanceQuery = subInstanceQuery.Where(link => link.Id == input.SubProductInstanceId.Value);
        }
        var subInstanceIds = await subInstanceQuery.Select(link => link.Id).ToListAsync(cancellationToken);
        if (!deleteAll && subInstanceIds.Count == 0)
            throw new InvalidOperationException("Alt ürün bağlantısı bulunamadı.");
        var portfolioMetricCount = deleteAll
            ? await db.Set<PortfolioSubProductMonthlyMetric>().AsNoTracking()
                .CountAsync(metric => metric.SubProductId == product.Id, cancellationToken)
            : 0;
        return new ManagementImpactViewModel
        {
            Operation = deleteAll ? "Alt ürünü kalıcı sil" : "Alt ürün bağlantısını kaldır",
            Subject = $"{product.Code} - {product.Name}",
            Summary = deleteAll
                ? "Alt ürün bütün ana ürünlerden ve geçmiş ölçüm kayıtlarından kaldırılır."
                : "Yalnız seçili ana ürün-dönem bağlantısı kaldırılır.",
            Counts =
            [
                Impact("Ana ürün bağlantısı", subInstanceIds.Count),
                Impact("Portföy gerçekleşme kaydı", portfolioMetricCount)
            ],
            Warnings = deleteAll
                ? ["Ortak kullanılan alt ürün de bütün ana ürünlerden kaldırılır."]
                : ["Yalnız ilişki kaldırılır; portföyün stabil alt ürün gerçekleşme kayıtları korunur."]
        };
    }

    public async Task UpsertProductGamutAsync(
        ProductGamutInput input, string actor, CancellationToken cancellationToken = default)
    {
        var code = NormalizeTwoCharacterCode(input.Code, "Ürün gamı kodu");
        var name = NormalizeName(input.Name, "Ürün gamı adı", 120);
        var group = await db.GroupDefinitions.FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        if (await db.Set<ProductGamut>().AnyAsync(item => item.GroupId == input.GroupId
            && item.Code == code && item.Id != input.Id, cancellationToken))
            throw new InvalidOperationException($"{group.GroupNo} grubunda {code} kodlu ürün gamı zaten var.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        ProductGamut gamut;
        if (input.Id == 0)
        {
            gamut = new ProductGamut
            {
                GroupId = input.GroupId,
                Code = code,
                Name = name,
                IsActive = input.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Set<ProductGamut>().Add(gamut);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateProductGamut", "ProductGamut", gamut.Id.ToString(),
                $"{group.GroupNo} grubuna {code} ürün gamı eklendi.", actor);
        }
        else
        {
            gamut = await db.Set<ProductGamut>()
                .Include(item => item.Portfolios).ThenInclude(portfolio => portfolio.Branch)
                .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Ürün gamı bulunamadı.");
            if (gamut.GroupId != input.GroupId)
                throw new InvalidOperationException("Ürün gamı başka gruba taşınamaz. Hedef grupta yeni bir ürün gamı oluşturun.");
            var oldCode = gamut.Code;
            if (!string.Equals(oldCode, code, StringComparison.Ordinal))
            {
                var portfolioIds = gamut.Portfolios.Select(portfolio => portfolio.Id).ToList();
                var rewrittenCodes = gamut.Portfolios.ToDictionary(
                    portfolio => portfolio.Id,
                    portfolio => RewritePortfolioCode(portfolio.Code, portfolio.Branch.BranchCode, oldCode, code));
                var rewrittenCodeValues = rewrittenCodes.Values.ToList();
                var duplicateCode = rewrittenCodeValues
                    .GroupBy(value => value, StringComparer.Ordinal).FirstOrDefault(grouping => grouping.Count() > 1)?.Key;
                if (duplicateCode is not null || await db.Set<Portfolio>().AsNoTracking()
                        .AnyAsync(portfolio => !portfolioIds.Contains(portfolio.Id)
                            && rewrittenCodeValues.Contains(portfolio.Code), cancellationToken))
                    throw new InvalidOperationException("Ürün gamı kodu değişikliği bağlı portföy kodlarıyla çakışıyor.");
                foreach (var portfolio in gamut.Portfolios)
                    portfolio.Code = rewrittenCodes[portfolio.Id];
            }
            gamut.GroupId = input.GroupId;
            gamut.Code = code;
            gamut.Name = name;
            gamut.IsActive = input.IsActive;
            gamut.UpdatedAt = now;
            AddAudit("UpdateProductGamut", "ProductGamut", gamut.Id.ToString(),
                $"{group.GroupNo} · {code} ürün gamı güncellendi.", actor);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetProductGamutDeleteImpactAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var gamut = await db.Set<ProductGamut>().AsNoTracking()
            .Include(item => item.Group)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Ürün gamı bulunamadı.");
        var portfolioCount = await db.Set<Portfolio>().AsNoTracking()
            .CountAsync(portfolio => portfolio.ProductGamutId == id, cancellationToken);
        var assignmentCount = await db.Set<ProductGamutMainProductAssignment>().AsNoTracking()
            .CountAsync(assignment => assignment.ProductGamutId == id, cancellationToken);
        return new ManagementImpactViewModel
        {
            Operation = "Ürün gamını sil",
            Subject = $"{gamut.Group.GroupNo} · {gamut.Code} - {gamut.Name}",
            Summary = portfolioCount == 0
                ? "Ürün gamı ve ana ürün atamaları kalıcı olarak silinir."
                : "Bağlı portföyler kaldırılmadan ürün gamı silinemez.",
            Allowed = portfolioCount == 0,
            Counts = [Impact("Ana ürün ataması", assignmentCount), Impact("Bağlı portföy", portfolioCount)],
            Warnings = assignmentCount > 0 ? ["Ürün gamındaki bütün ana ürün atamaları da silinir."] : [],
            Blockers = portfolioCount > 0 ? ["Ürün gamına bağlı portföyler vardır."] : []
        };
    }

    public async Task DeleteProductGamutAsync(
        LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        var gamut = await db.Set<ProductGamut>().Include(item => item.MainProductAssignments)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Ürün gamı bulunamadı.");
        if (await db.Set<Portfolio>().AnyAsync(portfolio => portfolio.ProductGamutId == gamut.Id, cancellationToken))
            throw new InvalidOperationException("Bağlı portföyleri olan ürün gamı silinemez.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        AddAudit("DeleteProductGamut", "ProductGamut", gamut.Id.ToString(),
            $"{gamut.Code} ürün gamı ve {gamut.MainProductAssignments.Count} ana ürün ataması silindi.", actor);
        db.Set<ProductGamutMainProductAssignment>().RemoveRange(gamut.MainProductAssignments);
        db.Set<ProductGamut>().Remove(gamut);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpsertProductGamutAssignmentAsync(
        ProductGamutAssignmentInput input, string actor, CancellationToken cancellationToken = default)
    {
        ValidateEffectivePeriod(input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm);
        var gamut = await db.Set<ProductGamut>().Include(item => item.Group)
            .FirstOrDefaultAsync(item => item.Id == input.ProductGamutId, cancellationToken)
            ?? throw new InvalidOperationException("Ürün gamı bulunamadı.");
        var mainProduct = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.MainProductId
            && item.Type == ProductType.Main, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün bulunamadı.");
        var siblings = await db.Set<ProductGamutMainProductAssignment>().AsNoTracking()
            .Where(item => item.ProductGamutId == input.ProductGamutId
                && item.MainProductId == input.MainProductId && item.Id != input.Id)
            .ToListAsync(cancellationToken);
        if (siblings.Any(item => ProductRelationshipLifecycle.PeriodsOverlap(
            input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm,
            item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm)))
            throw new InvalidOperationException("Aynı ana ürün için çakışan bir ürün gamı geçerlilik aralığı vardır.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        ProductGamutMainProductAssignment assignment;
        if (input.Id == 0)
        {
            assignment = new ProductGamutMainProductAssignment
            {
                ProductGamutId = input.ProductGamutId,
                MainProductId = input.MainProductId,
                EffectiveFromYear = input.EffectiveFromYear,
                EffectiveFromTerm = input.EffectiveFromTerm,
                EffectiveToYear = input.EffectiveToYear,
                EffectiveToTerm = input.EffectiveToTerm,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Set<ProductGamutMainProductAssignment>().Add(assignment);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateProductGamutAssignment", "ProductGamutMainProductAssignment", assignment.Id.ToString(),
                $"{gamut.Group.GroupNo} · {gamut.Code} ürün gamına {mainProduct.Code} ana ürünü atandı; kapsam={FormatPeriodRange(input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm)}, etkilenen atama=1.", actor);
        }
        else
        {
            assignment = await db.Set<ProductGamutMainProductAssignment>()
                .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Ürün gamı ataması bulunamadı.");
            var previousRange = FormatPeriodRange(
                assignment.EffectiveFromYear, assignment.EffectiveFromTerm,
                assignment.EffectiveToYear, assignment.EffectiveToTerm);
            assignment.ProductGamutId = input.ProductGamutId;
            assignment.MainProductId = input.MainProductId;
            assignment.EffectiveFromYear = input.EffectiveFromYear;
            assignment.EffectiveFromTerm = input.EffectiveFromTerm;
            assignment.EffectiveToYear = input.EffectiveToYear;
            assignment.EffectiveToTerm = input.EffectiveToTerm;
            assignment.UpdatedAt = now;
            AddAudit("UpdateProductGamutAssignment", "ProductGamutMainProductAssignment", assignment.Id.ToString(),
                $"{gamut.Group.GroupNo} · {gamut.Code} / {mainProduct.Code} ataması güncellendi; kapsam={previousRange} -> {FormatPeriodRange(input.EffectiveFromYear, input.EffectiveFromTerm, input.EffectiveToYear, input.EffectiveToTerm)}, etkilenen atama=1.", actor);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetProductGamutAssignmentRemovalImpactAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var assignment = await db.Set<ProductGamutMainProductAssignment>().AsNoTracking()
            .Include(item => item.ProductGamut)
            .Include(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Ürün gamı ataması bulunamadı.");
        var portfolioCount = await db.Set<Portfolio>().AsNoTracking()
            .CountAsync(portfolio => portfolio.ProductGamutId == assignment.ProductGamutId, cancellationToken);
        var periodRange = FormatPeriodRange(
            assignment.EffectiveFromYear, assignment.EffectiveFromTerm,
            assignment.EffectiveToYear, assignment.EffectiveToTerm);
        return new ManagementImpactViewModel
        {
            Operation = "Ana ürünü ürün gamından çıkar",
            Subject = $"{assignment.ProductGamut.Code} · {assignment.MainProduct.Code} - {assignment.MainProduct.Name} · {periodRange}",
            Summary = $"{periodRange} aralığındaki atama kalıcı olarak kaldırılır; saklanan hedef ve gerçekleşme kayıtları silinmez.",
            Counts = [Impact("Etkilenen portföy", portfolioCount)],
            Warnings = portfolioCount > 0
                ? ["Bu ürün, ilgili ürün gamını kullanan portföylerin yeni performans hesaplarına katılmaz."]
                : []
        };
    }

    public async Task DeleteProductGamutAssignmentAsync(
        LinkIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        var assignment = await db.Set<ProductGamutMainProductAssignment>()
            .Include(item => item.ProductGamut).Include(item => item.MainProduct)
            .FirstOrDefaultAsync(item => item.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Ürün gamı ataması bulunamadı.");
        AddAudit("DeleteProductGamutAssignment", "ProductGamutMainProductAssignment", assignment.Id.ToString(),
            $"{assignment.MainProduct.Code} ana ürünü {assignment.ProductGamut.Code} ürün gamından çıkarıldı; kapsam={FormatPeriodRange(assignment.EffectiveFromYear, assignment.EffectiveFromTerm, assignment.EffectiveToYear, assignment.EffectiveToTerm)}, silinen atama=1, hedef/gerçekleşme=0 (korundu).", actor);
        db.Set<ProductGamutMainProductAssignment>().Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetGroupMainProductRemovalImpactAsync(
        GroupMainProductRemovalInput input, CancellationToken cancellationToken = default)
    {
        ValidateEffectivePeriod(input.EffectiveFromYear, input.EffectiveFromTerm, null, null);
        var group = await db.GroupDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == input.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Grup bulunamadı.");
        var product = await db.ProductDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == input.MainProductId && item.Type == ProductType.Main, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün bulunamadı.");
        var gamutIds = await db.Set<ProductGamut>().AsNoTracking()
            .Where(item => item.GroupId == input.GroupId).Select(item => item.Id).ToListAsync(cancellationToken);
        var assignments = await db.Set<ProductGamutMainProductAssignment>().AsNoTracking()
            .Where(item => gamutIds.Contains(item.ProductGamutId) && item.MainProductId == input.MainProductId)
            .ToListAsync(cancellationToken);
        var removalPeriod = new PerformancePeriod(input.EffectiveFromYear, input.EffectiveFromTerm);
        var affectedAssignments = assignments.Where(item => ProductRelationshipLifecycle.PlanRemoval(
                item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm,
                removalPeriod) != EffectiveAssignmentRemovalAction.None)
            .ToList();
        var affected = affectedAssignments.Count;
        var affectedGamutIds = affectedAssignments.Select(item => item.ProductGamutId).Distinct().ToList();
        var portfolioCount = await db.Set<Portfolio>().AsNoTracking()
            .CountAsync(item => affectedGamutIds.Contains(item.ProductGamutId), cancellationToken);
        return new ManagementImpactViewModel
        {
            Operation = "Ana ürünü gruptan çıkar",
            Subject = $"{group.GroupNo} · {product.Code} - {product.Name}",
            Summary = $"Ürün {input.EffectiveFromYear}/{input.EffectiveFromTerm}. dönemden itibaren grubun bütün ürün gamlarından çıkarılır; geçmiş dönem atamaları ve ölçüm verileri korunur.",
            Allowed = affected > 0,
            Counts = [Impact("Kapatılacak ürün gamı ataması", affected), Impact("Kapsamdaki portföy", portfolioCount)],
            Warnings = portfolioCount > 0 ? ["Bu ürün yeni dönemlerde ilgili portföylerin performans hesabına katılmaz."] : [],
            Blockers = affected == 0 ? ["Seçilen dönemde kapatılabilecek etkin bir ürün gamı ataması yok."] : []
        };
    }

    public async Task RemoveMainProductFromGroupAsync(
        GroupMainProductRemovalInput input, string actor, CancellationToken cancellationToken = default)
    {
        var impact = await GetGroupMainProductRemovalImpactAsync(input, cancellationToken);
        if (!impact.Allowed) throw new InvalidOperationException(impact.Blockers.FirstOrDefault() ?? "Kapatılabilecek atama yok.");
        var gamutIds = await db.Set<ProductGamut>().AsNoTracking()
            .Where(item => item.GroupId == input.GroupId).Select(item => item.Id).ToListAsync(cancellationToken);
        var assignments = await db.Set<ProductGamutMainProductAssignment>()
            .Where(item => gamutIds.Contains(item.ProductGamutId) && item.MainProductId == input.MainProductId)
            .ToListAsync(cancellationToken);
        var removalPeriod = new PerformancePeriod(input.EffectiveFromYear, input.EffectiveFromTerm);
        var affected = ApplyAssignmentRemoval(assignments, removalPeriod);
        AddAudit("RemoveMainProductFromGroup", "ProductDefinition", input.MainProductId.ToString(),
            $"Ana ürün {input.GroupId} grubundan {input.EffectiveFromYear}/{input.EffectiveFromTerm} döneminden itibaren çıkarıldı; {affected} ürün gamı ataması kapatıldı.", actor);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ManagementImpactViewModel> GetProductGamutMainProductRemovalImpactAsync(
        ProductGamutMainProductRemovalInput input, CancellationToken cancellationToken = default)
    {
        ValidateEffectivePeriod(input.EffectiveFromYear, input.EffectiveFromTerm, null, null);
        var gamut = await db.Set<ProductGamut>().AsNoTracking().Include(item => item.Group)
            .FirstOrDefaultAsync(item => item.Id == input.ProductGamutId, cancellationToken)
            ?? throw new InvalidOperationException("Ürün gamı bulunamadı.");
        var product = await db.ProductDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == input.MainProductId && item.Type == ProductType.Main, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün bulunamadı.");
        var assignments = await db.Set<ProductGamutMainProductAssignment>().AsNoTracking()
            .Where(item => item.ProductGamutId == input.ProductGamutId && item.MainProductId == input.MainProductId)
            .ToListAsync(cancellationToken);
        var removalPeriod = new PerformancePeriod(input.EffectiveFromYear, input.EffectiveFromTerm);
        var affected = assignments.Count(item => ProductRelationshipLifecycle.PlanRemoval(
            item.EffectiveFromYear, item.EffectiveFromTerm, item.EffectiveToYear, item.EffectiveToTerm,
            removalPeriod) != EffectiveAssignmentRemovalAction.None);
        var portfolioCount = await db.Set<Portfolio>().AsNoTracking()
            .CountAsync(item => item.ProductGamutId == input.ProductGamutId, cancellationToken);
        return new ManagementImpactViewModel
        {
            Operation = "Ana ürünü ürün gamından çıkar",
            Subject = $"{gamut.Group.GroupNo} · {gamut.Code} · {product.Code} - {product.Name}",
            Summary = $"Ürün {input.EffectiveFromYear}/{input.EffectiveFromTerm}. dönemden itibaren yalnız bu ürün gamından çıkarılır; geçmiş atamalar ve ölçüm verileri korunur.",
            Allowed = affected > 0,
            Counts = [Impact("Kapatılacak ürün gamı ataması", affected), Impact("Kapsamdaki portföy", portfolioCount)],
            Warnings = portfolioCount > 0 ? ["Ürün yeni dönemlerde bu gamı kullanan portföylerin performans hesabına katılmaz."] : [],
            Blockers = affected == 0 ? ["Seçilen dönemde kapatılabilecek etkin bir ürün gamı ataması yok."] : []
        };
    }

    public async Task RemoveMainProductFromProductGamutAsync(
        ProductGamutMainProductRemovalInput input, string actor, CancellationToken cancellationToken = default)
    {
        var impact = await GetProductGamutMainProductRemovalImpactAsync(input, cancellationToken);
        if (!impact.Allowed)
            throw new InvalidOperationException(impact.Blockers.FirstOrDefault() ?? "Kapatılabilecek atama yok.");
        var assignments = await db.Set<ProductGamutMainProductAssignment>()
            .Where(item => item.ProductGamutId == input.ProductGamutId && item.MainProductId == input.MainProductId)
            .ToListAsync(cancellationToken);
        var affected = ApplyAssignmentRemoval(
            assignments, new PerformancePeriod(input.EffectiveFromYear, input.EffectiveFromTerm));
        AddAudit("RemoveMainProductFromProductGamut", "ProductDefinition", input.MainProductId.ToString(),
            $"Ana ürün {input.ProductGamutId} ürün gamından {input.EffectiveFromYear}/{input.EffectiveFromTerm} döneminden itibaren çıkarıldı; {affected} atama kapatıldı.", actor);
        await db.SaveChangesAsync(cancellationToken);
    }

    private int ApplyAssignmentRemoval(
        IEnumerable<ProductGamutMainProductAssignment> assignments, PerformancePeriod removalPeriod)
    {
        var previousPeriod = removalPeriod.Previous();
        var affected = 0;
        foreach (var assignment in assignments)
        {
            var action = ProductRelationshipLifecycle.PlanRemoval(
                assignment.EffectiveFromYear, assignment.EffectiveFromTerm,
                assignment.EffectiveToYear, assignment.EffectiveToTerm, removalPeriod);
            if (action == EffectiveAssignmentRemovalAction.None) continue;
            if (action == EffectiveAssignmentRemovalAction.Delete)
                db.Set<ProductGamutMainProductAssignment>().Remove(assignment);
            else
            {
                assignment.EffectiveToYear = previousPeriod.Year;
                assignment.EffectiveToTerm = previousPeriod.Term;
                assignment.UpdatedAt = DateTimeOffset.UtcNow;
            }
            affected++;
        }
        return affected;
    }

    private async Task<ProductDefinition> GetOrCreateProductDefinitionAsync(ProductType type, string code, string name, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Type == type && item.Code == code, cancellationToken);
        if (product is not null)
        {
            if (!product.IsActive)
            {
                throw new InvalidOperationException($"Pasif {ProductTypeName(type)} kullanılamaz.");
            }

            return product;
        }

        product = new ProductDefinition
        {
            Type = type,
            Code = code,
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.ProductDefinitions.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return product;
    }

    private async Task<string> ResolveCodeAsync(ProductType type, string codeMode, string? manualCode, int? mainProductInstanceId, CancellationToken cancellationToken)
    {
        var code = codeMode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
            ? codeService.NormalizeCode(manualCode ?? string.Empty)
            : await codeService.GenerateNextCodeAsync(type, mainProductInstanceId, cancellationToken);

        if (!codeService.IsValidCode(code))
        {
            throw new InvalidOperationException("Ürün kodu 2 karakter alfanumerik olmalı.");
        }

        return code;
    }

    private string ResolveOptionalCode(string? code, string fallbackCode)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return fallbackCode;
        }

        var normalized = codeService.NormalizeCode(code);
        if (!codeService.IsValidCode(normalized))
        {
            throw new InvalidOperationException("Ürün kodu 2 karakter alfanumerik olmalı.");
        }

        return normalized;
    }

    private static InvalidOperationException DuplicateCodeException(string code, string? suggestion)
    {
        return new InvalidOperationException(suggestion is null
            ? "Bu ana ürün altında boş 2 karakterli alt ürün kodu kalmadı."
            : $"{code} kodu bu ana ürün instance'ında zaten bağlı. En yakın uygun kod: {suggestion}.");
    }

    private static string ProductTypeName(ProductType type)
    {
        return type == ProductType.Main ? "ana ürün" : "alt ürün";
    }

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

    private static ProductRowViewModel ToRow(MainProductInstance mainInstance, SubProductInstance? subInstance)
    {
        return new ProductRowViewModel
        {
            MainProductId = mainInstance.MainProductId,
            MainProductInstanceId = mainInstance.Id,
            SubProductId = subInstance?.SubProductId,
            SubProductInstanceId = subInstance?.Id,
            Year = mainInstance.Year,
            Term = mainInstance.Term,
            MainProductCode = mainInstance.MainProduct.Code,
            MainProductName = mainInstance.MainProduct.Name,
            MainProductActive = mainInstance.MainProduct.IsActive,
            SubProductCode = subInstance?.SubProduct.Code,
            SubProductName = subInstance?.SubProduct.Name,
            SubProductActive = subInstance?.SubProduct.IsActive
        };
    }

    private static string NormalizeDeleteScope(string? scope)
    {
        if (string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase)) return "All";
        if (string.IsNullOrWhiteSpace(scope) || string.Equals(scope, "Single", StringComparison.OrdinalIgnoreCase)) return "Single";
        throw new InvalidOperationException("Geçersiz silme kapsamı.");
    }

    private static string FormatPeriodRange(int fromYear, int fromTerm, int? toYear, int? toTerm) =>
        $"{fromYear}/{fromTerm}–{(toYear.HasValue && toTerm.HasValue ? $"{toYear}/{toTerm}" : "devam")}";

    private static string NormalizeTwoCharacterCode(string value, string fieldName)
    {
        var code = value.Trim().ToUpperInvariant();
        if (code.Length != 2 || code.Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new InvalidOperationException($"{fieldName} 2 karakter alfanumerik olmalı.");
        return code;
    }

    private static string RewritePortfolioCode(
        string currentCode, string branchCode, string oldGamutCode, string newGamutCode)
    {
        var expectedPrefix = $"P{branchCode.Trim().ToUpperInvariant()}-{oldGamutCode.Trim().ToUpperInvariant()}";
        if (!currentCode.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || currentCode.Length != expectedPrefix.Length + 2
            || !currentCode[^2..].All(char.IsAsciiDigit))
            throw new InvalidOperationException($"{currentCode} portföy kodu beklenen {expectedPrefix}01 biçiminde değil; ürün gamı kodu otomatik güncellenemedi.");
        return $"P{branchCode.Trim().ToUpperInvariant()}-{newGamutCode.Trim().ToUpperInvariant()}{currentCode[^2..]}";
    }

    private static string NormalizeName(string value, string fieldName, int maxLength = 180)
    {
        var name = value.Trim();
        if (name.Length < 2 || name.Length > maxLength)
            throw new InvalidOperationException($"{fieldName} 2-{maxLength} karakter olmalı.");
        return name;
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

    private static ManagementImpactCountViewModel Impact(string label, int count) => new()
    {
        Label = label,
        Count = count
    };
}
