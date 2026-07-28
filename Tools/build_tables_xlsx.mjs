import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const projectRoot = path.resolve(__dirname, "..");
const csvDir = path.join(projectRoot, "DataTables", "csv");
const outDir = path.join(projectRoot, "DataTables");
const previewDir = path.join(projectRoot, ".codex-table-previews");

const sheetNames = {
  ui_texts: "UI文案",
  player_defaults: "玩家初始值",
  calendar: "校历",
  traits: "特性",
  character_origins: "创角出身",
  creation_memories: "创角往事",
  creation_talents: "创角天赋",
  creation_subjects: "创角学科",
  news: "报纸",
  campus_activities: "周末活动",
  narrative_fragments: "剧情碎片",
  courses: "课程",
  mood_rules: "心情规则",
  academy_levels: "属性等级",
  academy_core: "学院核心数值",
  exam_rewards: "校考奖励",
  ranks: "军衔",
  relationship_levels: "关系等级",
  belief_levels: "信念等级",
  factions: "势力显示",
  ideology_axes: "立场轴",
  politics_options: "讲座选项",
  relationships: "初始关系",
  stances: "初始立场",
  provinces: "战略省份",
  armies: "战略军团",
  battle_roles: "战斗兵种",
  common_units: "常见单位",
  terrain_rules: "地形规则",
  battle_unit_spawns: "战斗出场",
  battle_terrain_tiles: "战斗地形格",
  battle_role_damage_rules: "兵种克制",
  health_factors: "兵力伤害系数",
  battle_core: "战斗核心",
  story_characters: "剧情角色",
  story_events: "剧情事件",
  story_lines: "剧情对白",
  story_choices: "剧情选项"
};

function parseCsv(text) {
  const rows = [];
  let row = [];
  let cell = "";
  let quoted = false;
  for (let i = 0; i < text.length; i += 1) {
    const ch = text[i];
    const next = text[i + 1];
    if (quoted) {
      if (ch === '"' && next === '"') {
        cell += '"';
        i += 1;
      } else if (ch === '"') {
        quoted = false;
      } else {
        cell += ch;
      }
    } else if (ch === '"') {
      quoted = true;
    } else if (ch === ",") {
      row.push(cell);
      cell = "";
    } else if (ch === "\n") {
      row.push(cell);
      rows.push(row);
      row = [];
      cell = "";
    } else if (ch !== "\r") {
      cell += ch;
    }
  }
  if (cell.length > 0 || row.length > 0) {
    row.push(cell);
    rows.push(row);
  }
  return rows.filter((r) => r.some((v) => v !== ""));
}

function colName(index) {
  let n = index + 1;
  let s = "";
  while (n > 0) {
    const r = (n - 1) % 26;
    s = String.fromCharCode(65 + r) + s;
    n = Math.floor((n - 1) / 26);
  }
  return s;
}

function sanitizeTableName(name) {
  return `T_${name.replace(/[^A-Za-z0-9_]/g, "_")}`.slice(0, 60);
}

await fs.mkdir(outDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const workbook = Workbook.create();
const csvFiles = (await fs.readdir(csvDir))
  .filter((name) => name.endsWith(".csv"))
  .sort((a, b) => Object.keys(sheetNames).indexOf(a.replace(/\.csv$/, "")) - Object.keys(sheetNames).indexOf(b.replace(/\.csv$/, "")));

for (const file of csvFiles) {
  const base = file.replace(/\.csv$/, "");
  const sheetName = sheetNames[base] || base.slice(0, 31);
  const sheet = workbook.worksheets.add(sheetName.slice(0, 31));
  sheet.showGridLines = false;
  const matrix = parseCsv(await fs.readFile(path.join(csvDir, file), "utf8"));
  if (matrix.length === 0) {
    sheet.getRange("A1").values = [["空表"]];
    continue;
  }
  const rows = matrix.length;
  const cols = Math.max(...matrix.map((r) => r.length));
  const padded = matrix.map((r) => [...r, ...Array(cols - r.length).fill("")]);
  const range = sheet.getRangeByIndexes(0, 0, rows, cols);
  range.values = padded;
  sheet.freezePanes.freezeRows(1);

  const header = sheet.getRangeByIndexes(0, 0, 1, cols);
  header.format = {
    fill: "#603044",
    font: { bold: true, color: "#FFF4D8" },
    borders: { preset: "outside", style: "thin", color: "#C69A4C" }
  };
  if (rows > 1) {
    const dataRange = sheet.getRangeByIndexes(1, 0, rows - 1, cols);
    dataRange.format = {
      fill: "#F7EDD8",
      font: { color: "#211917" },
      borders: { insideHorizontal: { style: "thin", color: "#E3D2B4" } }
    };
  }
  range.format.wrapText = true;
  range.format.autofitColumns();
  range.format.autofitRows();
  const tableRange = `A1:${colName(cols - 1)}${rows}`;
  try {
    const table = sheet.tables.add(tableRange, true, sanitizeTableName(base));
    table.style = "TableStyleMedium2";
  } catch {
    // Table creation is a convenience; the raw sheet remains valid if a name/range is rejected.
  }
}

const overview = workbook.worksheets.add("说明");
overview.showGridLines = false;
overview.getRange("A1:D1").merge();
overview.getRange("A1").values = [["《明路》导表总览"]];
overview.getRange("A1").format = { fill: "#24151A", font: { bold: true, color: "#F7E4B0" } };
overview.getRange("A3:D6").values = [
  ["使用方式", "1. 改 DataTables/csv 或本 Excel 对应工作表。", "", ""],
  ["导出", "双击 导出配置表.bat 会从 JSON 生成 CSV。", "", ""],
  ["回写", "双击 回写配置表.bat 会从 CSV 写回 JSON。", "", ""],
  ["运行时", "Unity 游戏读取 Assets/Resources/Data/MingLuGameConfig.json 和 MingLuStoryData.json。", "", ""]
];
overview.getRange("A3:D6").format = { fill: "#F7EDD8", font: { color: "#211917" }, borders: { preset: "all", style: "thin", color: "#E3D2B4" } };
overview.getRange("A:D").format.autofitColumns();

const sheetList = await workbook.inspect({ kind: "sheet", include: "id,name" });
console.log(sheetList.ndjson);
const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "formula error scan"
});
console.log(errors.ndjson);

for (const sheet of workbook.worksheets.items) {
  const preview = await workbook.render({ sheetName: sheet.name, autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(path.join(previewDir, `${sheet.name}.png`), new Uint8Array(await preview.arrayBuffer()));
}

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(path.join(outDir, "MingLu_GameTables.xlsx"));
console.log(path.join(outDir, "MingLu_GameTables.xlsx"));
