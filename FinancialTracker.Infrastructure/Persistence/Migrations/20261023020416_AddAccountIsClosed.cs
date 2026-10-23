using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountIsClosed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Accounts");
        }
    }
}
