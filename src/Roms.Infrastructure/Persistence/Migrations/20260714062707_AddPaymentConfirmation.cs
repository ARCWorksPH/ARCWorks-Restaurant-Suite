using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentConfirmedBy",
                table: "Orders",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentConfirmedUtc",
                table: "Orders",
                type: "datetime(6)",
                nullable: true);

            // Preserve historical reporting. Orders completed before this workflow existed
            // cannot be distinguished from already-settled orders, so treat them as confirmed.
            migrationBuilder.Sql("UPDATE `Orders` SET `PaymentConfirmedUtc` = `CompletedUtc`, `PaymentConfirmedBy` = 'migration' WHERE `Status` = 'Completed' AND `CompletedUtc` IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentConfirmedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentConfirmedUtc",
                table: "Orders");
        }
    }
}
