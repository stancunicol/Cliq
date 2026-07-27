'use strict';

const fs = require('fs');
const path = require('path');
const puppeteer = require('puppeteer');
const { extractElements } = require('./extract-elements');

function parseArgs(argv) {
  const args = {};
  for (let i = 0; i < argv.length; i += 1) {
    const token = argv[i];
    if (!token.startsWith('--')) continue;
    const key = token.slice(2);
    const value = argv[i + 1];
    args[key] = value;
    i += 1;
  }
  return args;
}

// Puppeteer only bundles Chromium; a real Edge install has to be located on
// disk and launched via executablePath, the same way Selenium's EdgeDriver
// pointed at the system browser.
function resolveEdgeExecutable() {
  if (process.env.CLIQ_EDGE_PATH) return process.env.CLIQ_EDGE_PATH;

  const candidates = [
    path.join(process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)', 'Microsoft', 'Edge', 'Application', 'msedge.exe'),
    path.join(process.env.ProgramFiles || 'C:\\Program Files', 'Microsoft', 'Edge', 'Application', 'msedge.exe'),
    '/usr/bin/microsoft-edge',
    '/opt/microsoft/msedge/msedge',
  ];

  const found = candidates.find((candidate) => fs.existsSync(candidate));
  if (!found) {
    throw new Error(
      'Could not locate an installed Microsoft Edge executable. Set CLIQ_EDGE_PATH to override.'
    );
  }
  return found;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const url = args.url;
  const outPath = args.out;
  if (!url || !outPath) {
    throw new Error('Usage: scan.js --url <url> --out <path> [--browser chrome|edge] [--headless true|false] [--screenshot-dir <dir>] [--label <name>]');
  }

  const headless = args.headless !== 'false';
  const browser = (args.browser || 'chrome').toLowerCase();

  const launchOptions = { headless };
  if (browser === 'edge') {
    launchOptions.executablePath = resolveEdgeExecutable();
  }

  const browserInstance = await puppeteer.launch(launchOptions);
  try {
    const page = await browserInstance.newPage();
    await page.goto(url, { waitUntil: 'networkidle0', timeout: 30_000 });

    let screenshotPath = null;
    if (args['screenshot-dir']) {
      fs.mkdirSync(args['screenshot-dir'], { recursive: true });
      const label = args.label || 'page';
      const stamp = new Date().toISOString().replace(/[:.]/g, '-');
      screenshotPath = path.join(args['screenshot-dir'], `${label}_discovered_${stamp}.png`);
      await page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {
        screenshotPath = null;
      });
    }

    const elements = await page.evaluate(extractElements);

    fs.mkdirSync(path.dirname(outPath), { recursive: true });
    fs.writeFileSync(outPath, JSON.stringify({ elements, screenshotPath }), 'utf8');
  } finally {
    await browserInstance.close();
  }
}

main().catch((error) => {
  process.stderr.write(`${error && error.message ? error.message : error}\n`);
  process.exit(1);
});
