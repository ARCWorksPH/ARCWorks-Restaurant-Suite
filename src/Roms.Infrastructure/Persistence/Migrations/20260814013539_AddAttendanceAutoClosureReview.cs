using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceAutoClosureReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClosureKind",
                table: "AttendanceRecords",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresManagerReview",
                table: "AttendanceRecords",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "AttendanceRecords",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "AttendanceRecords",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedUtc",
                table: "AttendanceRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "AttendanceRecords",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ClockOutUtc_RequiresManagerReview",
                table: "AttendanceRecords",
                columns: new[] { "ClockOutUtc", "RequiresManagerReview" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ClockOutUtc_RequiresManagerReview",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ClosureKind",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "RequiresManagerReview",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ReviewedUtc",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AttendanceRecords");
        }
    }
}
