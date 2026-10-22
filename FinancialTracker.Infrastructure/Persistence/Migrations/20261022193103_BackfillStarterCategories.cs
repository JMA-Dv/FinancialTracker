using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillStarterCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users who signed up before Categories existed get the same starter set new Users do.
            // Ids only need to be unique: nothing orders Categories by them.
            migrationBuilder.Sql(
                """
                INSERT INTO "Categories" ("Id", "UserId", "Name", "Kind", "ParentId", "IsArchived")
                SELECT gen_random_uuid(), u."Id", starter."Name", starter."Kind", NULL, FALSE
                FROM "AspNetUsers" u
                CROSS JOIN (VALUES
                    ('Dining', 'Expense'), ('Groceries', 'Expense'), ('Shopping', 'Expense'),
                    ('Entertainment', 'Expense'), ('Bills', 'Expense'), ('Gifts', 'Expense'),
                    ('Work', 'Expense'), ('Income', 'Income'), ('Health', 'Expense'),
                    ('Investments', 'Expense'), ('Lend', 'Expense')) AS starter ("Name", "Kind")
                WHERE NOT EXISTS (SELECT 1 FROM "Categories" c WHERE c."UserId" = u."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
