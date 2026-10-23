using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PayeeId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Payees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payees_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PayeeId",
                table: "Transactions",
                column: "PayeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payees_UserId",
                table: "Payees",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Payees_PayeeId",
                table: "Transactions",
                column: "PayeeId",
                principalTable: "Payees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Payees_PayeeId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Payees");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PayeeId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayeeId",
                table: "Transactions");
        }
    }
}
