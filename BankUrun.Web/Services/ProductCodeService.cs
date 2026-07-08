using BankUrun.Web.Data;
using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Services;

public class ProductCodeService(AppDbContext db) : IProductCodeService
{
    private const string CodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeCount = 36 * 36;

    public bool IsValidCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 2)
        {
            return false;
        }

        return NormalizeCode(code).All(CodeChars.Contains);
    }

    public string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    public async Task<string> GenerateNextCodeAsync(ProductType type, int? mainProductInstanceId = null, CancellationToken cancellationToken = default)
    {
        var usedCodes = await GetUsedCodesAsync(type, null, cancellationToken);
        for (var i = 0; i < CodeCount; i++)
        {
            var code = FromIndex(i);
            if (!usedCodes.Contains(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Bu ürün tipi için boş 2 karakterli kod kalmadı.");
    }

    public async Task<string?> SuggestCodeAsync(ProductType type, string requestedCode, int? mainProductInstanceId = null, CancellationToken cancellationToken = default)
    {
        if (!IsValidCode(requestedCode))
        {
            return null;
        }

        var requested = NormalizeCode(requestedCode);
        var usedCodes = await GetUsedCodesAsync(type, mainProductInstanceId, cancellationToken);
        var startIndex = ToIndex(requested);

        for (var offset = 0; offset < CodeCount; offset++)
        {
            var candidate = FromIndex((startIndex + offset) % CodeCount);
            if (!usedCodes.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<HashSet<string>> GetUsedCodesAsync(ProductType type, int? mainProductInstanceId, CancellationToken cancellationToken)
    {
        List<string> codes;
        if (type == ProductType.Main)
        {
            codes = await db.ProductDefinitions
                .AsNoTracking()
                .Where(product => product.Type == ProductType.Main)
                .Select(product => product.Code)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        else if (mainProductInstanceId.HasValue)
        {
            codes = await db.SubProductInstances
                .AsNoTracking()
                .Where(instance => instance.MainProductInstanceId == mainProductInstanceId.Value)
                .Select(instance => instance.SubProduct.Code)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
        else
        {
            codes = await db.ProductDefinitions
                .AsNoTracking()
                .Where(product => product.Type == ProductType.Sub)
                .Select(product => product.Code)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static int ToIndex(string code)
    {
        return CodeChars.IndexOf(code[0]) * 36 + CodeChars.IndexOf(code[1]);
    }

    private static string FromIndex(int index)
    {
        return string.Create(2, index, (chars, value) =>
        {
            chars[0] = CodeChars[value / 36];
            chars[1] = CodeChars[value % 36];
        });
    }
}
