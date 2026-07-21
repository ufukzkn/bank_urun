using System.Collections.Concurrent;

namespace BankUrun.Web.Services;

public sealed record PerformanceFactCacheResult<T>(
    T Value,
    bool CacheHit,
    DateTimeOffset ExpiresAt);

public interface IPerformanceFactCache
{
    Task<PerformanceFactCacheResult<T>> GetOrCreateAsync<T>(
        string scopeKey,
        bool forceRefresh,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);
}

public interface IPerformanceCacheInvalidator
{
    long Version { get; }
    void Invalidate();
}

public sealed class PerformanceFactCache(TimeProvider timeProvider)
    : IPerformanceFactCache, IPerformanceCacheInvalidator, IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);
    private const int Capacity = 8;
    private readonly ConcurrentDictionary<string, CacheEntry> entries = new(StringComparer.Ordinal);
    private readonly object maintenanceLock = new();
    private long version;

    public long Version => Interlocked.Read(ref version);

    public async Task<PerformanceFactCacheResult<T>> GetOrCreateAsync<T>(
        string scopeKey,
        bool forceRefresh,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeKey);
        ArgumentNullException.ThrowIfNull(factory);

        var key = $"{Version}:{typeof(T).FullName}:{scopeKey}";
        if (forceRefresh)
        {
            entries.TryRemove(key, out _);
        }

        var now = timeProvider.GetUtcNow();
        if (!forceRefresh
            && entries.TryGetValue(key, out var existing)
            && existing.ExpiresAt > now)
        {
            existing.Touch(now);
            try
            {
                return new PerformanceFactCacheResult<T>(
                    (T)await existing.Value.Value
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false),
                    true,
                    existing.ExpiresAt);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                entries.TryRemove(new KeyValuePair<string, CacheEntry>(key, existing));
                throw;
            }
        }

        var created = new CacheEntry(
            new Lazy<Task<object>>(
                async () => (object)(await factory(CancellationToken.None).ConfigureAwait(false))!,
                LazyThreadSafetyMode.ExecutionAndPublication),
            now.Add(Lifetime),
            now,
            !string.Equals(scopeKey, "period-catalog", StringComparison.Ordinal));
        var entry = entries.AddOrUpdate(
            key,
            created,
            (_, current) => current.ExpiresAt > now && !forceRefresh ? current : created);
        var hit = !ReferenceEquals(entry, created);

        try
        {
            var sharedLoad = entry.Value.Value;
            var value = hit
                ? (T)await sharedLoad.WaitAsync(cancellationToken).ConfigureAwait(false)
                : (T)await sharedLoad.ConfigureAwait(false);
            Trim();
            return new PerformanceFactCacheResult<T>(value, hit, entry.ExpiresAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A browser abort must not cancel or evict a shared in-flight fact load.
            throw;
        }
        catch
        {
            entries.TryRemove(new KeyValuePair<string, CacheEntry>(key, entry));
            throw;
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref version);
        entries.Clear();
    }

    private void Trim()
    {
        lock (maintenanceLock)
        {
            var now = timeProvider.GetUtcNow();
            foreach (var expired in entries.Where(item => item.Value.ExpiresAt <= now).ToList())
            {
                entries.TryRemove(expired);
            }

            foreach (var overflow in entries
                         .Where(item => item.Value.CountsTowardsCapacity)
                         .OrderByDescending(item => item.Value.LastAccessed)
                         .Skip(Capacity)
                         .ToList())
            {
                entries.TryRemove(overflow);
            }
        }
    }

    public void Dispose()
    {
        entries.Clear();
    }

    private sealed class CacheEntry(
        Lazy<Task<object>> value,
        DateTimeOffset expiresAt,
        DateTimeOffset lastAccessed,
        bool countsTowardsCapacity)
    {
        private long lastAccessedTicks = lastAccessed.UtcTicks;
        public Lazy<Task<object>> Value { get; } = value;
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public bool CountsTowardsCapacity { get; } = countsTowardsCapacity;
        public DateTimeOffset LastAccessed => new(Interlocked.Read(ref lastAccessedTicks), TimeSpan.Zero);
        public void Touch(DateTimeOffset value) => Interlocked.Exchange(ref lastAccessedTicks, value.UtcTicks);
    }
}
