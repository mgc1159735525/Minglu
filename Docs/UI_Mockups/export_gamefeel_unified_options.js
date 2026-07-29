const fs = require("fs");
const path = require("path");
const { chromium } = require("playwright-core");
const sharp = require("sharp");

const outDir = __dirname;
const htmlPath = path.join(outDir, "redesign_gamefeel_unified_options.html");

const shots = [
  ["unified-a", "gamefeel_unified_a_heavy.png", "统一A 厚重养成"],
  ["unified-b", "gamefeel_unified_b_tactical.png", "统一B 战备养成"],
  ["unified-c", "gamefeel_unified_c_clean.png", "统一C 清爽养成"]
];

async function exportScreens() {
  const edgePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
  const launchOptions = { headless: true };
  if (fs.existsSync(edgePath)) launchOptions.executablePath = edgePath;

  const browser = await chromium.launch(launchOptions);
  const page = await browser.newPage({
    viewport: { width: 1280, height: 720 },
    deviceScaleFactor: 1
  });
  await page.goto(`file:///${htmlPath.replace(/\\/g, "/")}`, { waitUntil: "networkidle" });

  for (const [id, fileName] of shots) {
    await page.locator(`#${id}`).screenshot({ path: path.join(outDir, fileName) });
  }

  await browser.close();
}

async function makeContactSheet() {
  const thumbW = 512;
  const thumbH = 288;
  const gap = 24;
  const labelH = 42;
  const sheetW = shots.length * thumbW + (shots.length + 1) * gap;
  const sheetH = thumbH + labelH + gap * 2;
  const composites = [];

  for (let i = 0; i < shots.length; i += 1) {
    const [, fileName, label] = shots[i];
    const x = gap + i * (thumbW + gap);
    const image = await sharp(path.join(outDir, fileName)).resize(thumbW, thumbH).png().toBuffer();
    composites.push({ input: image, left: x, top: gap });
    const labelSvg = Buffer.from(`
      <svg width="${thumbW}" height="${labelH}" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#120d0a"/>
        <text x="16" y="28" font-size="20" font-family="Microsoft YaHei, SimHei, sans-serif" fill="#f2dfb5">${label}</text>
      </svg>
    `);
    composites.push({ input: labelSvg, left: x, top: gap + thumbH });
  }

  await sharp({
    create: {
      width: sheetW,
      height: sheetH,
      channels: 4,
      background: "#0b0908"
    }
  })
    .composite(composites)
    .png()
    .toFile(path.join(outDir, "gamefeel_unified_options_overview.png"));
}

async function main() {
  if (!fs.existsSync(htmlPath)) throw new Error(`Missing ${htmlPath}`);
  await exportScreens();
  await makeContactSheet();
  for (const [, fileName] of shots) console.log(path.join(outDir, fileName));
  console.log(path.join(outDir, "gamefeel_unified_options_overview.png"));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
