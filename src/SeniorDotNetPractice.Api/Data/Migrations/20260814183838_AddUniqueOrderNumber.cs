using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeniorDotNetPractice.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueOrderNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders");
        }
    }
}
