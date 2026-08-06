using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roms.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RomsDbContext))]
[Migration("20260806143000_AddWorkflowTimers")]
public partial class AddWorkflowTimers : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<int>("OrderEntryTargetMinutes", "Orders", type: "int", nullable: true);
        m.AddColumn<DateTime>("OrderEntryStartedUtc", "Orders", type: "datetime(6)", nullable: true);
        m.AddColumn<DateTime>("OrderEntryDueUtc", "Orders", type: "datetime(6)", nullable: true);
        m.AddColumn<int>("KitchenAcceptanceTargetMinutes", "Orders", type: "int", nullable: true);
        m.AddColumn<DateTime>("KitchenAcceptanceStartedUtc", "Orders", type: "datetime(6)", nullable: true);
        m.AddColumn<DateTime>("KitchenAcceptanceDueUtc", "Orders", type: "datetime(6)", nullable: true);
        m.CreateTable("WorkflowSettings", t => new
        {
            Id = t.Column<Guid>(type: "char(36)", nullable: false),
            OrderEntryMinutes = t.Column<int>(type: "int", nullable: false, defaultValue: 15),
            KitchenAcceptanceMinutes = t.Column<int>(type: "int", nullable: false, defaultValue: 5),
            UpdatedUtc = t.Column<DateTime>(type: "datetime(6)", nullable: false),
            UpdatedBy = t.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
        }, constraints: t => t.PrimaryKey("PK_WorkflowSettings", x => x.Id));
        m.CreateTable("OrderTimerExtensions", t => new
        {
            Id = t.Column<Guid>(type: "char(36)", nullable: false),
            OrderId = t.Column<Guid>(type: "char(36)", nullable: false),
            Kind = t.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
            AdditionalMinutes = t.Column<int>(type: "int", nullable: false),
            ExtensionCount = t.Column<int>(type: "int", nullable: false),
            Reason = t.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
            ActorId = t.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
            RequestedUtc = t.Column<DateTime>(type: "datetime(6)", nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_OrderTimerExtensions", x => x.Id);
            t.ForeignKey("FK_OrderTimerExtensions_Orders_OrderId", x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.Cascade);
        });
        m.CreateIndex("IX_OrderTimerExtensions_OrderId_Kind_RequestedUtc", "OrderTimerExtensions", new[] { "OrderId", "Kind", "RequestedUtc" });
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("OrderTimerExtensions");
        m.DropTable("WorkflowSettings");
        m.DropColumn("OrderEntryTargetMinutes", "Orders");
        m.DropColumn("OrderEntryStartedUtc", "Orders");
        m.DropColumn("OrderEntryDueUtc", "Orders");
        m.DropColumn("KitchenAcceptanceTargetMinutes", "Orders");
        m.DropColumn("KitchenAcceptanceStartedUtc", "Orders");
        m.DropColumn("KitchenAcceptanceDueUtc", "Orders");
    }
}
