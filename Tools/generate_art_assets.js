const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const projectRoot = path.resolve(__dirname, "..");
const storyPath = path.join(projectRoot, "Assets", "Resources", "MingLuStoryData.json");
const artRoot = path.join(projectRoot, "Assets", "Resources", "Art");
const portraitDir = path.join(artRoot, "Portraits");
const sceneDir = path.join(artRoot, "Scenes");
const uiDir = path.join(artRoot, "UI");
const manifestDir = path.join(artRoot, "Manifests");

for (const dir of [portraitDir, sceneDir, uiDir, manifestDir]) {
  fs.mkdirSync(dir, { recursive: true });
}

function hashText(value) {
  let h = 2166136261;
  const s = String(value || "");
  for (let i = 0; i < s.length; i += 1) {
    h ^= s.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return h >>> 0;
}

function pick(list, h, salt = 0) {
  return list[(h + salt) % list.length];
}

function clamp(v, min, max) {
  return Math.max(min, Math.min(max, v));
}

function xml(value) {
  return String(value || "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

const factionPalette = {
  "返乡团": ["#243854", "#c89a45", "#6b3134"],
  "陆军青壮派": ["#38563f", "#d0a352", "#7a3c30"],
  "印第安乡党": ["#6f4a32", "#d49a48", "#315f68"],
  "自由派": ["#314f75", "#d8c177", "#5f304b"],
  "法治派": ["#242b36", "#b98d46", "#5e2732"],
  "重要NPC（跨派系）": ["#4a334d", "#d2a951", "#253f5f"]
};

function characterProfile(character) {
  const text = `${character.name} ${character.identity} ${character.faction} ${character.traits} ${character.portrait} ${character.background}`;
  const h = hashText(text);
  const female = /夫人|太后|母亲|女子|少女|女|学姐|婉|素心|小满|小炮|新芽|半亩|秋霜|花·|溪·|月影|蛇语|鹿灵/.test(text);
  const old = /老|老太|年迈|白发|苍苍|太后|长老|老者|老将|老兵|遗老/.test(text);
  const child = /少年|少女|同窗|学生|学姐|之子|之女|孙|妹|侄女/.test(text) && !old;
  const native = /印第安|部落|酋长|祭司|鹿皮|编辫|面纹|鹰羽|雷鸟|地灵|水灵|草灵/.test(text);
  const european = /西班牙|英国|法国|欧洲|大英|传教士|总督|高鼻|金发|灰眼|十字/.test(text);
  const scholar = /教授|学者|书|笔|报纸|文人|儒|院长|主编|律师|县令|讼师/.test(text);
  const military = /军|将|兵|校|骑|炮|海军|陆军|战|甲|刀|剑|弓/.test(text);
  const noble = /皇|太子|贵|官|尚书|宗令|蟒袍|凤冠|礼服|世家|大理寺|御史/.test(text);
  const palette = factionPalette[character.faction] || factionPalette["重要NPC（跨派系）"];

  return {
    h,
    female,
    old,
    child,
    native,
    european,
    scholar,
    military,
    noble,
    skin: european ? pick(["#f0c5a8", "#e6b99c", "#d9aa8f"], h) :
      native ? pick(["#a86742", "#8f5638", "#b8744a"], h) :
      pick(["#efd0b8", "#e9c0a2", "#dca982", "#f3d7bd"], h),
    hair: old ? pick(["#d6d1c4", "#f1ead7", "#b8b4aa"], h) :
      european ? pick(["#d7b45f", "#6d4a32", "#b07a46"], h) :
      pick(["#241918", "#362220", "#4b3024", "#1d1a20"], h),
    primary: palette[0],
    gold: palette[1],
    accent: palette[2]
  };
}

function propSvg(character, profile) {
  const text = `${character.identity} ${character.portrait} ${character.traits}`;
  if (/弓|猎户|猎人|鹰眼/.test(text)) {
    return `<path d="M380 322 C438 402 438 560 380 636" fill="none" stroke="#5b3826" stroke-width="14"/><path d="M384 326 C410 440 410 520 384 632" fill="none" stroke="#ead6a7" stroke-width="3"/>`;
  }
  if (/炮|工装|工具|机械|船舰|研发|测距/.test(text)) {
    return `<rect x="352" y="462" width="92" height="22" rx="8" fill="#2c2c2c" stroke="#c49a50" stroke-width="5"/><circle cx="374" cy="473" r="13" fill="#c49a50"/><rect x="324" y="486" width="46" height="78" rx="8" fill="#423b32"/>`;
  }
  if (/书|学者|教授|院长|儒|报纸|笔|律师|法官|讼师|御史/.test(text)) {
    return `<rect x="332" y="468" width="86" height="116" rx="8" fill="#ead7b0" stroke="#5f4632" stroke-width="5"/><line x1="375" y1="476" x2="375" y2="576" stroke="#8e704c" stroke-width="3"/>`;
  }
  if (/扇/.test(text)) {
    return `<path d="M344 492 L434 438 L452 560 Z" fill="#e5c27a" stroke="#623036" stroke-width="5"/><path d="M354 490 L438 458 M366 506 L442 484 M378 522 L444 510" stroke="#6b3d3c" stroke-width="3"/>`;
  }
  if (/十字|传教士/.test(text)) {
    return `<rect x="383" y="430" width="12" height="82" fill="#d5b86c"/><rect x="354" y="454" width="70" height="12" fill="#d5b86c"/>`;
  }
  if (/算盘|账|商|田契|钱|珠/.test(text)) {
    return `<rect x="328" y="476" width="104" height="76" rx="8" fill="#5d3529" stroke="#c79b4e" stroke-width="5"/><line x1="340" y1="500" x2="420" y2="500" stroke="#e6c078" stroke-width="4"/><line x1="340" y1="524" x2="420" y2="524" stroke="#e6c078" stroke-width="4"/><circle cx="366" cy="500" r="8" fill="#e6c078"/><circle cx="394" cy="524" r="8" fill="#e6c078"/>`;
  }
  if (/剑|刀|佩剑|短刀|短剑|海盗|剑客|战斧|军法/.test(text) || profile.military) {
    return `<path d="M394 394 L424 618" stroke="#d6d6d0" stroke-width="13"/><path d="M374 422 L426 414" stroke="#c49a50" stroke-width="10"/><path d="M426 618 L442 648" stroke="#3b2523" stroke-width="14"/>`;
  }
  return "";
}

function portraitSvg(character) {
  const p = characterProfile(character);
  const h = p.h;
  const faceW = p.female ? 132 : 144;
  const faceH = p.old ? 156 : 146;
  const headX = 256 - faceW / 2 + ((h % 9) - 4);
  const headY = p.child ? 108 : 96;
  const shoulderW = p.military ? 300 : 278;
  const bodyTop = p.child ? 272 : 282;
  const bodyH = p.female ? 368 : 382;
  const eyeY = headY + 78;
  const mouthY = headY + 116;
  const hairLong = p.female && !p.old;
  const headdress = p.native || /羽|簪|凤冠|冠|帽|发簪|玉笏|法槌|眼罩|独眼/.test(`${character.portrait} ${character.identity}`);
  const medal = /将|军|兵|海军|陆军|勋章|军官|将官/.test(`${character.identity} ${character.portrait}`);
  const glasses = /眼镜|学者|教授|书生|法官|主编|新学/.test(`${character.identity} ${character.portrait}`);
  const scar = /刀疤|伤疤|旧伤|独眼|火药|海盗/.test(`${character.identity} ${character.portrait}`);

  const bodyShape = p.female
    ? `M${256 - shoulderW / 2} 690 C118 560 160 ${bodyTop} 256 ${bodyTop - 20} C352 ${bodyTop} 394 560 ${256 + shoulderW / 2} 690 Z`
    : `M${256 - shoulderW / 2} 690 C116 510 150 ${bodyTop} 256 ${bodyTop - 28} C362 ${bodyTop} 396 510 ${256 + shoulderW / 2} 690 Z`;

  const hairBack = hairLong
    ? `<path d="M${headX - 28} ${headY + 20} C124 180 116 430 206 500 L306 500 C398 420 390 178 ${headX + faceW + 28} ${headY + 22} Z" fill="${p.hair}" opacity="0.98"/>`
    : `<path d="M${headX - 24} ${headY + 32} C142 42 370 42 ${headX + faceW + 24} ${headY + 34} C360 70 358 144 344 184 C312 128 204 128 168 184 C152 148 148 78 ${headX - 24} ${headY + 32} Z" fill="${p.hair}"/>`;

  const nativeOrnament = p.native
    ? `<path d="M168 146 L132 70 L184 118 Z" fill="${p.gold}" stroke="#2b1b17" stroke-width="5"/><circle cx="166" cy="154" r="9" fill="#f2ead5"/><circle cx="346" cy="154" r="9" fill="#f2ead5"/><path d="M348 146 L384 70 L332 118 Z" fill="${p.gold}" stroke="#2b1b17" stroke-width="5"/>`
    : "";

  const nobleOrnament = p.noble
    ? `<path d="M188 88 L214 50 L246 84 L278 44 L316 88 Z" fill="${p.gold}" stroke="#3a2422" stroke-width="5"/>`
    : "";

  const hat = headdress && !p.native && !p.noble
    ? `<path d="M170 92 L342 92 L372 132 L140 132 Z" fill="${p.accent}" stroke="#251819" stroke-width="6"/>`
    : "";

  const wrinkles = p.old
    ? `<path d="M206 ${eyeY - 26} C226 ${eyeY - 34} 246 ${eyeY - 34} 264 ${eyeY - 26} M202 ${eyeY - 10} C230 ${eyeY - 18} 258 ${eyeY - 18} 286 ${eyeY - 10} M212 ${mouthY + 28} C244 ${mouthY + 40} 276 ${mouthY + 40} 308 ${mouthY + 28}" fill="none" stroke="#a36f5e" stroke-width="4" opacity="0.52"/>`
    : "";

  const prop = propSvg(character, p);

  return `
<svg width="512" height="768" viewBox="0 0 512 768" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="body" x1="0" x2="0" y1="0" y2="1">
      <stop offset="0" stop-color="${p.primary}"/>
      <stop offset="1" stop-color="#151820"/>
    </linearGradient>
    <radialGradient id="face" cx="45%" cy="32%" r="70%">
      <stop offset="0" stop-color="#ffe2ca"/>
      <stop offset="1" stop-color="${p.skin}"/>
    </radialGradient>
    <filter id="soft">
      <feDropShadow dx="0" dy="18" stdDeviation="14" flood-opacity="0.28"/>
    </filter>
  </defs>
  <g filter="url(#soft)">
    ${hairBack}
    <path d="${bodyShape}" fill="url(#body)" stroke="#211715" stroke-width="8"/>
    <path d="M132 438 C200 470 312 470 380 438 L366 496 C306 532 206 532 146 496 Z" fill="${p.accent}" opacity="0.72"/>
    <path d="M154 386 L358 386 L346 436 L166 436 Z" fill="${p.gold}" opacity="0.96"/>
    <path d="M210 268 L302 268 L318 390 C284 414 230 414 194 390 Z" fill="${p.skin}" stroke="#211715" stroke-width="6"/>
    <ellipse cx="256" cy="${headY + faceH / 2}" rx="${faceW / 2}" ry="${faceH / 2}" fill="url(#face)" stroke="#211715" stroke-width="8"/>
    <path d="M${headX + 12} ${headY + 46} C204 ${headY - 8} 308 ${headY - 8} ${headX + faceW - 8} ${headY + 48} C300 ${headY + 30} 218 ${headY + 30} ${headX + 12} ${headY + 46} Z" fill="${p.hair}"/>
    ${hat}${nativeOrnament}${nobleOrnament}
    <ellipse cx="218" cy="${eyeY}" rx="13" ry="7" fill="#211715"/>
    <ellipse cx="294" cy="${eyeY}" rx="13" ry="7" fill="#211715"/>
    ${glasses ? `<circle cx="218" cy="${eyeY}" r="22" fill="none" stroke="#2b2424" stroke-width="5"/><circle cx="294" cy="${eyeY}" r="22" fill="none" stroke="#2b2424" stroke-width="5"/><line x1="240" y1="${eyeY}" x2="272" y2="${eyeY}" stroke="#2b2424" stroke-width="5"/>` : ""}
    <path d="M254 ${eyeY + 12} C244 ${eyeY + 42} 246 ${eyeY + 56} 266 ${eyeY + 58}" fill="none" stroke="#9c604d" stroke-width="5" stroke-linecap="round"/>
    <path d="M220 ${mouthY} C242 ${mouthY + 14} 276 ${mouthY + 14} 300 ${mouthY}" fill="none" stroke="#643029" stroke-width="6" stroke-linecap="round"/>
    ${wrinkles}
    ${scar ? `<path d="M312 ${headY + 50} L286 ${headY + 122}" stroke="#8d392e" stroke-width="7" stroke-linecap="round"/><path d="M306 ${headY + 68} L322 ${headY + 74} M298 ${headY + 92} L314 ${headY + 98}" stroke="#f0b49e" stroke-width="3"/>` : ""}
    ${medal ? `<circle cx="214" cy="462" r="14" fill="${p.gold}" stroke="#33201d" stroke-width="4"/><rect x="240" y="448" width="42" height="28" fill="#c7483d" stroke="#33201d" stroke-width="4"/><rect x="292" y="448" width="42" height="28" fill="#3f6b8a" stroke="#33201d" stroke-width="4"/>` : ""}
    ${prop}
  </g>
</svg>`;
}

function playerSvg() {
  return portraitSvg({
    name: "夏邑",
    faction: "重要NPC（跨派系）",
    identity: "玩家主角，十六岁新京军事学院生",
    traits: "可塑、沉稳、少年、军校生",
    portrait: "俊秀少年，着深蓝学院制服，肩披金边短氅，神情尚未定型"
  });
}

function buttonSvg(name, top, bottom, edge, glow = "#f7deb0") {
  return `
<svg width="384" height="96" viewBox="0 0 384 96" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="g" x1="0" x2="0" y1="0" y2="1">
      <stop offset="0" stop-color="${top}"/>
      <stop offset="1" stop-color="${bottom}"/>
    </linearGradient>
  </defs>
  <rect x="5" y="5" width="374" height="86" rx="8" fill="#201515" opacity="0.72"/>
  <rect x="8" y="6" width="368" height="78" rx="6" fill="url(#g)" stroke="${edge}" stroke-width="4"/>
  <path d="M18 16 H366" stroke="${glow}" stroke-width="3" opacity="0.5"/>
  <path d="M20 70 H364" stroke="#160f12" stroke-width="5" opacity="0.36"/>
  <path d="M30 24 H354" stroke="#ffffff" stroke-width="1" opacity="0.18"/>
</svg>`;
}

function panelSvg() {
  return `
<svg width="512" height="512" viewBox="0 0 512 512" xmlns="http://www.w3.org/2000/svg">
  <rect width="512" height="512" fill="#efe1c6"/>
  <rect x="10" y="10" width="492" height="492" fill="none" stroke="#9a703c" stroke-width="14"/>
  <rect x="24" y="24" width="464" height="464" fill="none" stroke="#fff4d8" stroke-width="3" opacity="0.55"/>
  <path d="M34 42 H478" stroke="#ffffff" stroke-width="16" opacity="0.18"/>
</svg>`;
}

function topbarSvg() {
  return `
<svg width="768" height="96" viewBox="0 0 768 96" xmlns="http://www.w3.org/2000/svg">
  <defs><linearGradient id="g" x1="0" x2="0" y1="0" y2="1"><stop offset="0" stop-color="#4c2632"/><stop offset="1" stop-color="#25161b"/></linearGradient></defs>
  <rect x="4" y="4" width="760" height="88" rx="4" fill="url(#g)" stroke="#c69a4c" stroke-width="6"/>
  <path d="M18 18 H750" stroke="#fff0c8" stroke-width="3" opacity="0.38"/>
  <path d="M24 72 H744" stroke="#120b0d" stroke-width="6" opacity="0.38"/>
</svg>`;
}

function sceneSvg(id, title) {
  const configs = {
    title: ["#38202a", "#865d45", "#d7b36b"],
    academy: ["#43252a", "#8b684f", "#d6bd82"],
    library: ["#2d2532", "#69543e", "#d9c681"],
    palace: ["#332135", "#7a393f", "#d7a94d"],
    council: ["#27212a", "#5e4b3a", "#c59a54"],
    strategy: ["#25334a", "#655642", "#c6a466"],
    battlefield: ["#2b2d28", "#716442", "#b2473e"],
    frontier: ["#2f433a", "#74613e", "#bb8d4c"],
    harbor: ["#26384d", "#3f7b8c", "#d4ad62"],
    street: ["#38262a", "#705041", "#d4ae71"]
  };
  const c = configs[id] || configs.academy;
  const horizon = id === "harbor"
    ? `<path d="M0 412 C260 376 440 454 704 404 C948 358 1070 424 1280 388 L1280 720 L0 720 Z" fill="#3f7b8c"/><path d="M0 430 H1280" stroke="#d7c184" stroke-width="5" opacity="0.55"/>`
    : id === "battlefield"
      ? `<path d="M0 428 C240 374 410 452 632 410 C890 362 1020 430 1280 398 L1280 720 L0 720 Z" fill="#575438"/><path d="M180 454 L294 420 L376 472 L250 504 Z" fill="#8b4138"/><path d="M788 450 L936 410 L1070 492 L888 532 Z" fill="#314f75"/>`
      : `<path d="M0 428 C240 396 462 440 650 406 C846 372 1044 418 1280 390 L1280 720 L0 720 Z" fill="${c[1]}"/>`;
  const buildings = id === "frontier"
    ? `<path d="M0 418 L130 260 L246 420 Z M938 420 L1080 236 L1238 420 Z" fill="#6a715a"/><rect x="420" y="280" width="338" height="210" fill="#74553b" stroke="#241716" stroke-width="8"/><path d="M392 286 L590 176 L784 286 Z" fill="#50322b" stroke="#241716" stroke-width="8"/>`
    : id === "strategy"
      ? `<rect x="200" y="198" width="880" height="368" fill="#d8c08a" stroke="#49331f" stroke-width="12"/><path d="M282 326 C470 270 530 402 680 334 C820 270 902 382 1010 324" fill="none" stroke="#3c7a91" stroke-width="26" opacity="0.72"/><g stroke="#856f42" opacity="0.42">${Array.from({ length: 12 }, (_, i) => `<line x1="${220 + i * 72}" y1="218" x2="${220 + i * 72}" y2="548"/>`).join("")}${Array.from({ length: 6 }, (_, i) => `<line x1="220" y1="${240 + i * 52}" x2="1060" y2="${240 + i * 52}"/>`).join("")}</g>`
      : id === "palace"
        ? `<rect x="160" y="206" width="960" height="352" fill="#6f3438" stroke="#d2a951" stroke-width="12"/><path d="M128 214 L640 88 L1152 214 Z" fill="#d3a04a" stroke="#2b1718" stroke-width="10"/><rect x="520" y="314" width="240" height="244" fill="#2c1b20" stroke="#d2a951" stroke-width="8"/>`
        : id === "library"
          ? `<rect x="132" y="174" width="1016" height="392" fill="#5d432f" stroke="#d4b36b" stroke-width="10"/><g fill="#2d2532">${Array.from({ length: 12 }, (_, i) => `<rect x="${168 + i * 78}" y="214" width="42" height="250"/>`).join("")}</g><rect x="432" y="438" width="416" height="92" fill="#d8c08a" stroke="#4a3024" stroke-width="7"/>`
          : `<rect x="218" y="184" width="844" height="386" fill="${c[1]}" stroke="#d0a65f" stroke-width="10"/><path d="M176 196 L640 92 L1104 196 Z" fill="${c[2]}" stroke="#271716" stroke-width="8"/><rect x="534" y="330" width="212" height="240" fill="#2d1d20" stroke="#c8a45f" stroke-width="7"/><rect x="290" y="292" width="150" height="116" fill="#8fb0b8" stroke="#241716" stroke-width="7"/><rect x="842" y="292" width="150" height="116" fill="#8fb0b8" stroke="#241716" stroke-width="7"/>`;

  return `
<svg width="1280" height="720" viewBox="0 0 1280 720" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="sky" x1="0" x2="0" y1="0" y2="1">
      <stop offset="0" stop-color="${c[0]}"/>
      <stop offset="0.55" stop-color="${c[1]}"/>
      <stop offset="1" stop-color="#191012"/>
    </linearGradient>
    <radialGradient id="glow" cx="58%" cy="30%" r="55%">
      <stop offset="0" stop-color="${c[2]}" stop-opacity="0.5"/>
      <stop offset="1" stop-color="${c[2]}" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <rect width="1280" height="720" fill="url(#sky)"/>
  <rect width="1280" height="720" fill="url(#glow)"/>
  ${horizon}
  ${buildings}
  <path d="M0 610 H1280 V720 H0 Z" fill="#201416" opacity="0.72"/>
  <g opacity="0.12" stroke="#ffffff">${Array.from({ length: 18 }, (_, i) => `<line x1="${i * 80}" y1="0" x2="${i * 80 - 220}" y2="720"/>`).join("")}</g>
  <rect x="34" y="34" width="1212" height="652" fill="none" stroke="#d2a951" stroke-width="6" opacity="0.28"/>
  <text x="64" y="662" font-size="28" font-family="Microsoft YaHei, SimHei, sans-serif" fill="#f3dfad" opacity="0.68">${xml(title)}</text>
</svg>`;
}

async function pngFromSvg(svg, outPath) {
  await sharp(Buffer.from(svg)).png().toFile(outPath);
}

async function makeUiAssets() {
  const assets = {
    button_idle: buttonSvg("button_idle", "#7b4653", "#512838", "#c69a4c"),
    button_primary: buttonSvg("button_primary", "#4e755c", "#2d4e3b", "#d2aa62"),
    button_secondary: buttonSvg("button_secondary", "#456785", "#293f5e", "#d2aa62"),
    button_danger: buttonSvg("button_danger", "#995044", "#6f302d", "#d2aa62"),
    panel_paper: panelSvg(),
    topbar_wine: topbarSvg()
  };
  for (const [name, svg] of Object.entries(assets)) {
    await pngFromSvg(svg, path.join(uiDir, `${name}.png`));
  }
  return Object.fromEntries(Object.keys(assets).map((name) => [name, `Art/UI/${name}`]));
}

async function makeScenes() {
  const scenes = {
    title: ["scene_title", "新京军事学院 · 黄昏"],
    academy: ["scene_academy", "学院讲堂"],
    library: ["scene_library", "学院图书馆"],
    palace: ["scene_palace", "王城宫阙"],
    council: ["scene_council", "军议厅"],
    strategy: ["scene_strategy", "战区地图"],
    battlefield: ["scene_battlefield", "边境战场"],
    frontier: ["scene_frontier", "西部边疆"],
    harbor: ["scene_harbor", "海港与船坞"],
    street: ["scene_street", "新京街市"]
  };
  const manifest = {};
  for (const [key, [file, title]] of Object.entries(scenes)) {
    await pngFromSvg(sceneSvg(key, title), path.join(sceneDir, `${file}.png`));
    manifest[key] = `Art/Scenes/${file}`;
  }
  return manifest;
}

async function makePortraits(story) {
  const characters = [];
  await pngFromSvg(playerSvg(), path.join(portraitDir, "portrait_player_mo_mingyuan.png"));
  for (let i = 0; i < story.characters.length; i += 1) {
    const character = story.characters[i];
    const file = `portrait_${String(i + 1).padStart(3, "0")}`;
    await pngFromSvg(portraitSvg(character), path.join(portraitDir, `${file}.png`));
    character.asset = `Art/Portraits/${file}`;
    characters.push({
      name: character.name,
      faction: character.faction,
      identity: character.identity,
      resource: character.asset
    });
  }
  return {
    player: "Art/Portraits/portrait_player_mo_mingyuan",
    characters
  };
}

async function main() {
  const story = JSON.parse(fs.readFileSync(storyPath, "utf8"));
  if (!Array.isArray(story.characters)) story.characters = [];

  const ui = await makeUiAssets();
  const scenes = await makeScenes();
  const portraits = await makePortraits(story);

  const manifest = {
    generatedAt: new Date().toISOString(),
    style: "2D养成游戏风格，深酒红、旧纸、金边、军校制服与架空郑明历史题材",
    ui,
    scenes,
    portraits
  };

  fs.writeFileSync(storyPath, `${JSON.stringify(story, null, 2)}\n`, "utf8");
  fs.writeFileSync(path.join(manifestDir, "art_manifest.json"), `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
  console.log(`Generated ${Object.keys(ui).length} UI assets`);
  console.log(`Generated ${Object.keys(scenes).length} scene artworks`);
  console.log(`Generated ${portraits.characters.length + 1} portrait assets`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
