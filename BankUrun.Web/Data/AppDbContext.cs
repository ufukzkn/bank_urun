using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ProductDefinition> ProductDefinitions => Set<ProductDefinition>();
    public DbSet<MainProductInstance> MainProductInstances => Set<MainProductInstance>();
    public DbSet<SubProductInstance> SubProductInstances => Set<SubProductInstance>();
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
            entity.Property(item => item.MainProductType).HasColumnName("main_product_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(item => item.Year).HasColumnName("year").IsRequired();
            entity.Property(item => item.Term).HasColumnName("term").IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.MainProductId, item.Year, item.Term }).IsUnique();
            entity.HasOne(item => item.MainProduct)
                .WithMany(product => product.MainProductInstances)
                .HasForeignKey(item => new { item.MainProductId, item.MainProductType })
                .HasPrincipalKey(product => new { product.Id, product.Type })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_main_product_instances_type", "main_product_type = 'Main'");
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
            entity.Property(item => item.SubProductType).HasColumnName("sub_product_type").HasConversion<string>().HasMaxLength(12).IsRequired();
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.HasIndex(item => new { item.MainProductInstanceId, item.SubProductId }).IsUnique();
            entity.HasOne(item => item.MainProductInstance)
                .WithMany(instance => instance.SubProductInstances)
                .HasForeignKey(item => item.MainProductInstanceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.SubProduct)
                .WithMany(product => product.SubProductInstances)
                .HasForeignKey(item => new { item.SubProductId, item.SubProductType })
                .HasPrincipalKey(product => new { product.Id, product.Type })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_sub_product_instances_type", "sub_product_type = 'Sub'");
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
    }
}
