using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRecipeFunctionality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropColumn(
                name: "CancellationInventoryDisposition",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InventoryOverriddenBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InventoryOverrideReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InventoryOverrideUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RemovalInventoryDisposition",
                table: "OrderItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationInventoryDisposition",
                table: "Orders",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InventoryOverriddenBy",
                table: "Orders",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InventoryOverrideReason",
                table: "Orders",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InventoryOverrideUtc",
                table: "Orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemovalInventoryDisposition",
                table: "OrderItems",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_InventoryItemId",
                table: "RecipeIngredients",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MenuItemId_InventoryItemId",
                table: "RecipeIngredients",
                columns: new[] { "MenuItemId", "InventoryItemId" },
                unique: true);
        }
    }
}
