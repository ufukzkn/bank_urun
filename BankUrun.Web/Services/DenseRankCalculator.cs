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

    public static IReadOnlyList<int?> Calculate(
        IReadOnlyList<decimal?> scores,
        IReadOnlyList<decimal?> tieBreakers)
    {
        if (scores.Count != tieBreakers.Count)
        {
            throw new ArgumentException("Puan ve eşitlik anahtarı listeleri aynı uzunlukta olmalıdır.");
        }

        var keys = scores
            .Select((score, index) => score.HasValue
                ? new RankKey(score.Value, tieBreakers[index] ?? decimal.MinValue)
                : (RankKey?)null)
            .ToList();
        var ranks = keys
            .Where(key => key.HasValue)
            .Select(key => key!.Value)
            .Distinct()
            .OrderByDescending(key => key.Score)
            .ThenByDescending(key => key.TieBreaker)
            .Select((key, index) => new { key, rank = index + 1 })
            .ToDictionary(item => item.key, item => item.rank);

        return keys
            .Select(key => key.HasValue ? ranks[key.Value] : (int?)null)
            .ToList();
    }

    private readonly record struct RankKey(decimal Score, decimal TieBreaker);
}
