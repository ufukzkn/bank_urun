using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class RestrictMainInstanceDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sub_product_instances_main_product_instances_main_product_i~",
                table: "sub_product_instances");

            migrationBuilder.AddForeignKey(
                name: "FK_sub_product_instances_main_product_instances_main_product_i~",
                table: "sub_product_instances",
                column: "main_product_instance_id",
                principalTable: "main_product_instances",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sub_product_instances_main_product_instances_main_product_i~",
                table: "sub_product_instances");

            migrationBuilder.AddForeignKey(
                name: "FK_sub_product_instances_main_product_instances_main_product_i~",
                table: "sub_product_instances",
                column: "main_product_instance_id",
                principalTable: "main_product_instances",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
