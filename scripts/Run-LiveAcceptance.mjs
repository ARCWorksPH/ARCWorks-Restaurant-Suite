import fs from "node:fs";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const envPath = path.join(projectRoot, ".env");
const artifactDir = path.join(projectRoot, ".artifacts", "live-acceptance");
const nodeModules = process.env.CODEX_NODE_MODULES ??
  "C:\\Users\\GBServerPH\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules";
const playwrightUrl = pathToFileURL(path.join(nodeModules, "playwright", "index.mjs")).href;
const { chromium } = await import(playwrightUrl);

function readDotEnv(file) {
  const values = {};
  for (const rawLine of fs.readFileSync(file, "utf8").split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith("#") || !line.includes("=")) continue;
    const index = line.indexOf("=");
    let value = line.slice(index + 1).trim();
    if (value.length >= 2 &&
        ((value.startsWith("'") && value.endsWith("'")) ||
         (value.startsWith('"') && value.endsWith('"')))) {
      value = value.slice(1, -1);
    }
    values[line.slice(0, index).trim()] = value;
  }
  return values;
}

function requireValue(values, key) {
  if (!values[key]) throw new Error(`Missing ${key} in protected production environment.`);
  return values[key];
}

async function waitForText(page, selector, text) {
  await page.locator(selector).filter({ hasText: text }).first().waitFor({ state: "visible" });
}

async function waitForInteractiveCircuit(page) {
  await page.waitForTimeout(1_500);
}

const env = readDotEnv(envPath);
const baseUrl = process.env.ROMS_ACCEPTANCE_BASE_URL ?? `https://${requireValue(env, "ROMS_HOST")}`;
const username = requireValue(env, "ADMIN_USERNAME");
const password = requireValue(env, "ADMIN_PASSWORD");
fs.mkdirSync(artifactDir, { recursive: true });

const browser = await chromium.launch({
  headless: true,
  executablePath: "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
});
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
const page = await context.newPage();
page.setDefaultTimeout(20_000);
page.on("pageerror", error => console.error(`PAGE_ERROR=${error.message}`));
page.on("console", message => {
  if (message.type() === "error" || message.type() === "warning")
    console.error(`BROWSER_${message.type().toUpperCase()}=${message.text()}`);
});
page.on("response", response => {
  if (response.status() >= 400)
    console.error(`HTTP_${response.status()}=${response.url()}`);
});

let orderId = "";
try {
  await page.goto(`${baseUrl}/Account/Login`, { waitUntil: "domcontentloaded" });
  await page.getByLabel("Username").fill(username);
  await page.getByLabel("Password").fill(password);
  await Promise.all([
    page.waitForURL(/\/attendance(?:\?|$)/),
    page.getByRole("button", { name: "Log in" }).click()
  ]);

  await page.goto(`${baseUrl}/tables`, { waitUntil: "domcontentloaded" });
  await page.locator("button.table-card").first().waitFor();
  await waitForInteractiveCircuit(page);
  await page.screenshot({ path: path.join(artifactDir, "01-tables.png"), fullPage: true });
  await page.locator("button.table-card").first().click();
  await page.waitForURL(/\/orders\/[0-9a-f-]{36}$/i);
  orderId = page.url().split("/").at(-1);
  await waitForInteractiveCircuit(page);

  const burger = page.locator("button.menu-card").filter({ hasText: "Cheeseburger" }).first();
  await burger.waitFor();
  await burger.click();
  await waitForText(page, ".order-line", "Cheeseburger");
  await page.getByRole("button", { name: "Send to kitchen" }).click();
  await waitForText(page, ".status-pill", "New");

  await page.goto(`${baseUrl}/kitchen`, { waitUntil: "domcontentloaded" });
  await waitForInteractiveCircuit(page);
  await page.getByRole("button", { name: "Start preparing" }).click();
  await page.getByRole("button", { name: "Ready", exact: true }).waitFor();
  await page.screenshot({ path: path.join(artifactDir, "02-preparing.png"), fullPage: true });
  await page.getByRole("button", { name: "Ready", exact: true }).click();
  await page.locator(".ready-callout").waitFor();

  await page.goto(`${baseUrl}/orders/${orderId}`, { waitUntil: "domcontentloaded" });
  await waitForInteractiveCircuit(page);
  await page.getByRole("button", { name: "Mark served" }).click();
  await waitForText(page, ".alert-warning", "Pending payment");

  await page.goto(`${baseUrl}/admin/payments`, { waitUntil: "domcontentloaded" });
  await waitForInteractiveCircuit(page);
  await page.getByRole("button", { name: "Confirm payment received" }).click();
  await waitForText(page, ".alert-success", "Payment confirmed");

  await page.goto(`${baseUrl}/reports`, { waitUntil: "domcontentloaded" });
  await page.getByRole("heading", { name: "Confirmed payment reports" }).waitFor();
  const businessDate = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Manila",
    year: "numeric",
    month: "2-digit",
    day: "2-digit"
  }).format(new Date());
  const reportDates = page.locator('input[type="date"]');
  if (await reportDates.first().inputValue() !== businessDate ||
      await reportDates.nth(1).inputValue() !== businessDate) {
    throw new Error(`Report did not default to the current Asia/Manila business date (${businessDate}).`);
  }
  const paidOrdersText = await page.locator(".metric-grid > div")
    .filter({ hasText: "Paid orders" })
    .locator("strong")
    .innerText();
  if (Number.parseInt(paidOrdersText, 10) < 1) {
    throw new Error("Confirmed payment was not included in the current business-day report.");
  }
  await page.screenshot({ path: path.join(artifactDir, "03-report.png"), fullPage: true });

  await page.goto(`${baseUrl}/inventory`, { waitUntil: "domcontentloaded" });
  await waitForText(page, ".alert-warning", "Automatic stock deduction is paused");
  await page.getByRole("heading", { name: "Add inventory item" }).waitFor();
  await page.screenshot({ path: path.join(artifactDir, "04-inventory-setup.png"), fullPage: true });

  fs.writeFileSync(path.join(artifactDir, "last-order-id.txt"), `${orderId}\n`, "utf8");
  console.log(`LIVE_ACCEPTANCE=PASS`);
  console.log(`ORDER_ID=${orderId}`);
  console.log(`ARTIFACT_DIR=${artifactDir}`);
} catch (error) {
  await page.screenshot({ path: path.join(artifactDir, "failure.png"), fullPage: true });
  console.error(`LIVE_ACCEPTANCE=FAIL`);
  console.error(error instanceof Error ? error.stack : String(error));
  process.exitCode = 1;
} finally {
  await context.close();
  await browser.close();
}
