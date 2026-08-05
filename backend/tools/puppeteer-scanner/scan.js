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
    throw new Error('Usage: scan.js --url <url> --out <path> [--browser chrome|edge] [--headless true|false] [--screenshot-dir <dir>] [--label <name>] [--locator-config <json>] [--load-timeout-ms <ms>] [--network-idle-timeout-ms <ms>]');
  }

  const headless = args.headless !== 'false';
  const browser = (args.browser || 'chrome').toLowerCase();
  const loadTimeoutMs = args['load-timeout-ms'] ? Number(args['load-timeout-ms']) : 30_000;
  const networkIdleTimeoutMs = args['network-idle-timeout-ms'] ? Number(args['network-idle-timeout-ms']) : 3_000;

  const launchOptions = { headless };
  if (browser === 'edge') {
    launchOptions.executablePath = resolveEdgeExecutable();
  }

  const browserInstance = await puppeteer.launch(launchOptions);
  try {
    const page = await browserInstance.newPage();
    // 'load' alone guarantees we never hang on pages with persistent
    // connections (websockets, chat widgets, analytics beacons) - networkidle0
    // as the primary gate can time out on those even once the page is
    // visually done. The short waitForNetworkIdle afterwards is a best-effort
    // grace period for SPAs that fetch their real content after the load
    // event; if it doesn't settle in time we proceed with whatever rendered.
    await page.goto(url, { waitUntil: 'load', timeout: loadTimeoutMs });
    await page.waitForNetworkIdle({ idleTime: 500, timeout: networkIdleTimeoutMs }).catch(() => {});

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

    let locatorConfig;
    try {
      locatorConfig = args['locator-config'] ? JSON.parse(args['locator-config']) : undefined;
    } catch {
      throw new Error('--locator-config must be valid JSON.');
    }

    const { elements, dynamicSuggestions } = await page.evaluate(extractElements, locatorConfig);

    fs.mkdirSync(path.dirname(outPath), { recursive: true });
    fs.writeFileSync(outPath, JSON.stringify({ elements, dynamicSuggestions, screenshotPath }), 'utf8');
  } finally {
    await browserInstance.close();
  }
}

main().catch((error) => {
  process.stderr.write(`${error && error.message ? error.message : error}\n`);
  process.exit(1);
});
