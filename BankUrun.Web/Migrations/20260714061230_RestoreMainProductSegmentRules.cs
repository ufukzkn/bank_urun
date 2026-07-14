using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BankUrun.Web.Migrations
{
    /// <inheritdoc />
    public partial class RestoreMainProductSegmentRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "main_product_segment_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    main_product_parameter_id = table.Column<int>(type: "integer", nullable: false),
                    performance_segment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    target_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    size_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    scale_share = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    allocated_score = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    hgo_weight = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    development_weight = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    size_weight = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
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

            migrationBuilder.CreateIndex(
                name: "IX_main_product_segment_rules_main_product_parameter_id_perfor~",
                table: "main_product_segment_rules",
                columns: new[] { "main_product_parameter_id", "performance_segment" },
                unique: true);

            migrationBuilder.Sql(
                """
                insert into main_product_segment_rules
                    (main_product_parameter_id, performance_segment, sort_order, target_share,
                     size_share, scale_share, allocated_score, hgo_weight,
                     development_weight, size_weight, created_at, updated_at)
                select parameter.id,
                       distribution.segment,
                       distribution.sort_order,
                       distribution.target_share,
                       0.2000,
                       0.0000,
                       case
                           when distribution.sort_order = 5 then
                               parameter.criterion_score
                               - (round(parameter.criterion_score * 0.25, 2) * 2)
                               - (round(parameter.criterion_score * 0.20, 2) * 2)
                           else round(parameter.criterion_score * distribution.target_share, 2)
                       end,
                       0.7000,
                       0.1500,
                       0.1500,
                       now(),
                       now()
                from main_product_parameters parameter
                cross join (values
                    ('Kurumsal', 1, 0.2500::numeric),
                    ('Ticari', 2, 0.2500::numeric),
                    ('Kobi', 3, 0.2000::numeric),
                    ('Bireysel', 4, 0.2000::numeric),
                    ('Diger', 5, 0.1000::numeric)
                ) as distribution(segment, sort_order, target_share);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "main_product_segment_rules");
        }
    }
}
