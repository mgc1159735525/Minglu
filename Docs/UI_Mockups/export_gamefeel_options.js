const fs = require("fs");
const path = require("path");
const { chromium } = require("playwright-core");
const sharp = require("sharp");

const outDir = __dirname;
const htmlPath = path.join(outDir, "redesign_gamefeel_options.html");

const shots = [
  ["game-a", "gamefeel_option_a_schedule_adventure.png", "游戏感A 日程冒险"],
  ["game-b", "gamefeel_option_b_growth_board.png", "游戏感B 成长棋盘"],
  ["game-c", "gamefeel_option_c_quest_journal.png", "游戏感C 任务手账"]
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
    .toFile(path.join(outDir, "gamefeel_options_overview.png"));
}

async function main() {
  if (!fs.existsSync(htmlPath)) throw new Error(`Missing ${htmlPath}`);
  await exportScreens();
  await makeContactSheet();
  for (const [, fileName] of shots) console.log(path.join(outDir, fileName));
  console.log(path.join(outDir, "gamefeel_options_overview.png"));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
