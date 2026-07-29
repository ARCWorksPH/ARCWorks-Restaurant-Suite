using System.Text.RegularExpressions;
using Roms.Application.Commands;

namespace Roms.CommandGateway;

public sealed partial class CommandProposalValidator
{
    public InterpretCommandResponse Validate(
        InterpretCommandRequest request,
        ModelCommandProposal modelProposal)
    {
        if (modelProposal.Command == RestaurantCommandName.Unknown)
            return Response(request, InterpretationStatus.Unsupported, null,
                "The request is outside the supported restaurant commands.");

        var matches = request.Inventory
            .Where(item => ItemNames(item).Any(name =>
                Normalize(name) == Normalize(modelProposal.Item)))
            .ToList();

        if (matches.Count == 0)
            return Response(request, InterpretationStatus.ClarificationRequired, null,
                "The proposed inventory item does not exactly match the supplied catalog.");
        if (matches.Count > 1)
            return Response(request, InterpretationStatus.ClarificationRequired, null,
                "The proposed inventory item is ambiguous.");

        var item = matches[0];
        if (!TextContainsPhrase(request.Text, item.Name) &&
            !item.Aliases.Any(alias => TextContainsPhrase(request.Text, alias)))
            return Response(request, InterpretationStatus.ClarificationRequired, null,
                "The original request does not explicitly name the proposed catalog item.");

        return modelProposal.Command switch
        {
            RestaurantCommandName.InventoryLookup =>
                ValidateLookup(request, modelProposal, item),
            RestaurantCommandName.InventoryReceive =>
                ValidateReceipt(request, modelProposal, item),
            _ => Response(request, InterpretationStatus.Unsupported, null,
                "The proposed command is not allowed by this protocol version.")
        };
    }

    private static InterpretCommandResponse ValidateLookup(
        InterpretCommandRequest request,
        ModelCommandProposal modelProposal,
        InventoryCatalogItem item)
    {
        var issues = new List<string>();
        if (modelProposal.Quantity != 0)
            issues.Add("InventoryLookup must not contain a quantity.");
        if (!IsEmptyUnit(modelProposal.Unit))
            issues.Add("InventoryLookup must not contain a unit.");

        return issues.Count == 0
            ? Response(request, InterpretationStatus.Recognized,
                new ValidatedCommandProposal(
                    RestaurantCommandName.InventoryLookup, item.Key, item.Name, null, null))
            : Response(request, InterpretationStatus.ClarificationRequired, null, issues);
    }

    private static InterpretCommandResponse ValidateReceipt(
        InterpretCommandRequest request,
        ModelCommandProposal modelProposal,
        InventoryCatalogItem item)
    {
        var issues = new List<string>();
        if (!ReceiptVerb().IsMatch(request.Text))
            issues.Add("InventoryReceive requires an explicit receipt verb in the original request.");
        if (modelProposal.Quantity <= 0 || modelProposal.Quantity > 1_000_000m)
            issues.Add("InventoryReceive requires a positive quantity within the safety limit.");
        var quantities = NumericValue().Matches(request.Text)
            .Select(match => decimal.TryParse(
                match.Value,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
                ? (decimal?)value
                : null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();
        if (quantities.Count != 1 || quantities[0] != modelProposal.Quantity)
            issues.Add("The proposed quantity must exactly match one numeric quantity in the original request.");

        var units = item.AcceptedUnits.Append(item.Unit).Select(Normalize).ToHashSet();
        if (!units.Contains(Normalize(modelProposal.Unit)))
            issues.Add($"The proposed unit does not match the catalog unit for {item.Name}.");
        if (!item.AcceptedUnits.Append(item.Unit)
                .Any(unit => TextContainsPhrase(request.Text, unit)))
            issues.Add("InventoryReceive requires an explicit compatible unit in the original request.");

        return issues.Count == 0
            ? Response(request, InterpretationStatus.Recognized,
                new ValidatedCommandProposal(
                    RestaurantCommandName.InventoryReceive, item.Key, item.Name,
                    modelProposal.Quantity, item.Unit))
            : Response(request, InterpretationStatus.ClarificationRequired, null, issues);
    }

    private static IEnumerable<string> ItemNames(InventoryCatalogItem item) =>
        item.Aliases.Append(item.Name);

    private static bool IsEmptyUnit(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        Normalize(value) is "NONE" or "NULL" or "N/A" or "NA";

    private static string Normalize(string value) =>
        Whitespace().Replace(value.Trim().ToUpperInvariant().Replace('_', ' '), " ");

    private static bool TextContainsPhrase(string text, string phrase)
    {
        var normalizedText = $" {NonAlphaNumeric().Replace(Normalize(text), " ")} ";
        var normalizedPhrase = $" {NonAlphaNumeric().Replace(Normalize(phrase), " ")} ";
        return normalizedText.Contains(normalizedPhrase, StringComparison.Ordinal);
    }

    private static InterpretCommandResponse Response(
        InterpretCommandRequest request,
        InterpretationStatus status,
        ValidatedCommandProposal? proposal,
        params string[] issues) =>
        Response(request, status, proposal, (IReadOnlyList<string>)issues);

    private static InterpretCommandResponse Response(
        InterpretCommandRequest request,
        InterpretationStatus status,
        ValidatedCommandProposal? proposal,
        IReadOnlyList<string> issues) =>
        new(RestaurantCommandProtocol.CurrentSchemaVersion, request.RequestId,
            status, proposal, issues);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[^A-Z0-9]+")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex(@"\b(receive|received|receiving|add|added|deliver|delivered|delivery)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex ReceiptVerb();

    [GeneratedRegex(@"(?<![\w.])-?\d+(?:\.\d+)?(?![\w.])")]
    private static partial Regex NumericValue();
}
