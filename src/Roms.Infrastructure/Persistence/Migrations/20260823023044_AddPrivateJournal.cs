using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Ciphertext = table.Column<byte[]>(type: "longblob", nullable: false),
                    Nonce = table.Column<byte[]>(type: "varbinary(12)", maxLength: 12, nullable: false),
                    CryptoVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "JournalKeyEnvelopes",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    PassphraseSalt = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    PassphraseNonce = table.Column<byte[]>(type: "varbinary(12)", maxLength: 12, nullable: false),
                    PassphraseWrappedKey = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: false),
                    RecoveryNonce = table.Column<byte[]>(type: "varbinary(12)", maxLength: 12, nullable: false),
                    RecoveryWrappedKey = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: false),
                    KdfIterations = table.Column<int>(type: "int", nullable: false),
                    CryptoVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalKeyEnvelopes", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_JournalKeyEnvelopes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_UserId_DeletedUtc_UpdatedUtc",
                table: "JournalEntries",
                columns: new[] { "UserId", "DeletedUtc", "UpdatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "JournalKeyEnvelopes");
        }
    }
}
