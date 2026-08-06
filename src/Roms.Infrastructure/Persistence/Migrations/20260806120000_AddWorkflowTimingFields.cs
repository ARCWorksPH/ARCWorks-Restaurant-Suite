using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RomsDbContext))]
[Migration("20260806120000_AddWorkflowTimingFields")]
public partial class AddWorkflowTimingFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PreparationMinutes",
            table: "MenuItems",
            type: "int",
            nullable: false,
            defaultValue: 5);

        migrationBuilder.AddColumn<int>(
            name: "ResubmissionCount",
            table: "Orders",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "PreparationTargetMinutes",
            table: "Orders",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PreparationTargetDueUtc",
            table: "Orders",
            type: "datetime(6)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PreparationMinutes", table: "MenuItems");
        migrationBuilder.DropColumn(name: "ResubmissionCount", table: "Orders");
        migrationBuilder.DropColumn(name: "PreparationTargetMinutes", table: "Orders");
        migrationBuilder.DropColumn(name: "PreparationTargetDueUtc", table: "Orders");
    }
}
