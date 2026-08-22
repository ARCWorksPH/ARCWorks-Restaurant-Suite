using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    LeaveType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    PrivateMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    SubmittedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DecidedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    DecisionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DecisionReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CancelledUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeaveRequestDates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "char(36)", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestDates_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDates_LeaveRequestId_RequestedDate",
                table: "LeaveRequestDates",
                columns: new[] { "LeaveRequestId", "RequestedDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDates_RequestedDate",
                table: "LeaveRequestDates",
                column: "RequestedDate");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status_SubmittedUtc",
                table: "LeaveRequests",
                columns: new[] { "Status", "SubmittedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_UserId_Status_SubmittedUtc",
                table: "LeaveRequests",
                columns: new[] { "UserId", "Status", "SubmittedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveRequestDates");

            migrationBuilder.DropTable(
                name: "LeaveRequests");
        }
    }
}
