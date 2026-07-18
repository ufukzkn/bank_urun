namespace BankUrun.Web.Services;

public enum EffectiveAssignmentRemovalAction
{
    None,
    Delete,
    Close
}

public readonly record struct PerformancePeriod(int Year, int Term)
{
    public int Ordinal => checked(Year * 2 + Term - 1);

    public PerformancePeriod Previous() => Term == 1
        ? new PerformancePeriod(Year - 1, 2)
        : new PerformancePeriod(Year, 1);
}

public static class ProductRelationshipLifecycle
{
    public static bool MatchesConfirmationCode(string productCode, string? confirmationCode) =>
        string.Equals(productCode.Trim(), confirmationCode?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool PeriodsOverlap(
        int leftFromYear, int leftFromTerm, int? leftToYear, int? leftToTerm,
        int rightFromYear, int rightFromTerm, int? rightToYear, int? rightToTerm)
    {
        var leftStart = new PerformancePeriod(leftFromYear, leftFromTerm).Ordinal;
        var leftEnd = leftToYear.HasValue && leftToTerm.HasValue
            ? new PerformancePeriod(leftToYear.Value, leftToTerm.Value).Ordinal
            : int.MaxValue;
        var rightStart = new PerformancePeriod(rightFromYear, rightFromTerm).Ordinal;
        var rightEnd = rightToYear.HasValue && rightToTerm.HasValue
            ? new PerformancePeriod(rightToYear.Value, rightToTerm.Value).Ordinal
            : int.MaxValue;
        return leftStart <= rightEnd && rightStart <= leftEnd;
    }

    public static IReadOnlyList<int> FindOrphanSubProductIds(
        IEnumerable<int> linkedToDeletedMainProduct,
        IEnumerable<int> linkedToOtherMainProducts)
    {
        var shared = linkedToOtherMainProducts.ToHashSet();
        return linkedToDeletedMainProduct.Distinct().Where(id => !shared.Contains(id)).Order().ToList();
    }

    public static EffectiveAssignmentRemovalAction PlanRemoval(
        int effectiveFromYear,
        int effectiveFromTerm,
        int? effectiveToYear,
        int? effectiveToTerm,
        PerformancePeriod removalPeriod)
    {
        var start = new PerformancePeriod(effectiveFromYear, effectiveFromTerm).Ordinal;
        var end = effectiveToYear.HasValue && effectiveToTerm.HasValue
            ? new PerformancePeriod(effectiveToYear.Value, effectiveToTerm.Value).Ordinal
            : int.MaxValue;
        if (end < removalPeriod.Ordinal) return EffectiveAssignmentRemovalAction.None;
        return start >= removalPeriod.Ordinal
            ? EffectiveAssignmentRemovalAction.Delete
            : EffectiveAssignmentRemovalAction.Close;
    }
}
