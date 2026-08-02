using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Roms.E2ETests;

public sealed class InstallationSmokeTests : PageTest
{
    [Test]
    public async Task ChromiumCanRenderAPage()
    {
        await Page.GotoAsync("data:text/html,<title>Playwright Ready</title><h1>Playwright Ready</h1>");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Playwright Ready" })).ToBeVisibleAsync();
    }
}
