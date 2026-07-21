using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class OptimizePerformanceQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_portfolio_metrics_period_scope",
                table: "portfolio_sub_product_monthly_metrics",
                columns: new[] { "year", "term", "portfolio_id", "sub_product_id", "month" });

            migrationBuilder.CreateIndex(
                name: "ix_main_product_instances_period_scope",
                table: "main_product_instances",
                columns: new[] { "year", "term", "main_product_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_portfolio_metrics_period_scope",
                table: "portfolio_sub_product_monthly_metrics");

            migrationBuilder.DropIndex(
                name: "ix_main_product_instances_period_scope",
                table: "main_product_instances");
        }
    }
}
