using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class PerformanceFactCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ReusesCachedValueWithinLifetime()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);
        var factoryCalls = 0;

        var first = await cache.GetOrCreateAsync(
            "2025:2:all",
            forceRefresh: false,
            _ => Task.FromResult(++factoryCalls));
        clock.Advance(TimeSpan.FromSeconds(30));
        var second = await cache.GetOrCreateAsync(
            "2025:2:all",
            forceRefresh: false,
            _ => Task.FromResult(++factoryCalls));

        Assert.Equal(1, first.Value);
        Assert.False(first.CacheHit);
        Assert.Equal(1, second.Value);
        Assert.True(second.CacheHit);
        Assert.Equal(first.ExpiresAt, second.ExpiresAt);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 20, 12, 1, 0, TimeSpan.Zero),
            first.ExpiresAt);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_ForceRefreshReplacesOnlyRequestedScope()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);
        var scopeACalls = 0;
        var scopeBCalls = 0;

        await cache.GetOrCreateAsync(
            "scope-a",
            forceRefresh: false,
            _ => Task.FromResult(++scopeACalls));
        await cache.GetOrCreateAsync(
            "scope-b",
            forceRefresh: false,
            _ => Task.FromResult(++scopeBCalls));

        var refreshed = await cache.GetOrCreateAsync(
            "scope-a",
            forceRefresh: true,
            _ => Task.FromResult(++scopeACalls));
        var otherScope = await cache.GetOrCreateAsync(
            "scope-b",
            forceRefresh: false,
            _ => Task.FromResult(++scopeBCalls));
        var refreshedHit = await cache.GetOrCreateAsync(
            "scope-a",
            forceRefresh: false,
            _ => Task.FromResult(++scopeACalls));

        Assert.Equal(2, refreshed.Value);
        Assert.False(refreshed.CacheHit);
        Assert.Equal(1, otherScope.Value);
        Assert.True(otherScope.CacheHit);
        Assert.Equal(2, refreshedHit.Value);
        Assert.True(refreshedHit.CacheHit);
        Assert.Equal(2, scopeACalls);
        Assert.Equal(1, scopeBCalls);
    }

    [Fact]
    public async Task Invalidate_IncrementsVersionAndClearsEveryScope()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);
        var calls = 0;

        await cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            _ => Task.FromResult(++calls));
        var initialVersion = cache.Version;

        cache.Invalidate();
        var afterInvalidation = await cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            _ => Task.FromResult(++calls));

        Assert.Equal(initialVersion + 1, cache.Version);
        Assert.Equal(2, afterInvalidation.Value);
        Assert.False(afterInvalidation.CacheHit);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetOrCreateAsync_ExpiresEntryAtSixtySeconds()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);
        var factoryCalls = 0;

        await cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            _ => Task.FromResult(++factoryCalls));
        clock.Advance(TimeSpan.FromSeconds(59));
        var beforeExpiry = await cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            _ => Task.FromResult(++factoryCalls));
        clock.Advance(TimeSpan.FromSeconds(1));
        var atExpiry = await cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            _ => Task.FromResult(++factoryCalls));

        Assert.True(beforeExpiry.CacheHit);
        Assert.Equal(1, beforeExpiry.Value);
        Assert.False(atExpiry.CacheHit);
        Assert.Equal(2, atExpiry.Value);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_DeduplicatesConcurrentFactoryCalls()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);
        var factoryCalls = 0;
        var factoryStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<int> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref factoryCalls);
            factoryStarted.TrySetResult();
            await releaseFactory.Task.WaitAsync(cancellationToken);
            return 37;
        }

        var firstTask = cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            Factory);
        await factoryStarted.Task;
        var secondTask = cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            Factory);
        releaseFactory.SetResult();

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(1, factoryCalls);
        Assert.All(results, result => Assert.Equal(37, result.Value));
        Assert.Contains(results, result => !result.CacheHit);
        Assert.Contains(results, result => result.CacheHit);
    }

    [Fact]
    public async Task GetOrCreateAsync_JoiningCallerAbortDoesNotCancelOrEvictSharedLoad()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);
        using var callerCancellation = new CancellationTokenSource();
        var factoryCalls = 0;
        var factoryStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<int> Factory(CancellationToken cancellationToken)
        {
            Assert.False(cancellationToken.CanBeCanceled);
            Interlocked.Increment(ref factoryCalls);
            factoryStarted.TrySetResult();
            await releaseFactory.Task;
            return 41;
        }

        var producingRequest = cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            Factory);
        await factoryStarted.Task;
        var abortedWaiter = cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            Factory,
            callerCancellation.Token);

        callerCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await abortedWaiter);
        releaseFactory.SetResult();

        var produced = await producingRequest;
        var laterHit = await cache.GetOrCreateAsync(
            "scope",
            forceRefresh: false,
            Factory);

        Assert.Equal(1, factoryCalls);
        Assert.False(produced.CacheHit);
        Assert.Equal(41, produced.Value);
        Assert.True(laterHit.CacheHit);
        Assert.Equal(41, laterHit.Value);
    }

    [Fact]
    public async Task GetOrCreateAsync_CapacityEvictsLeastRecentlyUsedScope()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);
        var calls = new int[9];

        for (var index = 0; index < 8; index++)
        {
            var capturedIndex = index;
            await cache.GetOrCreateAsync(
                $"scope-{capturedIndex}",
                forceRefresh: false,
                _ => Task.FromResult(++calls[capturedIndex]));
            clock.Advance(TimeSpan.FromSeconds(1));
        }

        var recentlyUsed = await cache.GetOrCreateAsync(
            "scope-0",
            forceRefresh: false,
            _ => Task.FromResult(++calls[0]));
        clock.Advance(TimeSpan.FromSeconds(1));
        await cache.GetOrCreateAsync(
            "scope-8",
            forceRefresh: false,
            _ => Task.FromResult(++calls[8]));

        var evicted = await cache.GetOrCreateAsync(
            "scope-1",
            forceRefresh: false,
            _ => Task.FromResult(++calls[1]));
        var retained = await cache.GetOrCreateAsync(
            "scope-0",
            forceRefresh: false,
            _ => Task.FromResult(++calls[0]));

        Assert.True(recentlyUsed.CacheHit);
        Assert.False(evicted.CacheHit);
        Assert.Equal(2, evicted.Value);
        Assert.True(retained.CacheHit);
        Assert.Equal(1, retained.Value);
    }

    [Fact]
    public async Task GetOrCreateAsync_SeparatesEntriesByResultType()
    {
        var clock = new MutableTimeProvider();
        using var cache = new PerformanceFactCache(clock);

        var numeric = await cache.GetOrCreateAsync(
            "same-scope",
            forceRefresh: false,
            _ => Task.FromResult(7));
        var text = await cache.GetOrCreateAsync(
            "same-scope",
            forceRefresh: false,
            _ => Task.FromResult("seven"));

        Assert.Equal(7, numeric.Value);
        Assert.Equal("seven", text.Value);
        Assert.False(numeric.CacheHit);
        Assert.False(text.CacheHit);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan duration)
        {
            utcNow = utcNow.Add(duration);
        }
    }
}
