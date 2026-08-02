using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RomsDbContext))]
[Migration("20260730220000_AddInventoryCountRecords")]
public partial class AddInventoryCountRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InventoryCountRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                InventoryItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                LedgerQuantity = table.Column<decimal>(
                    type: "decimal(14,3)",
                    precision: 14,
                    scale: 3,
                    nullable: false),
                CountedQuantity = table.Column<decimal>(
                    type: "decimal(14,3)",
                    precision: 14,
                    scale: 3,
                    nullable: false),
                Variance = table.Column<decimal>(
                    type: "decimal(14,3)",
                    precision: 14,
                    scale: 3,
                    nullable: false),
                Reason = table.Column<string>(
                    type: "varchar(500)",
                    maxLength: 500,
                    nullable: false),
                CountedBy = table.Column<string>(
                    type: "varchar(256)",
                    maxLength: 256,
                    nullable: false),
                CountedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                IdempotencyKey = table.Column<string>(
                    type: "varchar(150)",
                    maxLength: 150,
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryCountRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_InventoryCountRecords_InventoryItems_InventoryItemId",
                    column: x => x.InventoryItemId,
                    principalTable: "InventoryItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InventoryCountRecords_IdempotencyKey",
            table: "InventoryCountRecords",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryCountRecords_InventoryItemId_CountedUtc",
            table: "InventoryCountRecords",
            columns: new[] { "InventoryItemId", "CountedUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InventoryCountRecords");
    }
}
