using System.Reflection;
using BankUrun.Web.Data;
using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace BankUrun.Tests;

public class PerformanceOptimizationSchemaTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Model_HasPeriodFocusedPerformanceIndexes()
    {
        using var db = CreateContext();

        AssertIndex<PortfolioSubProductMonthlyMetric>(
            db,
            "ix_portfolio_metrics_period_scope",
            nameof(PortfolioSubProductMonthlyMetric.Year),
            nameof(PortfolioSubProductMonthlyMetric.Term),
            nameof(PortfolioSubProductMonthlyMetric.PortfolioId),
            nameof(PortfolioSubProductMonthlyMetric.SubProductId),
            nameof(PortfolioSubProductMonthlyMetric.Month));
        AssertIndex<MainProductInstance>(
            db,
            "ix_main_product_instances_period_scope",
            nameof(MainProductInstance.Year),
            nameof(MainProductInstance.Term),
            nameof(MainProductInstance.MainProductId));
    }

    [Fact]
    public void OptimizationMigration_CreatesBothPeriodFocusedIndexes()
    {
        var migrationType = typeof(AppDbContext).Assembly
            .GetTypes()
            .Single(type => type.Name == "OptimizePerformanceQueryIndexes");
        var migration = Assert.IsAssignableFrom<Migration>(Activator.CreateInstance(migrationType));
        var upMethod = migrationType.GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(upMethod);
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        upMethod.Invoke(migration, [builder]);

        var indexes = builder.Operations.OfType<CreateIndexOperation>().ToList();
        Assert.Collection(
            indexes.OrderBy(index => index.Name),
            index =>
            {
                Assert.Equal("ix_main_product_instances_period_scope", index.Name);
                Assert.Equal("main_product_instances", index.Table);
                Assert.Equal(["year", "term", "main_product_id"], index.Columns);
            },
            index =>
            {
                Assert.Equal("ix_portfolio_metrics_period_scope", index.Name);
                Assert.Equal("portfolio_sub_product_monthly_metrics", index.Table);
                Assert.Equal(
                    ["year", "term", "portfolio_id", "sub_product_id", "month"],
                    index.Columns);
            });
    }

    [Fact]
    public void BatchedPeriodAndEffectiveRangePredicates_TranslateForPostgreSql()
    {
        using var db = CreateContext();
        var periodCodes = new[] { 20251, 20252, 20261 };
        var firstPeriodCode = periodCodes.Min();
        var lastPeriodCode = periodCodes.Max();

        var instanceSql = db.MainProductInstances
            .Where(instance => periodCodes.Contains(instance.Year * 10 + instance.Term))
            .ToQueryString();
        var assignmentSql = db.ProductGamutMainProductAssignments
            .Where(assignment =>
                assignment.EffectiveFromYear * 10 + assignment.EffectiveFromTerm <= lastPeriodCode
                && (!assignment.EffectiveToYear.HasValue
                    || assignment.EffectiveToYear.Value * 10
                        + assignment.EffectiveToTerm!.Value >= firstPeriodCode))
            .ToQueryString();

        Assert.Contains("main_product_instances", instanceSql);
        Assert.Contains("product_gamut_main_product_assignments", assignmentSql);
    }

    private static void AssertIndex<TEntity>(
        AppDbContext db,
        string databaseName,
        params string[] propertyNames)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        Assert.Contains(entity.GetIndexes(), index =>
            !index.IsUnique
            && index.GetDatabaseName() == databaseName
            && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }
}
