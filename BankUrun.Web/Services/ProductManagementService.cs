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

        return new ProductIndexViewModel
        {
            Rows = rows,
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
                MainProductType = ProductType.Main,
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
            SubProductType = ProductType.Sub,
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
        var deleteAll = input.DeleteScope.Equals("All", StringComparison.OrdinalIgnoreCase);

        if (input.Type == ProductType.Main)
        {
            if (deleteAll)
            {
                var product = await db.ProductDefinitions.FirstOrDefaultAsync(item => item.Id == input.ProductId && item.Type == ProductType.Main, cancellationToken)
                    ?? throw new InvalidOperationException("Ana ürün tanımı bulunamadı.");

                var linkedSubProductCount = await db.SubProductInstances
                    .AnyAsync(subInstance => subInstance.MainProductInstance.MainProductId == product.Id, cancellationToken);

                if (linkedSubProductCount)
                {
                    throw new InvalidOperationException($"{product.Code} ana ürününe bağlı alt ürünler var. Önce alt ürün instance'larını silmelisiniz.");
                }

                AddAudit("DeleteMainProductDefinition", "ProductDefinition", product.Id.ToString(), $"{product.Code} ana ürünü tüm instance'larıyla kalıcı silindi.", actor);
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

                AddAudit("DeleteSubProductDefinition", "ProductDefinition", product.Id.ToString(), $"{product.Code} alt ürünü tüm instance'larıyla kalıcı silindi.", actor);
                db.ProductDefinitions.Remove(product);
            }
            else
            {
                var instance = await db.SubProductInstances
                    .Include(item => item.SubProduct)
                    .FirstOrDefaultAsync(item => item.Id == input.SubProductInstanceId, cancellationToken)
                    ?? throw new InvalidOperationException("Alt ürün instance kaydı bulunamadı.");

                AddAudit("DeleteSubProductInstance", "SubProductInstance", instance.Id.ToString(), $"{instance.SubProduct.Code} alt ürün instance kaydı silindi.", actor);
                db.SubProductInstances.Remove(instance);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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
}
