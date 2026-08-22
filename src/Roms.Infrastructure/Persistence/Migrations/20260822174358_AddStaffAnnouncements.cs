using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StaffAnnouncements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AudienceRole = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    PublishUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAnnouncements", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StaffAnnouncementReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    AnnouncementId = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    AnnouncementVersion = table.Column<int>(type: "int", nullable: false),
                    AcknowledgedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DismissedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAnnouncementReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAnnouncementReceipts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffAnnouncementReceipts_StaffAnnouncements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "StaffAnnouncements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAnnouncementReceipts_AnnouncementId_UserId_Announcement~",
                table: "StaffAnnouncementReceipts",
                columns: new[] { "AnnouncementId", "UserId", "AnnouncementVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffAnnouncementReceipts_UserId",
                table: "StaffAnnouncementReceipts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAnnouncements_IsActive_PublishUtc_ExpiresUtc",
                table: "StaffAnnouncements",
                columns: new[] { "IsActive", "PublishUtc", "ExpiresUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffAnnouncementReceipts");

            migrationBuilder.DropTable(
                name: "StaffAnnouncements");
        }
    }
}
