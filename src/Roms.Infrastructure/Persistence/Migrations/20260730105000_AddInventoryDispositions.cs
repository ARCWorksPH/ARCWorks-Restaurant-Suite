using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RomsDbContext))]
[Migration("20260730105000_AddInventoryDispositions")]
public partial class AddInventoryDispositions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CancellationInventoryDisposition",
            table: "Orders",
            type: "varchar(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RemovalInventoryDisposition",
            table: "OrderItems",
            type: "varchar(40)",
            maxLength: 40,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CancellationInventoryDisposition",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "RemovalInventoryDisposition",
            table: "OrderItems");
    }
}
