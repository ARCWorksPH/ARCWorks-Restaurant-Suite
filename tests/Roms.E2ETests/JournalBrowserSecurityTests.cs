using System.Text;
using Microsoft.Playwright.NUnit;

namespace Roms.E2ETests;

public sealed class JournalBrowserSecurityTests : PageTest
{
    [Test]
    public async Task Restricted_markdown_preview_never_executes_or_materializes_untrusted_markup()
    {
        await Page.SetContentAsync("<main id='preview'></main>");
        var source = await File.ReadAllTextAsync(FindJournalModule());
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
        const string payload = "# Safe heading\n<img src=x onerror=window.journalPwned=1>\n<script>window.journalPwned=2</script>\n[jump](javascript:alert(1))\n**bold** and *italic*";

        await Page.EvaluateAsync("""
            async ({ source, payload }) => {
              const code = atob(source);
              const moduleUrl = URL.createObjectURL(new Blob([code], { type: 'text/javascript' }));
              try {
                const journal = await import(moduleUrl);
                journal.renderSafeMarkdown(payload, document.getElementById('preview'));
                window.journalValidation = [
                  '<img src=x onerror=alert(1)>',
                  '![remote](https://example.invalid/image.png)',
                  '[unsafe](javascript:alert(1))'
                ].map(value => {
                  try { journal.validateJournalMarkdown(value); return 'accepted'; }
                  catch { return 'rejected'; }
                });
              } finally {
                URL.revokeObjectURL(moduleUrl);
              }
            }
            """, new { source = encoded, payload });

        Assert.That(await Page.Locator("#preview img, #preview script, #preview a").CountAsync(), Is.Zero);
        Assert.That(await Page.Locator("#preview h1").TextContentAsync(), Is.EqualTo("Safe heading"));
        Assert.That(await Page.Locator("#preview strong").TextContentAsync(), Is.EqualTo("bold"));
        Assert.That(await Page.Locator("#preview em").TextContentAsync(), Is.EqualTo("italic"));
        Assert.That(await Page.Locator("#preview").TextContentAsync(), Does.Contain("<img src=x onerror=window.journalPwned=1>"));
        Assert.That(await Page.EvaluateAsync<int>("() => window.journalPwned ?? 0"), Is.Zero);
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.journalValidation"),
            Is.EqualTo(new[] { "rejected", "rejected", "rejected" }));
    }

    private static string FindJournalModule()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Roms.Web", "wwwroot", "js", "arcworks-journal.js");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("The ARCWorks journal module was not found from the test output path.");
    }
}
