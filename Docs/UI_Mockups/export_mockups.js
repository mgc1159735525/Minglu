const fs = require("fs");
const path = require("path");
const { chromium } = require("playwright-core");
const sharp = require("sharp");

const outDir = __dirname;
const htmlPath = path.join(outDir, "mockups.html");

const shots = [
  ["title", "01_title.png", "标题界面"],
  ["create", "02_create_character.png", "创建角色"],
  ["academy", "03_academy_home.png", "学院养成"],
  ["event", "04_event_choice_popup.png", "事件选择弹窗"],
  ["relationship", "05_relationship_personality_popup.png", "角色/性格弹窗"],
  ["newspaper", "06_newspaper_stance.png", "报纸与立场"],
  ["strategy", "07_strategy_map.png", "战略地图"],
  ["battle", "08_battle_confirm.png", "战棋确认"]
];

async function exportScreens() {
  const edgePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
  const launchOptions = { headless: true };
  if (fs.existsSync(edgePath)) {
    launchOptions.executablePath = edgePath;
  }
  const browser = await chromium.launch(launchOptions);
  const page = await browser.newPage({
    viewport: { width: 1280, height: 720 },
    deviceScaleFactor: 1
  });

  await page.goto(`file:///${htmlPath.replace(/\\/g, "/")}`, { waitUntil: "load" });

  for (const [id, fileName] of shots) {
    const locator = page.locator(`#${id}`);
    await locator.screenshot({ path: path.join(outDir, fileName) });
  }

  await browser.close();
}

async function makeContactSheet() {
  const thumbW = 512;
  const thumbH = 288;
  const gap = 24;
  const labelH = 38;
  const cols = 2;
  const rows = Math.ceil(shots.length / cols);
  const sheetW = cols * thumbW + (cols + 1) * gap;
  const sheetH = rows * (thumbH + labelH) + (rows + 1) * gap;

  const composites = [];
  for (let i = 0; i < shots.length; i += 1) {
    const [, fileName, label] = shots[i];
    const col = i % cols;
    const row = Math.floor(i / cols);
    const x = gap + col * (thumbW + gap);
    const y = gap + row * (thumbH + labelH + gap);
    const image = await sharp(path.join(outDir, fileName)).resize(thumbW, thumbH).png().toBuffer();
    composites.push({ input: image, left: x, top: y });

    const labelSvg = Buffer.from(`
      <svg width="${thumbW}" height="${labelH}" xmlns="http://www.w3.org/2000/svg">
        <rect width="100%" height="100%" fill="#24151a"/>
        <text x="16" y="25" font-size="19" font-family="Microsoft YaHei, SimHei, sans-serif" fill="#f7e4b0">${label}</text>
      </svg>
    `);
    composites.push({ input: labelSvg, left: x, top: y + thumbH });
  }

  await sharp({
    create: {
      width: sheetW,
      height: sheetH,
      channels: 4,
      background: "#171313"
    }
  })
    .composite(composites)
    .png()
    .toFile(path.join(outDir, "00_ui_mockup_overview.png"));
}

async function main() {
  if (!fs.existsSync(htmlPath)) {
    throw new Error(`Missing ${htmlPath}`);
  }

  await exportScreens();
  await makeContactSheet();

  for (const [, fileName] of shots) {
    console.log(path.join(outDir, fileName));
  }
  console.log(path.join(outDir, "00_ui_mockup_overview.png"));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
