using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Roms.Application;
using Roms.Application.Ai;
using Roms.Application.Commands;
using Roms.Domain;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class AiAssistantService(
    IDbContextFactory<RomsDbContext> factory,
    ICommandGatewayClient gateway,
    IAiFunctionService functions,
    IClock clock,
    AiRequestGate requestGate) : IAiAssistantService
{
    public async Task<AiAssistantResponse> AskAsync(
        string text,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > RestaurantCommandProtocol.MaximumRequestLength)
            return new(AiAssistantStatus.InvalidRequest,
                $"Enter a request between 1 and {RestaurantCommandProtocol.MaximumRequestLength} characters.");
        if (string.IsNullOrWhiteSpace(actorUsername))
            return new(AiAssistantStatus.Unauthorized, "Authentication is required.");

        var requestId = Guid.NewGuid().ToString("N");
        var normalizedText = text.Trim();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var roles = await GetRolesAsync(db, actorUsername, cancellationToken);
        var allowedCommands = AllowedCommands(roles);
        if (allowedCommands.Count == 0)
        {
            return await AuditAndReturnAsync(db, actorUsername, requestId, normalizedText,
                new(AiAssistantStatus.Unauthorized,
                    "You do not have permission to use the assistant."),
                null, null, cancellationToken);
        }

        var admission = requestGate.TryAcquire(actorUsername, clock.UtcNow);
        if (admission.Status != AiRequestAdmissionStatus.Accepted)
        {
            var message = admission.Status == AiRequestAdmissionStatus.UserRateLimited
                ? "Too many assistant requests were submitted. Please wait one minute and try again."
                : "The local assistant is already at capacity. Please try again shortly.";
            return await AuditAndReturnAsync(db, actorUsername, requestId, normalizedText,
                new(AiAssistantStatus.RateLimited, message), null, null, cancellationToken);
        }

        using var lease = admission.Lease;
        var inventory = new List<InventoryCatalogItem>();
        if (allowedCommands.Contains(RestaurantCommandName.GetInventoryBalance))
        {
            var inventoryRows = await db.InventoryItems.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Take(RestaurantCommandProtocol.MaximumCatalogItems)
                .Select(x => new { x.Id, x.Name, x.Unit })
                .ToListAsync(cancellationToken);
            inventory = inventoryRows.Select(x => new InventoryCatalogItem(
                x.Id.ToString(), x.Name, x.Unit, Array.Empty<string>())).ToList();
        }

        var menuRows = await db.MenuItems.AsNoTracking()
            .Where(x => x.IsActive && x.Category!.IsActive)
            .OrderBy(x => x.Category!.SortOrder)
            .ThenBy(x => x.Name)
            .Take(RestaurantCommandProtocol.MaximumCatalogItems)
            .Select(x => new { x.Id, x.Name, Category = x.Category!.Name })
            .ToListAsync(cancellationToken);
        var menu = menuRows.Select(x => new MenuCatalogItem(
            x.Id.ToString(), x.Name, x.Category, Array.Empty<string>())).ToList();
        var tables = await db.RestaurantTables.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Number)
            .Take(RestaurantCommandProtocol.MaximumCatalogItems)
            .Select(x => x.Number)
            .ToListAsync(cancellationToken);

        InterpretCommandResponse interpretation;
        try
        {
            interpretation = await gateway.InterpretAsync(
                new InterpretCommandRequest(
                    requestId, normalizedText, allowedCommands, inventory, menu, tables),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await AuditAttemptAsync(db, actorUsername, requestId, normalizedText,
                AiAssistantStatus.InterpreterUnavailable, null, null, CancellationToken.None);
            throw;
        }
        catch
        {
            return await AuditAndReturnAsync(db, actorUsername, requestId, normalizedText,
                new(AiAssistantStatus.InterpreterUnavailable,
                    "The local interpreter is unavailable. No database action was taken."),
                InterpretationStatus.InterpreterError, null, cancellationToken);
        }

        if (interpretation.Status != InterpretationStatus.Recognized || interpretation.Proposal is null)
        {
            var issue = interpretation.Issues.FirstOrDefault();
            var response = interpretation.Status switch
            {
                InterpretationStatus.ClarificationRequired => new AiAssistantResponse(
                    AiAssistantStatus.ClarificationRequired,
                    issue ?? "Please clarify the requested item, table, status, or date."),
                InterpretationStatus.Unsupported => new AiAssistantResponse(
                    AiAssistantStatus.Unsupported,
                    "That request is outside your approved read-only ROMS functions."),
                _ => new AiAssistantResponse(
                    AiAssistantStatus.InterpreterUnavailable,
                    "The local interpreter failed safely. No database action was taken.")
            };
            return await AuditAndReturnAsync(db, actorUsername, requestId, normalizedText,
                response, interpretation.Status, null, cancellationToken);
        }

        if (!allowedCommands.Contains(interpretation.Proposal.Command))
        {
            return await AuditAndReturnAsync(db, actorUsername, requestId, normalizedText,
                new(AiAssistantStatus.Unsupported,
                    "That function is not permitted for your ROMS role."),
                interpretation.Status, interpretation.Proposal.Command, cancellationToken);
        }

        var functionRequest = Map(interpretation.Proposal);
        if (functionRequest is null)
        {
            return await AuditAndReturnAsync(db, actorUsername, requestId, normalizedText,
                new(AiAssistantStatus.Unsupported,
                    "That request is outside the approved read-only ROMS functions."),
                interpretation.Status, interpretation.Proposal.Command, cancellationToken);
        }

        try
        {
            var result = await functions.ExecuteAsync(functionRequest, actorUsername, cancellationToken);
            return await AuditAndReturnAsync(db, actorUsername, requestId, normalizedText,
                new AiAssistantResponse(Map(result.Status), result.Message, result.Function, result.Data),
                interpretation.Status, interpretation.Proposal.Command, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await AuditAttemptAsync(db, actorUsername, requestId, normalizedText,
                AiAssistantStatus.InterpreterUnavailable, interpretation.Status,
                interpretation.Proposal.Command, CancellationToken.None);
            throw;
        }
    }

    private async Task<AiAssistantResponse> AuditAndReturnAsync(
        RomsDbContext db,
        string actor,
        string requestId,
        string text,
        AiAssistantResponse response,
        InterpretationStatus? interpretationStatus,
        RestaurantCommandName? command,
        CancellationToken ct)
    {
        await AuditAttemptAsync(db, actor, requestId, text, response.Status,
            interpretationStatus, command, ct);
        return response;
    }

    private async Task AuditAttemptAsync(
        RomsDbContext db,
        string actor,
        string requestId,
        string text,
        AiAssistantStatus status,
        InterpretationStatus? interpretationStatus,
        RestaurantCommandName? command,
        CancellationToken ct)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            ActorId = actor,
            Action = $"AiAssistant:{status}",
            EntityType = "AiAssistant",
            EntityId = requestId,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Status = status,
                InterpretationStatus = interpretationStatus,
                Command = command,
                PromptLength = text.Length,
                PromptSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))
            }),
            OccurredUtc = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private static List<RestaurantCommandName> AllowedCommands(IReadOnlySet<string> roles)
    {
        if (roles.Contains(RomsRoles.Admin))
            return Enum.GetValues<RestaurantCommandName>()
                .Where(x => x != RestaurantCommandName.Unknown).ToList();

        var commands = new List<RestaurantCommandName>
        {
            RestaurantCommandName.GetMenuItem,
            RestaurantCommandName.ListMenu,
            RestaurantCommandName.GetOrderStatus,
            RestaurantCommandName.ListOrdersByStatus,
            RestaurantCommandName.GetMenuAvailabilitySummary
        };
        if (roles.Contains(RomsRoles.Kitchen))
        {
            commands.AddRange([
                RestaurantCommandName.GetInventoryBalance,
                RestaurantCommandName.ListInventoryBalances,
                RestaurantCommandName.ListLowStockItems,
                RestaurantCommandName.GetLowStockSummary
            ]);
        }

        return roles.Contains(RomsRoles.Waiter) || roles.Contains(RomsRoles.Kitchen)
            ? commands.Distinct().ToList()
            : [];
    }

    private static async Task<HashSet<string>> GetRolesAsync(
        RomsDbContext db,
        string username,
        CancellationToken ct) =>
        (await (from user in db.Users.AsNoTracking()
                join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where user.UserName == username && user.IsActive
                select role.Name!)
            .ToListAsync(ct))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static AiFunctionRequest? Map(ValidatedCommandProposal proposal)
    {
        var function = proposal.Command switch
        {
            RestaurantCommandName.GetMenuItem => AiFunctionName.GetMenuItem,
            RestaurantCommandName.ListMenu => AiFunctionName.ListMenu,
            RestaurantCommandName.GetInventoryBalance => AiFunctionName.GetInventoryBalance,
            RestaurantCommandName.ListInventoryBalances => AiFunctionName.ListInventoryBalances,
            RestaurantCommandName.ListLowStockItems => AiFunctionName.ListLowStockItems,
            RestaurantCommandName.GetOrderStatus => AiFunctionName.GetOrderStatus,
            RestaurantCommandName.ListOrdersByStatus => AiFunctionName.ListOrdersByStatus,
            RestaurantCommandName.GetDailyOrderSummary => AiFunctionName.GetDailyOrderSummary,
            RestaurantCommandName.GetOrderStatusSummary => AiFunctionName.GetOrderStatusSummary,
            RestaurantCommandName.GetLowStockSummary => AiFunctionName.GetLowStockSummary,
            RestaurantCommandName.GetMenuAvailabilitySummary => AiFunctionName.GetMenuAvailabilitySummary,
            RestaurantCommandName.GetOperationalSummary => AiFunctionName.GetOperationalSummary,
            _ => (AiFunctionName?)null
        };
        return function is null ? null : new AiFunctionRequest(
            function.Value,
            proposal.ItemName,
            proposal.Category,
            proposal.Available,
            proposal.OrderId,
            proposal.TableNumber,
            proposal.Status,
            proposal.BusinessDate);
    }

    private static AiAssistantStatus Map(AiFunctionStatus status) => status switch
    {
        AiFunctionStatus.Success => AiAssistantStatus.Success,
        AiFunctionStatus.Unauthorized => AiAssistantStatus.Unauthorized,
        AiFunctionStatus.InvalidRequest => AiAssistantStatus.InvalidRequest,
        AiFunctionStatus.Unsupported => AiAssistantStatus.Unsupported,
        AiFunctionStatus.NotFound or AiFunctionStatus.Ambiguous => AiAssistantStatus.ClarificationRequired,
        _ => AiAssistantStatus.Unsupported
    };
}
