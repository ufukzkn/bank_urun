using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchSubProductMonthlyMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_sub_product_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_definition_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    term = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    actual_as_of_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_sub_product_monthly_metrics", x => x.id);
                    table.CheckConstraint("ck_branch_sub_product_metrics_actual_pair", "(actual_value is null and actual_as_of_date is null) or (actual_value is not null and actual_as_of_date is not null)");
                    table.CheckConstraint("ck_branch_sub_product_metrics_month", "month between 1 and 12");
                    table.CheckConstraint("ck_branch_sub_product_metrics_term", "term in (1, 2)");
                    table.CheckConstraint("ck_branch_sub_product_metrics_type", "product_definition_type = 'Sub'");
                    table.CheckConstraint("ck_branch_sub_product_metrics_values", "target_value >= 0 and (actual_value is null or actual_value >= 0)");
                    table.CheckConstraint("ck_branch_sub_product_metrics_year", "year between 2000 and 2100");
                    table.ForeignKey(
                        name: "FK_branch_sub_product_monthly_metrics_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_sub_product_monthly_metrics_product_definitions_sub_~",
                        columns: x => new { x.sub_product_id, x.product_definition_type },
                        principalTable: "product_definitions",
                        principalColumns: new[] { "id", "product_type" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_sub_product_monthly_metrics_branch_id_sub_product_id~",
                table: "branch_sub_product_monthly_metrics",
                columns: new[] { "branch_id", "sub_product_id", "year", "term", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_sub_product_monthly_metrics_sub_product_id_product_d~",
                table: "branch_sub_product_monthly_metrics",
                columns: new[] { "sub_product_id", "product_definition_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_sub_product_monthly_metrics");
        }
    }
}
