using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class ProductManagementService(AppDbContext db, IProductCodeService codeService) : IProductManagementService
{
    public async Task<ProductIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var mainProducts = await db.MainProducts
            .AsNoTracking()
            .Include(product => product.SubProducts)
            .OrderByDescending(product => product.Year)
            .ThenByDescending(product => product.Term)
            .ThenBy(product => product.Code)
            .ToListAsync(cancellationToken);

        var rows = new List<ProductRowViewModel>();
        foreach (var mainProduct in mainProducts)
        {
            var subProducts = mainProduct.SubProducts.OrderBy(product => product.Code).ToList();
            if (subProducts.Count == 0)
            {
                rows.Add(ToRow(mainProduct, null));
                continue;
            }

            rows.AddRange(subProducts.Select(subProduct => ToRow(mainProduct, subProduct)));
        }

        return new ProductIndexViewModel
        {
            Rows = rows,
            MainProducts = mainProducts
                .Select(product => new MainProductOptionViewModel
                {
                    Id = product.Id,
                    Code = product.Code,
                    Name = product.Name,
                    Year = product.Year,
                    Term = product.Term,
                    IsActive = product.IsActive
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
            if (await db.MainProducts.AnyAsync(product => product.Code == code, cancellationToken))
            {
                var suggestion = await codeService.SuggestCodeAsync(ProductType.Main, code, null, cancellationToken);
                throw DuplicateCodeException(code, suggestion);
            }

            var mainProduct = new MainProduct
            {
                Code = code,
                Name = input.Name.Trim(),
                Year = input.Year ?? throw new InvalidOperationException("Ana ürün için yıl zorunlu."),
                Term = input.Term ?? throw new InvalidOperationException("Ana ürün için dönem zorunlu."),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.MainProducts.Add(mainProduct);
            await db.SaveChangesAsync(cancellationToken);
            AddAudit("CreateMainProduct", "MainProduct", mainProduct.Id.ToString(), $"{mainProduct.Code} ana ürünü {mainProduct.Year}/{mainProduct.Term} için oluşturuldu.", actor);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (!input.MainProductId.HasValue)
        {
            throw new InvalidOperationException("Alt ürün için bağlı ana ürün seçilmeli.");
        }

        var main = await db.MainProducts.FirstOrDefaultAsync(product => product.Id == input.MainProductId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Bağlı ana ürün bulunamadı.");

        if (!main.IsActive)
        {
            throw new InvalidOperationException("Pasif ana ürüne alt ürün eklenemez.");
        }

        var subCode = await ResolveCodeAsync(input.Type, input.CodeMode, input.ManualCode, main.Id, cancellationToken);
        if (await db.SubProducts.AnyAsync(product => product.MainProductId == main.Id && product.Code == subCode, cancellationToken))
        {
            var suggestion = await codeService.SuggestCodeAsync(ProductType.Sub, subCode, main.Id, cancellationToken);
            throw DuplicateCodeException(subCode, suggestion);
        }

        var subProduct = new SubProduct
        {
            MainProductId = main.Id,
            Code = subCode,
            Name = input.Name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.SubProducts.Add(subProduct);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateSubProduct", "SubProduct", subProduct.Id.ToString(), $"{subProduct.Code} alt ürünü {main.Code} ana ürününe bağlandı.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RenameProductAsync(RenameProductInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (input.Type == ProductType.Main)
        {
            var product = await db.MainProducts.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Ana ürün bulunamadı.");
            var oldName = product.Name;
            product.Name = input.Name.Trim();
            product.UpdatedAt = now;
            AddAudit("RenameMainProduct", "MainProduct", product.Id.ToString(), $"{product.Code} adı '{oldName}' -> '{product.Name}' değişti.", actor);
        }
        else
        {
            var product = await db.SubProducts.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Alt ürün bulunamadı.");
            var oldName = product.Name;
            product.Name = input.Name.Trim();
            product.UpdatedAt = now;
            AddAudit("RenameSubProduct", "SubProduct", product.Id.ToString(), $"{product.Code} adı '{oldName}' -> '{product.Name}' değişti.", actor);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeactivateProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (input.Type == ProductType.Main)
        {
            var product = await db.MainProducts
                .Include(item => item.SubProducts)
                .FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Ana ürün bulunamadı.");

            product.IsActive = false;
            product.UpdatedAt = now;
            foreach (var subProduct in product.SubProducts)
            {
                subProduct.IsActive = false;
                subProduct.UpdatedAt = now;
            }

            AddAudit("DeactivateMainProduct", "MainProduct", product.Id.ToString(), $"{product.Code} ana ürünü ve alt ürünleri pasifleştirildi.", actor);
        }
        else
        {
            var product = await db.SubProducts.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Alt ürün bulunamadı.");

            product.IsActive = false;
            product.UpdatedAt = now;
            AddAudit("DeactivateSubProduct", "SubProduct", product.Id.ToString(), $"{product.Code} alt ürünü pasifleştirildi.", actor);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (input.Type == ProductType.Main)
        {
            var product = await db.MainProducts.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Ana ürün bulunamadı.");

            AddAudit("DeleteMainProduct", "MainProduct", product.Id.ToString(), $"{product.Code} ana ürünü kalıcı silindi.", actor);
            db.MainProducts.Remove(product);
        }
        else
        {
            var product = await db.SubProducts.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("Alt ürün bulunamadı.");

            AddAudit("DeleteSubProduct", "SubProduct", product.Id.ToString(), $"{product.Code} alt ürünü kalıcı silindi.", actor);
            db.SubProducts.Remove(product);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<string> ResolveCodeAsync(ProductType type, string codeMode, string? manualCode, int? mainProductId, CancellationToken cancellationToken)
    {
        var code = codeMode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
            ? codeService.NormalizeCode(manualCode ?? string.Empty)
            : await codeService.GenerateNextCodeAsync(type, mainProductId, cancellationToken);

        if (!codeService.IsValidCode(code))
        {
            throw new InvalidOperationException("Ürün kodu 2 karakter alfanumerik olmalı.");
        }

        return code;
    }

    private static InvalidOperationException DuplicateCodeException(string code, string? suggestion)
    {
        return new InvalidOperationException(suggestion is null
            ? "Boş 2 karakterli kod kalmadı."
            : $"{code} kodu dolu. En yakın uygun kod: {suggestion}.");
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

    private static ProductRowViewModel ToRow(MainProduct mainProduct, SubProduct? subProduct)
    {
        return new ProductRowViewModel
        {
            MainProductId = mainProduct.Id,
            SubProductId = subProduct?.Id,
            Year = mainProduct.Year,
            Term = mainProduct.Term,
            MainProductCode = mainProduct.Code,
            MainProductName = mainProduct.Name,
            MainProductActive = mainProduct.IsActive,
            SubProductCode = subProduct?.Code,
            SubProductName = subProduct?.Name,
            SubProductActive = subProduct?.IsActive
        };
    }
}
