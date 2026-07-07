using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<MainProductPeriod> MainProductPeriods => Set<MainProductPeriod>();
    public DbSet<SubProductAssignment> SubProductAssignments => Set<SubProductAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).HasColumnName("id");
            entity.Property(product => product.Code).HasColumnName("code").HasMaxLength(2).IsRequired();
            entity.Property(product => product.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(product => product.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(product => product.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(product => product.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(product => product.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(product => new { product.Type, product.Code }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_products_code_format", "code ~ '^[A-Z0-9]{2}$'");
                table.HasCheckConstraint("ck_products_name_not_blank", "length(btrim(name)) > 0");
            });
        });

        modelBuilder.Entity<Period>(entity =>
        {
            entity.ToTable("periods");
            entity.HasKey(period => period.Id);
            entity.Property(period => period.Id).HasColumnName("id");
            entity.Property(period => period.Year).HasColumnName("year");
            entity.Property(period => period.Term).HasColumnName("term");
            entity.HasIndex(period => new { period.Year, period.Term }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_periods_year_range", "year between 2000 and 2100");
                table.HasCheckConstraint("ck_periods_term_range", "term between 1 and 12");
            });
        });

        modelBuilder.Entity<MainProductPeriod>(entity =>
        {
            entity.ToTable("main_product_periods");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.MainProductId).HasColumnName("main_product_id");
            entity.Property(item => item.PeriodId).HasColumnName("period_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => new { item.MainProductId, item.PeriodId }).IsUnique();
            entity.HasOne(item => item.MainProduct)
                .WithMany(product => product.MainProductPeriods)
                .HasForeignKey(item => item.MainProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Period)
                .WithMany(period => period.MainProductPeriods)
                .HasForeignKey(item => item.PeriodId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubProductAssignment>(entity =>
        {
            entity.ToTable("sub_product_assignments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.MainProductPeriodId).HasColumnName("main_product_period_id");
            entity.Property(item => item.SubProductId).HasColumnName("sub_product_id");
            entity.Property(item => item.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(item => new { item.MainProductPeriodId, item.SubProductId }).IsUnique();
            entity.HasOne(item => item.MainProductPeriod)
                .WithMany(period => period.SubProductAssignments)
                .HasForeignKey(item => item.MainProductPeriodId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.SubProduct)
                .WithMany(product => product.SubProductAssignments)
                .HasForeignKey(item => item.SubProductId)
                .OnDelete(DeleteBehavior.Cascade);
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
    }
}
