const { chromium } = require('playwright');
const path = require('path');

const BASE = 'http://localhost:3000';
const OUT = path.join(__dirname, '..', 'docs', 'screenshots');
const VIEWPORT = { width: 1920, height: 1080 };

const CREDENTIALS = { email: 'demo@seixal.pt', password: 'Demo123!' };

async function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

async function screenshot(page, name) {
  const filePath = path.join(OUT, `${name}.png`);
  await page.screenshot({ path: filePath, fullPage: false });
  console.log(`✓ ${name}.png`);
  return filePath;
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: VIEWPORT });
  const page = await context.newPage();

  // ─── Login Page ───────────────────────────────────────────────
  console.log('\n📸 Capturing screenshots...\n');

  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
  await sleep(1000);
  await screenshot(page, '01-login-page');

  // Fill login form and submit
  await page.fill('input[type="email"]', CREDENTIALS.email);
  await page.fill('input[type="password"]', CREDENTIALS.password);
  await screenshot(page, '02-login-filled');

  await page.click('button[type="submit"]');
  await page.waitForURL('**/dashboard**', { timeout: 15000 });
  await sleep(3000);

  // ─── Dashboard ────────────────────────────────────────────────
  await screenshot(page, '03-dashboard');

  // Scroll to air quality panel
  const aqPanel = page.locator('text=Qualidade do Ar').first();
  if (await aqPanel.isVisible()) {
    await aqPanel.scrollIntoViewIfNeeded();
    await sleep(500);
    await screenshot(page, '04-dashboard-air-quality');
  }

  // ─── Map ──────────────────────────────────────────────────────
  await page.goto(`${BASE}/map`, { waitUntil: 'networkidle' });
  await sleep(4000);
  await screenshot(page, '05-map');

  // Click on first event marker to show popup
  const eventMarker = page.locator('.custom-marker').first();
  if (await eventMarker.isVisible()) {
    await eventMarker.click({ force: true });
    await sleep(1000);
    await screenshot(page, '06-map-event-popup');
  }

  // ─── Terrain ──────────────────────────────────────────────────
  await page.goto(`${BASE}/terrain`, { waitUntil: 'networkidle' });
  await sleep(4000);
  await screenshot(page, '07-terrain');

  // ─── Risk Zones ───────────────────────────────────────────────
  await page.goto(`${BASE}/risk-zones`, { waitUntil: 'networkidle' });
  await sleep(4000);
  await screenshot(page, '08-risk-zones');

  // ─── Fire Spread ──────────────────────────────────────────────
  await page.goto(`${BASE}/fire-spread`, { waitUntil: 'networkidle' });
  await sleep(3000);
  await screenshot(page, '09-fire-spread');

  // Select first fire event if dropdown exists
  const selectTrigger = page.locator('button:has-text("Selecione"), select').first();
  if (await selectTrigger.isVisible()) {
    await selectTrigger.click();
    await sleep(500);
    const firstOption = page.locator('[role="option"], option').first();
    if (await firstOption.isVisible()) {
      await firstOption.click();
      await sleep(3000);
      await screenshot(page, '10-fire-spread-simulation');
    }
  }

  // ─── Air Quality ─────────────────────────────────────────────
  await page.goto(`${BASE}/air-quality`, { waitUntil: 'networkidle' });
  await sleep(5000);
  await screenshot(page, '11-air-quality');

  // Scroll to map section
  const aqMapText = page.locator('text=Estação de Monitorização').first();
  if (await aqMapText.isVisible()) {
    await aqMapText.scrollIntoViewIfNeeded();
    await sleep(500);
    await screenshot(page, '12-air-quality-map');
  }

  // Click marker to show popup
  const aqMarker = page.locator('.aqi-station-marker').first();
  if (await aqMarker.isVisible()) {
    await aqMarker.click({ force: true });
    await sleep(1000);
    await screenshot(page, '13-air-quality-map-popup');
  }

  // Scroll to pollutant table
  const pollutantHeader = page.locator('text=Detalhes dos Poluentes').first();
  if (await pollutantHeader.isVisible()) {
    await pollutantHeader.scrollIntoViewIfNeeded();
    await sleep(500);
    await screenshot(page, '14-air-quality-pollutants');
  }

  // ─── Insights ─────────────────────────────────────────────────
  await page.goto(`${BASE}/insights`, { waitUntil: 'networkidle' });
  await sleep(3000);
  await screenshot(page, '15-insights');

  // ─── Alerts ───────────────────────────────────────────────────
  await page.goto(`${BASE}/alerts`, { waitUntil: 'networkidle' });
  await sleep(3000);
  await screenshot(page, '16-alerts');

  // Alert rules page
  await page.goto(`${BASE}/alerts/rules`, { waitUntil: 'networkidle' });
  await sleep(2000);
  await screenshot(page, '17-alert-rules');

  // ─── Settings ─────────────────────────────────────────────────
  await page.goto(`${BASE}/settings`, { waitUntil: 'networkidle' });
  await sleep(2000);
  await screenshot(page, '18-settings');

  // ─── Sidebar collapsed ────────────────────────────────────────
  await page.goto(`${BASE}/dashboard`, { waitUntil: 'networkidle' });
  await sleep(2000);
  const collapseBtn = page.locator('button[title="Recolher"], button[title="Expandir"]').first();
  if (await collapseBtn.isVisible()) {
    await collapseBtn.click();
    await sleep(500);
    await screenshot(page, '19-sidebar-collapsed');
  }

  // ─── Mobile view ──────────────────────────────────────────────
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto(`${BASE}/dashboard`, { waitUntil: 'networkidle' });
  await sleep(3000);
  await screenshot(page, '20-mobile-dashboard');

  await page.goto(`${BASE}/air-quality`, { waitUntil: 'networkidle' });
  await sleep(4000);
  await screenshot(page, '21-mobile-air-quality');

  // Reset viewport
  await page.setViewportSize(VIEWPORT);

  console.log('\n✅ All screenshots captured!\n');

  await browser.close();
}

main().catch(err => {
  console.error('❌ Error:', err.message);
  process.exit(1);
});
