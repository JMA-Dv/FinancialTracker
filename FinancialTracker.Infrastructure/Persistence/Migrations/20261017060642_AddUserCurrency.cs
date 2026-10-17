using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "AspNetUsers",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "AspNetUsers");
        }
    }
}
