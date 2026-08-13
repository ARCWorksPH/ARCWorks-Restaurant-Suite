using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RomsDbContext))]
[Migration("20260813090000_AddApplicationInstanceLease")]
public partial class AddApplicationInstanceLease : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ActiveApplicationInstanceId",
            table: "AspNetUsers",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ActiveApplicationInstanceId", table: "AspNetUsers");
    }
}
