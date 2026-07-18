using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioPerformanceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_main_product_monthly_metrics");

            migrationBuilder.DropTable(
                name: "branch_sub_product_monthly_metrics");

            migrationBuilder.DropTable(
                name: "main_product_segment_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_group_definitions_segment",
                table: "group_definitions");

            migrationBuilder.RenameColumn(
                name: "group_segment",
                table: "group_definitions",
                newName: "group_type");

            migrationBuilder.CreateTable(
                name: "branch_main_product_exclusions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    main_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_definition_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    effective_from_year = table.Column<int>(type: "integer", nullable: false),
                    effective_from_term = table.Column<int>(type: "integer", nullable: false),
                    effective_to_year = table.Column<int>(type: "integer", nullable: true),
                    effective_to_term = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_main_product_exclusions", x => x.id);
                    table.CheckConstraint("ck_branch_exclusions_from", "effective_from_year between 2000 and 2100 and effective_from_term in (1, 2)");
                    table.CheckConstraint("ck_branch_exclusions_to", "(effective_to_year is null and effective_to_term is null) or (effective_to_year between 2000 and 2100 and effective_to_term in (1, 2) and (effective_to_year * 2 + effective_to_term) >= (effective_from_year * 2 + effective_from_term))");
                    table.CheckConstraint("ck_branch_exclusions_type", "product_definition_type = 'Main'");
                    table.ForeignKey(
                        name: "FK_branch_main_product_exclusions_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_main_product_exclusions_product_definitions_main_pro~",
                        columns: x => new { x.main_product_id, x.product_definition_type },
                        principalTable: "product_definitions",
                        principalColumns: new[] { "id", "product_type" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_types", x => x.id);
                    table.CheckConstraint("ck_portfolio_types_code", "code ~ '^[A-Z0-9]{2}$'");
                    table.CheckConstraint("ck_portfolio_types_name", "length(btrim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "product_gamuts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_gamuts", x => x.id);
                    table.UniqueConstraint("AK_product_gamuts_id_group_id", x => new { x.id, x.group_id });
                    table.CheckConstraint("ck_product_gamuts_code", "code ~ '^[A-Z0-9]{2}$'");
                    table.CheckConstraint("ck_product_gamuts_name", "length(btrim(name)) > 0");
                    table.ForeignKey(
                        name: "FK_product_gamuts_group_definitions_group_id",
                        column: x => x.group_id,
                        principalTable: "group_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portfolios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    product_gamut_id = table.Column<int>(type: "integer", nullable: false),
                    portfolio_type_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolios", x => x.id);
                    table.UniqueConstraint("AK_portfolios_id_group_id", x => new { x.id, x.group_id });
                    table.CheckConstraint("ck_portfolios_code", "code ~ '^P[A-Z0-9]+-[A-Z0-9]{2}[0-9]{2}$'");
                    table.CheckConstraint("ck_portfolios_name", "length(btrim(name)) > 0");
                    table.ForeignKey(
                        name: "FK_portfolios_branches_branch_id_group_id",
                        columns: x => new { x.branch_id, x.group_id },
                        principalTable: "branches",
                        principalColumns: new[] { "id", "group_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_portfolios_portfolio_types_portfolio_type_id",
                        column: x => x.portfolio_type_id,
                        principalTable: "portfolio_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_portfolios_product_gamuts_product_gamut_id_group_id",
                        columns: x => new { x.product_gamut_id, x.group_id },
                        principalTable: "product_gamuts",
                        principalColumns: new[] { "id", "group_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_gamut_main_product_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_gamut_id = table.Column<int>(type: "integer", nullable: false),
                    main_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_definition_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    effective_from_year = table.Column<int>(type: "integer", nullable: false),
                    effective_from_term = table.Column<int>(type: "integer", nullable: false),
                    effective_to_year = table.Column<int>(type: "integer", nullable: true),
                    effective_to_term = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_gamut_main_product_assignments", x => x.id);
                    table.CheckConstraint("ck_gamut_assignments_from", "effective_from_year between 2000 and 2100 and effective_from_term in (1, 2)");
                    table.CheckConstraint("ck_gamut_assignments_to", "(effective_to_year is null and effective_to_term is null) or (effective_to_year between 2000 and 2100 and effective_to_term in (1, 2) and (effective_to_year * 2 + effective_to_term) >= (effective_from_year * 2 + effective_from_term))");
                    table.CheckConstraint("ck_gamut_assignments_type", "product_definition_type = 'Main'");
                    table.ForeignKey(
                        name: "FK_product_gamut_main_product_assignments_product_definitions_~",
                        columns: x => new { x.main_product_id, x.product_definition_type },
                        principalTable: "product_definitions",
                        principalColumns: new[] { "id", "product_type" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_gamut_main_product_assignments_product_gamuts_produ~",
                        column: x => x.product_gamut_id,
                        principalTable: "product_gamuts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_main_product_monthly_targets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    portfolio_id = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    main_product_parameter_id = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio_main_product_monthly_targets", x => x.id);
                    table.CheckConstraint("ck_portfolio_targets_month", "month between 1 and 12");
                    table.CheckConstraint("ck_portfolio_targets_value", "target_value >= 0");
                    table.ForeignKey(
                        name: "FK_portfolio_main_product_monthly_targets_main_product_paramet~",
                        columns: x => new { x.main_product_parameter_id, x.group_id },
                        principalTable: "main_product_parameters",
                        principalColumns: new[] { "id", "group_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_portfolio_main_product_monthly_targets_portfolios_portfolio~",
                        columns: x => new { x.portfolio_id, x.group_id },
                        principalTable: "portfolios",
                        principalColumns: new[] { "id", "group_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_sub_product_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.AddCheckConstraint(
                name: "ck_group_definitions_type",
                table: "group_definitions",
                sql: "group_type in ('Karma', 'Kurumsal', 'Ticari', 'Kobi', 'Diger')");

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_exclusions_branch_id_main_product_id_ef~",
                table: "branch_main_product_exclusions",
                columns: new[] { "branch_id", "main_product_id", "effective_from_year", "effective_from_term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_exclusions_main_product_id_product_defi~",
                table: "branch_main_product_exclusions",
                columns: new[] { "main_product_id", "product_definition_type" });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_main_product_monthly_targets_main_product_paramet~",
                table: "portfolio_main_product_monthly_targets",
                columns: new[] { "main_product_parameter_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_main_product_monthly_targets_portfolio_id_group_id",
                table: "portfolio_main_product_monthly_targets",
                columns: new[] { "portfolio_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_main_product_monthly_targets_portfolio_id_main_pr~",
                table: "portfolio_main_product_monthly_targets",
                columns: new[] { "portfolio_id", "main_product_parameter_id", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_sub_product_monthly_metrics_portfolio_id_sub_prod~",
                table: "portfolio_sub_product_monthly_metrics",
                columns: new[] { "portfolio_id", "sub_product_instance_id", "year", "term", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_sub_product_monthly_metrics_sub_product_instance_~",
                table: "portfolio_sub_product_monthly_metrics",
                column: "sub_product_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_types_code",
                table: "portfolio_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_branch_id_group_id",
                table: "portfolios",
                columns: new[] { "branch_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_branch_id_product_gamut_id",
                table: "portfolios",
                columns: new[] { "branch_id", "product_gamut_id" });

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_code",
                table: "portfolios",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_portfolio_type_id",
                table: "portfolios",
                column: "portfolio_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_portfolios_product_gamut_id_group_id",
                table: "portfolios",
                columns: new[] { "product_gamut_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_gamut_main_product_assignments_main_product_id_prod~",
                table: "product_gamut_main_product_assignments",
                columns: new[] { "main_product_id", "product_definition_type" });

            migrationBuilder.CreateIndex(
                name: "IX_product_gamut_main_product_assignments_product_gamut_id_mai~",
                table: "product_gamut_main_product_assignments",
                columns: new[] { "product_gamut_id", "main_product_id", "effective_from_year", "effective_from_term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_gamuts_group_id_code",
                table: "product_gamuts",
                columns: new[] { "group_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_main_product_exclusions");

            migrationBuilder.DropTable(
                name: "portfolio_main_product_monthly_targets");

            migrationBuilder.DropTable(
                name: "portfolio_sub_product_monthly_metrics");

            migrationBuilder.DropTable(
                name: "product_gamut_main_product_assignments");

            migrationBuilder.DropTable(
                name: "portfolios");

            migrationBuilder.DropTable(
                name: "portfolio_types");

            migrationBuilder.DropTable(
                name: "product_gamuts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_group_definitions_type",
                table: "group_definitions");

            migrationBuilder.RenameColumn(
                name: "group_type",
                table: "group_definitions",
                newName: "group_segment");

            migrationBuilder.CreateTable(
                name: "branch_main_product_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    main_product_parameter_id = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    actual_as_of_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    month = table.Column<int>(type: "integer", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_main_product_monthly_metrics", x => x.id);
                    table.CheckConstraint("ck_branch_main_product_monthly_metrics_actual_pair", "(actual_value is null and actual_as_of_date is null) or (actual_value is not null and actual_as_of_date is not null)");
                    table.CheckConstraint("ck_branch_main_product_monthly_metrics_month", "month between 1 and 12");
                    table.CheckConstraint("ck_branch_main_product_monthly_metrics_values", "target_value >= 0 and (actual_value is null or actual_value >= 0)");
                    table.ForeignKey(
                        name: "FK_branch_main_product_monthly_metrics_branches_branch_id_grou~",
                        columns: x => new { x.branch_id, x.group_id },
                        principalTable: "branches",
                        principalColumns: new[] { "id", "group_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_main_product_monthly_metrics_main_product_parameters~",
                        columns: x => new { x.main_product_parameter_id, x.group_id },
                        principalTable: "main_product_parameters",
                        principalColumns: new[] { "id", "group_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_sub_product_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_definition_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    actual_as_of_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    month = table.Column<int>(type: "integer", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    term = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    year = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "main_product_segment_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    main_product_parameter_id = table.Column<int>(type: "integer", nullable: false),
                    allocated_score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    development_weight = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    hgo_weight = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    performance_segment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scale_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    size_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    size_weight = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    target_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_main_product_segment_rules", x => x.id);
                    table.CheckConstraint("ck_main_product_segment_rules_ratios", "target_share between 0 and 1 and size_share between 0 and 1 and scale_share between 0 and 1 and hgo_weight between 0 and 1 and development_weight between 0 and 1 and size_weight between 0 and 1");
                    table.CheckConstraint("ck_main_product_segment_rules_score", "allocated_score >= 0");
                    table.CheckConstraint("ck_main_product_segment_rules_segment", "performance_segment in ('Kurumsal', 'Ticari', 'Kobi', 'Bireysel', 'Diger')");
                    table.CheckConstraint("ck_main_product_segment_rules_sort_order", "sort_order > 0");
                    table.ForeignKey(
                        name: "FK_main_product_segment_rules_main_product_parameters_main_pro~",
                        column: x => x.main_product_parameter_id,
                        principalTable: "main_product_parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_group_definitions_segment",
                table: "group_definitions",
                sql: "group_segment in ('Karma', 'Kurumsal', 'Ticari', 'Kobi', 'Diger')");

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_branch_id_group_id",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "branch_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_branch_id_main_product_~",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "branch_id", "main_product_parameter_id", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_main_product_parameter_~",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "main_product_parameter_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "IX_branch_sub_product_monthly_metrics_branch_id_sub_product_id~",
                table: "branch_sub_product_monthly_metrics",
                columns: new[] { "branch_id", "sub_product_id", "year", "term", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_sub_product_monthly_metrics_sub_product_id_product_d~",
                table: "branch_sub_product_monthly_metrics",
                columns: new[] { "sub_product_id", "product_definition_type" });

            migrationBuilder.CreateIndex(
                name: "IX_main_product_segment_rules_main_product_parameter_id_perfor~",
                table: "main_product_segment_rules",
                columns: new[] { "main_product_parameter_id", "performance_segment" },
                unique: true);
        }
    }
}
