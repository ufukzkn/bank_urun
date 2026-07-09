using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProductDefinition> ProductDefinitions => Set<ProductDefinition>();
    public DbSet<MainProductInstance> MainProductInstances => Set<MainProductInstance>();
    public DbSet<SubProductInstance> SubProductInstances => Set<SubProductInstance>();
    public DbSet<GroupDefinition> GroupDefinitions => Set<GroupDefinition>();
    public DbSet<UnitDefinition> UnitDefinitions => Set<UnitDefinition>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<GroupUnit> GroupUnits => Set<GroupUnit>();
    public DbSet<BranchUnit> BranchUnits => Set<BranchUnit>();
    public DbSet<GroupProductScore> GroupProductScores => Set<GroupProductScore>();
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
            entity.HasOne(item => item.MainProduct)
                .WithMany(product => product.MainProductInstances)
                .HasForeignKey(item => new { item.MainProductId, item.ProductDefinitionType })
                .HasPrincipalKey(product => new { product.Id, product.Type })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_main_product_instances_type", "product_definition_type = 'Main'");
                table.HasCheckConstraint("ck_main_product_instances_year_range", "year between 2000 and 2100");
                table.HasCheckConstraint("ck_main_product_instances_term_range", "term between 1 and 12");
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
            entity.Property(group => group.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(group => group.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(group => group.GroupNo).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_group_definitions_group_no_not_blank", "length(btrim(group_no)) > 0");
                table.HasCheckConstraint("ck_group_definitions_name_not_blank", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<UnitDefinition>(entity =>
        {
            entity.ToTable("unit_definitions");
            entity.HasKey(unit => unit.Id);
            entity.Property(unit => unit.Id).HasColumnName("id");
            entity.Property(unit => unit.UnitNo).HasColumnName("unit_no").HasMaxLength(24).IsRequired();
            entity.Property(unit => unit.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(unit => unit.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(unit => unit.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(unit => unit.UnitNo).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_unit_definitions_unit_no_not_blank", "length(btrim(unit_no)) > 0");
                table.HasCheckConstraint("ck_unit_definitions_name_not_blank", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches");
            entity.HasKey(branch => branch.Id);
            entity.Property(branch => branch.Id).HasColumnName("id");
            entity.Property(branch => branch.BranchCode).HasColumnName("branch_code").HasMaxLength(24).IsRequired();
            entity.Property(branch => branch.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(branch => branch.BranchType).HasColumnName("branch_type").HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(branch => branch.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(branch => branch.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(branch => branch.BranchCode).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_branches_code_not_blank", "length(btrim(branch_code)) > 0");
                table.HasCheckConstraint("ck_branches_name_not_blank", "length(btrim(name)) > 0");
                table.HasCheckConstraint("ck_branches_type", "branch_type in ('Karma', 'Kurumsal', 'Ticari')");
            });
        });

        modelBuilder.Entity<GroupUnit>(entity =>
        {
            entity.ToTable("group_units");
            entity.HasKey(link => link.Id);
            entity.Property(link => link.Id).HasColumnName("id");
            entity.Property(link => link.GroupId).HasColumnName("group_id");
            entity.Property(link => link.UnitId).HasColumnName("unit_id");
            entity.Property(link => link.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.HasIndex(link => new { link.GroupId, link.UnitId }).IsUnique();
            entity.HasOne(link => link.Group)
                .WithMany(group => group.GroupUnits)
                .HasForeignKey(link => link.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Unit)
                .WithMany(unit => unit.GroupUnits)
                .HasForeignKey(link => link.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BranchUnit>(entity =>
        {
            entity.ToTable("branch_units");
            entity.HasKey(link => link.Id);
            entity.Property(link => link.Id).HasColumnName("id");
            entity.Property(link => link.BranchId).HasColumnName("branch_id");
            entity.Property(link => link.UnitId).HasColumnName("unit_id");
            entity.Property(link => link.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.HasIndex(link => new { link.BranchId, link.UnitId }).IsUnique();
            entity.HasOne(link => link.Branch)
                .WithMany(branch => branch.BranchUnits)
                .HasForeignKey(link => link.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(link => link.Unit)
                .WithMany(unit => unit.BranchUnits)
                .HasForeignKey(link => link.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupProductScore>(entity =>
        {
            entity.ToTable("group_product_scores");
            entity.HasKey(score => score.Id);
            entity.Property(score => score.Id).HasColumnName("id");
            entity.Property(score => score.GroupId).HasColumnName("group_id");
            entity.Property(score => score.SubProductInstanceId).HasColumnName("sub_product_instance_id");
            entity.Property(score => score.Score).HasColumnName("score").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(score => score.TargetValue).HasColumnName("target_value").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(score => score.HgoShare).HasColumnName("hgo_share").HasColumnType("numeric(9,4)").IsRequired();
            entity.Property(score => score.DevelopmentShare).HasColumnName("development_share").HasColumnType("numeric(9,4)").IsRequired();
            entity.Property(score => score.SizeShare).HasColumnName("size_share").HasColumnType("numeric(9,4)").IsRequired();
            entity.Property(score => score.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(score => score.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(score => new { score.GroupId, score.SubProductInstanceId }).IsUnique();
            entity.HasOne(score => score.Group)
                .WithMany(group => group.GroupProductScores)
                .HasForeignKey(score => score.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(score => score.SubProductInstance)
                .WithMany(instance => instance.GroupProductScores)
                .HasForeignKey(score => score.SubProductInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_group_product_scores_score_non_negative", "score >= 0");
                table.HasCheckConstraint("ck_group_product_scores_target_non_negative", "target_value >= 0");
                table.HasCheckConstraint("ck_group_product_scores_hgo_share_range", "hgo_share between 0 and 1");
                table.HasCheckConstraint("ck_group_product_scores_development_share_range", "development_share between 0 and 1");
                table.HasCheckConstraint("ck_group_product_scores_size_share_range", "size_share between 0 and 1");
            });
        });
    }
}
