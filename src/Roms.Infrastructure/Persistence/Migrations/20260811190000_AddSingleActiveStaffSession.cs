using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds server-owned session state for the one-active-device staff-login rule.
/// Existing staff are deliberately left without an active session and must log
/// in again after deployment.
/// </summary>
[DbContext(typeof(RomsDbContext))]
[Migration("20260811190000_AddSingleActiveStaffSession")]
public partial class AddSingleActiveStaffSession : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ActiveSessionId",
            table: "AspNetUsers",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SessionLastActivityUtc",
            table: "AspNetUsers",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_ActiveSessionId",
            table: "AspNetUsers",
            column: "ActiveSessionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_ActiveSessionId",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "ActiveSessionId",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "SessionLastActivityUtc",
            table: "AspNetUsers");
    }
}
