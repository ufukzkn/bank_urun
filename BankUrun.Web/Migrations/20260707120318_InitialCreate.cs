using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    actor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    year = table.Column<int>(type: "integer", nullable: false),
                    term = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periods", x => x.id);
                    table.CheckConstraint("ck_periods_term_range", "term between 1 and 12");
                    table.CheckConstraint("ck_periods_year_range", "year between 2000 and 2100");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_code_format", "code ~ '^[A-Z0-9]{2}$'");
                    table.CheckConstraint("ck_products_name_not_blank", "length(btrim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "main_product_periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    main_product_id = table.Column<int>(type: "integer", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_main_product_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_main_product_periods_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_main_product_periods_products_main_product_id",
                        column: x => x.main_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sub_product_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    main_product_period_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sub_product_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_sub_product_assignments_main_product_periods_main_product_p~",
                        column: x => x.main_product_period_id,
                        principalTable: "main_product_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sub_product_assignments_products_sub_product_id",
                        column: x => x.sub_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_main_product_periods_main_product_id_period_id",
                table: "main_product_periods",
                columns: new[] { "main_product_id", "period_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_main_product_periods_period_id",
                table: "main_product_periods",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "IX_periods_year_term",
                table: "periods",
                columns: new[] { "year", "term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_type_code",
                table: "products",
                columns: new[] { "type", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sub_product_assignments_main_product_period_id_sub_product_~",
                table: "sub_product_assignments",
                columns: new[] { "main_product_period_id", "sub_product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sub_product_assignments_sub_product_id",
                table: "sub_product_assignments",
                column: "sub_product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "sub_product_assignments");

            migrationBuilder.DropTable(
                name: "main_product_periods");

            migrationBuilder.DropTable(
                name: "periods");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
