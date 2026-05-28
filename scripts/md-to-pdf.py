"""
Seixal Risk Monitor — Markdown to Professional PDF Converter
Uses: markdown (MD→HTML) + Playwright (HTML→PDF via Chromium)

WeasyPrint requires GTK/Pango which isn't available on this Windows system.
Playwright's built-in PDF generation provides equivalent quality with full CSS support.
"""

import os
import re
import base64
import json
import subprocess
import sys
from pathlib import Path
from datetime import datetime
from markdown import markdown

# ── Paths ──────────────────────────────────────────────────────────────────
BASE_DIR = Path(__file__).resolve().parent.parent
MD_FILE = BASE_DIR / "docs" / "Manual_Seixal_Risk_Monitor.md"
CSS_FILE = Path(__file__).resolve().parent / "pdf-template.css"
SCREENSHOTS_DIR = BASE_DIR / "docs" / "screenshots"
OUTPUT_HTML = BASE_DIR / "docs" / "manual-print.html"
OUTPUT_PDF = BASE_DIR / "docs" / "Manual_Seixal_Risk_Monitor.pdf"

# ── Date ───────────────────────────────────────────────────────────────────
MESES_PT = {
    1: "janeiro", 2: "fevereiro", 3: "março", 4: "abril",
    5: "maio", 6: "junho", 7: "julho", 8: "agosto",
    9: "setembro", 10: "outubro", 11: "novembro", 12: "dezembro",
}
now = datetime.now()
DATE_STR = f"{now.day} de {MESES_PT[now.month]} de {now.year}"


def load_css() -> str:
    """Load CSS template and inject dynamic date."""
    css = CSS_FILE.read_text(encoding="utf-8")
    css = css.replace("28 de Maio de 2026", DATE_STR)
    # Remove duplicate @page block
    css = re.sub(
        r'/\*\s*Alternative:\s*use CSS to set a fixed date\s*\*/\s*@page\s*\{[^}]*@bottom-right\s*\{[^}]*\}[^}]*\}',
        '',
        css,
        flags=re.DOTALL
    )
    return css


def encode_image(img_path: Path) -> str:
    """Encode an image file to base64 data URI."""
    mime_map = {
        ".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg",
        ".gif": "image/gif", ".svg": "image/svg+xml",
    }
    ext = img_path.suffix.lower()
    mime = mime_map.get(ext, "image/png")
    data = img_path.read_bytes()
    b64 = base64.b64encode(data).decode("ascii")
    return f"data:{mime};base64,{b64}"


def process_images(html: str) -> str:
    """Convert <img> tags to embedded base64 images in <figure> with <figcaption>."""
    img_pattern = re.compile(
        r'<img\s+alt="([^"]*)"\s+src="([^"]+)"(?:\s+title="([^"]*)")?\s*/?>',
        re.IGNORECASE
    )

    def replace_img(match):
        alt = match.group(1)
        src = match.group(2)
        title = match.group(3) or alt

        img_path = (BASE_DIR / "docs" / src).resolve()

        if img_path.exists():
            data_uri = encode_image(img_path)
            return (
                f'<figure>\n'
                f'  <img src="{data_uri}" alt="{alt}" />\n'
                f'  <figcaption>{title}</figcaption>\n'
                f'</figure>'
            )
        else:
            return (
                f'<div style="border:2px dashed #ef4444;padding:10mm;'
                f'text-align:center;margin:5mm 0;color:#ef4444;'
                f'font-style:italic;">'
                f'[Imagem não encontrada: {src}]'
                f'</div>'
            )

    return img_pattern.sub(replace_img, html)


def build_cover_page() -> str:
    """Generate the cover page HTML."""
    return f'''
    <div class="cover-page">
      <div class="cover-page__badge">Manual do Utilizador</div>
      <div class="cover-page__title">Seixal<br/>Risk Monitor</div>
      <div class="cover-page__accent-bar"></div>
      <div class="cover-page__subtitle">
        Plataforma de Monitorização de Riscos Geográficos<br/>
        Concelho do Seixal
      </div>
      <div class="cover-page__meta">
        <span class="cover-page__version">Versão 1.0</span>
        <span>{DATE_STR}</span>
        <span>Aplicação Web (Docker)</span>
      </div>
    </div>
    '''


def build_toc(md_content: str) -> str:
    """Extract h2 headings and build a styled table of contents."""
    toc_items = []
    heading_pattern = re.compile(r'^##\s+(\d+)\.\s+(.+)$', re.MULTILINE)

    for match in heading_pattern.finditer(md_content):
        num = match.group(1)
        title = match.group(2).strip()
        toc_items.append(
            f'<li>\n'
            f'  <span class="toc__item toc__item--chapter">{num}.</span>\n'
            f'  <span class="toc__leaders"></span>\n'
            f'  <span class="toc__page">{title}</span>\n'
            f'</li>'
        )

    toc_html = '\n'.join(toc_items)
    return f'''
    <div class="toc">
      <div class="toc__title">Índice</div>
      <ul class="toc__list">
        {toc_html}
      </ul>
    </div>
    '''


def convert_tables(html: str) -> str:
    """Add class to tables for styling."""
    return html.replace('<table>', '<table class="data-table">')


def convert_md_to_html(md_content: str) -> str:
    """Convert markdown to styled HTML with cover, TOC, and processed images."""

    # Split by --- to isolate sections
    parts = md_content.split('---', 2)
    main_content = parts[2] if len(parts) >= 3 else md_content

    # Remove the original h1 title and TOC section
    main_content = re.sub(r'^# Manual do Utilizador.*\n', '', main_content)

    # Remove metadata block
    main_content = re.sub(r'^\*\*Versão:.*?\n\n', '', main_content, flags=re.DOTALL)

    # Remove the original TOC
    main_content = re.sub(r'## Índice\s*\n(?:.*?\n)*?(?=\n---|\n## )', '', main_content, flags=re.DOTALL)

    # Convert markdown to HTML
    extensions = ['tables', 'fenced_code', 'sane_lists', 'attr_list']
    html_body = markdown(main_content, extensions=extensions)

    # Post-process
    html_body = process_images(html_body)
    html_body = convert_tables(html_body)

    # Assemble final page
    cover = build_cover_page()
    toc = build_toc(md_content)

    return cover + toc + html_body


def build_full_html(html_content: str, css: str) -> str:
    """Build the complete HTML document with embedded CSS."""
    return f'''<!DOCTYPE html>
<html lang="pt-PT">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Manual do Utilizador — Seixal Risk Monitor</title>
  <style>
    /* Reset for consistent rendering */
    *, *::before, *::after {{ box-sizing: border-box; margin: 0; padding: 0; }}

    {css}
  </style>
</head>
<body>
{html_content}
</body>
</html>'''


def generate_pdf_playwright(html_path: Path, output_pdf: Path):
    """Use Playwright to print HTML to PDF with full CSS support."""
    playwright_script = f'''
const {{ chromium }} = require('playwright');

(async () => {{
  const browser = await chromium.launch({{ headless: true }});
  const page = await browser.newPage();

  await page.goto('file:///{str(html_path).replace(os.sep, "/")}', {{
    waitUntil: 'networkidle',
    timeout: 30000
  }});

  // Wait for images to load
  await page.waitForTimeout(2000);

  await page.pdf({{
    path: '{str(output_pdf).replace(os.sep, "/")}',
    format: 'A4',
    margin: {{
      top: '20mm',
      bottom: '20mm',
      left: '18mm',
      right: '18mm'
    }},
    printBackground: true,
    displayHeaderFooter: true,
    headerTemplate: `
      <div style="width:100%;font-size:8px;color:#475569;padding:0 18mm;display:flex;justify-content:space-between;border-bottom:1px solid #10b981;padding-bottom:4mm;">
        <span>Manual do Utilizador</span>
        <span>Seixal Risk Monitor</span>
      </div>
    `,
    footerTemplate: `
      <div style="width:100%;font-size:8px;color:#94a3b8;padding:0 18mm;display:flex;justify-content:space-between;padding-top:4mm;">
        <span>&copy; 2026 Seixal Risk Monitor</span>
        <span class="pageNumber"></span>
        <span>{DATE_STR}</span>
      </div>
    `,
    preferCSSPageSize: false
  }});

  console.log('PDF generated: {str(output_pdf).replace(os.sep, "/")}');
  await browser.close();
}})().catch(err => {{
  console.error('Error:', err.message);
  process.exit(1);
}});
'''

    # Write the Node script
    script_path = BASE_DIR / "scripts" / "_playwright-pdf.js"
    script_path.write_text(playwright_script, encoding="utf-8")

    # Run it
    env = os.environ.copy()
    env["NODE_PATH"] = str(BASE_DIR / "web" / "node_modules")

    result = subprocess.run(
        ["node", str(script_path)],
        capture_output=True,
        text=True,
        env=env,
        cwd=str(BASE_DIR),
        timeout=60
    )

    if result.returncode != 0:
        print(f"  ❌ Playwright error: {result.stderr}")
        raise RuntimeError(f"Playwright PDF generation failed: {result.stderr}")
    else:
        print(f"  {result.stdout.strip()}")

    # Clean up temp script
    script_path.unlink(missing_ok=True)


def main():
    print("=" * 60)
    print("  Seixal Risk Monitor — PDF Manual Generator")
    print("=" * 60)
    print()

    # 1. Load source files
    print("[1/5] Loading source files...")
    md_content = MD_FILE.read_text(encoding="utf-8")
    css = load_css()
    print(f"  Markdown: {len(md_content):,} chars")
    print(f"  CSS:      {len(css):,} chars")
    print()

    # 2. Convert MD → HTML
    print("[2/5] Converting Markdown to HTML...")
    html_body = convert_md_to_html(md_content)
    fig_count = html_body.count('<figure>')
    missing_count = html_body.count('[Imagem não encontrada')
    print(f"  Figures embedded: {fig_count}")
    if missing_count:
        print(f"  ⚠ Images missing: {missing_count}")
    print()

    # 3. Build full HTML document
    print("[3/5] Building full HTML document with CSS...")
    full_html = build_full_html(html_body, css)
    print(f"  Total HTML size: {len(full_html):,} chars")
    print()

    # 4. Save intermediate HTML
    print(f"[4/5] Saving intermediate HTML...")
    OUTPUT_HTML.write_text(full_html, encoding="utf-8")
    print(f"  Saved to: {OUTPUT_HTML}")
    print()

    # 5. Generate PDF
    print(f"[5/5] Generating PDF via Playwright...")
    generate_pdf_playwright(OUTPUT_HTML, OUTPUT_PDF)

    size_mb = OUTPUT_PDF.stat().st_size / (1024 * 1024)
    print()
    print("=" * 60)
    print(f"  [OK] PDF generated successfully!")
    print(f"  Path: {OUTPUT_PDF}")
    print(f"  Size: {size_mb:.2f} MB")
    print("=" * 60)


if __name__ == "__main__":
    main()
