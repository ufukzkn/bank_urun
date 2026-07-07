using BankUrun.Web.Models;

namespace BankUrun.Web.Services;

public interface IProductCodeService
{
    bool IsValidCode(string? code);
    string NormalizeCode(string code);
    Task<string> GenerateNextCodeAsync(ProductType type, CancellationToken cancellationToken = default);
    Task<string?> SuggestCodeAsync(ProductType type, string requestedCode, CancellationToken cancellationToken = default);
}
