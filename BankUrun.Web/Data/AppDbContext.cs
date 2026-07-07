using BankUrun.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankUrun.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MainProduct> MainProducts => Set<MainProduct>();
    public DbSet<SubProduct> SubProducts => Set<SubProduct>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MainProduct>(entity =>
        {
            entity.ToTable("main_products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).HasColumnName("id");
            entity.Property(product => product.Code).HasColumnName("code").HasMaxLength(2).IsRequired();
            entity.Property(product => product.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(product => product.Year).HasColumnName("year").IsRequired();
            entity.Property(product => product.Term).HasColumnName("term").IsRequired();
            entity.Property(product => product.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(product => product.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(product => product.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(product => new { product.Code, product.Year, product.Term }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_main_products_code_format", "code ~ '^[A-Z0-9]{2}$'");
                table.HasCheckConstraint("ck_main_products_name_not_blank", "length(btrim(name)) > 0");
                table.HasCheckConstraint("ck_main_products_year_range", "year between 2000 and 2100");
                table.HasCheckConstraint("ck_main_products_term_range", "term between 1 and 12");
            });
        });

        modelBuilder.Entity<SubProduct>(entity =>
        {
            entity.ToTable("sub_products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).HasColumnName("id");
            entity.Property(product => product.MainProductId).HasColumnName("main_product_id");
            entity.Property(product => product.Code).HasColumnName("code").HasMaxLength(2).IsRequired();
            entity.Property(product => product.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
            entity.Property(product => product.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(product => product.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(product => product.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            entity.HasIndex(product => new { product.MainProductId, product.Code }).IsUnique();
            entity.HasOne(product => product.MainProduct)
                .WithMany(mainProduct => mainProduct.SubProducts)
                .HasForeignKey(product => product.MainProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_sub_products_code_format", "code ~ '^[A-Z0-9]{2}$'");
                table.HasCheckConstraint("ck_sub_products_name_not_blank", "length(btrim(name)) > 0");
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
