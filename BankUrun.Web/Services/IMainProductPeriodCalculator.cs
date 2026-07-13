using BankUrun.Web.Models;

namespace BankUrun.Web.Services;

public interface IMainProductPeriodCalculator
{
    MainProductPeriodCalculationResult Calculate(MainProductPeriodCalculationInput input);
    IReadOnlyList<int> GetTermMonths(int term);
    IReadOnlyList<int> GetExpectedMonths(int year, int term, DateOnly asOfDate);
}

public sealed record MainProductPeriodCalculationInput(
    int Year,
    int Term,
    DateOnly AsOfDate,
    MainProductCalculationType CalculationType,
    decimal CriterionScore,
    IReadOnlyCollection<MainProductMonthlyValue> Months);

public sealed record MainProductMonthlyValue(
    int Month,
    decimal TargetValue,
    decimal? ActualValue,
    DateOnly? ActualAsOfDate);

public sealed record MainProductPeriodCalculationResult(
    decimal TargetValue,
    decimal? ActualValue,
    decimal? HgRatioPercent,
    decimal? HgoScore,
    decimal? TotalScore,
    bool HasCompleteBatchData,
    IReadOnlyList<int> ExpectedMonths);
