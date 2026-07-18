using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class UseStablePortfolioSubProductMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_main_product_instances_term_range",
                table: "main_product_instances");

            // Ham batch verisinin tek kaynağı artık dönemsel ana ürün bağlantısı değil,
            // stabil alt ürün tanımıdır. Demo veri taşınmayacağı için eski çoğaltılmış
            // satırları dönüştürmek yerine tabloyu tutarlı iş anahtarıyla yeniden kuruyoruz.
            migrationBuilder.DropTable(name: "portfolio_sub_product_monthly_metrics");

            migrationBuilder.CreateTable(
                name: "portfolio_sub_product_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    portfolio_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_definition_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    term = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    actual_as_of_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_sub_product_monthly_metrics", x => x.id);
                    table.CheckConstraint("ck_portfolio_sub_metrics_actual_pair", "(actual_value is null and actual_as_of_date is null) or (actual_value is not null and actual_as_of_date is not null)");
                    table.CheckConstraint("ck_portfolio_sub_metrics_month", "month between 1 and 12");
                    table.CheckConstraint("ck_portfolio_sub_metrics_term", "term in (1, 2)");
                    table.CheckConstraint("ck_portfolio_sub_metrics_type", "product_definition_type = 'Sub'");
                    table.CheckConstraint("ck_portfolio_sub_metrics_value", "actual_value is null or actual_value >= 0");
                    table.CheckConstraint("ck_portfolio_sub_metrics_year", "year between 2000 and 2100");
                    table.ForeignKey(
                        name: "FK_portfolio_sub_product_monthly_metrics_portfolios_portfolio_~",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_portfolio_sub_product_monthly_metrics_product_definitions_s~",
                        columns: x => new { x.sub_product_id, x.product_definition_type },
                        principalTable: "product_definitions",
                        principalColumns: new[] { "id", "product_type" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_sub_product_monthly_metrics_portfolio_id_sub_prod~",
                table: "portfolio_sub_product_monthly_metrics",
                columns: new[] { "portfolio_id", "sub_product_id", "year", "term", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_sub_product_monthly_metrics_sub_product_id_produc~",
                table: "portfolio_sub_product_monthly_metrics",
                columns: new[] { "sub_product_id", "product_definition_type" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_main_product_instances_term_range",
                table: "main_product_instances",
                sql: "term in (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_main_product_instances_term_range",
                table: "main_product_instances");

            migrationBuilder.DropTable(name: "portfolio_sub_product_monthly_metrics");

            migrationBuilder.CreateTable(
                name: "portfolio_sub_product_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    portfolio_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_instance_id = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    term = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    actual_as_of_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_sub_product_monthly_metrics", x => x.id);
                    table.CheckConstraint("ck_portfolio_sub_metrics_actual_pair", "(actual_value is null and actual_as_of_date is null) or (actual_value is not null and actual_as_of_date is not null)");
                    table.CheckConstraint("ck_portfolio_sub_metrics_month", "month between 1 and 12");
                    table.CheckConstraint("ck_portfolio_sub_metrics_term", "term in (1, 2)");
                    table.CheckConstraint("ck_portfolio_sub_metrics_value", "actual_value is null or actual_value >= 0");
                    table.CheckConstraint("ck_portfolio_sub_metrics_year", "year between 2000 and 2100");
                    table.ForeignKey(
                        name: "FK_portfolio_sub_product_monthly_metrics_portfolios_portfolio_~",
                        column: x => x.portfolio_id,
                        principalTable: "portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_portfolio_sub_product_monthly_metrics_sub_product_instances~",
                        column: x => x.sub_product_instance_id,
                        principalTable: "sub_product_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_sub_product_monthly_metrics_portfolio_id_sub_prod~",
                table: "portfolio_sub_product_monthly_metrics",
                columns: new[] { "portfolio_id", "sub_product_instance_id", "year", "term", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_sub_product_monthly_metrics_sub_product_instance_~",
                table: "portfolio_sub_product_monthly_metrics",
                column: "sub_product_instance_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_main_product_instances_term_range",
                table: "main_product_instances",
                sql: "term between 1 and 12");
        }
    }
}
