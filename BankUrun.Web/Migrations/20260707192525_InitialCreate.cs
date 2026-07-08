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
                name: "product_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_definitions", x => x.id);
                    table.UniqueConstraint("AK_product_definitions_id_product_type", x => new { x.id, x.product_type });
                    table.CheckConstraint("ck_product_definitions_code_format", "code ~ '^[A-Z0-9]{2}$'");
                    table.CheckConstraint("ck_product_definitions_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_product_definitions_type", "product_type in ('Main', 'Sub')");
                });

            migrationBuilder.CreateTable(
                name: "main_product_instances",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    main_product_id = table.Column<int>(type: "integer", nullable: false),
                    main_product_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    term = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_main_product_instances", x => x.id);
                    table.CheckConstraint("ck_main_product_instances_term_range", "term between 1 and 12");
                    table.CheckConstraint("ck_main_product_instances_type", "main_product_type = 'Main'");
                    table.CheckConstraint("ck_main_product_instances_year_range", "year between 2000 and 2100");
                    table.ForeignKey(
                        name: "FK_main_product_instances_product_definitions_main_product_id_~",
                        columns: x => new { x.main_product_id, x.main_product_type },
                        principalTable: "product_definitions",
                        principalColumns: new[] { "id", "product_type" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sub_product_instances",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    main_product_instance_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sub_product_instances", x => x.id);
                    table.CheckConstraint("ck_sub_product_instances_type", "sub_product_type = 'Sub'");
                    table.ForeignKey(
                        name: "FK_sub_product_instances_main_product_instances_main_product_i~",
                        column: x => x.main_product_instance_id,
                        principalTable: "main_product_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sub_product_instances_product_definitions_sub_product_id_su~",
                        columns: x => new { x.sub_product_id, x.sub_product_type },
                        principalTable: "product_definitions",
                        principalColumns: new[] { "id", "product_type" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_main_product_instances_main_product_id_main_product_type",
                table: "main_product_instances",
                columns: new[] { "main_product_id", "main_product_type" });

            migrationBuilder.CreateIndex(
                name: "IX_main_product_instances_main_product_id_year_term",
                table: "main_product_instances",
                columns: new[] { "main_product_id", "year", "term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_definitions_product_type_code",
                table: "product_definitions",
                columns: new[] { "product_type", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sub_product_instances_main_product_instance_id_sub_product_~",
                table: "sub_product_instances",
                columns: new[] { "main_product_instance_id", "sub_product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sub_product_instances_sub_product_id_sub_product_type",
                table: "sub_product_instances",
                columns: new[] { "sub_product_id", "sub_product_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "sub_product_instances");

            migrationBuilder.DropTable(
                name: "main_product_instances");

            migrationBuilder.DropTable(
                name: "product_definitions");
        }
    }
}
