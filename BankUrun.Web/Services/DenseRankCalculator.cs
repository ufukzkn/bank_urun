namespace BankUrun.Web.Services;

public static class DenseRankCalculator
{
    public static IReadOnlyList<int?> Calculate(IReadOnlyList<decimal?> scores)
    {
        var ranks = scores
            .Where(score => score.HasValue)
            .Select(score => score!.Value)
            .Distinct()
            .OrderByDescending(score => score)
            .Select((score, index) => new { score, rank = index + 1 })
            .ToDictionary(item => item.score, item => item.rank);

        return scores
            .Select(score => score.HasValue ? ranks[score.Value] : (int?)null)
            .ToList();
    }
}
