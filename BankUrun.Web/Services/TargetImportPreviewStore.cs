using System.Collections.Concurrent;

namespace BankUrun.Web.Services;

public sealed class TargetImportPreviewStore(TimeProvider timeProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private const int Capacity = 8;
    private readonly ConcurrentDictionary<string, StoredTargetImport> entries = new(StringComparer.Ordinal);

    public string Put(IReadOnlyList<TargetImportCommand> commands)
    {
        Trim();
        var token = Guid.NewGuid().ToString("N");
        entries[token] = new StoredTargetImport(
            commands,
            timeProvider.GetUtcNow().Add(Lifetime));
        Trim();
        return token;
    }

    public IReadOnlyList<TargetImportCommand> Take(string token)
    {
        if (string.IsNullOrWhiteSpace(token)
            || !entries.TryRemove(token, out var stored)
            || stored.ExpiresAt <= timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("Excel önizlemesinin süresi dolmuş. Dosyayı yeniden yükleyin.");
        }

        return stored.Commands;
    }

    private void Trim()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var expired in entries.Where(item => item.Value.ExpiresAt <= now).ToList())
        {
            entries.TryRemove(expired.Key, out _);
        }

        foreach (var overflow in entries
                     .OrderByDescending(item => item.Value.ExpiresAt)
                     .Skip(Capacity)
                     .ToList())
        {
            entries.TryRemove(overflow.Key, out _);
        }
    }

    private sealed record StoredTargetImport(
        IReadOnlyList<TargetImportCommand> Commands,
        DateTimeOffset ExpiresAt);
}

public sealed record TargetImportCommand(
    int ParameterId,
    int PortfolioId,
    IReadOnlyList<MonthlyTargetValue> Months,
    string Context,
    string EntryMode);
