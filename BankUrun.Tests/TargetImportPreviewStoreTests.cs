using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class TargetImportPreviewStoreTests
{
    [Fact]
    public void PreviewToken_IsSingleUse()
    {
        var time = new MutableTimeProvider(
            new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));
        var store = new TargetImportPreviewStore(time);
        var command = new TargetImportCommand(
            1,
            2,
            [new MonthlyTargetValue(1, 100)],
            "1001 · 120 · P0120-BI01 · AU · 2026/1",
            "Aylık");

        var token = store.Put([command]);

        Assert.Single(store.Take(token));
        Assert.Throws<InvalidOperationException>(() => store.Take(token));
    }

    [Fact]
    public void PreviewToken_ExpiresAfterTenMinutes()
    {
        var time = new MutableTimeProvider(
            new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));
        var store = new TargetImportPreviewStore(time);
        var token = store.Put(
        [
            new TargetImportCommand(
                1,
                2,
                [new MonthlyTargetValue(1, 100)],
                "bağlam",
                "Aylık")
        ]);

        time.Advance(TimeSpan.FromMinutes(11));

        Assert.Throws<InvalidOperationException>(() => store.Take(token));
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan duration) => current += duration;
    }
}
