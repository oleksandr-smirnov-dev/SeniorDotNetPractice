using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorDotNetPractice.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertOrderStatusToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "Status" = 'Pending'
                WHERE "Status" = 'pending';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
