using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductDefinitionTypeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sub_product_instances_product_definitions_sub_product_id_su~",
                table: "sub_product_instances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sub_product_instances_type",
                table: "sub_product_instances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_main_product_instances_type",
                table: "main_product_instances");

            migrationBuilder.RenameColumn(
                name: "sub_product_type",
                table: "sub_product_instances",
                newName: "product_definition_type");

            migrationBuilder.RenameIndex(
                name: "IX_sub_product_instances_sub_product_id_sub_product_type",
                table: "sub_product_instances",
                newName: "IX_sub_product_instances_sub_product_id_product_definition_type");

            migrationBuilder.RenameColumn(
                name: "main_product_type",
                table: "main_product_instances",
                newName: "product_definition_type");

            migrationBuilder.RenameIndex(
                name: "IX_main_product_instances_main_product_id_main_product_type",
                table: "main_product_instances",
                newName: "IX_main_product_instances_main_product_id_product_definition_t~");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sub_product_instances_type",
                table: "sub_product_instances",
                sql: "product_definition_type = 'Sub'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_main_product_instances_type",
                table: "main_product_instances",
                sql: "product_definition_type = 'Main'");

            migrationBuilder.AddForeignKey(
                name: "FK_sub_product_instances_product_definitions_sub_product_id_pr~",
                table: "sub_product_instances",
                columns: new[] { "sub_product_id", "product_definition_type" },
                principalTable: "product_definitions",
                principalColumns: new[] { "id", "product_type" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sub_product_instances_product_definitions_sub_product_id_pr~",
                table: "sub_product_instances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sub_product_instances_type",
                table: "sub_product_instances");

            migrationBuilder.DropCheckConstraint(
                name: "ck_main_product_instances_type",
                table: "main_product_instances");

            migrationBuilder.RenameColumn(
                name: "product_definition_type",
                table: "sub_product_instances",
                newName: "sub_product_type");

            migrationBuilder.RenameIndex(
                name: "IX_sub_product_instances_sub_product_id_product_definition_type",
                table: "sub_product_instances",
                newName: "IX_sub_product_instances_sub_product_id_sub_product_type");

            migrationBuilder.RenameColumn(
                name: "product_definition_type",
                table: "main_product_instances",
                newName: "main_product_type");

            migrationBuilder.RenameIndex(
                name: "IX_main_product_instances_main_product_id_product_definition_t~",
                table: "main_product_instances",
                newName: "IX_main_product_instances_main_product_id_main_product_type");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sub_product_instances_type",
                table: "sub_product_instances",
                sql: "sub_product_type = 'Sub'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_main_product_instances_type",
                table: "main_product_instances",
                sql: "main_product_type = 'Main'");

            migrationBuilder.AddForeignKey(
                name: "FK_sub_product_instances_product_definitions_sub_product_id_su~",
                table: "sub_product_instances",
                columns: new[] { "sub_product_id", "sub_product_type" },
                principalTable: "product_definitions",
                principalColumns: new[] { "id", "product_type" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
