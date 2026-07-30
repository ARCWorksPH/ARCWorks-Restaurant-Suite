using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using Roms.Application;
using Roms.Domain;
using Roms.Infrastructure.Identity;
using Roms.Infrastructure.Persistence;
using Testcontainers.MariaDb;

namespace Roms.E2ETests;

public sealed class RomsApplicationSmokeTests : PageTest
{
    private const string E2ePassword = "E2E-Only-Password!29";

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
            var preflightPanel = Page.Locator("section.panel").Filter(new()
            {
                Has = Page.GetByRole(AriaRole.Heading, new() { Name = "Inventory activation preflight" })
            });
            await Expect(preflightPanel).ToBeVisibleAsync();
            await Expect(preflightPanel.GetByText("blocker(s)")).ToBeVisibleAsync();
            await Expect(preflightPanel.GetByText("External audit acceptance")).ToBeVisibleAsync();
            await Expect(preflightPanel.GetByText("Manual gate")).ToHaveCountAsync(3);
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
            var balancesPanel = Page.Locator("section.panel").Filter(new()
            {
                Has = Page.GetByRole(AriaRole.Heading, new() { Name = "Current balances" })
            });
            await Expect(balancesPanel.GetByText("-1.000 piece")).ToBeVisibleAsync();
            await Page.GetByLabel("Received inventory item").SelectOptionAsync(
                new SelectOptionValue { Label = "Test milk (piece)" });
            await Page.GetByPlaceholder("Received quantity").FillAsync("10");
            await Page.GetByPlaceholder("Delivery or invoice reference (required)").FillAsync("E2E-DR-001");
            await Page.GetByPlaceholder("Delivery note (optional)").FillAsync("Synthetic delivery");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Post receipt" }).ClickAsync();
            await Expect(Page.GetByText("Receipt: +10.000 piece Test milk")).ToBeVisibleAsync();
            await Page.GetByLabel("Counted inventory item").SelectOptionAsync(
                new SelectOptionValue { Label = "Test milk (piece)" });
            await Page.GetByPlaceholder("Physical quantity counted").FillAsync("7.5");
            await Page.GetByPlaceholder("Count reason or count-sheet reference (required)")
                .FillAsync("E2E closing count");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Reconcile count" }).ClickAsync();
            await Expect(Page.GetByText("Test milk: 7.500 piece counted")).ToBeVisibleAsync();
            await Expect(balancesPanel.GetByText("7.500 piece", new() { Exact = true })).ToBeVisibleAsync();
            await Expect(preflightPanel.GetByText("Every active item has a witnessed opening count"))
                .ToBeVisibleAsync();
            await Expect(preflightPanel.GetByText("Pass", new() { Exact = true }))
                .ToHaveCountAsync(8);

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

            var sidebar = Page.Locator(".sidebar");
            await Expect(sidebar).ToBeVisibleAsync();
            var sidebarBox = await sidebar.BoundingBoxAsync();
            Assert.That(sidebarBox, Is.Not.Null);
            Assert.That(sidebarBox!.Width, Is.InRange(70, 74));

            var compactLabel = sidebar.Locator(".nav-text").First;
            await Expect(compactLabel).ToBeAttachedAsync();
            var compactLabelBox = await compactLabel.BoundingBoxAsync();
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

    [Test]
    public async Task Independent_waiter_kitchen_and_cashier_sessions_complete_one_order_in_real_time()
    {
        const string adminUsername = "synthetic-admin";
        const string waiterUsername = "synthetic-waiter";
        const string kitchenUsername = "synthetic-kitchen";
        await using var database = new MariaDbBuilder("mariadb:11.4")
            .WithDatabase("roms_multiuser")
            .WithUsername("root")
            .WithPassword($"roms-{Guid.NewGuid():N}")
            .Build();
        await database.StartAsync();

        var port = ReservePort();
        var baseAddress = $"http://127.0.0.1:{port}";
        var keysPath = Path.Combine(Path.GetTempPath(), $"roms-multiuser-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keysPath);
        var runningApplication = StartApplication(
            baseAddress,
            database.GetConnectionString(),
            keysPath,
            adminUsername,
            E2ePassword);
        using var application = runningApplication.Process;

        try
        {
            await WaitUntilHealthyAsync(runningApplication, baseAddress);
            await SeedStaffAsync(database.GetConnectionString(),
                (waiterUsername, "Synthetic Waiter", RomsRoles.Waiter),
                (kitchenUsername, "Synthetic Kitchen", RomsRoles.Kitchen));

            await using var waiterContext = await Browser.NewContextAsync();
            await using var kitchenContext = await Browser.NewContextAsync();
            await using var cashierContext = await Browser.NewContextAsync();
            var waiterPage = await waiterContext.NewPageAsync();
            var kitchenPage = await kitchenContext.NewPageAsync();
            var cashierPage = await cashierContext.NewPageAsync();

            await Task.WhenAll(
                LoginAsync(waiterPage, baseAddress, waiterUsername),
                LoginAsync(kitchenPage, baseAddress, kitchenUsername),
                LoginAsync(cashierPage, baseAddress, adminUsername));

            await kitchenPage.GotoAsync($"{baseAddress}/kitchen");
            await WaitForInteractiveAsync(kitchenPage);
            await cashierPage.GotoAsync($"{baseAddress}/admin/payments");
            await WaitForInteractiveAsync(cashierPage);

            await waiterPage.GotoAsync($"{baseAddress}/tables");
            await WaitForInteractiveAsync(waiterPage);
            await waiterPage.GetByRole(AriaRole.Button, new()
            {
                NameRegex = new Regex("^Table 1 Available$")
            }).ClickAsync();
            await Expect(waiterPage.GetByRole(AriaRole.Heading, new() { Name = "Table 1" }))
                .ToBeVisibleAsync();
            var orderUrl = waiterPage.Url;
            const string hostileNote = "<script>window.__romsXss=true</script> no onions";
            await waiterPage.GetByPlaceholder("Special instructions").FillAsync(hostileNote);
            await waiterPage.Locator(".menu-card").Filter(new() { HasText = "Cheeseburger" }).ClickAsync();
            await waiterPage.GetByRole(AriaRole.Button, new() { Name = "Send to kitchen" }).ClickAsync();

            await Expect(kitchenPage.GetByText(hostileNote, new() { Exact = true }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            Assert.That(await kitchenPage.EvaluateAsync<bool>("() => Boolean(window.__romsXss)"), Is.False);
            await kitchenPage.GetByRole(AriaRole.Button, new() { Name = "Start preparing" }).ClickAsync();
            await Expect(waiterPage.Locator(".status-pill.status-preparing"))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await kitchenPage.GetByRole(AriaRole.Button, new() { Name = "Ready" }).ClickAsync();

            await waiterPage.GotoAsync(orderUrl);
            await WaitForInteractiveAsync(waiterPage);
            await waiterPage.GetByRole(AriaRole.Button, new() { Name = "Mark served" }).ClickAsync();
            await Expect(cashierPage.GetByRole(AriaRole.Button, new() { Name = "Confirm payment received" }))
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await cashierPage.GetByRole(AriaRole.Button, new() { Name = "Confirm payment received" }).ClickAsync();
            await Expect(cashierPage.GetByText("No served orders are waiting for payment."))
                .ToBeVisibleAsync();

            await waiterPage.GotoAsync($"{baseAddress}/tables");
            await WaitForInteractiveAsync(waiterPage);
            await Expect(waiterPage.GetByRole(AriaRole.Button, new()
            {
                NameRegex = new Regex("^Table 1 Available$")
            })).ToBeVisibleAsync();

            await using var verificationDb = CreateDbContext(database.GetConnectionString());
            var order = await verificationDb.Orders.Include(x => x.Items).SingleAsync();
            Assert.Multiple(() =>
            {
                Assert.That(order.Status, Is.EqualTo(OrderStatus.Completed));
                Assert.That(order.WaiterId, Is.EqualTo(waiterUsername));
                Assert.That(order.PaymentConfirmedBy, Is.EqualTo(adminUsername));
                Assert.That(order.PaymentConfirmedUtc, Is.Not.Null);
                Assert.That(order.Items.Single().Notes, Is.EqualTo(hostileNote));
            });
            Assert.That(await verificationDb.AuditEntries.CountAsync(x =>
                x.EntityId == order.Id.ToString()), Is.GreaterThanOrEqualTo(6));
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

    private async Task LoginAsync(IPage page, string baseAddress, string username)
    {
        await page.GotoAsync($"{baseAddress}/Account/Login");
        await page.GetByLabel("Username").FillAsync(username);
        await page.GetByLabel("Password").FillAsync(E2ePassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "My attendance" }))
            .ToBeVisibleAsync();
        await WaitForInteractiveAsync(page);
    }

    private async Task WaitForInteractiveAsync(IPage page) =>
        await Expect(page.Locator("#roms-connection-indicator")).ToContainTextAsync(
            "Connected", new() { Timeout = 15_000 });

    private static async Task SeedStaffAsync(
        string connectionString,
        params (string Username, string DisplayName, string Role)[] staff)
    {
        await using var db = CreateDbContext(connectionString);
        var roles = await db.Roles.ToDictionaryAsync(x => x.Name!, StringComparer.OrdinalIgnoreCase);
        var hasher = new PasswordHasher<ApplicationUser>();
        foreach (var definition in staff)
        {
            var user = new ApplicationUser
            {
                UserName = definition.Username,
                NormalizedUserName = definition.Username.ToUpperInvariant(),
                DisplayName = definition.DisplayName,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            user.PasswordHash = hasher.HashPassword(user, E2ePassword);
            db.Users.Add(user);
            db.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = roles[definition.Role].Id
            });
        }
        await db.SaveChangesAsync();
    }

    private static RomsDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<RomsDbContext>()
            .UseMySQL(connectionString)
            .Options;
        return new RomsDbContext(options);
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
