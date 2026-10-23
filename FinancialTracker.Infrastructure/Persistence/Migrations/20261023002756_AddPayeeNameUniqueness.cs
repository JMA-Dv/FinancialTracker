using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayeeNameUniqueness : Migration
    {
        // Also named in PayeeStore, which turns a violation of it into a 409.
        internal const string PayeeNameIndex = "IX_Payees_UserId_LowerName";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw SQL because EF can't model an index on lower("Name"). It makes Payee names unique per User ignoring
            // case, even when two requests name the same new Payee at once.
            migrationBuilder.Sql(
                $"CREATE UNIQUE INDEX \"{PayeeNameIndex}\" ON \"Payees\" (\"UserId\", lower(\"Name\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DROP INDEX \"{PayeeNameIndex}\";");
        }
    }
}
