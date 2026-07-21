using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProductDefinition> ProductDefinitions => Set<ProductDefinition>();
    public DbSet<MainProductInstance> MainProductInstances => Set<MainProductInstance>();
    public DbSet<SubProductInstance> SubProductInstances => Set<SubProductInstance>();
    public DbSet<GroupDefinition> GroupDefinitions => Set<GroupDefinition>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<MainProductParameter> MainProductParameters => Set<MainProductParameter>();
    public DbSet<PortfolioType> PortfolioTypes => Set<PortfolioType>();
    public DbSet<ProductGamut> ProductGamuts => Set<ProductGamut>();
    public DbSet<ProductGamutMainProductAssignment> ProductGamutMainProductAssignments => Set<ProductGamutMainProductAssignment>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<BranchMainProductExclusion> BranchMainProductExclusions => Set<BranchMainProductExclusion>();
    public DbSet<PortfolioMainProductMonthlyTarget> PortfolioMainProductMonthlyTargets => Set<PortfolioMainProductMonthlyTarget>();
    public DbSet<PortfolioSubProductMonthlyMetric> PortfolioSubProductMonthlyMetrics => Set<PortfolioSubProductMonthlyMetric>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductDefinition>(entity =>
        {
            entity.ToTable("product_definitions");
            entity.HasKey(product => product.Id);
            entity.HasAlternateKey(product => new { product.Id, product.Type });
            entity.Property(product => product.Id).HasColumnName("id");
            entity.Property(product => product.Type).HasColumnName("product_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(product => product.Code).HasColumnName("code").HasMaxLength(2).IsRequired();
            entity.Property(product => product.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(product => product.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(product => product.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(product => product.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(product => new { product.Type, product.Code }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_product_definitions_type", "product_type in ('Main', 'Sub')");
                table.HasCheckConstraint("ck_product_definitions_code_format", "code ~ '^[A-Z0-9]{2}$'");
                table.HasCheckConstraint("ck_product_definitions_name_not_blank", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<MainProductInstance>(entity =>
        {
            entity.ToTable("main_product_instances");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.MainProductId).HasColumnName("main_product_id");
            entity.Property(item => item.ProductDefinitionType).HasColumnName("product_definition_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(item => item.Year).HasColumnName("year").IsRequired();
            entity.Property(item => item.Term).HasColumnName("term").IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.MainProductId, item.Year, item.Term }).IsUnique();
            entity.HasIndex(item => new { item.Year, item.Term, item.MainProductId })
                .HasDatabaseName("ix_main_product_instances_period_scope");
            entity.HasOne(item => item.MainProduct)
                .WithMany(product => product.MainProductInstances)
                .HasForeignKey(item => new { item.MainProductId, item.ProductDefinitionType })
                .HasPrincipalKey(product => new { product.Id, product.Type })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_main_product_instances_type", "product_definition_type = 'Main'");
                table.HasCheckConstraint("ck_main_product_instances_year_range", "year between 2000 and 2100");
                table.HasCheckConstraint("ck_main_product_instances_term_range", "term in (1, 2)");
            });
        });

        modelBuilder.Entity<SubProductInstance>(entity =>
        {
            entity.ToTable("sub_product_instances");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.MainProductInstanceId).HasColumnName("main_product_instance_id");
            entity.Property(item => item.SubProductId).HasColumnName("sub_product_id");
            entity.Property(item => item.ProductDefinitionType).HasColumnName("product_definition_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.MainProductInstanceId, item.SubProductId }).IsUnique();
            entity.HasOne(item => item.MainProductInstance)
                .WithMany(instance => instance.SubProductInstances)
                .HasForeignKey(item => item.MainProductInstanceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.SubProduct)
                .WithMany(product => product.SubProductInstances)
                .HasForeignKey(item => new { item.SubProductId, item.ProductDefinitionType })
                .HasPrincipalKey(product => new { product.Id, product.Type })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_sub_product_instances_type", "product_definition_type = 'Sub'");
            });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).HasColumnName("id");
            entity.Property(log => log.Action).HasColumnName("action").HasMaxLength(60).IsRequired();
            entity.Property(log => log.EntityName).HasColumnName("entity_name").HasMaxLength(80).IsRequired();
            entity.Property(log => log.EntityKey).HasColumnName("entity_key").HasMaxLength(80).IsRequired();
            entity.Property(log => log.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(log => log.Actor).HasColumnName("actor").HasMaxLength(120).IsRequired();
            entity.Property(log => log.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<GroupDefinition>(entity =>
        {
            entity.ToTable("group_definitions");
            entity.HasKey(group => group.Id);
            entity.Property(group => group.Id).HasColumnName("id");
            entity.Property(group => group.GroupNo).HasColumnName("group_no").HasMaxLength(24).IsRequired();
            entity.Property(group => group.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(group => group.GroupType).HasColumnName("group_type").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(group => group.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(group => group.BranchPerformanceEnabled).HasColumnName("branch_performance_enabled").HasDefaultValue(true);
            entity.Property(group => group.MiyPerformanceEnabled).HasColumnName("miy_performance_enabled").HasDefaultValue(true);
            entity.Property(group => group.ScaleEnabled).HasColumnName("scale_enabled").HasDefaultValue(true);
            entity.Property(group => group.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(group => group.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(group => group.GroupNo).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_group_definitions_type", "group_type in ('Karma', 'Kurumsal', 'Ticari', 'Kobi', 'Diger')");
                table.HasCheckConstraint("ck_group_definitions_group_no_not_blank", "length(btrim(group_no)) > 0");
                table.HasCheckConstraint("ck_group_definitions_name_not_blank", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches");
            entity.HasKey(branch => branch.Id);
            entity.Property(branch => branch.Id).HasColumnName("id");
            entity.Property(branch => branch.GroupId).HasColumnName("group_id");
            entity.Property(branch => branch.BranchCode).HasColumnName("branch_code").HasMaxLength(24).IsRequired();
            entity.Property(branch => branch.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(branch => branch.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(branch => branch.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(branch => branch.BranchCode).IsUnique();
            entity.HasIndex(branch => branch.GroupId);
            entity.HasAlternateKey(branch => new { branch.Id, branch.GroupId });
            entity.HasOne(branch => branch.Group)
                .WithMany(group => group.Branches)
                .HasForeignKey(branch => branch.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_branches_code_not_blank", "length(btrim(branch_code)) > 0");
                table.HasCheckConstraint("ck_branches_name_not_blank", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<MainProductParameter>(entity =>
        {
            entity.ToTable("main_product_parameters");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.GroupId).HasColumnName("group_id");
            entity.Property(item => item.MainProductInstanceId).HasColumnName("main_product_instance_id");
            entity.Property(item => item.CalculationType).HasColumnName("calculation_type").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.CriterionScore).HasColumnName("criterion_score").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.GroupId, item.MainProductInstanceId }).IsUnique();
            entity.HasAlternateKey(item => new { item.Id, item.GroupId });
            entity.HasOne(item => item.Group)
                .WithMany(group => group.MainProductParameters)
                .HasForeignKey(item => item.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.MainProductInstance)
                .WithMany(instance => instance.Parameters)
                .HasForeignKey(item => item.MainProductInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_main_product_parameters_calculation_type", "calculation_type in ('Average', 'Cumulative')");
                table.HasCheckConstraint("ck_main_product_parameters_criterion_score", "criterion_score >= 0");
            });
        });

        modelBuilder.Entity<PortfolioType>(entity =>
        {
            entity.ToTable("portfolio_types");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Code).HasColumnName("code").HasMaxLength(2).IsRequired();
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => item.Code).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_portfolio_types_code", "code ~ '^[A-Z0-9]{2}$'");
                table.HasCheckConstraint("ck_portfolio_types_name", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<ProductGamut>(entity =>
        {
            entity.ToTable("product_gamuts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.GroupId).HasColumnName("group_id");
            entity.Property(item => item.Code).HasColumnName("code").HasMaxLength(2).IsRequired();
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.GroupId, item.Code }).IsUnique();
            entity.HasAlternateKey(item => new { item.Id, item.GroupId });
            entity.HasOne(item => item.Group).WithMany(group => group.ProductGamuts)
                .HasForeignKey(item => item.GroupId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_product_gamuts_code", "code ~ '^[A-Z0-9]{2}$'");
                table.HasCheckConstraint("ck_product_gamuts_name", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<ProductGamutMainProductAssignment>(entity =>
        {
            entity.ToTable("product_gamut_main_product_assignments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.ProductGamutId).HasColumnName("product_gamut_id");
            entity.Property(item => item.MainProductId).HasColumnName("main_product_id");
            entity.Property(item => item.ProductDefinitionType).HasColumnName("product_definition_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(item => item.EffectiveFromYear).HasColumnName("effective_from_year");
            entity.Property(item => item.EffectiveFromTerm).HasColumnName("effective_from_term");
            entity.Property(item => item.EffectiveToYear).HasColumnName("effective_to_year");
            entity.Property(item => item.EffectiveToTerm).HasColumnName("effective_to_term");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.ProductGamutId, item.MainProductId, item.EffectiveFromYear, item.EffectiveFromTerm }).IsUnique();
            entity.HasOne(item => item.ProductGamut).WithMany(gamut => gamut.MainProductAssignments)
                .HasForeignKey(item => item.ProductGamutId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.MainProduct).WithMany(product => product.ProductGamutAssignments)
                .HasForeignKey(item => new { item.MainProductId, item.ProductDefinitionType })
                .HasPrincipalKey(product => new { product.Id, product.Type }).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_gamut_assignments_type", "product_definition_type = 'Main'");
                table.HasCheckConstraint("ck_gamut_assignments_from", "effective_from_year between 2000 and 2100 and effective_from_term in (1, 2)");
                table.HasCheckConstraint("ck_gamut_assignments_to", "(effective_to_year is null and effective_to_term is null) or (effective_to_year between 2000 and 2100 and effective_to_term in (1, 2) and (effective_to_year * 2 + effective_to_term) >= (effective_from_year * 2 + effective_from_term))");
            });
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("portfolios");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.BranchId).HasColumnName("branch_id");
            entity.Property(item => item.GroupId).HasColumnName("group_id");
            entity.Property(item => item.ProductGamutId).HasColumnName("product_gamut_id");
            entity.Property(item => item.PortfolioTypeId).HasColumnName("portfolio_type_id");
            entity.Property(item => item.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => item.Code).IsUnique();
            entity.HasIndex(item => new { item.BranchId, item.ProductGamutId });
            entity.HasAlternateKey(item => new { item.Id, item.GroupId });
            entity.HasOne(item => item.Branch).WithMany(branch => branch.Portfolios)
                .HasForeignKey(item => new { item.BranchId, item.GroupId })
                .HasPrincipalKey(branch => new { branch.Id, branch.GroupId }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.ProductGamut).WithMany(gamut => gamut.Portfolios)
                .HasForeignKey(item => new { item.ProductGamutId, item.GroupId })
                .HasPrincipalKey(gamut => new { gamut.Id, gamut.GroupId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PortfolioType).WithMany(type => type.Portfolios)
                .HasForeignKey(item => item.PortfolioTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_portfolios_code", "code ~ '^P[A-Z0-9]+-[A-Z0-9]{2}[0-9]{2}$'");
                table.HasCheckConstraint("ck_portfolios_name", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<BranchMainProductExclusion>(entity =>
        {
            entity.ToTable("branch_main_product_exclusions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.BranchId).HasColumnName("branch_id");
            entity.Property(item => item.MainProductId).HasColumnName("main_product_id");
            entity.Property(item => item.ProductDefinitionType).HasColumnName("product_definition_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(item => item.EffectiveFromYear).HasColumnName("effective_from_year");
            entity.Property(item => item.EffectiveFromTerm).HasColumnName("effective_from_term");
            entity.Property(item => item.EffectiveToYear).HasColumnName("effective_to_year");
            entity.Property(item => item.EffectiveToTerm).HasColumnName("effective_to_term");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.BranchId, item.MainProductId, item.EffectiveFromYear, item.EffectiveFromTerm }).IsUnique();
            entity.HasOne(item => item.Branch).WithMany(branch => branch.MainProductExclusions)
                .HasForeignKey(item => item.BranchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.MainProduct).WithMany(product => product.BranchExclusions)
                .HasForeignKey(item => new { item.MainProductId, item.ProductDefinitionType })
                .HasPrincipalKey(product => new { product.Id, product.Type }).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_branch_exclusions_type", "product_definition_type = 'Main'");
                table.HasCheckConstraint("ck_branch_exclusions_from", "effective_from_year between 2000 and 2100 and effective_from_term in (1, 2)");
                table.HasCheckConstraint("ck_branch_exclusions_to", "(effective_to_year is null and effective_to_term is null) or (effective_to_year between 2000 and 2100 and effective_to_term in (1, 2) and (effective_to_year * 2 + effective_to_term) >= (effective_from_year * 2 + effective_from_term))");
            });
        });

        modelBuilder.Entity<PortfolioMainProductMonthlyTarget>(entity =>
        {
            entity.ToTable("portfolio_main_product_monthly_targets");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.PortfolioId).HasColumnName("portfolio_id");
            entity.Property(item => item.GroupId).HasColumnName("group_id");
            entity.Property(item => item.MainProductParameterId).HasColumnName("main_product_parameter_id");
            entity.Property(item => item.Month).HasColumnName("month");
            entity.Property(item => item.TargetValue).HasColumnName("target_value").HasColumnType("numeric(18,2)");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.PortfolioId, item.MainProductParameterId, item.Month }).IsUnique();
            entity.HasOne(item => item.Portfolio).WithMany(portfolio => portfolio.MainProductMonthlyTargets)
                .HasForeignKey(item => new { item.PortfolioId, item.GroupId })
                .HasPrincipalKey(portfolio => new { portfolio.Id, portfolio.GroupId }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.MainProductParameter).WithMany(parameter => parameter.PortfolioMonthlyTargets)
                .HasForeignKey(item => new { item.MainProductParameterId, item.GroupId })
                .HasPrincipalKey(parameter => new { parameter.Id, parameter.GroupId }).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_portfolio_targets_month", "month between 1 and 12");
                table.HasCheckConstraint("ck_portfolio_targets_value", "target_value >= 0");
            });
        });

        modelBuilder.Entity<PortfolioSubProductMonthlyMetric>(entity =>
        {
            entity.ToTable("portfolio_sub_product_monthly_metrics");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.PortfolioId).HasColumnName("portfolio_id");
            entity.Property(item => item.SubProductId).HasColumnName("sub_product_id");
            entity.Property(item => item.ProductDefinitionType).HasColumnName("product_definition_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(item => item.Year).HasColumnName("year");
            entity.Property(item => item.Term).HasColumnName("term");
            entity.Property(item => item.Month).HasColumnName("month");
            entity.Property(item => item.ActualValue).HasColumnName("actual_value").HasColumnType("numeric(18,2)");
            entity.Property(item => item.ActualAsOfDate).HasColumnName("actual_as_of_date").HasColumnType("date");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.PortfolioId, item.SubProductId, item.Year, item.Term, item.Month }).IsUnique();
            entity.HasIndex(item => new { item.Year, item.Term, item.PortfolioId, item.SubProductId, item.Month })
                .HasDatabaseName("ix_portfolio_metrics_period_scope");
            entity.HasOne(item => item.Portfolio).WithMany(portfolio => portfolio.SubProductMonthlyMetrics)
                .HasForeignKey(item => item.PortfolioId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.SubProduct).WithMany(product => product.PortfolioMonthlyMetrics)
                .HasForeignKey(item => new { item.SubProductId, item.ProductDefinitionType })
                .HasPrincipalKey(product => new { product.Id, product.Type }).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_portfolio_sub_metrics_year", "year between 2000 and 2100");
                table.HasCheckConstraint("ck_portfolio_sub_metrics_term", "term in (1, 2)");
                table.HasCheckConstraint("ck_portfolio_sub_metrics_month", "month between 1 and 12");
                table.HasCheckConstraint("ck_portfolio_sub_metrics_type", "product_definition_type = 'Sub'");
                table.HasCheckConstraint("ck_portfolio_sub_metrics_value", "actual_value is null or actual_value >= 0");
                table.HasCheckConstraint("ck_portfolio_sub_metrics_actual_pair", "(actual_value is null and actual_as_of_date is null) or (actual_value is not null and actual_as_of_date is not null)");
            });
        });
    }
}
