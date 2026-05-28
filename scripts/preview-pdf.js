const { chromium } = require('playwright');
const path = require('path');

const HTML_FILE = path.join(__dirname, '..', 'docs', 'manual-print.html');
const OUT = path.join(__dirname, '..', 'docs', 'screenshots');

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 794, height: 1123 } }); // A4 at 96dpi

  await page.goto('file:///' + HTML_FILE.replace(/\\/g, '/'), { waitUntil: 'networkidle', timeout: 30000 });
  await page.waitForTimeout(2000);

  // Screenshot cover page
  await page.screenshot({ path: path.join(OUT, 'pdf-preview-cover.png'), fullPage: false });

  // Scroll down to TOC
  await page.evaluate(() => {
    const toc = document.querySelector('.toc');
    if (toc) toc.scrollIntoView();
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(OUT, 'pdf-preview-toc.png'), fullPage: false });

  // Scroll to first content section
  await page.evaluate(() => {
    const h2 = document.querySelectorAll('h2')[0];
    if (h2) h2.scrollIntoView();
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(OUT, 'pdf-preview-section1.png'), fullPage: false });

  // Scroll to a section with images
  await page.evaluate(() => {
    const figs = document.querySelectorAll('figure');
    if (figs.length > 2) figs[2].scrollIntoView({ block: 'center' });
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(OUT, 'pdf-preview-images.png'), fullPage: false });

  // Scroll to a table section
  await page.evaluate(() => {
    const tables = document.querySelectorAll('table');
    if (tables.length > 2) tables[2].scrollIntoView({ block: 'center' });
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(OUT, 'pdf-preview-table.png'), fullPage: false });

  console.log('PDF preview screenshots captured!');
  await browser.close();
}

main().catch(err => { console.error('Error:', err.message); process.exit(1); });
