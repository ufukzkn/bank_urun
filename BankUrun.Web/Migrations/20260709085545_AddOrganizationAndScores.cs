using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAndScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    branch_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                    table.CheckConstraint("ck_branches_code_not_blank", "length(btrim(branch_code)) > 0");
                    table.CheckConstraint("ck_branches_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_branches_type", "branch_type in ('Karma', 'Kurumsal', 'Ticari')");
                });

            migrationBuilder.CreateTable(
                name: "group_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_no = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_definitions", x => x.id);
                    table.CheckConstraint("ck_group_definitions_group_no_not_blank", "length(btrim(group_no)) > 0");
                    table.CheckConstraint("ck_group_definitions_name_not_blank", "length(btrim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "unit_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit_no = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_definitions", x => x.id);
                    table.CheckConstraint("ck_unit_definitions_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_unit_definitions_unit_no_not_blank", "length(btrim(unit_no)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "group_product_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_instance_id = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    hgo_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    development_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    size_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_product_scores", x => x.id);
                    table.CheckConstraint("ck_group_product_scores_development_share_range", "development_share between 0 and 1");
                    table.CheckConstraint("ck_group_product_scores_hgo_share_range", "hgo_share between 0 and 1");
                    table.CheckConstraint("ck_group_product_scores_score_non_negative", "score >= 0");
                    table.CheckConstraint("ck_group_product_scores_size_share_range", "size_share between 0 and 1");
                    table.CheckConstraint("ck_group_product_scores_target_non_negative", "target_value >= 0");
                    table.ForeignKey(
                        name: "FK_group_product_scores_group_definitions_group_id",
                        column: x => x.group_id,
                        principalTable: "group_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_product_scores_sub_product_instances_sub_product_inst~",
                        column: x => x.sub_product_instance_id,
                        principalTable: "sub_product_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_units",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    unit_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_units_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_units_unit_definitions_unit_id",
                        column: x => x.unit_id,
                        principalTable: "unit_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_units",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    unit_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_units_group_definitions_group_id",
                        column: x => x.group_id,
                        principalTable: "group_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_units_unit_definitions_unit_id",
                        column: x => x.unit_id,
                        principalTable: "unit_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_units_branch_id_unit_id",
                table: "branch_units",
                columns: new[] { "branch_id", "unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_units_unit_id",
                table: "branch_units",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_branches_branch_code",
                table: "branches",
                column: "branch_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_definitions_group_no",
                table: "group_definitions",
                column: "group_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_product_scores_group_id_sub_product_instance_id",
                table: "group_product_scores",
                columns: new[] { "group_id", "sub_product_instance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_product_scores_sub_product_instance_id",
                table: "group_product_scores",
                column: "sub_product_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_units_group_id_unit_id",
                table: "group_units",
                columns: new[] { "group_id", "unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_units_unit_id",
                table: "group_units",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_unit_definitions_unit_no",
                table: "unit_definitions",
                column: "unit_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_units");

            migrationBuilder.DropTable(
                name: "group_product_scores");

            migrationBuilder.DropTable(
                name: "group_units");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "group_definitions");

            migrationBuilder.DropTable(
                name: "unit_definitions");
        }
    }
}
