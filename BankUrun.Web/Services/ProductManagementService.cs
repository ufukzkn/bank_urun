using BankUrun.Web.Data;
using BankUrun.Web.Models;
using BankUrun.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class ProductManagementService(AppDbContext db, IProductCodeService codeService) : IProductManagementService
{
    public async Task<ProductIndexViewModel> GetIndexAsync(ProductFilterInput filter, CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = NormalizePageSize(filter.PageSize);

        var query = db.MainProductPeriods
            .AsNoTracking()
            .Include(item => item.MainProduct)
            .Include(item => item.Period)
            .Include(item => item.SubProductAssignments)
                .ThenInclude(assignment => assignment.SubProduct)
            .AsQueryable();

        if (filter.Year.HasValue)
        {
            query = query.Where(item => item.Period.Year == filter.Year.Value);
        }

        if (filter.Term.HasValue)
        {
            query = query.Where(item => item.Period.Term == filter.Term.Value);
        }

        if (!filter.IncludeInactive)
        {
            query = query.Where(item => item.MainProduct.IsActive);
        }

        var searchQuery = NormalizeSearch(filter.Search);
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(item =>
                item.MainProduct.Code.Contains(searchQuery) ||
                item.MainProduct.Name.ToUpper().Contains(searchQuery) ||
                item.SubProductAssignments.Any(assignment =>
                    assignment.SubProduct.Code.Contains(searchQuery) ||
                    assignment.SubProduct.Name.ToUpper().Contains(searchQuery)));
        }

        var mainQuery = NormalizeSearch(filter.MainQuery);
        if (!string.IsNullOrEmpty(mainQuery))
        {
            query = query.Where(item =>
                item.MainProduct.Code.Contains(mainQuery) ||
                item.MainProduct.Name.ToUpper().Contains(mainQuery));
        }

        var rows = new List<ProductRowViewModel>();
        var records = await query
            .OrderByDescending(item => item.Period.Year)
            .ThenByDescending(item => item.Period.Term)
            .ThenBy(item => item.MainProduct.Code)
            .ToListAsync(cancellationToken);

        var subQuery = NormalizeSearch(filter.SubQuery);
        foreach (var record in records)
        {
            var assignments = record.SubProductAssignments
                .Where(assignment => filter.IncludeInactive || assignment.SubProduct.IsActive)
                .OrderBy(assignment => assignment.SubProduct.Code)
                .ToList();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                var mainMatches =
                    record.MainProduct.Code.Contains(searchQuery) ||
                    record.MainProduct.Name.ToUpperInvariant().Contains(searchQuery);

                assignments = assignments
                    .Where(assignment =>
                        mainMatches ||
                        assignment.SubProduct.Code.Contains(searchQuery) ||
                        assignment.SubProduct.Name.ToUpperInvariant().Contains(searchQuery))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(subQuery))
            {
                assignments = assignments
                    .Where(assignment =>
                        assignment.SubProduct.Code.Contains(subQuery) ||
                        assignment.SubProduct.Name.ToUpperInvariant().Contains(subQuery))
                    .ToList();
            }

            if (assignments.Count == 0 && string.IsNullOrEmpty(subQuery))
            {
                rows.Add(ToRow(record, null));
                continue;
            }

            rows.AddRange(assignments.Select(assignment => ToRow(record, assignment)));
        }

        var totalRows = rows.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)filter.PageSize));
        if (filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var pagedRows = rows
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        var products = await db.Products
            .AsNoTracking()
            .OrderBy(product => product.Type)
            .ThenBy(product => product.Code)
            .Select(product => new ProductOptionViewModel
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                Type = product.Type,
                IsActive = product.IsActive
            })
            .ToListAsync(cancellationToken);

        var periodOptions = await db.MainProductPeriods
            .AsNoTracking()
            .Include(item => item.MainProduct)
            .Include(item => item.Period)
            .Where(item => item.MainProduct.IsActive)
            .OrderByDescending(item => item.Period.Year)
            .ThenByDescending(item => item.Period.Term)
            .ThenBy(item => item.MainProduct.Code)
            .Select(item => new MainProductPeriodOptionViewModel
            {
                Id = item.Id,
                MainProductId = item.MainProductId,
                Year = item.Period.Year,
                Term = item.Period.Term,
                MainProductCode = item.MainProduct.Code,
                MainProductName = item.MainProduct.Name
            })
            .ToListAsync(cancellationToken);

        return new ProductIndexViewModel
        {
            Filter = filter,
            Rows = pagedRows,
            TotalRows = totalRows,
            Page = filter.Page,
            PageSize = filter.PageSize,
            MainProducts = products.Where(product => product.Type == ProductType.Main).ToList(),
            SubProducts = products.Where(product => product.Type == ProductType.Sub).ToList(),
            MainProductPeriods = periodOptions
        };
    }

    public async Task CreateProductAsync(CreateProductInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var code = input.CodeMode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
            ? codeService.NormalizeCode(input.ManualCode ?? string.Empty)
            : await codeService.GenerateNextCodeAsync(input.Type, cancellationToken);

        if (!codeService.IsValidCode(code))
        {
            throw new InvalidOperationException("Ürün kodu 2 karakter alfanumerik olmalı.");
        }

        if (await db.Products.AnyAsync(product => product.Type == input.Type && product.Code == code, cancellationToken))
        {
            var suggestion = await codeService.SuggestCodeAsync(input.Type, code, cancellationToken);
            throw new InvalidOperationException(suggestion is null
                ? "Bu ürün tipi için boş 2 karakterli kod kalmadı."
                : $"{code} kodu dolu. En yakın uygun kod: {suggestion}.");
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Type = input.Type,
            Code = code,
            Name = input.Name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("CreateProduct", "Product", product.Id.ToString(), $"{product.Type} {product.Code} oluşturuldu.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddMainToPeriodAsync(AddMainToPeriodInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var mainProduct = await GetProductAsync(input.MainProductId, ProductType.Main, cancellationToken);
        var period = await GetOrCreatePeriodAsync(input.Year, input.Term, cancellationToken);

        var exists = await db.MainProductPeriods
            .AnyAsync(item => item.MainProductId == mainProduct.Id && item.PeriodId == period.Id, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"{mainProduct.Code} ana ürünü {period.Year}/{period.Term} döneminde zaten var.");
        }

        var item = new MainProductPeriod
        {
            MainProductId = mainProduct.Id,
            PeriodId = period.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.MainProductPeriods.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("AddMainToPeriod", "MainProductPeriod", item.Id.ToString(), $"{mainProduct.Code} {period.Year}/{period.Term} dönemine eklendi.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AssignSubProductAsync(AssignSubProductInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var mainPeriod = await db.MainProductPeriods
            .Include(item => item.MainProduct)
            .Include(item => item.Period)
            .FirstOrDefaultAsync(item => item.Id == input.MainProductPeriodId, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün dönem kaydı bulunamadı.");

        var subProduct = await GetProductAsync(input.SubProductId, ProductType.Sub, cancellationToken);
        var exists = await db.SubProductAssignments
            .AnyAsync(item => item.MainProductPeriodId == input.MainProductPeriodId && item.SubProductId == subProduct.Id, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"{subProduct.Code} alt ürünü bu ana ürün dönemine zaten bağlı.");
        }

        var assignment = new SubProductAssignment
        {
            MainProductPeriodId = mainPeriod.Id,
            SubProductId = subProduct.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.SubProductAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        AddAudit("AssignSubProduct", "SubProductAssignment", assignment.Id.ToString(), $"{subProduct.Code}, {mainPeriod.MainProduct.Code} ürününe {mainPeriod.Period.Year}/{mainPeriod.Period.Term} döneminde bağlandı.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RenameProductAsync(RenameProductInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var product = await db.Products.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("Ürün bulunamadı.");

        var oldName = product.Name;
        product.Name = input.Name.Trim();
        product.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("RenameProduct", "Product", product.Id.ToString(), $"{product.Code} adı '{oldName}' -> '{product.Name}' değişti.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeactivateProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var product = await db.Products.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("Ürün bulunamadı.");

        product.IsActive = false;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit("DeactivateProduct", "Product", product.Id.ToString(), $"{product.Type} {product.Code} pasifleştirildi.", actor);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(ProductIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var product = await db.Products.FirstOrDefaultAsync(item => item.Id == input.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("Ürün bulunamadı.");

        AddAudit("DeleteProduct", "Product", product.Id.ToString(), $"{product.Type} {product.Code} kalıcı silindi.", actor);
        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveMainProductPeriodAsync(EntityIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var item = await db.MainProductPeriods
            .Include(record => record.MainProduct)
            .Include(record => record.Period)
            .FirstOrDefaultAsync(record => record.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Ana ürün dönem kaydı bulunamadı.");

        AddAudit("RemoveMainProductPeriod", "MainProductPeriod", item.Id.ToString(), $"{item.MainProduct.Code} {item.Period.Year}/{item.Period.Term} döneminden kaldırıldı.", actor);
        db.MainProductPeriods.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveSubProductAssignmentAsync(EntityIdInput input, string actor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var item = await db.SubProductAssignments
            .Include(record => record.SubProduct)
            .Include(record => record.MainProductPeriod)
                .ThenInclude(record => record.MainProduct)
            .Include(record => record.MainProductPeriod)
                .ThenInclude(record => record.Period)
            .FirstOrDefaultAsync(record => record.Id == input.Id, cancellationToken)
            ?? throw new InvalidOperationException("Alt ürün bağlantısı bulunamadı.");

        AddAudit("RemoveSubProductAssignment", "SubProductAssignment", item.Id.ToString(), $"{item.SubProduct.Code}, {item.MainProductPeriod.MainProduct.Code} ürününden {item.MainProductPeriod.Period.Year}/{item.MainProductPeriod.Period.Term} döneminde kaldırıldı.", actor);
        db.SubProductAssignments.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Product> GetProductAsync(int id, ProductType type, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(item => item.Id == id && item.Type == type, cancellationToken)
            ?? throw new InvalidOperationException("Ürün bulunamadı.");

        if (!product.IsActive)
        {
            throw new InvalidOperationException("Pasif ürünle işlem yapılamaz.");
        }

        return product;
    }

    private async Task<Period> GetOrCreatePeriodAsync(int year, int term, CancellationToken cancellationToken)
    {
        var period = await db.Periods.FirstOrDefaultAsync(item => item.Year == year && item.Term == term, cancellationToken);
        if (period is not null)
        {
            return period;
        }

        period = new Period { Year = year, Term = term };
        db.Periods.Add(period);
        await db.SaveChangesAsync(cancellationToken);
        return period;
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

    private static ProductRowViewModel ToRow(MainProductPeriod mainPeriod, SubProductAssignment? assignment)
    {
        return new ProductRowViewModel
        {
            MainProductPeriodId = mainPeriod.Id,
            AssignmentId = assignment?.Id,
            Year = mainPeriod.Period.Year,
            Term = mainPeriod.Period.Term,
            MainProductId = mainPeriod.MainProductId,
            MainProductCode = mainPeriod.MainProduct.Code,
            MainProductName = mainPeriod.MainProduct.Name,
            MainProductActive = mainPeriod.MainProduct.IsActive,
            SubProductId = assignment?.SubProductId,
            SubProductCode = assignment?.SubProduct.Code,
            SubProductName = assignment?.SubProduct.Name,
            SubProductActive = assignment?.SubProduct.IsActive
        };
    }

    private static string? NormalizeSearch(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize switch
        {
            10 or 25 or 50 => pageSize,
            _ => 10
        };
    }
}
