using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class GroupScopedMainProductParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branch_main_product_monthly_metrics_branches_branch_id",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.DropForeignKey(
                name: "FK_branch_main_product_monthly_metrics_main_product_parameters~",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.DropIndex(
                name: "IX_main_product_parameters_main_product_instance_id",
                table: "main_product_parameters");

            migrationBuilder.DropIndex(
                name: "IX_branch_main_product_monthly_metrics_main_product_parameter_~",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.AddColumn<int>(
                name: "group_id",
                table: "main_product_parameters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "group_id",
                table: "branch_main_product_monthly_metrics",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE _old_main_product_parameters ON COMMIT DROP AS
                SELECT id, main_product_instance_id, calculation_type, criterion_score,
                       is_active, created_at, updated_at
                FROM main_product_parameters;

                UPDATE main_product_parameters
                SET group_id = (SELECT MIN(id) FROM group_definitions);

                INSERT INTO main_product_parameters
                    (group_id, main_product_instance_id, calculation_type, criterion_score,
                     is_active, created_at, updated_at)
                SELECT groups.id, old.main_product_instance_id, old.calculation_type,
                       old.criterion_score, old.is_active, old.created_at, old.updated_at
                FROM _old_main_product_parameters old
                CROSS JOIN group_definitions groups
                WHERE groups.id <> (SELECT MIN(id) FROM group_definitions);

                UPDATE branch_main_product_monthly_metrics metrics
                SET group_id = branches.group_id,
                    main_product_parameter_id = scoped_parameter.id
                FROM branches,
                     _old_main_product_parameters old_parameter,
                     main_product_parameters scoped_parameter
                WHERE metrics.branch_id = branches.id
                  AND metrics.main_product_parameter_id = old_parameter.id
                  AND scoped_parameter.group_id = branches.group_id
                  AND scoped_parameter.main_product_instance_id = old_parameter.main_product_instance_id;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "group_id",
                table: "main_product_parameters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "group_id",
                table: "branch_main_product_monthly_metrics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_main_product_parameters_id_group_id",
                table: "main_product_parameters",
                columns: new[] { "id", "group_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_branches_id_group_id",
                table: "branches",
                columns: new[] { "id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_main_product_parameters_group_id_main_product_instance_id",
                table: "main_product_parameters",
                columns: new[] { "group_id", "main_product_instance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_main_product_parameters_main_product_instance_id",
                table: "main_product_parameters",
                column: "main_product_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_branch_id_group_id",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "branch_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_main_product_parameter_~",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "main_product_parameter_id", "group_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_branch_main_product_monthly_metrics_branches_branch_id_grou~",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "branch_id", "group_id" },
                principalTable: "branches",
                principalColumns: new[] { "id", "group_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_branch_main_product_monthly_metrics_main_product_parameters~",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "main_product_parameter_id", "group_id" },
                principalTable: "main_product_parameters",
                principalColumns: new[] { "id", "group_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_main_product_parameters_group_definitions_group_id",
                table: "main_product_parameters",
                column: "group_id",
                principalTable: "group_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branch_main_product_monthly_metrics_branches_branch_id_grou~",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.DropForeignKey(
                name: "FK_branch_main_product_monthly_metrics_main_product_parameters~",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.DropForeignKey(
                name: "FK_main_product_parameters_group_definitions_group_id",
                table: "main_product_parameters");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_main_product_parameters_id_group_id",
                table: "main_product_parameters");

            migrationBuilder.DropIndex(
                name: "IX_main_product_parameters_group_id_main_product_instance_id",
                table: "main_product_parameters");

            migrationBuilder.DropIndex(
                name: "IX_main_product_parameters_main_product_instance_id",
                table: "main_product_parameters");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_branches_id_group_id",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "IX_branch_main_product_monthly_metrics_branch_id_group_id",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.DropIndex(
                name: "IX_branch_main_product_monthly_metrics_main_product_parameter_~",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE _kept_main_product_parameters ON COMMIT DROP AS
                SELECT main_product_instance_id, MIN(id) AS kept_id
                FROM main_product_parameters
                GROUP BY main_product_instance_id;

                UPDATE branch_main_product_monthly_metrics metrics
                SET main_product_parameter_id = kept.kept_id
                FROM main_product_parameters parameter,
                     _kept_main_product_parameters kept
                WHERE metrics.main_product_parameter_id = parameter.id
                  AND kept.main_product_instance_id = parameter.main_product_instance_id;

                DELETE FROM main_product_parameters parameter
                USING _kept_main_product_parameters kept
                WHERE parameter.main_product_instance_id = kept.main_product_instance_id
                  AND parameter.id <> kept.kept_id;
                """);

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "main_product_parameters");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "branch_main_product_monthly_metrics");

            migrationBuilder.CreateIndex(
                name: "IX_main_product_parameters_main_product_instance_id",
                table: "main_product_parameters",
                column: "main_product_instance_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_main_product_parameter_~",
                table: "branch_main_product_monthly_metrics",
                column: "main_product_parameter_id");

            migrationBuilder.AddForeignKey(
                name: "FK_branch_main_product_monthly_metrics_branches_branch_id",
                table: "branch_main_product_monthly_metrics",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_branch_main_product_monthly_metrics_main_product_parameters~",
                table: "branch_main_product_monthly_metrics",
                column: "main_product_parameter_id",
                principalTable: "main_product_parameters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
