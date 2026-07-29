'use strict';

const fs = require('fs');
const path = require('path');
const puppeteer = require('puppeteer-core');
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

function resolveBrowserExecutable(browser) {
  const configuration = browser === 'chrome'
    ? {
        displayName: 'Google Chrome or Chromium',
        environmentVariables: ['CLIQ_CHROME_PATH', 'CLIQ_BROWSER_PATH', 'PUPPETEER_EXECUTABLE_PATH'],
        candidates: [
          path.join(process.env.ProgramFiles || 'C:\\Program Files', 'Google', 'Chrome', 'Application', 'chrome.exe'),
          path.join(process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)', 'Google', 'Chrome', 'Application', 'chrome.exe'),
          process.env.LOCALAPPDATA
            ? path.join(process.env.LOCALAPPDATA, 'Google', 'Chrome', 'Application', 'chrome.exe')
            : undefined,
          '/usr/bin/google-chrome-stable',
          '/usr/bin/google-chrome',
          '/opt/google/chrome/chrome',
          '/usr/bin/chromium',
          '/usr/bin/chromium-browser',
          '/snap/bin/chromium',
          '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
        ],
      }
    : browser === 'edge'
      ? {
          displayName: 'Microsoft Edge',
          environmentVariables: ['CLIQ_EDGE_PATH', 'CLIQ_BROWSER_PATH', 'PUPPETEER_EXECUTABLE_PATH'],
          candidates: [
            path.join(process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)', 'Microsoft', 'Edge', 'Application', 'msedge.exe'),
            path.join(process.env.ProgramFiles || 'C:\\Program Files', 'Microsoft', 'Edge', 'Application', 'msedge.exe'),
            process.env.LOCALAPPDATA
              ? path.join(process.env.LOCALAPPDATA, 'Microsoft', 'Edge', 'Application', 'msedge.exe')
              : undefined,
            '/usr/bin/microsoft-edge-stable',
            '/usr/bin/microsoft-edge',
            '/opt/microsoft/msedge/msedge',
            '/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge',
          ],
        }
      : undefined;

  if (!configuration) {
    throw new Error(`Unsupported browser '${browser}'. Expected 'chrome' or 'edge'.`);
  }

  for (const variable of configuration.environmentVariables) {
    const configuredPath = process.env[variable];
    if (!configuredPath) continue;
    if (fs.existsSync(configuredPath)) return configuredPath;
    throw new Error(`${variable} points to a browser executable that does not exist.`);
  }

  const found = configuration.candidates.find((candidate) => candidate && fs.existsSync(candidate));
  if (!found) {
    throw new Error(
      `Could not locate ${configuration.displayName}. Set ${configuration.environmentVariables[0]} to its executable path.`
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

  const launchOptions = {
    headless,
    executablePath: resolveBrowserExecutable(browser),
  };

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

    const elements = await page.evaluate(extractElements, locatorConfig);

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
