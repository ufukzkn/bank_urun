using BankUrun.Web.Services;

namespace BankUrun.Tests;

public class ProductRelationshipLifecycleTests
{
    [Theory]
    [InlineData("MA", "MA", true)]
    [InlineData("MA", " ma ", true)]
    [InlineData("MA", "MC", false)]
    [InlineData("MA", null, false)]
    public void ConfirmationCode_MustMatchProductCode(
        string productCode, string? confirmationCode, bool expected)
    {
        Assert.Equal(expected,
            ProductRelationshipLifecycle.MatchesConfirmationCode(productCode, confirmationCode));
    }

    [Theory]
    [InlineData(2024, 1, null, null, 2026, 2, null, null, true)]
    [InlineData(2024, 1, 2025, 2, 2025, 2, 2026, 1, true)]
    [InlineData(2024, 1, 2025, 1, 2025, 2, 2026, 1, false)]
    public void PeriodOverlap_IsInclusiveAndSupportsOpenEndedRanges(
        int leftYear, int leftTerm, int? leftToYear, int? leftToTerm,
        int rightYear, int rightTerm, int? rightToYear, int? rightToTerm,
        bool expected)
    {
        Assert.Equal(expected, ProductRelationshipLifecycle.PeriodsOverlap(
            leftYear, leftTerm, leftToYear, leftToTerm,
            rightYear, rightTerm, rightToYear, rightToTerm));
    }

    [Fact]
    public void FindOrphans_PreservesSharedSubProductsAndDeduplicates()
    {
        var result = ProductRelationshipLifecycle.FindOrphanSubProductIds(
            [10, 11, 11, 12],
            [11, 99]);

        Assert.Equal([10, 12], result);
    }

    [Theory]
    [InlineData(2024, 1, null, null, 2026, 2, EffectiveAssignmentRemovalAction.Close)]
    [InlineData(2026, 2, null, null, 2026, 2, EffectiveAssignmentRemovalAction.Delete)]
    [InlineData(2027, 1, null, null, 2026, 2, EffectiveAssignmentRemovalAction.Delete)]
    [InlineData(2024, 1, 2026, 1, 2026, 2, EffectiveAssignmentRemovalAction.None)]
    public void PlanRemoval_PreservesHistoryAndDropsOnlyCurrentOrFutureRanges(
        int fromYear,
        int fromTerm,
        int? toYear,
        int? toTerm,
        int removalYear,
        int removalTerm,
        EffectiveAssignmentRemovalAction expected)
    {
        var result = ProductRelationshipLifecycle.PlanRemoval(
            fromYear, fromTerm, toYear, toTerm, new PerformancePeriod(removalYear, removalTerm));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void PreviousPeriod_CrossesYearBoundary()
    {
        Assert.Equal(new PerformancePeriod(2025, 2), new PerformancePeriod(2026, 1).Previous());
        Assert.Equal(new PerformancePeriod(2026, 1), new PerformancePeriod(2026, 2).Previous());
    }
}
