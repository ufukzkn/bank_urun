using BankUrun.Web.Data;
using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Tests;

public class PortfolioSchemaTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Model_UsesPortfolioTargetsAndActualsAsSeparateSources()
    {
        using var db = CreateContext();

        var target = db.Model.FindEntityType(typeof(PortfolioMainProductMonthlyTarget));
        var actual = db.Model.FindEntityType(typeof(PortfolioSubProductMonthlyMetric));

        Assert.NotNull(target);
        Assert.Equal("portfolio_main_product_monthly_targets", target.GetTableName());
        Assert.NotNull(target.FindProperty(nameof(PortfolioMainProductMonthlyTarget.TargetValue)));
        Assert.Null(target.FindProperty("ActualValue"));

        Assert.NotNull(actual);
        Assert.Equal("portfolio_sub_product_monthly_metrics", actual.GetTableName());
        Assert.NotNull(actual.FindProperty(nameof(PortfolioSubProductMonthlyMetric.ActualValue)));
        Assert.Null(actual.FindProperty("TargetValue"));
    }

    [Fact]
    public void Model_EnforcesPortfolioBranchAndGamutGroupCompatibility()
    {
        using var db = CreateContext();
        var portfolio = db.Model.FindEntityType(typeof(Portfolio));

        Assert.NotNull(portfolio);
        var foreignKeyPropertySets = portfolio.GetForeignKeys()
            .Select(key => key.Properties.Select(property => property.Name).ToArray())
            .ToList();

        Assert.Contains(foreignKeyPropertySets,
            properties => properties.SequenceEqual([nameof(Portfolio.BranchId), nameof(Portfolio.GroupId)]));
        Assert.Contains(foreignKeyPropertySets,
            properties => properties.SequenceEqual([nameof(Portfolio.ProductGamutId), nameof(Portfolio.GroupId)]));
    }

    [Fact]
    public void Model_ExcludesRetiredPerformanceTables()
    {
        using var db = CreateContext();

        var entityNames = db.Model.GetEntityTypes().Select(entity => entity.ClrType.Name).ToHashSet();
        Assert.DoesNotContain("BranchMainProductMonthlyMetric", entityNames);
        Assert.DoesNotContain("BranchSubProductMonthlyMetric", entityNames);
        Assert.DoesNotContain("MainProductSegmentRule", entityNames);
    }

    [Fact]
    public void Model_HasRequiredUniqueBusinessKeys()
    {
        using var db = CreateContext();

        AssertUniqueIndex<ProductGamut>(db, nameof(ProductGamut.GroupId), nameof(ProductGamut.Code));
        AssertUniqueIndex<Portfolio>(db, nameof(Portfolio.Code));
        AssertUniqueIndex<PortfolioMainProductMonthlyTarget>(db,
            nameof(PortfolioMainProductMonthlyTarget.PortfolioId),
            nameof(PortfolioMainProductMonthlyTarget.MainProductParameterId),
            nameof(PortfolioMainProductMonthlyTarget.Month));
        AssertUniqueIndex<PortfolioSubProductMonthlyMetric>(db,
            nameof(PortfolioSubProductMonthlyMetric.PortfolioId),
            nameof(PortfolioSubProductMonthlyMetric.SubProductId),
            nameof(PortfolioSubProductMonthlyMetric.Year),
            nameof(PortfolioSubProductMonthlyMetric.Term),
            nameof(PortfolioSubProductMonthlyMetric.Month));
    }

    [Fact]
    public void ActualMetric_ReferencesStableSubProductDefinition()
    {
        using var db = CreateContext();
        var actual = db.Model.FindEntityType(typeof(PortfolioSubProductMonthlyMetric));

        Assert.NotNull(actual);
        Assert.Null(actual.FindProperty("SubProductInstanceId"));
        Assert.NotNull(actual.FindProperty(nameof(PortfolioSubProductMonthlyMetric.SubProductId)));
        Assert.Contains(actual.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductDefinition)
            && foreignKey.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(PortfolioSubProductMonthlyMetric.SubProductId),
                 nameof(PortfolioSubProductMonthlyMetric.ProductDefinitionType)]));
    }

    private static void AssertUniqueIndex<TEntity>(AppDbContext db, params string[] propertyNames)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }
}
