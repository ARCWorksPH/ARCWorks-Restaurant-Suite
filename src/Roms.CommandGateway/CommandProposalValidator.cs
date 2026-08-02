using System.Text.RegularExpressions;
using Roms.Application.Commands;
using Roms.Domain;

namespace Roms.CommandGateway;

public sealed partial class CommandProposalValidator
{
    public InterpretCommandResponse Validate(
        InterpretCommandRequest request,
        ModelCommandProposal proposal)
    {
        if (proposal.Command == RestaurantCommandName.Unknown)
            return Response(request, InterpretationStatus.Unsupported, null,
                ["The request is outside the approved read-only functions."]);
        if (!request.AllowedCommands.Contains(proposal.Command))
            return Response(request, InterpretationStatus.Unsupported, null,
                ["The proposed function is not permitted for this caller."]);

        return proposal.Command switch
        {
            RestaurantCommandName.GetMenuItem => ValidateCatalogItem(request, proposal, true),
            RestaurantCommandName.GetInventoryBalance => ValidateCatalogItem(request, proposal, false),
            RestaurantCommandName.ListMenu => ValidateMenuList(request, proposal),
            RestaurantCommandName.ListInventoryBalances or
            RestaurantCommandName.ListLowStockItems or
            RestaurantCommandName.GetOrderStatusSummary or
            RestaurantCommandName.GetLowStockSummary or
            RestaurantCommandName.GetMenuAvailabilitySummary => ValidateNoArguments(request, proposal),
            RestaurantCommandName.GetOrderStatus => ValidateOrderLookup(request, proposal),
            RestaurantCommandName.ListOrdersByStatus => ValidateStatusList(request, proposal),
            RestaurantCommandName.GetDailyOrderSummary or
            RestaurantCommandName.GetOperationalSummary => ValidateDateSummary(request, proposal),
            _ => Response(request, InterpretationStatus.Unsupported, null,
                ["The proposed function is not approved by this protocol version."])
        };
    }

    private static InterpretCommandResponse ValidateCatalogItem(
        InterpretCommandRequest request,
        ModelCommandProposal proposal,
        bool menu)
    {
        if (HasArgumentsOtherThan(proposal, item: true))
            return Clarify(request, "The item lookup contains unexpected arguments.");

        var matches = menu
            ? request.Menu.Where(x => Names(x.Name, x.Aliases).Any(name => Same(name, proposal.Item)))
                .Select(x => new CatalogMatch(x.Key, x.Name)).ToList()
            : request.Inventory.Where(x => Names(x.Name, x.Aliases).Any(name => Same(name, proposal.Item)))
                .Select(x => new CatalogMatch(x.Key, x.Name)).ToList();

        if (matches.Count == 0)
            return Clarify(request, "The proposed item does not exactly match the supplied catalog.");
        if (matches.Count > 1)
            return Clarify(request, "The proposed item is ambiguous.");
        var match = matches[0];
        if (!TextNamesCatalogItem(request.Text, match.Name,
                menu
                    ? request.Menu.Single(x => x.Key == match.Key).Aliases
                    : request.Inventory.Single(x => x.Key == match.Key).Aliases))
            return Clarify(request, "The original request does not explicitly name the proposed catalog item.");

        return Recognized(request, new(
            proposal.Command, match.Key, match.Name, null, null, null, null, null, null));
    }

    private static InterpretCommandResponse ValidateMenuList(
        InterpretCommandRequest request,
        ModelCommandProposal proposal)
    {
        if (HasArgumentsOtherThan(proposal, category: true, available: true))
            return Clarify(request, "The menu list contains unexpected arguments.");

        string? category = null;
        if (!string.IsNullOrWhiteSpace(proposal.Category))
        {
            var categories = request.Menu.Select(x => x.Category).Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => Same(x, proposal.Category)).ToList();
            if (categories.Count != 1 || !TextContainsPhrase(request.Text, categories[0]))
                return Clarify(request, "The proposed category is absent, ambiguous, or was not named by the user.");
            category = categories[0];
        }

        if (proposal.Available is not null &&
            !ContainsAny(request.Text, "available", "unavailable", "not available"))
            return Clarify(request, "The proposal added an availability filter that the user did not request.");

        return Recognized(request, new(
            proposal.Command, null, null, category, proposal.Available, null, null, null, null));
    }

    private static InterpretCommandResponse ValidateNoArguments(
        InterpretCommandRequest request,
        ModelCommandProposal proposal)
    {
        if (HasArgumentsOtherThan(proposal))
            return Clarify(request,
                $"This function does not accept arguments; unexpected field(s): {string.Join(", ", PresentArguments(proposal))}.");
        return Recognized(request, new(
            proposal.Command, null, null, null, null, null, null, null, null));
    }

    private static InterpretCommandResponse ValidateOrderLookup(
        InterpretCommandRequest request,
        ModelCommandProposal proposal)
    {
        if (HasArgumentsOtherThan(proposal, orderId: true, tableNumber: true))
            return Clarify(request, "The order lookup contains unexpected arguments.");
        var hasOrder = !string.IsNullOrWhiteSpace(proposal.OrderId);
        var hasTable = !string.IsNullOrWhiteSpace(proposal.TableNumber);
        if (hasOrder == hasTable)
            return Clarify(request, "Provide exactly one order ID or table number.");

        if (hasOrder)
        {
            if (!Guid.TryParse(proposal.OrderId, out var orderId) ||
                !request.Text.Contains(proposal.OrderId, StringComparison.OrdinalIgnoreCase))
                return Clarify(request, "The proposed order ID is invalid or was not supplied by the user.");
            return Recognized(request, new(
                proposal.Command, null, null, null, null, orderId, null, null, null));
        }

        var table = request.TableNumbers.SingleOrDefault(x => Same(x, proposal.TableNumber));
        if (table is null || !TextContainsPhrase(request.Text, table))
            return Clarify(request, "The proposed table number is invalid or was not supplied by the user.");
        return Recognized(request, new(
            proposal.Command, null, null, null, null, null, table, null, null));
    }

    private static InterpretCommandResponse ValidateStatusList(
        InterpretCommandRequest request,
        ModelCommandProposal proposal)
    {
        if (HasArgumentsOtherThan(proposal, status: true))
            return Clarify(request, "The order-status list contains unexpected arguments.");
        if (!Enum.TryParse<OrderStatus>(proposal.Status, true, out var status) ||
            !TextContainsPhrase(request.Text, status.ToString()))
            return Clarify(request, "The proposed order status is invalid or was not named by the user.");
        return Recognized(request, new(
            proposal.Command, null, null, null, null, null, null, status, null));
    }

    private static InterpretCommandResponse ValidateDateSummary(
        InterpretCommandRequest request,
        ModelCommandProposal proposal)
    {
        if (HasArgumentsOtherThan(proposal, businessDate: true))
            return Clarify(request, "The summary contains unexpected arguments.");
        DateOnly? date = null;
        if (!string.IsNullOrWhiteSpace(proposal.BusinessDate))
        {
            if (!DateOnly.TryParseExact(proposal.BusinessDate, "yyyy-MM-dd", out var parsed) ||
                !request.Text.Contains(proposal.BusinessDate, StringComparison.OrdinalIgnoreCase))
                return Clarify(request, "The proposed business date is invalid or was not supplied by the user.");
            date = parsed;
        }
        return Recognized(request, new(
            proposal.Command, null, null, null, null, null, null, null, date));
    }

    private static bool HasArgumentsOtherThan(
        ModelCommandProposal proposal,
        bool item = false,
        bool category = false,
        bool available = false,
        bool orderId = false,
        bool tableNumber = false,
        bool status = false,
        bool businessDate = false) =>
        (!item && !string.IsNullOrWhiteSpace(proposal.Item)) ||
        (!category && !string.IsNullOrWhiteSpace(proposal.Category)) ||
        (!available && proposal.Available is not null) ||
        (!orderId && !string.IsNullOrWhiteSpace(proposal.OrderId)) ||
        (!tableNumber && !string.IsNullOrWhiteSpace(proposal.TableNumber)) ||
        (!status && !string.IsNullOrWhiteSpace(proposal.Status)) ||
        (!businessDate && !string.IsNullOrWhiteSpace(proposal.BusinessDate));

    private static IEnumerable<string> PresentArguments(ModelCommandProposal proposal)
    {
        if (!string.IsNullOrWhiteSpace(proposal.Item)) yield return "item";
        if (!string.IsNullOrWhiteSpace(proposal.Category)) yield return "category";
        if (proposal.Available is not null) yield return "available";
        if (!string.IsNullOrWhiteSpace(proposal.OrderId)) yield return "orderId";
        if (!string.IsNullOrWhiteSpace(proposal.TableNumber)) yield return "tableNumber";
        if (!string.IsNullOrWhiteSpace(proposal.Status)) yield return "status";
        if (!string.IsNullOrWhiteSpace(proposal.BusinessDate)) yield return "businessDate";
    }

    private static bool TextNamesCatalogItem(string text, string name, IReadOnlyList<string> aliases) =>
        Names(name, aliases).Any(value => TextContainsPhrase(text, value));

    private static IEnumerable<string> Names(string name, IReadOnlyList<string> aliases) =>
        aliases.Append(name);

    private static bool Same(string left, string right) =>
        Normalize(left) == Normalize(right);

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(value => TextContainsPhrase(text, value));

    private static string Normalize(string value) =>
        Whitespace().Replace(value.Trim().ToUpperInvariant().Replace('_', ' '), " ");

    private static bool TextContainsPhrase(string text, string phrase)
    {
        var normalizedText = $" {NonAlphaNumeric().Replace(Normalize(text), " ")} ";
        var normalizedPhrase = $" {NonAlphaNumeric().Replace(Normalize(phrase), " ")} ";
        return normalizedText.Contains(normalizedPhrase, StringComparison.Ordinal);
    }

    private static InterpretCommandResponse Recognized(
        InterpretCommandRequest request,
        ValidatedCommandProposal proposal) =>
        Response(request, InterpretationStatus.Recognized, proposal, []);

    private static InterpretCommandResponse Clarify(
        InterpretCommandRequest request,
        string issue) =>
        Response(request, InterpretationStatus.ClarificationRequired, null, [issue]);

    private static InterpretCommandResponse Response(
        InterpretCommandRequest request,
        InterpretationStatus status,
        ValidatedCommandProposal? proposal,
        IReadOnlyList<string> issues) =>
        new(RestaurantCommandProtocol.CurrentSchemaVersion, request.RequestId,
            status, proposal, issues);

    private sealed record CatalogMatch(string Key, string Name);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[^A-Z0-9]+")]
    private static partial Regex NonAlphaNumeric();
}
