using System.Text.Json;

namespace Roms.CommandGateway.Tests;

public sealed class CommandCorpusTests
{
    [Fact]
    public void Corpus_has_unique_complete_cases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "command-corpus.json");
        var cases = JsonSerializer.Deserialize<List<CorpusCase>>(
            File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.True(cases.Count >= 20);
        Assert.Equal(cases.Count, cases.Select(x => x.Id).Distinct().Count());
        Assert.All(cases, test =>
        {
            Assert.False(string.IsNullOrWhiteSpace(test.Id));
            Assert.False(string.IsNullOrWhiteSpace(test.Text));
            Assert.Contains(test.ExpectedStatus,
                new[] { "Recognized", "Unsupported", "ClarificationRequired" });
            if (test.ExpectedStatus == "Recognized")
                Assert.Contains(test.ExpectedCommand,
                    new[] { "InventoryLookup" });
            else
                Assert.Null(test.ExpectedCommand);
        });
    }

    private sealed record CorpusCase(
        string Id,
        string Text,
        string ExpectedStatus,
        string? ExpectedCommand);
}
