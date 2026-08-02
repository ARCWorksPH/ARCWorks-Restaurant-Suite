using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class ReportService(IDbContextFactory<RomsDbContext> factory) : IReportService
{
    public async Task<DashboardReport> GetDashboardAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        if (toUtc <= fromUtc) throw new DomainException("Report end time must be after the start time.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Items)
            .Where(x => x.Status == OrderStatus.Completed && x.PaymentConfirmedUtc >= fromUtc && x.PaymentConfirmedUtc < toUtc).ToListAsync(ct);
        var value = orders.Sum(x => x.Total);
        var sellers = orders.SelectMany(x => x.Items).Where(x => !x.IsRemoved)
            .GroupBy(x => x.MenuItemName).Select(g => new BestSeller(g.Key, g.Sum(x => x.Quantity), g.Sum(x => x.Quantity * x.UnitPrice)))
            .OrderByDescending(x => x.Quantity).ThenBy(x => x.Name).Take(10).ToList();
        return new DashboardReport(value, orders.Count, orders.Count == 0 ? 0 : value / orders.Count, sellers);
    }
}

public sealed class InventoryService(
    IDbContextFactory<RomsDbContext> factory,
    IClock clock,
    ILogger<InventoryService>? logger = null) : IInventoryService
{
    private static readonly HashSet<string> SupportedUnits =
        new(StringComparer.OrdinalIgnoreCase) { "piece", "g", "ml" };

    public async Task<IReadOnlyList<InventoryBalance>> GetBalancesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryItems.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new InventoryBalance(x.Id, x.Name, x.Unit, x.Movements.Sum(m => m.QuantityDelta), x.MinimumStock,
                x.Movements.Sum(m => m.QuantityDelta) <= x.MinimumStock)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetItemsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryItems.AsNoTracking().Include(x => x.Movements).OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task SaveItemAsync(InventoryItem item, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.Name)) throw new DomainException("Inventory item name is required.");
        if (string.IsNullOrWhiteSpace(item.Unit)) throw new DomainException("Inventory unit is required.");
        if (item.Name.Trim().Length > 120) throw new DomainException("Inventory item name cannot exceed 120 characters.");
        if (item.Unit.Trim().Length > 20) throw new DomainException("Inventory unit cannot exceed 20 characters.");
        if (item.MinimumStock < 0) throw new DomainException("Minimum stock cannot be negative.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, actorId, ct);
        var current = await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == item.Id, ct);
        if (current is null) db.InventoryItems.Add(item);
        else { current.Name = item.Name.Trim(); current.Unit = item.Unit.Trim(); current.MinimumStock = item.MinimumStock; current.IsActive = item.IsActive; }
        db.AuditEntries.Add(new AuditEntry { ActorId = actorId, Action = "SaveInventoryItem", EntityType = nameof(InventoryItem), EntityId = item.Id.ToString(), OccurredUtc = clock.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    public async Task<InventoryReadinessReport> EvaluateReadinessAsync(
        string adminId,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureAdminAsync(db, adminId, ct);

        var activeItems = await db.InventoryItems.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Unit,
                Balance = x.Movements.Sum(movement => movement.QuantityDelta)
            })
            .ToListAsync(ct);
        var countedItemIds = await db.InventoryCountRecords.AsNoTracking()
            .Select(x => x.InventoryItemId)
            .Distinct()
            .ToListAsync(ct);
        var pendingLosses = await db.InventoryLossRequests.AsNoTracking()
            .CountAsync(x => x.Status == InventoryLossStatus.Pending, ct);

        var duplicateNames = activeItems
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(x => x)
            .ToList();
        var unsupportedUnits = activeItems
            .Where(x => !SupportedUnits.Contains(x.Unit.Trim()))
            .Select(x => $"{x.Name} ({x.Unit})")
            .OrderBy(x => x)
            .ToList();
        var uncountedItems = activeItems
            .Where(x => !countedItemIds.Contains(x.Id))
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToList();
        var negativeBalances = activeItems
            .Where(x => x.Balance < 0)
            .Select(x => $"{x.Name} ({x.Balance:0.###} {x.Unit})")
            .OrderBy(x => x)
            .ToList();
        var checks = new List<InventoryReadinessCheck>
        {
            Check("INV-001", "Active inventory catalog exists", activeItems.Count > 0,
                activeItems.Count > 0
                    ? $"{activeItems.Count} active inventory item(s) found."
                    : "No active inventory items are configured."),
            Check("INV-002", "Active inventory names are unique", duplicateNames.Count == 0,
                duplicateNames.Count == 0
                    ? "No duplicate active item names found."
                    : $"Duplicate names: {ListEvidence(duplicateNames)}."),
            Check("INV-003", "Inventory units use the supported canonical set", unsupportedUnits.Count == 0,
                unsupportedUnits.Count == 0
                    ? "All active items use piece, g, or ml."
                    : $"Unsupported units: {ListEvidence(unsupportedUnits)}."),
            Check("INV-004", "Every active item has a witnessed opening count", uncountedItems.Count == 0,
                uncountedItems.Count == 0
                    ? "Every active item has at least one durable physical-count record."
                    : $"Missing physical counts: {ListEvidence(uncountedItems)}."),
            Check("INV-005", "Current inventory balances are non-negative", negativeBalances.Count == 0,
                negativeBalances.Count == 0
                    ? "No active item has a negative ledger balance."
                    : $"Negative balances: {ListEvidence(negativeBalances)}."),
            Check("LOSS-001", "No waste or spoilage reports await review", pendingLosses == 0,
                pendingLosses == 0
                    ? "No pending inventory loss requests."
                    : $"{pendingLosses} loss request(s) still require an administrator decision."),
            Manual("MAN-001", "Restaurant data-owner sign-off",
                "A restaurant representative must confirm item names, canonical units, opening counts, and minimum levels."),
            Manual("MAN-002", "Independent external audit acceptance",
                "The external reviewer must accept the evidence and record any required remediation."),
            Manual("MAN-003", "Supervised multi-device pilot and rollback approval",
                "Run the waiter-kitchen-cashier pilot against a backed-up disposable environment before production use.")
        };

        return new InventoryReadinessReport(
            clock.UtcNow,
            activeItems.Count,
            checks);
    }

    public async Task ReceiveAsync(
        Guid itemId,
        decimal quantity,
        string deliveryReference,
        string? note,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (quantity <= 0) throw new DomainException("Received quantity must be greater than zero.");
        if (quantity > 99_999_999_999.999m) throw new DomainException("Received quantity is too large.");
        if (string.IsNullOrWhiteSpace(deliveryReference)) throw new DomainException("A delivery reference is required.");
        if (deliveryReference.Trim().Length > 120) throw new DomainException("Delivery reference cannot exceed 120 characters.");
        if ((note?.Trim().Length ?? 0) > 350) throw new DomainException("Delivery note cannot exceed 350 characters.");
        ValidateKey(idempotencyKey, "receipt");

        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await EnsureAdminAsync(db, actorId, ct);
            if (await db.StockMovements.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct)) return;
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == itemId && x.IsActive, ct)
                ?? throw new DomainException("Inventory item not found.");
            var reference = deliveryReference.Trim();
            var cleanNote = note?.Trim();
            var reason = string.IsNullOrWhiteSpace(cleanNote)
                ? $"Delivery {reference}"
                : $"Delivery {reference}: {cleanNote}";
            db.StockMovements.Add(new StockMovement
            {
                InventoryItemId = itemId,
                QuantityDelta = quantity,
                Type = StockMovementType.Receipt,
                Reason = reason,
                ActorId = actorId,
                IdempotencyKey = idempotencyKey,
                OccurredUtc = clock.UtcNow
            });
            db.AuditEntries.Add(Audit(actorId, "ReceiveInventory", nameof(InventoryItem), item.Id,
                reference, JsonSerializer.Serialize(new { Quantity = quantity, item.Unit, Note = cleanNote })));
            await db.SaveChangesAsync(ct);
        }
        catch (Exception exception) when (IsTransientTransactionConflict(exception))
        {
            logger?.LogWarning(exception, "Inventory receipt for item {InventoryItemId} hit a transient database conflict.", itemId);
            throw RetryableInventoryConflict();
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            await using var verificationDb = await factory.CreateDbContextAsync(ct);
            if (await verificationDb.StockMovements.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct))
                return;
            throw;
        }
    }

    public async Task<Guid> ReconcileCountAsync(
        Guid itemId,
        decimal countedQuantity,
        string reason,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        ValidateKey(idempotencyKey, "physical-count");
        try
        {
            await using var strategyContext = await factory.CreateDbContextAsync(ct);
            var strategy = strategyContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var db = await factory.CreateDbContextAsync(ct);
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                await EnsureAdminAsync(db, actorId, ct);
                var existing = await db.InventoryCountRecords.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
                if (existing is not null) return existing.Id;
                var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == itemId && x.IsActive, ct)
                    ?? throw new DomainException("Inventory item not found.");
                var ledgerQuantity = await db.StockMovements
                    .Where(x => x.InventoryItemId == itemId)
                    .SumAsync(x => x.QuantityDelta, ct);
                var count = InventoryCountRecord.Record(
                    itemId, ledgerQuantity, countedQuantity, reason, actorId, idempotencyKey, clock.UtcNow);
                db.InventoryCountRecords.Add(count);
                if (count.Variance != 0)
                {
                    db.StockMovements.Add(new StockMovement
                    {
                        InventoryItemId = itemId,
                        QuantityDelta = count.Variance,
                        Type = StockMovementType.Adjustment,
                        Reason = $"Physical count: {count.Reason}",
                        ActorId = actorId,
                        IdempotencyKey = $"count:{count.Id}:variance",
                        OccurredUtc = clock.UtcNow
                    });
                }
                db.AuditEntries.Add(Audit(actorId, "ReconcileInventoryCount", nameof(InventoryCountRecord), count.Id,
                    count.Reason, JsonSerializer.Serialize(new
                    {
                        count.InventoryItemId,
                        item.Name,
                        item.Unit,
                        count.LedgerQuantity,
                        count.CountedQuantity,
                        count.Variance
                    })));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return count.Id;
            });
        }
        catch (Exception exception) when (IsTransientTransactionConflict(exception))
        {
            logger?.LogWarning(exception, "Physical count for item {InventoryItemId} hit a transient database conflict.", itemId);
            throw RetryableInventoryConflict();
        }
    }

    private static InventoryReadinessCheck Check(string code, string name, bool passed, string evidence) =>
        new(code, name, passed ? InventoryReadinessStatus.Pass : InventoryReadinessStatus.Blocked, evidence);

    private static InventoryReadinessCheck Manual(string code, string name, string evidence) =>
        new(code, name, InventoryReadinessStatus.Manual, evidence);

    private static string ListEvidence(IReadOnlyList<string> values)
    {
        const int visibleLimit = 5;
        var visible = string.Join(", ", values.Take(visibleLimit));
        return values.Count <= visibleLimit ? visible : $"{visible}, and {values.Count - visibleLimit} more";
    }

    public async Task AdjustAsync(
        Guid itemId,
        decimal delta,
        string reason,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default,
        bool allowNegativeStock = false,
        string? inventoryOverrideReason = null)
    {
        if (delta == 0 || string.IsNullOrWhiteSpace(reason)) throw new DomainException("A non-zero quantity and reason are required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 150)
            throw new DomainException("A valid adjustment key is required.");
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await EnsureAdminAsync(db, actorId, ct);
            if (await db.StockMovements.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct)) return;
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.Id == itemId && x.IsActive, ct)
                ?? throw new DomainException("Inventory item not found.");
            var current = await db.StockMovements.Where(x => x.InventoryItemId == itemId)
                .SumAsync(x => x.QuantityDelta, ct);
            var projected = current + delta;
            if (projected < 0)
            {
                if (!allowNegativeStock)
                    throw new DomainException(
                        $"Insufficient stock: {item.Name} has {current:0.###} {item.Unit}; this adjustment would leave {projected:0.###}.");
                if (string.IsNullOrWhiteSpace(inventoryOverrideReason))
                    throw new DomainException("A manager override reason is required.");
                db.AuditEntries.Add(DiscrepancyAlert(
                    actorId,
                    nameof(InventoryItem),
                    item.Id,
                    inventoryOverrideReason,
                    new { item.Name, item.Unit, Current = current, Delta = delta, Projected = projected }));
            }
            db.StockMovements.Add(new StockMovement
            {
                InventoryItemId = itemId,
                QuantityDelta = delta,
                Type = StockMovementType.Adjustment,
                Reason = reason.Trim(),
                ActorId = actorId,
                IdempotencyKey = idempotencyKey,
                OccurredUtc = clock.UtcNow
            });
            db.AuditEntries.Add(Audit(actorId, "AdjustInventory", nameof(InventoryItem), item.Id,
                reason.Trim(), JsonSerializer.Serialize(new { Delta = delta, Projected = projected })));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task<IReadOnlyList<InventoryLossRequestView>> GetLossRequestsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryLossRequests.AsNoTracking()
            .Include(x => x.InventoryItem)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.ReportedUtc)
            .Select(x => new InventoryLossRequestView(
                x.Id,
                x.InventoryItemId,
                x.InventoryItem!.Name,
                x.InventoryItem.Unit,
                x.Type,
                x.Quantity,
                x.Reason,
                x.ReportedBy,
                x.ReportedUtc,
                x.Status,
                x.ReviewedBy,
                x.ReviewedUtc,
                x.ReviewReason))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockMovementView>> GetRecentMovementsAsync(
        int take = 50,
        CancellationToken ct = default)
    {
        if (take is < 1 or > 200) throw new DomainException("Movement history limit must be between 1 and 200.");
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.StockMovements.AsNoTracking()
            .Include(x => x.InventoryItem)
            .OrderByDescending(x => x.OccurredUtc)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .Select(x => new StockMovementView(
                x.Id,
                x.InventoryItemId,
                x.InventoryItem!.Name,
                x.InventoryItem.Unit,
                x.Type,
                x.QuantityDelta,
                x.Reason,
                x.ActorId,
                x.OccurredUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InventoryCountView>> GetRecentCountsAsync(
        int take = 25,
        CancellationToken ct = default)
    {
        if (take is < 1 or > 200) throw new DomainException("Count history limit must be between 1 and 200.");
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.InventoryCountRecords.AsNoTracking()
            .Include(x => x.InventoryItem)
            .OrderByDescending(x => x.CountedUtc)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .Select(x => new InventoryCountView(
                x.Id,
                x.InventoryItemId,
                x.InventoryItem!.Name,
                x.InventoryItem.Unit,
                x.LedgerQuantity,
                x.CountedQuantity,
                x.Variance,
                x.Reason,
                x.CountedBy,
                x.CountedUtc))
            .ToListAsync(ct);
    }

    public async Task<Guid> ReportLossAsync(
        Guid itemId,
        InventoryLossType type,
        decimal quantity,
        string reason,
        string actorId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureKitchenOrAdminAsync(db, actorId, ct);
        var existing = await db.InventoryLossRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null) return existing.Id;
        if (!await db.InventoryItems.AnyAsync(x => x.Id == itemId && x.IsActive, ct))
            throw new DomainException("Inventory item not found.");
        var request = InventoryLossRequest.Report(
            itemId, type, quantity, reason, actorId, idempotencyKey, clock.UtcNow);
        db.InventoryLossRequests.Add(request);
        db.AuditEntries.Add(Audit(actorId, "ReportInventoryLoss", nameof(InventoryLossRequest),
            request.Id, reason.Trim(), JsonSerializer.Serialize(new { Type = type, Quantity = quantity, itemId })));
        await db.SaveChangesAsync(ct);
        return request.Id;
    }

    public async Task ReviewLossAsync(
        Guid requestId,
        bool approve,
        string? reviewReason,
        string adminId,
        CancellationToken ct = default)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await EnsureAdminAsync(db, adminId, ct);
            var request = await db.InventoryLossRequests.Include(x => x.InventoryItem)
                .SingleOrDefaultAsync(x => x.Id == requestId, ct)
                ?? throw new DomainException("Loss request not found.");
            if (approve)
            {
                request.Approve(adminId, reviewReason, clock.UtcNow);
                var movementKey = $"loss:{request.Id}:approved";
                if (!await db.StockMovements.AnyAsync(x => x.IdempotencyKey == movementKey, ct))
                {
                    var current = await db.StockMovements
                        .Where(x => x.InventoryItemId == request.InventoryItemId)
                        .SumAsync(x => x.QuantityDelta, ct);
                    var projected = current - request.Quantity;
                    if (projected < 0)
                    {
                        db.AuditEntries.Add(DiscrepancyAlert(
                            adminId,
                            nameof(InventoryLossRequest),
                            request.Id,
                            reviewReason ?? request.Reason,
                            new
                            {
                                request.InventoryItemId,
                                request.InventoryItem!.Name,
                                request.InventoryItem.Unit,
                                Current = current,
                                Loss = request.Quantity,
                                Projected = projected
                            }));
                    }
                    db.StockMovements.Add(new StockMovement
                    {
                        InventoryItemId = request.InventoryItemId,
                        Type = request.Type == InventoryLossType.Waste
                            ? StockMovementType.Waste
                            : StockMovementType.Spoilage,
                        QuantityDelta = -request.Quantity,
                        Reason = request.Reason,
                        ActorId = adminId,
                        IdempotencyKey = movementKey,
                        OccurredUtc = clock.UtcNow
                    });
                }
            }
            else
            {
                request.Reject(adminId, reviewReason ?? string.Empty, clock.UtcNow);
            }
            db.AuditEntries.Add(Audit(adminId,
                approve ? "ApproveInventoryLoss" : "RejectInventoryLoss",
                nameof(InventoryLossRequest),
                request.Id,
                reviewReason?.Trim(),
                JsonSerializer.Serialize(new { request.Type, request.Quantity, request.InventoryItemId })));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    private AuditEntry DiscrepancyAlert(
        string actor,
        string entityType,
        Guid entityId,
        string reason,
        object values) =>
        Audit(actor, "INVENTORY_DISCREPANCY_ALERT", entityType, entityId,
            reason.Trim(), JsonSerializer.Serialize(values));

    private AuditEntry Audit(
        string actor,
        string action,
        string entityType,
        Guid entityId,
        string? reason,
        string? values) => new()
        {
            ActorId = actor,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Reason = reason,
            NewValuesJson = values,
            OccurredUtc = clock.UtcNow
        };

    private static async Task EnsureKitchenOrAdminAsync(
        RomsDbContext db, string actorId, CancellationToken ct)
    {
        if (await IsInRoleAsync(db, actorId, RomsRoles.Kitchen, ct) ||
            await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct)) return;
        throw new DomainException("Only Kitchen staff or an administrator can report inventory loss.");
    }

    private static async Task EnsureAdminAsync(RomsDbContext db, string actorId, CancellationToken ct)
    {
        if (await IsInRoleAsync(db, actorId, RomsRoles.Admin, ct)) return;
        throw new DomainException("Only an administrator can manage inventory or approve inventory loss.");
    }

    private static Task<bool> IsInRoleAsync(
        RomsDbContext db, string actorId, string role, CancellationToken ct) =>
        (from user in db.Users
         join userRole in db.UserRoles on user.Id equals userRole.UserId
         join existingRole in db.Roles on userRole.RoleId equals existingRole.Id
         where user.UserName == actorId && existingRole.Name == role
         select user.Id).AnyAsync(ct);

    private static void ValidateKey(string idempotencyKey, string operation)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 150)
            throw new DomainException($"A valid {operation} key is required.");
    }

    private static bool IsTransientTransactionConflict(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException { Number: 1205 or 1213 })
                return true;
        }
        return false;
    }

    private static bool IsDuplicateKey(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException { Number: 1062 })
                return true;
        }
        return false;
    }

    private static DomainException RetryableInventoryConflict() =>
        new("Another inventory update happened at the same time. Reload and try this action again.");
}
