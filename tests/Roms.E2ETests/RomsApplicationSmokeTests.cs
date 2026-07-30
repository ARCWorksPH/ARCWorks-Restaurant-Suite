using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Roms.Domain;
using Testcontainers.MariaDb;

namespace Roms.E2ETests;

public sealed class RomsApplicationSmokeTests : PageTest
{
    [Test]
    public async Task Admin_can_log_in_to_an_isolated_real_application()
    {
        const string username = "e2e-admin";
        const string password = "E2E-Only-Password!29";
        await using var database = new MariaDbBuilder("mariadb:11.4")
            .WithDatabase("roms_e2e")
            .WithUsername("root")
            .WithPassword($"roms-{Guid.NewGuid():N}")
            .Build();
        await database.StartAsync();

        var port = ReservePort();
        var baseAddress = $"http://127.0.0.1:{port}";
        var keysPath = Path.Combine(Path.GetTempPath(), $"roms-e2e-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keysPath);
        var runningApplication = StartApplication(
            baseAddress,
            database.GetConnectionString(),
            keysPath,
            username,
            password);
        using var application = runningApplication.Process;

        try
        {
            await WaitUntilHealthyAsync(runningApplication, baseAddress);
            await Page.GotoAsync($"{baseAddress}/Account/Login");
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Staff login" }))
                .ToBeVisibleAsync();

            await Page.GetByLabel("Username").FillAsync(username);
            await Page.GetByLabel("Password").FillAsync(password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My attendance" }))
                .ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Menu & Tables" }))
                .ToBeVisibleAsync();

            await Page.SetViewportSizeAsync(390, 844);
            await Page.GotoAsync($"{baseAddress}/inventory");
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inventory", Exact = true }))
                .ToBeVisibleAsync();
            var connectionIndicator = Page.Locator("#roms-connection-indicator");
            await Expect(connectionIndicator).ToContainTextAsync("Connected");
            await Page.GetByPlaceholder("Name", new() { Exact = true })
                .PressSequentiallyAsync("Test milk", new() { Delay = 40 });
            await Page.WaitForTimeoutAsync(500);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Add item" }).ClickAsync();
            await Expect(Page.Locator(".alert").Last).ToContainTextAsync("Saved.");
            await Page.GetByLabel("Loss inventory item").SelectOptionAsync(
                new SelectOptionValue { Label = "Test milk (piece)" });
            await Page.GetByPlaceholder("Quantity", new() { Exact = true }).FillAsync("1");
            await Page.GetByPlaceholder("What happened? (required)").FillAsync("Container damaged in receiving");
            await Page.WaitForTimeoutAsync(500);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for approval" }).ClickAsync();
            await Expect(Page.GetByText("Pending", new() { Exact = true })).ToBeVisibleAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Approve" }).ClickAsync();
            await Expect(Page.GetByText("Approved", new() { Exact = true })).ToBeVisibleAsync();
            await Expect(Page.GetByText("-1.000 piece")).ToBeVisibleAsync();

            var navigationToggle = Page.GetByRole(AriaRole.Button, new() { Name = "Toggle navigation menu" });
            await Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "false");
            await navigationToggle.ClickAsync();
            await Expect(navigationToggle).ToHaveAttributeAsync("aria-expanded", "true");
            await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Kitchen Display" }))
                .ToBeVisibleAsync();

            await Page.Context.SetOfflineAsync(true);
            try
            {
                await Expect(connectionIndicator).ToContainTextAsync("Connection lost");
            }
            finally
            {
                await Page.Context.SetOfflineAsync(false);
            }
            await Expect(connectionIndicator).ToContainTextAsync("Connected");

            await Page.SetViewportSizeAsync(1920, 1080);
            await Page.GotoAsync($"{baseAddress}/kitchen");
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Kitchen Display" }))
                .ToBeVisibleAsync();
            await Expect(Page.Locator(".page")).ToHaveClassAsync(new Regex("\\bkds-mode\\b"));

            var sidebarBox = await Page.Locator(".sidebar").BoundingBoxAsync();
            Assert.That(sidebarBox, Is.Not.Null);
            Assert.That(sidebarBox!.Width, Is.InRange(70, 74));

            var compactLabelBox = await Page.Locator(".sidebar .nav-text").First.BoundingBoxAsync();
            Assert.That(compactLabelBox, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(compactLabelBox!.Width, Is.LessThanOrEqualTo(1));
                Assert.That(compactLabelBox.Height, Is.LessThanOrEqualTo(1));
            });

            await Page.GotoAsync($"{baseAddress}/tables");
            await Page.GetByRole(AriaRole.Button, new()
            {
                NameRegex = new Regex("^Table 1 Available$")
            }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Table 1" }))
                .ToBeVisibleAsync();
            var orderUrl = Page.Url;
            await Page.Locator(".menu-card").Filter(new() { HasText = "Cheeseburger" }).ClickAsync();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Send to kitchen" }).ClickAsync();

            await Page.GotoAsync($"{baseAddress}/kitchen");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Start preparing" }).ClickAsync();
            await Page.GotoAsync(orderUrl);

            await Expect(Page.GetByLabel("Inventory disposition for cancellation"))
                .ToBeVisibleAsync();
            await Page.GetByPlaceholder("Cancellation reason (required)")
                .FillAsync("Converted to staff meal");
            await Page.GetByLabel("Inventory disposition for cancellation")
                .SelectOptionAsync(InventoryDisposition.ConsumedAsWasteOrStaffMeal.ToString());
            await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel order" }).ClickAsync();
            await Expect(Page.GetByText("Order cancelled."))
                .ToBeVisibleAsync();
            await Expect(Page.GetByText("ingredients retained as consumed waste or staff meal"))
                .ToBeVisibleAsync();
        }
        finally
        {
            if (!application.HasExited)
                application.Kill(entireProcessTree: true);
            await application.WaitForExitAsync();
            Directory.Delete(keysPath, recursive: true);
        }
    }

    private static RunningApplication StartApplication(
        string baseAddress,
        string connectionString,
        string keysPath,
        string username,
        string password)
    {
        var webAssembly = Path.Combine(AppContext.BaseDirectory, "Roms.Web.dll");
        if (!File.Exists(webAssembly))
            throw new FileNotFoundException("The ROMS web application was not copied to the E2E output.", webAssembly);

        var output = new StringBuilder();
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(webAssembly);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = baseAddress;
        startInfo.Environment["ConnectionStrings__DefaultConnection"] = connectionString;
        startInfo.Environment["DataProtection__KeysPath"] = keysPath;
        startInfo.Environment["Features__Inventory__Enabled"] = "false";
        startInfo.Environment["Seed__AdminUsername"] = username;
        startInfo.Environment["Seed__AdminPassword"] = password;
        startInfo.Environment["Seed__DemoData"] = "true";

        var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null) output.AppendLine(args.Data);
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null) output.AppendLine(args.Data);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return new RunningApplication(process, output);
    }

    private static async Task WaitUntilHealthyAsync(RunningApplication runningApplication, string baseAddress)
    {
        var application = runningApplication.Process;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (application.HasExited)
                throw new InvalidOperationException(
                    $"ROMS exited before becoming healthy.{Environment.NewLine}{runningApplication.Output}");

            try
            {
                using var response = await client.GetAsync($"{baseAddress}/health");
                if (response.StatusCode == HttpStatusCode.OK)
                    return;
            }
            catch (HttpRequestException)
            {
                // Startup and database migrations are still in progress.
            }
            catch (TaskCanceledException)
            {
                // The short health-probe timeout elapsed; retry until the overall deadline.
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("ROMS did not become healthy within 60 seconds.");
    }

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed record RunningApplication(Process Process, StringBuilder Output);
}
