using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTimers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KitchenAcceptanceDueUtc",
                table: "Orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KitchenAcceptanceStartedUtc",
                table: "Orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KitchenAcceptanceTargetMinutes",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderEntryDueUtc",
                table: "Orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderEntryStartedUtc",
                table: "Orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderEntryTargetMinutes",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderTimerExtensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OrderId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Kind = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    AdditionalMinutes = table.Column<int>(type: "int", nullable: false),
                    ExtensionCount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    ActorId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    RequestedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTimerExtensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTimerExtensions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkflowSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OrderEntryMinutes = table.Column<int>(type: "int", nullable: false),
                    KitchenAcceptanceMinutes = table.Column<int>(type: "int", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSettings", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTimerExtensions_OrderId_Kind_RequestedUtc",
                table: "OrderTimerExtensions",
                columns: new[] { "OrderId", "Kind", "RequestedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSettings_Id",
                table: "WorkflowSettings",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderTimerExtensions");

            migrationBuilder.DropTable(
                name: "WorkflowSettings");

            migrationBuilder.DropColumn(
                name: "KitchenAcceptanceDueUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "KitchenAcceptanceStartedUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "KitchenAcceptanceTargetMinutes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderEntryDueUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderEntryStartedUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderEntryTargetMinutes",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
