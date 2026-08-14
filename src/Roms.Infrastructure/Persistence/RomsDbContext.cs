using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Roms.Domain;
using Roms.Infrastructure.Identity;

namespace Roms.Infrastructure.Persistence;

public sealed class RomsDbContext(DbContextOptions<RomsDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<InventoryCountRecord> InventoryCountRecords => Set<InventoryCountRecord>();
    public DbSet<InventoryLossRequest> InventoryLossRequests => Set<InventoryLossRequest>();
    public DbSet<StaffSchedule> StaffSchedules => Set<StaffSchedule>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<WorkflowSettings> WorkflowSettings => Set<WorkflowSettings>();
    public DbSet<OrderTimerExtension> OrderTimerExtensions => Set<OrderTimerExtension>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.ArchivedUserName).HasMaxLength(256);
            e.Property(x => x.ActiveSessionId).HasMaxLength(64);
            e.HasIndex(x => x.ActiveSessionId);
            e.Property(x => x.ActiveApplicationInstanceId).HasMaxLength(64);
        });

        builder.Entity<RestaurantTable>(e =>
        {
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Number).HasMaxLength(20);
        });
        builder.Entity<MenuCategory>(e => e.Property(x => x.Name).HasMaxLength(80));
        builder.Entity<MenuItem>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.Property(x => x.PreparationMinutes).HasDefaultValue(5);
            e.HasOne(x => x.Category).WithMany(x => x.Items).HasForeignKey(x => x.CategoryId);
        });
        builder.Entity<Order>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PaymentConfirmedBy).HasMaxLength(256);
            e.Property(x => x.CancellationReason).HasMaxLength(500);
            // The production relational provider enforces optimistic concurrency.
            // EF's in-memory test provider doesn't preserve manually incremented tokens reliably.
            if (Database.IsRelational()) e.Property(x => x.Version).IsConcurrencyToken();
            e.Ignore(x => x.Total);
            e.HasIndex(x => new { x.TableId, x.Status });
            e.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.StatusHistory).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany<OrderTimerExtension>().WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<WorkflowSettings>(e =>
        {
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => x.Id).IsUnique();
        });
        builder.Entity<OrderTimerExtension>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ActorId).HasMaxLength(256);
            e.HasIndex(x => new { x.OrderId, x.Kind, x.RequestedUtc });
        });
        builder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.MenuItemName).HasMaxLength(120);
            e.Property(x => x.UnitPrice).HasPrecision(12, 2);
            e.Property(x => x.Notes).HasMaxLength(500);
        });
        builder.Entity<OrderStatusHistory>(e =>
        {
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Reason).HasMaxLength(500);
        });
        builder.Entity<AuditEntry>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(80);
            e.Property(x => x.EntityType).HasMaxLength(80);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasIndex(x => x.OccurredUtc);
        });
        builder.Entity<IdempotencyRecord>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.Operation).HasMaxLength(80);
        });
        builder.Entity<InventoryItem>(e =>
        {
            e.Ignore(x => x.CurrentStock);
            e.Property(x => x.MinimumStock).HasPrecision(14, 3);
        });
        builder.Entity<StockMovement>(e =>
        {
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.QuantityDelta).HasPrecision(14, 3);
            e.Property(x => x.IdempotencyKey).HasMaxLength(150);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => new { x.InventoryItemId, x.OccurredUtc });
        });
        builder.Entity<InventoryCountRecord>(e =>
        {
            e.Property(x => x.LedgerQuantity).HasPrecision(14, 3);
            e.Property(x => x.CountedQuantity).HasPrecision(14, 3);
            e.Property(x => x.Variance).HasPrecision(14, 3);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.CountedBy).HasMaxLength(256);
            e.Property(x => x.IdempotencyKey).HasMaxLength(150);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => new { x.InventoryItemId, x.CountedUtc });
            e.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<InventoryLossRequest>(e =>
        {
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(14, 3);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ReportedBy).HasMaxLength(256);
            e.Property(x => x.ReviewedBy).HasMaxLength(256);
            e.Property(x => x.ReviewReason).HasMaxLength(500);
            e.Property(x => x.IdempotencyKey).HasMaxLength(150);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => new { x.Status, x.ReportedUtc });
            e.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StaffSchedule>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(255);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.UserId, x.ScheduledStartUtc });
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<AttendanceRecord>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(255);
            e.Property(x => x.AdjustedBy).HasMaxLength(256);
            e.Property(x => x.AdjustmentReason).HasMaxLength(500);
            e.Property(x => x.ClosureKind).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.ReviewedBy).HasMaxLength(256);
            e.Property(x => x.ReviewReason).HasMaxLength(500);
            if (Database.IsRelational()) e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => new { x.UserId, x.ClockInUtc });
            e.HasIndex(x => new { x.ClockOutUtc, x.RequiresManagerReview });
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StaffSchedule).WithMany().HasForeignKey(x => x.StaffScheduleId).OnDelete(DeleteBehavior.SetNull);
        });

        // Domain entities create GUID keys client-side. Marking them non-generated ensures
        // items attached through an existing aggregate are inserted rather than updated.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var key = entityType.FindPrimaryKey();
            if (key?.Properties.Count == 1 && key.Properties[0].ClrType == typeof(Guid))
                key.Properties[0].ValueGenerated = ValueGenerated.Never;
        }
    }
}
