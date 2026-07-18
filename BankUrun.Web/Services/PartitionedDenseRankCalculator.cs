namespace BankUrun.Web.Services;

public sealed record PartitionedRank(int? Rank, int CandidateCount);

public static class PartitionedDenseRankCalculator
{
    public static IReadOnlyList<PartitionedRank> Calculate<TRow, TKey>(
        IReadOnlyList<TRow> rows,
        Func<TRow, TKey> partition,
        Func<TRow, decimal?> score) where TKey : notnull
    {
        var results = Enumerable.Repeat(new PartitionedRank(null, 0), rows.Count).ToArray();
        foreach (var group in rows.Select((row, index) => new { row, index }).GroupBy(item => partition(item.row)))
        {
            var items = group.ToList();
            var scores = items.Select(item => score(item.row)).ToList();
            var ranks = DenseRankCalculator.Calculate(scores);
            var candidateCount = scores.Count(value => value.HasValue);
            for (var index = 0; index < items.Count; index++)
            {
                results[items[index].index] = new PartitionedRank(ranks[index], candidateCount);
            }
        }
        return results;
    }
}
