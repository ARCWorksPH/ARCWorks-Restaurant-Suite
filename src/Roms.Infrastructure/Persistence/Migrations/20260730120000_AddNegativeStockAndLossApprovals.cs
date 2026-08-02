using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RomsDbContext))]
[Migration("20260730120000_AddNegativeStockAndLossApprovals")]
public partial class AddNegativeStockAndLossApprovals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "InventoryOverrideReason",
            table: "Orders",
            type: "varchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InventoryOverriddenBy",
            table: "Orders",
            type: "varchar(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "InventoryOverrideUtc",
            table: "Orders",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "InventoryLossRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                InventoryItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                Type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                ReportedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                ReportedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                ReviewedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                ReviewedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ReviewReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                IdempotencyKey = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryLossRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_InventoryLossRequests_InventoryItems_InventoryItemId",
                    column: x => x.InventoryItemId,
                    principalTable: "InventoryItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InventoryLossRequests_IdempotencyKey",
            table: "InventoryLossRequests",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryLossRequests_InventoryItemId",
            table: "InventoryLossRequests",
            column: "InventoryItemId");

        migrationBuilder.CreateIndex(
            name: "IX_InventoryLossRequests_Status_ReportedUtc",
            table: "InventoryLossRequests",
            columns: new[] { "Status", "ReportedUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InventoryLossRequests");

        migrationBuilder.DropColumn(name: "InventoryOverrideReason", table: "Orders");
        migrationBuilder.DropColumn(name: "InventoryOverriddenBy", table: "Orders");
        migrationBuilder.DropColumn(name: "InventoryOverrideUtc", table: "Orders");
    }
}
