using BankUrun.Web.Models;
using BankUrun.Web.Services;
using BankUrun.Web.ViewModels;

namespace BankUrun.Tests;

public class PerformanceQueryProcessorTests
{
    public static TheoryData<
        PerformanceMode,
        int?,
        int?,
        int?,
        int?,
        int?,
        int?,
        int?> ModeFilterCases =>
        new()
        {
            {
                PerformanceMode.Branch,
                42, 77, 88,
                null, null, null, null
            },
            {
                PerformanceMode.BranchProduct,
                42, 77, 88,
                42, 77, null, null
            },
            {
                PerformanceMode.MainProduct,
                42, 77, 88,
                null, 77, null, null
            },
            {
                PerformanceMode.Portfolio,
                42, 77, 88,
                42, null, 77, 88
            }
        };

    [Theory]
    [MemberData(nameof(ModeFilterCases))]
    public void Normalize_KeepsOnlyFiltersSupportedBySelectedMode(
        PerformanceMode mode,
        int? branchId,
        int? productOrGamutId,
        int? portfolioTypeId,
        int? expectedBranchId,
        int? expectedMainProductId,
        int? expectedProductGamutId,
        int? expectedPortfolioTypeId)
    {
        var query = new PerformanceQuery
        {
            Mode = mode,
            GroupId = 9,
            BranchId = branchId,
            MainProductId = productOrGamutId,
            ProductGamutId = productOrGamutId,
            PortfolioTypeId = portfolioTypeId,
            Year = 2025,
            Term = 2
        };

        var result = PerformanceQueryProcessor.Normalize(query);

        Assert.Equal(mode, result.Mode);
        Assert.Equal(9, result.GroupId);
        Assert.Equal(2025, result.Year);
        Assert.Equal(2, result.Term);
        Assert.Equal(expectedBranchId, result.BranchId);
        Assert.Equal(expectedMainProductId, result.MainProductId);
        Assert.Equal(expectedProductGamutId, result.ProductGamutId);
        Assert.Equal(expectedPortfolioTypeId, result.PortfolioTypeId);
    }

    [Fact]
    public void Normalize_InvalidValuesUseSafeDefaultsAndPreserveForceRefresh()
    {
        var query = new PerformanceQuery
        {
            Mode = (PerformanceMode)999,
            Term = 3,
            Search = $"  {new string('x', 120)}  ",
            SortKey = "  totalScore ",
            SortDirection = "SIDEWAYS",
            Page = -8,
            PageSize = 500,
            ForceRefresh = true
        };

        var result = PerformanceQueryProcessor.Normalize(query);

        Assert.Equal(PerformanceMode.BranchProduct, result.Mode);
        Assert.Null(result.Term);
        Assert.Equal(new string('x', 100), result.Search);
        Assert.Equal("totalScore", result.SortKey);
        Assert.Null(result.SortDirection);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.True(result.ForceRefresh);
    }

    [Theory]
    [InlineData(PerformanceMode.Branch, 0, 25)]
    [InlineData(PerformanceMode.BranchProduct, -1, 25)]
    [InlineData(PerformanceMode.MainProduct, 0, 10)]
    [InlineData(PerformanceMode.MainProduct, 51, 10)]
    [InlineData(PerformanceMode.Portfolio, 500, 25)]
    [InlineData(PerformanceMode.Portfolio, 5, 25)]
    [InlineData(PerformanceMode.Branch, 10, 10)]
    [InlineData(PerformanceMode.BranchProduct, 25, 25)]
    [InlineData(PerformanceMode.MainProduct, 50, 50)]
    public void Normalize_AcceptsOnlyConfiguredPageSizes(
        PerformanceMode mode,
        int requestedPageSize,
        int expectedPageSize)
    {
        var result = PerformanceQueryProcessor.Normalize(new PerformanceQuery
        {
            Mode = mode,
            PageSize = requestedPageSize
        });

        Assert.Equal(expectedPageSize, result.PageSize);
    }

    [Theory]
    [InlineData(" ASC ", "asc")]
    [InlineData("desc", "desc")]
    [InlineData("DeSc", "desc")]
    public void Normalize_NormalizesValidSortDirection(
        string requestedDirection,
        string expectedDirection)
    {
        var result = PerformanceQueryProcessor.Normalize(new PerformanceQuery
        {
            SortDirection = requestedDirection
        });

        Assert.Equal(expectedDirection, result.SortDirection);
    }

    [Fact]
    public void Normalize_WhitespaceSearchAndSortBecomeNull()
    {
        var result = PerformanceQueryProcessor.Normalize(new PerformanceQuery
        {
            Search = "   ",
            SortKey = "\t",
            SortDirection = " "
        });

        Assert.Null(result.Search);
        Assert.Null(result.SortKey);
        Assert.Null(result.SortDirection);
    }

    [Fact]
    public void Page_ClampsRequestedPageToLastAvailablePage()
    {
        var result = PerformanceQueryProcessor.Page(
            Enumerable.Range(1, 26),
            requestedPage: 99,
            pageSize: 10);

        Assert.Equal(26, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(3, result.Page);
        Assert.Equal([21, 22, 23, 24, 25, 26], result.Items);
    }

    [Fact]
    public void Page_ClampsNonPositivePageToFirstPage()
    {
        var result = PerformanceQueryProcessor.Page(
            Enumerable.Range(1, 12),
            requestedPage: 0,
            pageSize: 5);

        Assert.Equal(1, result.Page);
        Assert.Equal([1, 2, 3, 4, 5], result.Items);
    }

    [Fact]
    public void Page_EmptySourceStillReturnsStableFirstPageMetadata()
    {
        var result = PerformanceQueryProcessor.Page(
            Array.Empty<int>(),
            requestedPage: 7,
            pageSize: 25);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
    }

    [Fact]
    public void Page_MaximumAllowedPageSizeNeverRendersMoreThanFiftyRows()
    {
        var normalized = PerformanceQueryProcessor.Normalize(new PerformanceQuery
        {
            Page = 1,
            PageSize = 50
        });

        var result = PerformanceQueryProcessor.Page(
            Enumerable.Range(1, 120),
            normalized.Page,
            normalized.PageSize);

        Assert.Equal(50, result.Items.Count);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(120, result.TotalCount);
    }

    [Fact]
    public void BuildFactScopeKey_ChangesWhenResolvedPeriodCatalogChanges()
    {
        DashboardPeriodOptionViewModel[] originalPeriods =
        [
            new() { Year = 2025, Term = 2 },
            new() { Year = 2025, Term = 1 }
        ];
        DashboardPeriodOptionViewModel[] refreshedPeriods =
        [
            new() { Year = 2026, Term = 1 },
            .. originalPeriods
        ];

        var originalKey = PerformanceQueryProcessor.BuildFactScopeKey(
            groupId: null, year: null, term: null, originalPeriods);
        var refreshedKey = PerformanceQueryProcessor.BuildFactScopeKey(
            groupId: null, year: null, term: null, refreshedPeriods);
        var reorderedKey = PerformanceQueryProcessor.BuildFactScopeKey(
            groupId: null, year: null, term: null, originalPeriods.Reverse());

        Assert.NotEqual(originalKey, refreshedKey);
        Assert.Equal(originalKey, reorderedKey);
        Assert.Contains("p:2025-1,2025-2", originalKey);
        Assert.Contains("p:2025-1,2025-2,2026-1", refreshedKey);
    }

    [Theory]
    [InlineData("istanbul", "İSTANBUL ŞUBESİ")]
    [InlineData("ısparta", "ISPARTA ŞUBESİ")]
    public void MatchesTurkish_UsesTurkishCaseRules(
        string search,
        string candidate)
    {
        Assert.True(PerformanceQueryProcessor.MatchesTurkish(search, candidate));
    }

    [Fact]
    public void MatchesTurkish_SearchesAcrossAllProvidedFields()
    {
        Assert.True(PerformanceQueryProcessor.MatchesTurkish(
            "vadeli",
            "120",
            "Güngören Şubesi",
            "TL Vadeli Kaynak"));
        Assert.False(PerformanceQueryProcessor.MatchesTurkish(
            "kurumsal",
            "120",
            "Güngören Şubesi",
            "TL Vadeli Kaynak"));
        Assert.True(PerformanceQueryProcessor.MatchesTurkish(
            null,
            null,
            null));
    }

    [Fact]
    public void TurkishTextComparer_UsesTurkishAlphabetAndIgnoresCase()
    {
        var values = new[] { "İzmir", "Jandarma", "Isparta" };

        var result = values
            .OrderBy(value => value, PerformanceQueryProcessor.TurkishTextComparer)
            .ToArray();

        Assert.Equal(["Isparta", "İzmir", "Jandarma"], result);
        Assert.Equal(
            0,
            PerformanceQueryProcessor.TurkishTextComparer.Compare("İZMİR", "izmir"));
        Assert.Equal(
            0,
            PerformanceQueryProcessor.TurkishTextComparer.Compare("ISPARTA", "ısparta"));
    }
}
