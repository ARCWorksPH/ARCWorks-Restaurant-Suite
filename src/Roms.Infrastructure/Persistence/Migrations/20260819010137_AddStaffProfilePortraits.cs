using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffProfilePortraits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDemoProfile",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfileLifecycle",
                table: "AspNetUsers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePortraitPath",
                table: "AspNetUsers",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileUpdatedUtc",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDemoProfile",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileLifecycle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePortraitPath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileUpdatedUtc",
                table: "AspNetUsers");
        }
    }
}
