using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class RefactorBranchPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_branches_type",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "branch_type",
                table: "branches");

            migrationBuilder.AddColumn<bool>(
                name: "branch_performance_enabled",
                table: "group_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "group_segment",
                table: "group_definitions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Karma");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "group_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "miy_performance_enabled",
                table: "group_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "scale_enabled",
                table: "group_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "group_id",
                table: "branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_group_definitions_segment",
                table: "group_definitions",
                sql: "group_segment in ('Karma', 'Kurumsal', 'Ticari', 'Kobi', 'Diger')");

            migrationBuilder.Sql("""
                insert into group_definitions (
                    group_no,
                    name,
                    group_segment,
                    is_active,
                    branch_performance_enabled,
                    miy_performance_enabled,
                    scale_enabled,
                    created_at,
                    updated_at
                )
                select
                    'GENEL',
                    'Genel Grup',
                    'Karma',
                    true,
                    true,
                    true,
                    true,
                    now(),
                    now()
                where exists (select 1 from branches)
                  and not exists (select 1 from group_definitions);
                """);

            migrationBuilder.Sql("""
                update branches branch
                set group_id = mapping.group_id
                from (
                    select branch_unit.branch_id, min(group_unit.group_id) as group_id
                    from branch_units branch_unit
                    join group_units group_unit
                      on group_unit.unit_id = branch_unit.unit_id
                    group by branch_unit.branch_id
                ) mapping
                where branch.id = mapping.branch_id;
                """);

            migrationBuilder.Sql("""
                update branches
                set group_id = (
                    select id
                    from group_definitions
                    order by group_no, id
                    limit 1
                )
                where group_id is null;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "group_id",
                table: "branches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "branch_product_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_branch_product_scores", x => x.id);
                    table.CheckConstraint("ck_branch_product_scores_development_share_range", "development_share between 0 and 1");
                    table.CheckConstraint("ck_branch_product_scores_hgo_share_range", "hgo_share between 0 and 1");
                    table.CheckConstraint("ck_branch_product_scores_score_non_negative", "score >= 0");
                    table.CheckConstraint("ck_branch_product_scores_size_share_range", "size_share between 0 and 1");
                    table.CheckConstraint("ck_branch_product_scores_target_non_negative", "target_value >= 0");
                    table.ForeignKey(
                        name: "FK_branch_product_scores_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_product_scores_sub_product_instances_sub_product_ins~",
                        column: x => x.sub_product_instance_id,
                        principalTable: "sub_product_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                insert into branch_product_scores (
                    branch_id,
                    sub_product_instance_id,
                    score,
                    target_value,
                    hgo_share,
                    development_share,
                    size_share,
                    created_at,
                    updated_at
                )
                select
                    first_branch.id,
                    group_score.sub_product_instance_id,
                    group_score.score,
                    group_score.target_value,
                    group_score.hgo_share,
                    group_score.development_share,
                    group_score.size_share,
                    group_score.created_at,
                    group_score.updated_at
                from group_product_scores group_score
                join lateral (
                    select branch.id
                    from branches branch
                    where branch.group_id = group_score.group_id
                    order by branch.branch_code, branch.id
                    limit 1
                ) first_branch on true
                on conflict do nothing;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_branches_group_id",
                table: "branches",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_product_scores_branch_id_sub_product_instance_id",
                table: "branch_product_scores",
                columns: new[] { "branch_id", "sub_product_instance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_product_scores_sub_product_instance_id",
                table: "branch_product_scores",
                column: "sub_product_instance_id");

            migrationBuilder.AddForeignKey(
                name: "FK_branches_group_definitions_group_id",
                table: "branches",
                column: "group_id",
                principalTable: "group_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "branch_units");

            migrationBuilder.DropTable(
                name: "group_product_scores");

            migrationBuilder.DropTable(
                name: "group_units");

            migrationBuilder.DropTable(
                name: "unit_definitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branches_group_definitions_group_id",
                table: "branches");

            migrationBuilder.DropTable(
                name: "branch_product_scores");

            migrationBuilder.DropCheckConstraint(
                name: "ck_group_definitions_segment",
                table: "group_definitions");

            migrationBuilder.DropIndex(
                name: "IX_branches_group_id",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "branch_performance_enabled",
                table: "group_definitions");

            migrationBuilder.DropColumn(
                name: "group_segment",
                table: "group_definitions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "group_definitions");

            migrationBuilder.DropColumn(
                name: "miy_performance_enabled",
                table: "group_definitions");

            migrationBuilder.DropColumn(
                name: "scale_enabled",
                table: "group_definitions");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "branches");

            migrationBuilder.AddColumn<string>(
                name: "branch_type",
                table: "branches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Karma");

            migrationBuilder.CreateTable(
                name: "group_product_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_instance_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    development_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    hgo_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    size_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                name: "unit_definitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    unit_no = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_definitions", x => x.id);
                    table.CheckConstraint("ck_unit_definitions_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_unit_definitions_unit_no_not_blank", "length(btrim(unit_no)) > 0");
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

            migrationBuilder.AddCheckConstraint(
                name: "ck_branches_type",
                table: "branches",
                sql: "branch_type in ('Karma', 'Kurumsal', 'Ticari')");

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
    }
}
