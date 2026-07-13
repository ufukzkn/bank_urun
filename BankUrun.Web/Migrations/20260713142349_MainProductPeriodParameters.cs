using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class MainProductPeriodParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_product_metric_results");

            migrationBuilder.DropTable(
                name: "branch_product_scores");

            migrationBuilder.DropTable(
                name: "group_product_segment_rules");

            migrationBuilder.DropTable(
                name: "group_product_parameters");

            migrationBuilder.CreateTable(
                name: "main_product_parameters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    main_product_instance_id = table.Column<int>(type: "integer", nullable: false),
                    calculation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criterion_score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_main_product_parameters", x => x.id);
                    table.CheckConstraint("ck_main_product_parameters_calculation_type", "calculation_type in ('Average', 'Cumulative')");
                    table.CheckConstraint("ck_main_product_parameters_criterion_score", "criterion_score >= 0");
                    table.ForeignKey(
                        name: "FK_main_product_parameters_main_product_instances_main_product~",
                        column: x => x.main_product_instance_id,
                        principalTable: "main_product_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_main_product_monthly_metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    main_product_parameter_id = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    actual_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    actual_as_of_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_main_product_monthly_metrics", x => x.id);
                    table.CheckConstraint("ck_branch_main_product_monthly_metrics_actual_pair", "(actual_value is null and actual_as_of_date is null) or (actual_value is not null and actual_as_of_date is not null)");
                    table.CheckConstraint("ck_branch_main_product_monthly_metrics_month", "month between 1 and 12");
                    table.CheckConstraint("ck_branch_main_product_monthly_metrics_values", "target_value >= 0 and (actual_value is null or actual_value >= 0)");
                    table.ForeignKey(
                        name: "FK_branch_main_product_monthly_metrics_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_main_product_monthly_metrics_main_product_parameters~",
                        column: x => x.main_product_parameter_id,
                        principalTable: "main_product_parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_branch_id_main_product_~",
                table: "branch_main_product_monthly_metrics",
                columns: new[] { "branch_id", "main_product_parameter_id", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_main_product_monthly_metrics_main_product_parameter_~",
                table: "branch_main_product_monthly_metrics",
                column: "main_product_parameter_id");

            migrationBuilder.CreateIndex(
                name: "IX_main_product_parameters_main_product_instance_id",
                table: "main_product_parameters",
                column: "main_product_instance_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_main_product_monthly_metrics");

            migrationBuilder.DropTable(
                name: "main_product_parameters");

            migrationBuilder.CreateTable(
                name: "branch_product_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "group_product_parameters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    sub_product_instance_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    total_score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_product_parameters", x => x.id);
                    table.CheckConstraint("ck_group_product_parameters_total_score", "total_score >= 0");
                    table.ForeignKey(
                        name: "FK_group_product_parameters_group_definitions_group_id",
                        column: x => x.group_id,
                        principalTable: "group_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_group_product_parameters_sub_product_instances_sub_product_~",
                        column: x => x.sub_product_instance_id,
                        principalTable: "sub_product_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_product_segment_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_product_parameter_id = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_group_product_segment_rules", x => x.id);
                    table.CheckConstraint("ck_group_product_segment_rules_ratios", "target_share between 0 and 1 and size_share between 0 and 1 and scale_share between 0 and 1 and hgo_weight between 0 and 1 and development_weight between 0 and 1 and size_weight between 0 and 1");
                    table.CheckConstraint("ck_group_product_segment_rules_scores", "allocated_score >= 0");
                    table.CheckConstraint("ck_group_product_segment_rules_segment", "performance_segment in ('Kurumsal', 'Ticari', 'Kobi', 'Bireysel', 'Diger')");
                    table.ForeignKey(
                        name: "FK_group_product_segment_rules_group_product_parameters_group_~",
                        column: x => x.group_product_parameter_id,
                        principalTable: "group_product_parameters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_product_metric_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    group_product_segment_rule_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    development_achievement = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    hgo_achievement = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    size_achievement = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_product_metric_results", x => x.id);
                    table.CheckConstraint("ck_branch_product_metric_results_non_negative", "hgo_achievement >= 0 and development_achievement >= 0 and size_achievement >= 0");
                    table.ForeignKey(
                        name: "FK_branch_product_metric_results_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_product_metric_results_group_product_segment_rules_g~",
                        column: x => x.group_product_segment_rule_id,
                        principalTable: "group_product_segment_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_product_metric_results_branch_id_group_product_segme~",
                table: "branch_product_metric_results",
                columns: new[] { "branch_id", "group_product_segment_rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_product_metric_results_group_product_segment_rule_id",
                table: "branch_product_metric_results",
                column: "group_product_segment_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_product_scores_branch_id_sub_product_instance_id",
                table: "branch_product_scores",
                columns: new[] { "branch_id", "sub_product_instance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_product_scores_sub_product_instance_id",
                table: "branch_product_scores",
                column: "sub_product_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_product_parameters_group_id_sub_product_instance_id",
                table: "group_product_parameters",
                columns: new[] { "group_id", "sub_product_instance_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_product_parameters_sub_product_instance_id",
                table: "group_product_parameters",
                column: "sub_product_instance_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_product_segment_rules_group_product_parameter_id_perf~",
                table: "group_product_segment_rules",
                columns: new[] { "group_product_parameter_id", "performance_segment" },
                unique: true);
        }
    }
}
