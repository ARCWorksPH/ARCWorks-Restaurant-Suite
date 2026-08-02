using Microsoft.EntityFrameworkCore;
using Roms.Application.Ai;
using Roms.Application.Commands;
using Roms.Infrastructure.Persistence;

namespace Roms.Infrastructure.Services;

public sealed class AiAssistantService(
    IDbContextFactory<RomsDbContext> factory,
    ICommandGatewayClient gateway,
    IAiFunctionService functions) : IAiAssistantService
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

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var inventoryRows = await db.InventoryItems.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Take(RestaurantCommandProtocol.MaximumCatalogItems)
            .Select(x => new { x.Id, x.Name, x.Unit })
            .ToListAsync(cancellationToken);
        var inventory = inventoryRows.Select(x => new InventoryCatalogItem(
            x.Id.ToString(), x.Name, x.Unit, Array.Empty<string>())).ToList();
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
                    Guid.NewGuid().ToString("N"), text.Trim(), inventory, menu, tables),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new(AiAssistantStatus.InterpreterUnavailable,
                "The local interpreter is unavailable. No database action was taken.");
        }

        if (interpretation.Status != InterpretationStatus.Recognized || interpretation.Proposal is null)
        {
            var issue = interpretation.Issues.FirstOrDefault();
            return interpretation.Status switch
            {
                InterpretationStatus.ClarificationRequired => new(
                    AiAssistantStatus.ClarificationRequired,
                    issue ?? "Please clarify the requested item, table, status, or date."),
                InterpretationStatus.Unsupported => new(
                    AiAssistantStatus.Unsupported,
                    "That request is outside the approved read-only ROMS functions."),
                _ => new(
                    AiAssistantStatus.InterpreterUnavailable,
                    "The local interpreter failed safely. No database action was taken.")
            };
        }

        var functionRequest = Map(interpretation.Proposal);
        if (functionRequest is null)
            return new(AiAssistantStatus.Unsupported,
                "That request is outside the approved read-only ROMS functions.");

        var result = await functions.ExecuteAsync(functionRequest, actorUsername, cancellationToken);
        return new AiAssistantResponse(
            Map(result.Status),
            result.Message,
            result.Function,
            result.Data);
    }

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
