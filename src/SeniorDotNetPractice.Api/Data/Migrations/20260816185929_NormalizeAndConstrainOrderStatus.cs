using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorDotNetPractice.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeAndConstrainOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    unknown_statuses text;
                BEGIN
                    SELECT string_agg(DISTINCT quote_literal("Status"), ', ')
                    INTO unknown_statuses
                    FROM "Orders"
                    WHERE lower("Status") NOT IN (
                        'pending',
                        'rejected',
                        'completed',
                        'cancelled'
                    );

                    IF unknown_statuses IS NOT NULL THEN
                        RAISE EXCEPTION
                            'Cannot normalize Orders.Status. Unknown status value(s): %',
                            unknown_statuses;
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "Status" = CASE lower("Status")
                    WHEN 'pending'   THEN 'Pending'
                    WHEN 'rejected'  THEN 'Rejected'
                    WHEN 'completed' THEN 'Completed'
                    WHEN 'cancelled' THEN 'Cancelled'
                END
                WHERE "Status" NOT IN (
                    'Pending',
                    'Rejected',
                    'Completed',
                    'Cancelled'
                );
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Status_Valid",
                table: "Orders",
                sql: "\"Status\" IN ('Pending', 'Rejected', 'Completed', 'Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Status_Valid",
                table: "Orders");
        }
    }
}
