const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "..");
const configPath = path.join(projectRoot, "Assets", "Resources", "Data", "MingLuGameConfig.json");
const storyPath = path.join(projectRoot, "Assets", "Resources", "MingLuStoryData.json");
const sourcePath = path.join(projectRoot, "Assets", "Scripts", "MingLuGame.cs");
const outDir = path.join(projectRoot, "DataTables", "csv");

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

function csvEscape(value) {
  if (value === null || value === undefined) return "";
  const s = String(value);
  return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
}

function writeCsv(name, rows, headers) {
  const lines = [headers.map(csvEscape).join(",")];
  for (const row of rows) {
    lines.push(headers.map((h) => csvEscape(row[h])).join(","));
  }
  fs.writeFileSync(path.join(outDir, `${name}.csv`), `${lines.join("\n")}\n`, "utf8");
}

function defaultBattleRoles() {
  return [
    { id: "infantry", displayName: "步兵", symbol: "步", baseHp: 100, move: 2, range: 1, attackBonus: 0, formation: 3 },
    { id: "musket", displayName: "火枪", symbol: "铳", baseHp: 82, move: 2, range: 3, attackBonus: 2, formation: 1 },
    { id: "skirmisher", displayName: "散兵", symbol: "散", baseHp: 72, move: 3, range: 2, attackBonus: -1, formation: 1 },
    { id: "heavy_spear", displayName: "重枪", symbol: "枪", baseHp: 128, move: 2, range: 1, attackBonus: 4, formation: 4 },
    { id: "heavy_cavalry", displayName: "重骑", symbol: "骑", baseHp: 145, move: 3, range: 1, attackBonus: 8, formation: 3 },
    { id: "heavy_infantry", displayName: "重步", symbol: "甲", baseHp: 135, move: 2, range: 1, attackBonus: 3, formation: 4 },
    { id: "heavy_archer", displayName: "重弓", symbol: "弩", baseHp: 92, move: 2, range: 4, attackBonus: 1, formation: 2 },
    { id: "heavy_brute", displayName: "重猛", symbol: "猛", baseHp: 130, move: 2, range: 1, attackBonus: 7, formation: 3 },
    { id: "cavalry", displayName: "骑兵", symbol: "骑", baseHp: 115, move: 3, range: 1, attackBonus: 6, formation: 2 },
    { id: "artillery", displayName: "重器", symbol: "器", baseHp: 105, move: 1, range: 4, attackBonus: 10, formation: 1 },
    { id: "brute", displayName: "猛士", symbol: "斧", baseHp: 105, move: 2, range: 1, attackBonus: 5, formation: 2 },
    { id: "archer", displayName: "弓兵", symbol: "弓", baseHp: 78, move: 2, range: 3, attackBonus: -3, formation: 1 }
  ];
}

function defaultCommonUnits() {
  const rows = [
    ["swordsmen_volunteers", "剑士队", "义勇军", "infantry"],
    ["matchlock_volunteers", "火绳枪队", "义勇军", "musket"],
    ["militia_volunteers", "民兵团", "义勇军", "skirmisher"],
    ["outlaw_skirmishers", "亡徒军", "贼徒", "skirmisher"],
    ["imperial_halberdiers", "禁卫长戟队", "禁军", "heavy_spear"],
    ["armored_iron_cavalry", "具装铁骑军", "禁军", "heavy_cavalry"],
    ["steel_helmet_heavy_infantry", "钢盔军", "义勇军", "heavy_infantry"],
    ["imperial_longbowmen", "禁军长弓兵", "禁军", "heavy_archer"],
    ["sword_guard_corps", "剑卫军团", "义勇军", "infantry"],
    ["imperial_axe_guard", "禁军斧卫", "禁军", "heavy_brute"],
    ["vanguard_cavalry", "先锋骑军", "义勇军", "cavalry"],
    ["solemn_guard_matchlocks", "肃卫火枪队", "义勇军", "musket"],
    ["raiders", "掠杀军", "贼徒", "skirmisher"],
    ["imperial_heavy_guard", "重甲禁卫军", "禁军", "heavy_infantry"],
    ["warhammer_volunteers", "重锤军", "义勇军", "heavy_brute"],
    ["imperial_shenji_artillery", "禁军神机队", "禁军", "artillery"],
    ["zealot_believers", "狂热信众", "信徒", "skirmisher"],
    ["zealot_mob", "狂热暴徒", "信徒", "brute"],
    ["leader_guard", "领袖卫队", "信徒", "heavy_infantry"],
    ["elite_archers", "精锐弓兵队", "义勇军", "archer"],
    ["bandits", "土匪", "贼徒", "skirmisher"],
    ["great_axe_warriors", "巨斧军", "义勇军", "brute"],
    ["believer_elites", "信徒精锐", "信徒", "infantry"]
  ];
  return rows.map(([id, name, keyword, role]) => ({
    id,
    name,
    keyword,
    role,
    asset: `Art/BattleUnits/${id}`,
    idleFrames: 2,
    moveFrames: 4,
    attackFrames: 4,
    hitFrames: 4
  }));
}

function defaultCharacterOrigins() {
  return [
    { id: "noble", name: "勋贵之后", subtitle: "伯爵之子，门第深厚。", description: "你的父亲是宁远伯爵，镇守西境多年。家族与返乡团关系密切，却也背负旧族的沉默。", talentPool: "noble;leader;strategy", clueId: "father_silence", clueName: "父亲的沉默", infantryExp: 0, cavalryExp: 0, artilleryExp: 0, managementExp: 50, logisticsExp: 50, trainingExp: 0, nationAxis: -3, classAxis: -3, governanceAxis: -5, regionAxis: -3, stanceHome: 10, stanceArmy: 0, stanceNative: -8, stanceLiberal: -5, stanceLegal: 5, relZhao: 5, relLin: 0, relYierde: -5, relChen: 0, relSu: -3, relLi: 3 },
    { id: "scholar", name: "书香门第", subtitle: "翰林之后，重文重法。", description: "你出身南方文脉，家中藏书与辩论伴随童年。学院里的共和与法理，对你并不陌生。", talentPool: "scholar;strategy;social", clueId: "", clueName: "", infantryExp: 0, cavalryExp: 0, artilleryExp: 0, managementExp: 0, logisticsExp: 0, trainingExp: 50, nationAxis: 5, classAxis: 3, governanceAxis: 5, regionAxis: 0, stanceHome: -3, stanceArmy: 0, stanceNative: 3, stanceLiberal: 12, stanceLegal: 0, relZhao: 0, relLin: 8, relYierde: 3, relChen: -5, relSu: 5, relLi: 0 },
    { id: "military", name: "将门虎子", subtitle: "总兵之子，少年习武。", description: "你熟悉军营号角，也熟悉军中对软弱的轻蔑。干城派看重你的血脉，文官则未必信任你。", talentPool: "military;leader;bravery", clueId: "", clueName: "", infantryExp: 50, cavalryExp: 0, artilleryExp: 0, managementExp: 0, logisticsExp: 0, trainingExp: 50, nationAxis: 0, classAxis: -3, governanceAxis: -3, regionAxis: -3, stanceHome: 0, stanceArmy: 12, stanceNative: 0, stanceLiberal: -5, stanceLegal: -5, relZhao: 5, relLin: -3, relYierde: 0, relChen: -5, relSu: -3, relLi: 5 },
    { id: "border", name: "边民出身", subtitle: "边陲小族，熟悉荒原。", description: "你来自边境，见过贸易、冲突和饥荒。你比多数同窗更懂得地图边缘的人如何活下去。", talentPool: "border;bravery;resilience", clueId: "", clueName: "", infantryExp: 0, cavalryExp: 50, artilleryExp: 0, managementExp: 0, logisticsExp: 50, trainingExp: 0, nationAxis: 5, classAxis: 3, governanceAxis: 0, regionAxis: 3, stanceHome: -8, stanceArmy: 3, stanceNative: 12, stanceLiberal: 5, stanceLegal: -5, relZhao: -3, relLin: 0, relYierde: 10, relChen: -8, relSu: 3, relLi: 0 },
    { id: "merchant", name: "商贾之家", subtitle: "远洋巨商，消息灵通。", description: "母族经营海贸，银钱与情报在你幼年便是同一种语言。你知道港口的风向，也知道宫廷的价码。", talentPool: "merchant;social;strategy", clueId: "shen_ledger", clueName: "沈家账册", infantryExp: 0, cavalryExp: 0, artilleryExp: 0, managementExp: 50, logisticsExp: 50, trainingExp: 0, nationAxis: 0, classAxis: 0, governanceAxis: 0, regionAxis: 0, stanceHome: 3, stanceArmy: -3, stanceNative: 0, stanceLiberal: 5, stanceLegal: 5, relZhao: 3, relLin: 0, relYierde: 0, relChen: -3, relSu: 3, relLi: 0 },
    { id: "tribal", name: "部落血裔", subtitle: "归化部落，双重身份。", description: "你身上流着归化部落的血。有人把这当作污点，也有人把这视为新大陆未来的证明。", talentPool: "tribal;social;resilience", clueId: "", clueName: "", infantryExp: 0, cavalryExp: 50, artilleryExp: 0, managementExp: 0, logisticsExp: 0, trainingExp: 50, nationAxis: 8, classAxis: 5, governanceAxis: 3, regionAxis: 5, stanceHome: -10, stanceArmy: 0, stanceNative: 15, stanceLiberal: 3, stanceLegal: -8, relZhao: 0, relLin: 0, relYierde: 15, relChen: -10, relSu: 5, relLi: 0 }
  ];
}

function defaultCreationMemories() {
  return [
    { id: "market", title: "往事一：边境集市", body: "老妇人递来一串祈福珠。你看见贫穷，也看见谎言。", optionAId: "market_doubt", optionAText: "拉住父亲，提醒这是骗局。", optionATraitId: "trait_cautious", optionANation: -3, optionAClass: 0, optionAGovernance: -2, optionARegion: 0, optionBId: "market_mercy", optionBText: "把零花钱给她，愿她今日能饱腹。", optionBTraitId: "trait_kind", optionBNation: 4, optionBClass: 2, optionBGovernance: 0, optionBRegion: 0 },
    { id: "yard", title: "往事二：演武场", body: "同伴被高年级欺辱。你可以忍，也可以立刻冲上去。", optionAId: "yard_endure", optionAText: "先记下对方名字，等机会再还。", optionATraitId: "trait_stoic", optionANation: 0, optionAClass: 0, optionAGovernance: -2, optionARegion: -1, optionBId: "yard_charge", optionBText: "冲上前，把人从泥地里拉起来。", optionBTraitId: "trait_decisive", optionBNation: 2, optionBClass: 0, optionBGovernance: 2, optionBRegion: 0 },
    { id: "book", title: "往事三：禁书一页", body: "你在书房里发现被撕下的海外手札。父亲沉默，烛火摇晃。", optionAId: "book_hide", optionAText: "藏起残页，等待能看懂它的一天。", optionATraitId: "trait_sensitive", optionANation: 0, optionAClass: 3, optionAGovernance: 2, optionARegion: 0, optionBId: "book_ask", optionBText: "直接追问父亲，哪怕惹怒他。", optionBTraitId: "trait_radical", optionBNation: 4, optionBClass: 3, optionBGovernance: 3, optionBRegion: 0 }
  ];
}

function defaultCreationTalents() {
  return [
    { id: "talent_old_blood", name: "贵胄之姿", category: "统率", tier: 1, originTags: "noble", description: "社交好感+2，养成进度+6%。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 2, cultivationPercent: 6, staminaSave: 0, intelligenceBonus: 0 },
    { id: "talent_family_learning", name: "家学渊源", category: "谋略", tier: 1, originTags: "noble;scholar", description: "养成进度+10%。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: 10, staminaSave: 0, intelligenceBonus: 0 },
    { id: "talent_argument", name: "明辨章句", category: "谋略", tier: 1, originTags: "scholar", description: "社交好感+1，养成进度+8%。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 1, cultivationPercent: 8, staminaSave: 0, intelligenceBonus: 0 },
    { id: "talent_warborn", name: "百战余习", category: "武勇", tier: 1, originTags: "military;border", description: "攻击+3。", battleAttack: 3, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 0, intelligenceBonus: 0 },
    { id: "talent_drillmaster", name: "校场老手", category: "统率", tier: 1, originTags: "military", description: "兵力+12，攻击+1。", battleAttack: 1, battleHp: 12, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 0, intelligenceBonus: 0 },
    { id: "talent_frontier", name: "荒原识途", category: "坚韧", tier: 1, originTags: "border;tribal", description: "移动+1，体力消耗-1。", battleAttack: 0, battleHp: 0, battleMove: 1, socialBonus: 0, cultivationPercent: 0, staminaSave: 1, intelligenceBonus: 0 },
    { id: "talent_tradewind", name: "海贸耳目", category: "通达", tier: 1, originTags: "merchant", description: "情报+4，社交好感+1。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 1, cultivationPercent: 0, staminaSave: 0, intelligenceBonus: 4 },
    { id: "talent_abacus", name: "账册心算", category: "谋略", tier: 1, originTags: "merchant;scholar", description: "养成进度+6%，体力消耗-1。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: 6, staminaSave: 1, intelligenceBonus: 0 },
    { id: "talent_two_worlds", name: "双界行者", category: "通达", tier: 1, originTags: "tribal;border", description: "社交好感+3。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 3, cultivationPercent: 0, staminaSave: 0, intelligenceBonus: 0 },
    { id: "talent_bloodfire", name: "血性激昂", category: "武勇", tier: 1, originTags: "", description: "攻击+2，兵力+8。", battleAttack: 2, battleHp: 8, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 0, intelligenceBonus: 0 }
  ];
}

function defaultCreationSubjects() {
  return [
    { id: "infantry", name: "步兵", target: "infantryExp", description: "近战、长枪、重步基础。", expGain: 50 },
    { id: "cavalry", name: "骑兵", target: "cavalryExp", description: "骑兵冲击和战场机动。", expGain: 50 },
    { id: "artillery", name: "炮兵", target: "artilleryExp", description: "火枪、弓兵和重器输出。", expGain: 50 },
    { id: "management", name: "管理", target: "managementExp", description: "政务、国库和任务效率。", expGain: 50 },
    { id: "logistics", name: "后勤", target: "logisticsExp", description: "补给、行军和消耗控制。", expGain: 50 },
    { id: "training", name: "训练", target: "trainingExp", description: "演训、情报和综合准备。", expGain: 50 }
  ];
}

function defaultBattleUnitSpawns() {
  return [
    { side: "attacker", suffix: "剑士队", role: "infantry", q: 1, r: 6, attackBonus: 0, troopDivisor: 4 },
    { side: "attacker", suffix: "火绳枪队", role: "musket", q: 3, r: 6, attackBonus: 2, troopDivisor: 5 },
    { side: "attacker", suffix: "先锋骑军", role: "cavalry", q: 5, r: 6, attackBonus: 4, troopDivisor: 5 },
    { side: "attacker", suffix: "精锐弓兵队", role: "archer", q: 2, r: 5, attackBonus: 1, troopDivisor: 5 },
    { side: "attacker", suffix: "钢盔军", role: "heavy_infantry", q: 4, r: 5, attackBonus: 2, troopDivisor: 4 },
    { side: "defender", suffix: "禁卫长戟队", role: "heavy_spear", q: 7, r: 0, attackBonus: 2, troopDivisor: 4 },
    { side: "defender", suffix: "具装铁骑军", role: "heavy_cavalry", q: 5, r: 1, attackBonus: 6, troopDivisor: 5 },
    { side: "defender", suffix: "禁军长弓兵", role: "heavy_archer", q: 3, r: 0, attackBonus: 2, troopDivisor: 5 },
    { side: "defender", suffix: "重甲禁卫军", role: "heavy_infantry", q: 6, r: 1, attackBonus: 4, troopDivisor: 4 },
    { side: "defender", suffix: "禁军神机队", role: "artillery", q: 2, r: 1, attackBonus: 5, troopDivisor: 6 }
  ];
}

function defaultBattleRoleDamageRules() {
  return [
    { attackerRole: "cavalry", defenderRole: "archer", modifier: 12 },
    { attackerRole: "archer", defenderRole: "cavalry", modifier: -5 },
    { attackerRole: "infantry", defenderRole: "cavalry", modifier: 3 },
    { attackerRole: "heavy_cavalry", defenderRole: "archer", modifier: 14 },
    { attackerRole: "heavy_cavalry", defenderRole: "heavy_archer", modifier: 10 },
    { attackerRole: "heavy_spear", defenderRole: "cavalry", modifier: 14 },
    { attackerRole: "heavy_spear", defenderRole: "heavy_cavalry", modifier: 18 },
    { attackerRole: "musket", defenderRole: "heavy_infantry", modifier: 8 },
    { attackerRole: "musket", defenderRole: "heavy_spear", modifier: 8 },
    { attackerRole: "skirmisher", defenderRole: "artillery", modifier: 10 },
    { attackerRole: "artillery", defenderRole: "heavy_infantry", modifier: 12 },
    { attackerRole: "artillery", defenderRole: "heavy_spear", modifier: 12 },
    { attackerRole: "heavy_archer", defenderRole: "brute", modifier: 7 },
    { attackerRole: "heavy_archer", defenderRole: "heavy_brute", modifier: 5 }
  ];
}

function defaultConfig() {
  return {
    version: "1.0.0",
    uiTexts: [
      { key: "game.title", value: "明路", note: "标题界面主标题" },
      { key: "title.subtitle", value: "1760，新京军事学院。你的道路，将通向王朝、共和、边疆，或未曾设想的远方。", note: "标题副标题" },
      { key: "top.hint", value: "鼠标/触摸点击优先，键盘仅作备用", note: "顶部提示" },
      { key: "button.new_game", value: "新的传奇", note: "标题按钮" },
      { key: "button.strategy", value: "进入战略地图", note: "标题按钮" },
      { key: "button.continue", value: "继续征途", note: "标题按钮" },
      { key: "button.credits", value: "开发团队", note: "标题按钮" },
      { key: "button.exit", value: "退出", note: "标题按钮" },
      { key: "button.back_title", value: "返回标题", note: "通用按钮" },
      { key: "character_create.title", value: "创建角色", note: "创角标题" },
      { key: "character_create.description", value: "选择你的开局特性。它们会在战斗、社交和养成中持续生效。", note: "创角说明" },
      { key: "character_create.message", value: "选择 1-3 个特性，确认后进入新京军事学院。", note: "创角默认提示" },
      { key: "academy.title", value: "新京军事学院", note: "学院标题" },
      { key: "credits.body", value: "《明路》Unity 原型\n设计来源：明路第一版文档\n实现：运行时 UI / 学院养成 / 战略地图 / 六边形战棋\n素材：项目内可配置美术资源。", note: "制作组文案" },
      { key: "story.missing", value: "未找到剧情事件：", note: "剧情缺失提示" },
      { key: "battle.message.start", value: "选择蓝方军团开始行动。拖动地图可以调整视野。", note: "战斗初始提示" }
    ],
    playerDefaults: {
      name: "夏邑",
      age: 16,
      year: 1,
      week: 1,
      mood: 50,
      stamina: 80,
      merit: 0,
      treasury: 120,
      infantryExp: 0,
      cavalryExp: 0,
      artilleryExp: 0,
      managementExp: 0,
      logisticsExp: 0,
      trainingExp: 0,
      title: "新京军事学院生",
      courtesyName: "",
      originId: "",
      personalityId: "",
      traits: [],
      creationMemoryChoices: [],
      subjectFocusIds: [],
      lastCourse: "",
      lastExamScore: 0,
      nationAxis: 0,
      classAxis: 0,
      governanceAxis: 0,
      regionAxis: 0,
      commandLockTurns: 0,
      prisoners: [],
      intelligence: 12,
      spyNetwork: 0,
      newGamePlus: 0,
      achievementPoints: 0,
      battlesFought: 0,
      battleWins: 0,
      battleLosses: 0,
      enemiesDefeated: 0,
      questsCompleted: 0,
      spySuccesses: 0,
      supplyBreaks: 0,
      equippedTitle: "",
      unlockedSkills: [],
      equippedSkills: [],
      activeQuests: [],
      completedQuests: [],
      unlockedAchievements: [],
      unlockedTitles: [],
      unlockedEndings: [],
      eventReview: []
    },
    calendar: {
      examWeeks: "25;50",
      holidayWeeks: "26;27;51;52",
      maxWeek: 52,
      maxYear: 4
    },
    traits: [
      { id: "trait_cautious", name: "审慎", description: "战斗：兵力 +8；养成：体力消耗 -1。", battleAttack: 0, battleHp: 8, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 1 },
      { id: "trait_kind", name: "仁厚", description: "社交：好感收益 +3。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 3, cultivationPercent: 0, staminaSave: 0 },
      { id: "trait_decisive", name: "果决", description: "战斗：攻击 +2。", battleAttack: 2, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 0 },
      { id: "trait_sensitive", name: "善感", description: "社交：好感 +1；养成进度 +8%。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 1, cultivationPercent: 8, staminaSave: 0 },
      { id: "trait_stoic", name: "隐忍", description: "战斗：兵力 +12；体力消耗 -1。", battleAttack: 0, battleHp: 12, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 1 },
      { id: "trait_radical", name: "激进", description: "战斗：攻击 +3；养成进度 -5%。", battleAttack: 3, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: -5, staminaSave: 0 },
      { id: "field_commander", name: "阵前直觉", description: "战斗：全军攻击 +3。", battleAttack: 3, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 0 },
      { id: "iron_body", name: "坚韧体魄", description: "战斗：兵力 +15；养成：体力消耗 -2。", battleAttack: 0, battleHp: 15, battleMove: 0, socialBonus: 0, cultivationPercent: 0, staminaSave: 2 },
      { id: "wild_runner", name: "野外行军", description: "战斗：移动 +1。", battleAttack: 0, battleHp: 0, battleMove: 1, socialBonus: 0, cultivationPercent: 0, staminaSave: 0 },
      { id: "honor_student", name: "军校优等生", description: "养成：课程进度 +20%。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: 20, staminaSave: 0 },
      { id: "methodical", name: "勤勉自律", description: "养成：课程进度 +12%，体力消耗 -1。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 0, cultivationPercent: 12, staminaSave: 1 },
      { id: "silver_tongue", name: "辩才无碍", description: "社交：好感收益 +3。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 3, cultivationPercent: 0, staminaSave: 0 },
      { id: "noble_manners", name: "名门礼仪", description: "社交：好感收益 +2；养成 +8%。", battleAttack: 0, battleHp: 0, battleMove: 0, socialBonus: 2, cultivationPercent: 8, staminaSave: 0 },
      { id: "balanced", name: "文武兼修", description: "战斗：攻击 +1；社交 +1；养成 +8%。", battleAttack: 1, battleHp: 0, battleMove: 0, socialBonus: 1, cultivationPercent: 8, staminaSave: 0 }
    ],
    characterOrigins: defaultCharacterOrigins(),
    creationMemories: defaultCreationMemories(),
    creationTalents: defaultCreationTalents(),
    creationSubjects: defaultCreationSubjects(),
    news: [
      { id: "N001", unlockWeek: 1, title: "新京学报：新生入校", source: "新京学报", stanceHint: "温和官报", body: "军事学院迎来新一届学生。学报称，王朝需要既懂战术又懂人心的年轻军官。" },
      { id: "N002", unlockWeek: 4, title: "海潮报：远航派争论", source: "海潮报", stanceHint: "返乡团", body: "返乡团再次推动远航预算，反对者认为大陆防线更需要兵员和补给。" },
      { id: "N003", unlockWeek: 8, title: "民声小报：议会呼声", source: "民声小报", stanceHint: "自由派", body: "地方士绅与商人联名要求设立民间代表团。文章认为，军国政治无法回答所有民生问题。" },
      { id: "N004", unlockWeek: 13, title: "红林周刊：归化部落", source: "红林周刊", stanceHint: "印第安乡党", body: "归化部落在边境贸易中承担了越来越多的运输与侦察职责，但他们在朝堂中的声音仍然微弱。" },
      { id: "N005", unlockWeek: 20, title: "律令汇编：法治与皇权", source: "律令汇编", stanceHint: "法治派", body: "法治派主张重整贵族和地方豪强秩序，以严密法令削弱商团与宗教组织的影响。" },
      { id: "N006", unlockWeek: 32, title: "前线简讯：北岭摩擦", source: "前线简讯", stanceHint: "陆军青壮派", body: "北岭附近出现小规模冲突。年轻军官要求增兵，海军派则担心这会拖慢远航计划。" }
    ],
    campusActivities: [
      { id: "drill", name: "战术演练", description: "参加六日战术演练。训练成长，并获得少量战功。", moodDelta: -3, meritDelta: 6, treasuryDelta: 0, socialGain: 0, trainingGain: 12, axisId: "", axisDelta: 0 },
      { id: "salon", name: "同窗沙龙", description: "加入朋友圈闲谈。全员好感提升，心情恢复。", moodDelta: 5, meritDelta: 0, treasuryDelta: -6, socialGain: 4, trainingGain: 0, axisId: "", axisDelta: 0 },
      { id: "lecture", name: "公共讲座", description: "聆听一场政治讲座。选择后会推动立场轴。", moodDelta: 1, meritDelta: 0, treasuryDelta: 0, socialGain: 0, trainingGain: 0, axisId: "governance", axisDelta: 6 },
      { id: "volunteer", name: "边民救济", description: "协助学院救济边民。共治与民主倾向上升。", moodDelta: 2, meritDelta: 0, treasuryDelta: -10, socialGain: 2, trainingGain: 0, axisId: "nation", axisDelta: 5 }
    ],
    courses: [
      { id: "infantry", label: "步兵课程", target: "infantryExp" },
      { id: "cavalry", label: "骑兵课程", target: "cavalryExp" },
      { id: "artillery", label: "炮兵课程", target: "artilleryExp" },
      { id: "management", label: "管理课程", target: "managementExp" },
      { id: "logistics", label: "后勤课程", target: "logisticsExp" },
      { id: "training", label: "训练课程", target: "trainingExp" },
      { id: "wander", label: "校园闲逛", target: "social" }
    ],
    moodRules: [
      { minMood: 90, maxMood: 100, label: "超好", studyMin: 3, studyMax: 5 },
      { minMood: 75, maxMood: 89, label: "好", studyMin: 2, studyMax: 5 },
      { minMood: 50, maxMood: 74, label: "一般", studyMin: 2, studyMax: 4 },
      { minMood: 30, maxMood: 49, label: "低落", studyMin: 1, studyMax: 4 },
      { minMood: 15, maxMood: 29, label: "难过", studyMin: 1, studyMax: 3 },
      { minMood: 0, maxMood: 14, label: "痛苦", studyMin: 1, studyMax: 2 }
    ],
    academyLevels: [
      { level: 1, floorExp: 0, nextExp: 50 },
      { level: 2, floorExp: 50, nextExp: 150 },
      { level: 3, floorExp: 150, nextExp: 400 },
      { level: 4, floorExp: 400, nextExp: 1000 },
      { level: 5, floorExp: 1000, nextExp: 2000 },
      { level: 6, floorExp: 2000, nextExp: -1 }
    ],
    ranks: [
      { minMerit: 10000, name: "元帅", commandLimit: 4 },
      { minMerit: 5000, name: "上将", commandLimit: 6 },
      { minMerit: 2500, name: "中将", commandLimit: 5 },
      { minMerit: 1500, name: "少将", commandLimit: 4 },
      { minMerit: 1000, name: "上校", commandLimit: 6 },
      { minMerit: 600, name: "中校", commandLimit: 5 },
      { minMerit: 300, name: "少校", commandLimit: 4 },
      { minMerit: 0, name: "士", commandLimit: 2 }
    ],
    relationships: [
      { id: "zhao", name: "赵伯衡", stance: "返乡团", affection: 10, circle: "将门子弟", knownLevel: 1, lastInteractionWeek: 1, note: "豪爽热血的将门子弟，常把复国与军功挂在嘴边。" },
      { id: "lin", name: "林素心", stance: "自由派", affection: 10, circle: "图书馆", knownLevel: 1, lastInteractionWeek: 1, note: "图书馆常客，温雅而坚定，关心民权与共和。" },
      { id: "yierde", name: "伊尔德", stance: "印第安乡党", affection: 10, circle: "归化部落", knownLevel: 1, lastInteractionWeek: 1, note: "部落首领之子，在两个世界之间寻找自己的道路。" },
      { id: "chen", name: "陈敬之", stance: "法治派", affection: 10, circle: "世家子弟", knownLevel: 1, lastInteractionWeek: 1, note: "世家子弟，骄矜冷峻，相信秩序与门第。" },
      { id: "li", name: "李婉清", stance: "陆军青壮派", affection: 10, circle: "南方军校生", knownLevel: 1, lastInteractionWeek: 1, note: "南方转学生，清冷果断，兼具军人气质与改革锋芒。" }
    ],
    stances: [
      { id: "home", name: "返乡团", value: 20 },
      { id: "army", name: "陆军青壮派", value: 20 },
      { id: "native", name: "印第安乡党", value: 20 },
      { id: "liberal", name: "自由派", value: 20 },
      { id: "legal", name: "法治派", value: 20 }
    ],
    provinces: [
      { id: "xinjing", name: "新京", owner: "Player", defense: 72, income: 20, x: -325, y: 175, roads: "linhai;hegu", armyId: "" },
      { id: "linhai", name: "临海", owner: "Player", defense: 55, income: 14, x: -210, y: 5, roads: "xinjing;beiling", armyId: "" },
      { id: "hegu", name: "河谷", owner: "Player", defense: 48, income: 11, x: -95, y: 190, roads: "xinjing;beiling;nanze", armyId: "" },
      { id: "beiling", name: "北岭", owner: "Imperial", defense: 62, income: 13, x: 95, y: 105, roads: "linhai;hegu;songlin;shigu", armyId: "" },
      { id: "songlin", name: "松林", owner: "Native", defense: 44, income: 9, x: 250, y: 215, roads: "beiling;xigang", armyId: "" },
      { id: "shigu", name: "石谷", owner: "Reformist", defense: 58, income: 12, x: 225, y: -40, roads: "beiling;xigang;hongyuan;nanze", armyId: "" },
      { id: "xigang", name: "西港", owner: "Foreign", defense: 68, income: 18, x: 410, y: 60, roads: "songlin;shigu", armyId: "" },
      { id: "hongyuan", name: "红原", owner: "Neutral", defense: 40, income: 8, x: 405, y: -175, roads: "shigu", armyId: "" },
      { id: "nanze", name: "南泽", owner: "Player", defense: 50, income: 10, x: -40, y: -145, roads: "hegu;shigu", armyId: "" }
    ],
    armies: [
      { id: "a1", name: "第一军团", faction: "Player", provinceId: "xinjing", troops: 120, maxTroops: 120, move: 1, maxMove: 1, level: 0, exp: 0, attack: 34 },
      { id: "a2", name: "归化骑队", faction: "Player", provinceId: "hegu", troops: 82, maxTroops: 82, move: 1, maxMove: 1, level: 0, exp: 0, attack: 28 },
      { id: "e1", name: "禁卫前锋", faction: "Imperial", provinceId: "beiling", troops: 96, maxTroops: 96, move: 1, maxMove: 1, level: 0, exp: 0, attack: 30 },
      { id: "e2", name: "革故民兵", faction: "Reformist", provinceId: "shigu", troops: 104, maxTroops: 104, move: 1, maxMove: 1, level: 0, exp: 0, attack: 27 },
      { id: "e3", name: "西港殖民队", faction: "Foreign", provinceId: "xigang", troops: 118, maxTroops: 118, move: 1, maxMove: 1, level: 0, exp: 0, attack: 32 }
    ],
    battleRoles: defaultBattleRoles(),
    commonUnits: defaultCommonUnits(),
    terrainRules: [
      { id: "plain", name: "原", defenseInfantry: 0, defenseCavalry: 0, defenseArcher: 0, moveInfantry: 1, moveCavalry: 1, moveArcher: 1, color: "#626d45" },
      { id: "mountain", name: "山", defenseInfantry: 0, defenseCavalry: 5, defenseArcher: 5, moveInfantry: 2, moveCavalry: 3, moveArcher: 2, color: "#5c5140" },
      { id: "forest", name: "林", defenseInfantry: 0, defenseCavalry: 0, defenseArcher: 5, moveInfantry: 1, moveCavalry: 2, moveArcher: 1, color: "#45633a" },
      { id: "river", name: "河", defenseInfantry: 0, defenseCavalry: 0, defenseArcher: 0, moveInfantry: 2, moveCavalry: 3, moveArcher: 2, color: "#2e577a" },
      { id: "city", name: "城", defenseInfantry: 0, defenseCavalry: 5, defenseArcher: 5, moveInfantry: 1, moveCavalry: 1, moveArcher: 1, color: "#a88538" }
    ],
    battleCore: {
      playerObjectiveRequiredTurns: 2,
      enemyObjectiveRequiredTurns: 2,
      playerStartMorale: 1,
      enemyStartMorale: 0,
      victoryHighDefenseMerit: 50,
      victoryMidDefenseMerit: 24,
      victoryLowDefenseMerit: 15,
      defeatCommandLockTurns: 2,
      captureChancePercent: 5
    }
  };
}

function decodeCString(raw) {
  try {
    return JSON.parse(`"${raw.replace(/"/g, '\\"')}"`);
  } catch {
    return raw.replace(/\\n/g, "\n").replace(/\\"/g, '"').replace(/\\\\/g, "\\");
  }
}

function extractUiTextDefaults() {
  if (!fs.existsSync(sourcePath)) return [];
  const source = fs.readFileSync(sourcePath, "utf8");
  const rows = [];
  const re = /\bT(F?)\("([^"]+)",\s*"((?:\\.|[^"\\])*)"/g;
  let match;
  while ((match = re.exec(source))) {
    rows.push({ key: match[2], value: decodeCString(match[3]), note: match[1] ? "模板文案" : "界面文案" });
  }
  return rows;
}

function mergeByKey(defaultRows, currentRows, keyField = "key") {
  const byKey = new Map();
  for (const row of defaultRows || []) {
    if (row && row[keyField]) byKey.set(row[keyField], row);
  }
  for (const row of currentRows || []) {
    if (row && row[keyField]) byKey.set(row[keyField], { ...byKey.get(row[keyField]), ...row });
  }
  return Array.from(byKey.values());
}

function battleTerrainTiles() {
  const tiles = [];
  for (let r = 0; r < 7; r += 1) {
    for (let q = 0; q < 9; q += 1) {
      let terrain = "plain";
      if (q === 4 && r === 3) terrain = "city";
      else if ((q === 2 && r === 1) || (q === 6 && r === 4) || (q === 7 && r === 2)) terrain = "mountain";
      else if ((q + r) % 5 === 0 || (q === 1 && r === 4) || (q === 5 && r === 2)) terrain = "forest";
      else if ((r === 5 && q > 1 && q < 8) || (q === 3 && r === 3)) terrain = "river";
      tiles.push({ q, r, terrain });
    }
  }
  return tiles;
}

function additionalDefaults() {
  return {
    uiTexts: extractUiTextDefaults(),
    academyCore: {
      studyDays: 6,
      studyLowDailyMoodThreshold: 2,
      studyLowDailyMoodDelta: 1,
      campusWanderMinGain: 2,
      campusWanderMaxExclusive: 8,
      campusWanderMoodGain: 4,
      courseStaminaLossMin: 4,
      courseStaminaLossMaxExclusive: 10,
      courseMinStaminaLoss: 1,
      sundayRestMoodGain: 8,
      sundayRestStaminaGain: 18,
      sundayStudyBaseGain: 5,
      sundayStudyMoodDelta: -2,
      inviteGain: 8,
      inviteMoodGain: 2,
      friendGatheringGain: 3,
      friendGatheringTreasuryCost: 8,
      politicsMoodGain: 1,
      lowStaminaThreshold: 15,
      lowStaminaMoodPenalty: 5,
      holidayMoodGain: 5,
      holidayStaminaGain: 12,
      examWrittenMin: 24,
      examWrittenMaxExclusive: 61,
      examCourseScoreMultiplier: 5.5,
      examHighMoodThreshold: 75,
      examHighMoodBonus: 6,
      examLowMoodThreshold: 30,
      examLowMoodPenalty: -6
    },
    examRewards: [
      { minScore: 85, merit: 24, treasury: 35 },
      { minScore: 70, merit: 16, treasury: 22 },
      { minScore: 50, merit: 8, treasury: 10 },
      { minScore: 0, merit: 2, treasury: 0 }
    ],
    relationshipLevels: [
      { minAffection: 90, label: "莫逆", knownLevel: 4 },
      { minAffection: 75, label: "亲密", knownLevel: 3 },
      { minAffection: 50, label: "朋友", knownLevel: 2 },
      { minAffection: 10, label: "熟人", knownLevel: 1 },
      { minAffection: -30, label: "冷漠", knownLevel: 1 },
      { minAffection: -70, label: "敌对", knownLevel: 1 },
      { minAffection: -90, label: "仇视", knownLevel: 1 },
      { minAffection: -100, label: "死敌", knownLevel: 1 }
    ],
    beliefLevels: [
      { minAbsValue: 80, label: "狂热" },
      { minAbsValue: 60, label: "忠诚" },
      { minAbsValue: 40, label: "坚定" },
      { minAbsValue: 20, label: "倾向" },
      { minAbsValue: 1, label: "认可" },
      { minAbsValue: 0, label: "中立" }
    ],
    factions: [
      { id: "Player", displayName: "我方" },
      { id: "Imperial", displayName: "返乡团/朝廷" },
      { id: "Reformist", displayName: "革故派" },
      { id: "Native", displayName: "印第安乡党" },
      { id: "Foreign", displayName: "外邦势力" },
      { id: "Neutral", displayName: "中立" }
    ],
    ideologyAxes: [
      { id: "nation", label: "民族", negativeLabel: "皇汉", positiveLabel: "共治" },
      { id: "class", label: "阶级", negativeLabel: "君主", positiveLabel: "民主" },
      { id: "governance", label: "治国", negativeLabel: "独裁", positiveLabel: "共和" },
      { id: "region", label: "地域", negativeLabel: "统一", positiveLabel: "分裂" }
    ],
    politicsOptions: [
      { id: "home_voyage", label: "支持远航光复", stanceId: "home", stanceValue: 8, axisId: "region", axisValue: -5 },
      { id: "army_reform", label: "支持大陆军改革", stanceId: "army", stanceValue: 8, axisId: "governance", axisValue: -5 },
      { id: "native_co_rule", label: "主张族群共治", stanceId: "native", stanceValue: 8, axisId: "nation", axisValue: 6 },
      { id: "liberal_constitution", label: "主张议会立宪", stanceId: "liberal", stanceValue: 8, axisId: "class", axisValue: 6 },
      { id: "legal_order", label: "主张严法强国", stanceId: "legal", stanceValue: 8, axisId: "governance", axisValue: -6 }
    ],
    passiveSkills: [
      { id: "field_sense", name: "战场嗅觉", category: "机动", rarity: "普通", slot: "天赋", unlockKind: "always", unlockTarget: "", unlockValue: 0, description: "攻击+5%，开局士气+1。", attackPercent: 5, defensePercent: 0, hpPercent: 0, moveBonus: 0, moraleBonus: 1, supplySavePercent: 0, intelBonus: 0, expBonusPercent: 0 },
      { id: "iron_wall", name: "铁壁之心", category: "防御", rarity: "普通", slot: "学识", unlockKind: "trainingExp", unlockTarget: "", unlockValue: 50, description: "受到伤害-8%。", attackPercent: 0, defensePercent: 8, hpPercent: 0, moveBonus: 0, moraleBonus: 0, supplySavePercent: 0, intelBonus: 0, expBonusPercent: 0 },
      { id: "forced_march", name: "急掠如风", category: "机动", rarity: "普通", slot: "学识", unlockKind: "logisticsExp", unlockTarget: "", unlockValue: 50, description: "战斗移动+1。", attackPercent: 0, defensePercent: 0, hpPercent: 0, moveBonus: 1, moraleBonus: 0, supplySavePercent: 0, intelBonus: 0, expBonusPercent: 0 },
      { id: "veteran_drill", name: "身经百战", category: "指挥", rarity: "精锐", slot: "经验", unlockKind: "battleWins", unlockTarget: "", unlockValue: 1, description: "战斗经验+20%。", attackPercent: 0, defensePercent: 0, hpPercent: 0, moveBonus: 0, moraleBonus: 0, supplySavePercent: 0, intelBonus: 0, expBonusPercent: 20 },
      { id: "supply_master", name: "补给大师", category: "生存", rarity: "精锐", slot: "指挥", unlockKind: "logisticsExp", unlockTarget: "", unlockValue: 150, description: "补给消耗-25%。", attackPercent: 0, defensePercent: 0, hpPercent: 0, moveBonus: 0, moraleBonus: 0, supplySavePercent: 25, intelBonus: 0, expBonusPercent: 0 },
      { id: "precise_fire", name: "精准打击", category: "攻击", rarity: "精锐", slot: "经验", unlockKind: "artilleryExp", unlockTarget: "", unlockValue: 150, description: "攻击+12%。", attackPercent: 12, defensePercent: 0, hpPercent: 0, moveBonus: 0, moraleBonus: 0, supplySavePercent: 0, intelBonus: 0, expBonusPercent: 0 },
      { id: "covert_network", name: "暗线经营", category: "情报", rarity: "精锐", slot: "经验", unlockKind: "intelligence", unlockTarget: "", unlockValue: 30, description: "情报收益+3。", attackPercent: 0, defensePercent: 0, hpPercent: 0, moveBonus: 0, moraleBonus: 0, supplySavePercent: 0, intelBonus: 3, expBonusPercent: 0 },
      { id: "unyielding", name: "众志成城", category: "士气", rarity: "传说", slot: "指挥", unlockKind: "merit", unlockTarget: "", unlockValue: 300, description: "兵力+10%，士气+1。", attackPercent: 0, defensePercent: 0, hpPercent: 10, moveBonus: 0, moraleBonus: 1, supplySavePercent: 0, intelBonus: 0, expBonusPercent: 0 }
    ],
    quests: [
      { id: "main_01", type: "主线", name: "第一堂课", description: "完成任意一门课程。", unlockKind: "always", unlockTarget: "", unlockValue: 0, targetKind: "anyCourseExp", targetId: "", targetValue: 25, rewardMerit: 8, rewardTreasury: 20, rewardExpTarget: "trainingExp", rewardExp: 12, rewardAffectionTarget: "", rewardAffection: 0, rewardAchievement: "A01", nextQuestId: "main_02" },
      { id: "main_02", type: "主线", name: "夏季大演习", description: "赢得一场战斗。", unlockKind: "quest", unlockTarget: "main_01", unlockValue: 1, targetKind: "battleWins", targetId: "", targetValue: 1, rewardMerit: 18, rewardTreasury: 30, rewardExpTarget: "managementExp", rewardExp: 15, rewardAffectionTarget: "", rewardAffection: 0, rewardAchievement: "B01", nextQuestId: "main_03" },
      { id: "main_03", type: "主线", name: "暗线初成", description: "建立个人情报来源。", unlockKind: "quest", unlockTarget: "main_02", unlockValue: 1, targetKind: "spyNetwork", targetId: "", targetValue: 2, rewardMerit: 10, rewardTreasury: 10, rewardExpTarget: "logisticsExp", rewardExp: 12, rewardAffectionTarget: "", rewardAffection: 0, rewardAchievement: "I01", nextQuestId: "" },
      { id: "rel_zhao_01", type: "角色支线", name: "将门之后", description: "与赵伯衍好感达到50。", unlockKind: "always", unlockTarget: "", unlockValue: 0, targetKind: "relationship", targetId: "zhao", targetValue: 50, rewardMerit: 10, rewardTreasury: 0, rewardExpTarget: "infantryExp", rewardExp: 12, rewardAffectionTarget: "zhao", rewardAffection: 8, rewardAchievement: "", nextQuestId: "" },
      { id: "faction_liberal_01", type: "派系任务", name: "鼎新之光", description: "自由派立场达到40。", unlockKind: "always", unlockTarget: "", unlockValue: 0, targetKind: "stance", targetId: "liberal", targetValue: 40, rewardMerit: 12, rewardTreasury: 8, rewardExpTarget: "managementExp", rewardExp: 10, rewardAffectionTarget: "", rewardAffection: 0, rewardAchievement: "", nextQuestId: "" },
      { id: "daily_drill", type: "日常", name: "晨练操枪", description: "训练经验达到50。", unlockKind: "always", unlockTarget: "", unlockValue: 0, targetKind: "trainingExp", targetId: "", targetValue: 50, rewardMerit: 6, rewardTreasury: 0, rewardExpTarget: "infantryExp", rewardExp: 8, rewardAffectionTarget: "", rewardAffection: 0, rewardAchievement: "", nextQuestId: "" },
      { id: "intel_scout_01", type: "情报", name: "边境耳目", description: "情报值达到30。", unlockKind: "always", unlockTarget: "", unlockValue: 0, targetKind: "intelligence", targetId: "", targetValue: 30, rewardMerit: 8, rewardTreasury: 0, rewardExpTarget: "logisticsExp", rewardExp: 8, rewardAffectionTarget: "", rewardAffection: 0, rewardAchievement: "", nextQuestId: "" }
    ],
    achievements: [
      { id: "B01", category: "战斗", name: "初试锋芒", description: "赢得第一场战斗。", conditionKind: "battleWins", conditionTarget: "", conditionValue: 1, rewardTitle: "rookie", rewardPoints: 10, rarity: "铜" },
      { id: "B02", category: "战斗", name: "连战连胜", description: "累计赢得3场战斗。", conditionKind: "battleWins", conditionTarget: "", conditionValue: 3, rewardTitle: "veteran", rewardPoints: 30, rarity: "银" },
      { id: "A01", category: "养成", name: "军校生涯", description: "任意学科达到2级。", conditionKind: "anyCourseLevel", conditionTarget: "", conditionValue: 2, rewardTitle: "honor_student", rewardPoints: 10, rarity: "铜" },
      { id: "S01", category: "社交", name: "朋友之证", description: "任意角色好感达到50。", conditionKind: "anyRelationship", conditionTarget: "", conditionValue: 50, rewardTitle: "trusted_friend", rewardPoints: 20, rarity: "铜" },
      { id: "I01", category: "情报", name: "暗线初成", description: "间谍网络达到2。", conditionKind: "spyNetwork", conditionTarget: "", conditionValue: 2, rewardTitle: "shadow_listener", rewardPoints: 20, rarity: "铜" },
      { id: "Q01", category: "任务", name: "有令必达", description: "完成3个任务。", conditionKind: "questsCompleted", conditionTarget: "", conditionValue: 3, rewardTitle: "reliable_officer", rewardPoints: 20, rarity: "银" },
      { id: "NG01", category: "多周目", name: "记忆回响", description: "开启二周目。", conditionKind: "newGamePlus", conditionTarget: "", conditionValue: 1, rewardTitle: "echo_memory", rewardPoints: 50, rarity: "金" }
    ],
    titles: [
      { id: "rookie", name: "新兵", category: "军事", description: "第一次胜利的纪念。", attackBonus: 1, hpBonus: 0, socialBonus: 0, cultivationBonus: 0, intelligenceBonus: 0, supplyBonus: 0 },
      { id: "veteran", name: "老兵", category: "军事", description: "战斗经验带来的沉稳。", attackBonus: 2, hpBonus: 5, socialBonus: 0, cultivationBonus: 0, intelligenceBonus: 0, supplyBonus: 0 },
      { id: "honor_student", name: "优等生", category: "学术", description: "课程成长更快。", attackBonus: 0, hpBonus: 0, socialBonus: 0, cultivationBonus: 5, intelligenceBonus: 0, supplyBonus: 0 },
      { id: "trusted_friend", name: "挚友", category: "社交", description: "更容易获得信任。", attackBonus: 0, hpBonus: 0, socialBonus: 2, cultivationBonus: 0, intelligenceBonus: 0, supplyBonus: 0 },
      { id: "shadow_listener", name: "听影者", category: "情报", description: "情报收益提升。", attackBonus: 0, hpBonus: 0, socialBonus: 0, cultivationBonus: 0, intelligenceBonus: 2, supplyBonus: 0 },
      { id: "reliable_officer", name: "可靠军官", category: "任务", description: "补给上限提升。", attackBonus: 0, hpBonus: 0, socialBonus: 0, cultivationBonus: 0, intelligenceBonus: 0, supplyBonus: 4 },
      { id: "echo_memory", name: "记忆回响", category: "多周目", description: "保留另一条道路的余温。", attackBonus: 1, hpBonus: 4, socialBonus: 1, cultivationBonus: 3, intelligenceBonus: 1, supplyBonus: 2 }
    ],
    intelligenceActions: [
      { id: "scout", name: "刺探军情", type: "侦察", description: "确认敌军兵力并提高情报。", cost: 8, successRate: 78, risk: 10, intelGain: 10, spyNetworkGain: 1, enemyTroopDamage: 0, enemySupplyDamage: 0, targetFaction: "" },
      { id: "sabotage_supply", name: "破坏补给", type: "破坏", description: "降低敌军补给并造成少量损失。", cost: 14, successRate: 62, risk: 22, intelGain: 5, spyNetworkGain: 1, enemyTroopDamage: 4, enemySupplyDamage: 10, targetFaction: "" },
      { id: "rumor", name: "散布谣言", type: "扰乱", description: "削弱敌军战前状态。", cost: 10, successRate: 70, risk: 18, intelGain: 6, spyNetworkGain: 1, enemyTroopDamage: 0, enemySupplyDamage: 4, targetFaction: "" },
      { id: "counter_spy", name: "反间清查", type: "反间", description: "安全提升情报网络。", cost: 6, successRate: 84, risk: 6, intelGain: 4, spyNetworkGain: 2, enemyTroopDamage: 0, enemySupplyDamage: 0, targetFaction: "" }
    ],
    aiProfiles: [
      { id: "balanced", name: "均衡型", aggression: 100, caution: 100, focusFire: 100, retreatHpPercent: 25, terrainPreference: 100, objectiveBias: 100, guardBias: 90, flankBias: 80, rangedSpacing: 1, finishBias: 110, avoidCounter: 80 },
      { id: "aggressive", name: "激进型", aggression: 165, caution: 55, focusFire: 95, retreatHpPercent: 12, terrainPreference: 65, objectiveBias: 70, guardBias: 35, flankBias: 90, rangedSpacing: 1, finishBias: 155, avoidCounter: 35 },
      { id: "tactical", name: "智将型", aggression: 115, caution: 105, focusFire: 165, retreatHpPercent: 24, terrainPreference: 135, objectiveBias: 105, guardBias: 75, flankBias: 110, rangedSpacing: 2, finishBias: 145, avoidCounter: 115 },
      { id: "defensive", name: "防守型", aggression: 65, caution: 165, focusFire: 120, retreatHpPercent: 32, terrainPreference: 190, objectiveBias: 180, guardBias: 170, flankBias: 45, rangedSpacing: 2, finishBias: 95, avoidCounter: 145 },
      { id: "mobile", name: "机动型", aggression: 130, caution: 85, focusFire: 105, retreatHpPercent: 20, terrainPreference: 100, objectiveBias: 120, guardBias: 65, flankBias: 180, rangedSpacing: 2, finishBias: 125, avoidCounter: 75 },
      { id: "siege", name: "攻坚型", aggression: 120, caution: 115, focusFire: 135, retreatHpPercent: 22, terrainPreference: 120, objectiveBias: 190, guardBias: 105, flankBias: 70, rangedSpacing: 3, finishBias: 135, avoidCounter: 95 },
      { id: "skirmish", name: "游击型", aggression: 105, caution: 135, focusFire: 120, retreatHpPercent: 35, terrainPreference: 130, objectiveBias: 85, guardBias: 85, flankBias: 170, rangedSpacing: 3, finishBias: 115, avoidCounter: 160 },
      { id: "berserker", name: "狂热型", aggression: 190, caution: 30, focusFire: 70, retreatHpPercent: 5, terrainPreference: 45, objectiveBias: 55, guardBias: 15, flankBias: 120, rangedSpacing: 1, finishBias: 180, avoidCounter: 15 }
    ],
    supplyRules: [
      { id: "core", name: "标准补给", standbyCost: 2, moveCost: 3, attackCost: 5, moveAttackCost: 6, shortageThreshold: 8, shortageAttackPenalty: 5, shortageMoralePenalty: 1 }
    ],
    narrativeFragments: [
      { id: "nf_course_infantry_001", triggerKind: "course", triggerTarget: "infantry", minWeek: 1, maxWeek: 18, title: "操场边的旧口令", speaker: "赵伯衡", body: "步兵课结束后，赵伯衡低声纠正你的枪阵步点。他提到父辈口中的旧军号令，又突然收住话头：那套口令据说只在东渡前夜用过。", sceneId: "academy", relationshipTarget: "zhao", affectionDelta: 4, axisId: "region", axisDelta: -2, intelligenceDelta: 1, suspicionFaction: "Imperial", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_course_cavalry_001", triggerKind: "course", triggerTarget: "cavalry", minWeek: 2, maxWeek: 24, title: "马房里的铜印", speaker: "伊尔德", body: "马房角落的旧鞍袋里夹着一枚磨损铜印。伊尔德认出边地商路的纹样，说它不像军需官的东西，更像某个远航商团的信物。", sceneId: "academy", relationshipTarget: "yierde", affectionDelta: 4, axisId: "nation", axisDelta: 2, intelligenceDelta: 2, suspicionFaction: "Foreign", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_course_artillery_001", triggerKind: "course", triggerTarget: "artillery", minWeek: 3, maxWeek: 28, title: "靶场上的异式刻度", speaker: "李婉清", body: "炮兵靶场的旧火炮上刻着一组异式刻度。李婉清认为这是海图修正用的记号，若真如此，学院早在数年前就有人筹备过跨洋航线。", sceneId: "battlefield", relationshipTarget: "li", affectionDelta: 4, axisId: "governance", axisDelta: -2, intelligenceDelta: 2, suspicionFaction: "Foreign", suspicionDelta: 2, nextStoryId: "", once: "true" },
      { id: "nf_course_management_001", triggerKind: "course", triggerTarget: "management", minWeek: 4, maxWeek: 30, title: "账册空页", speaker: "林素心", body: "管理课的账册范本中少了一整页。林素心在页缝里找到淡墨痕迹：一串粮船编号被人为刮去，末尾留下「东」字残笔。", sceneId: "library", relationshipTarget: "lin", affectionDelta: 5, axisId: "class", axisDelta: 2, intelligenceDelta: 3, suspicionFaction: "Reformist", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_course_logistics_001", triggerKind: "course", triggerTarget: "logistics", minWeek: 5, maxWeek: 34, title: "被改写的补给线", speaker: "旁白", body: "后勤教官让你复盘一次失败转运。图上某条补给线被人用朱砂改过，改线后的终点不是前线，而是旧港仓库。", sceneId: "library", relationshipTarget: "", affectionDelta: 0, axisId: "region", axisDelta: -1, intelligenceDelta: 3, suspicionFaction: "Foreign", suspicionDelta: 2, nextStoryId: "", once: "true" },
      { id: "nf_course_training_001", triggerKind: "course", triggerTarget: "training", minWeek: 6, maxWeek: 36, title: "夜训后的暗号", speaker: "旁白", body: "夜训结束时，你在靶壕边听见两名低年级学生交换暗号。他们谈起一份「父辈留下的名单」，并在看见你后匆匆离开。", sceneId: "academy", relationshipTarget: "", affectionDelta: 0, axisId: "governance", axisDelta: 1, intelligenceDelta: 2, suspicionFaction: "Imperial", suspicionDelta: 2, nextStoryId: "", once: "true" },
      { id: "nf_social_zhao_001", triggerKind: "social", triggerTarget: "zhao", minWeek: 1, maxWeek: 44, title: "梦话里的海门", speaker: "赵伯衡", body: "邀约归来时，赵伯衡半醉半醒，说父亲曾把一封信藏在「海门之后」。醒来后他矢口否认，却答应帮你打听旧将门的传闻。", sceneId: "street", relationshipTarget: "zhao", affectionDelta: 6, axisId: "region", axisDelta: -3, intelligenceDelta: 2, suspicionFaction: "Imperial", suspicionDelta: 2, nextStoryId: "", once: "true" },
      { id: "nf_social_lin_001", triggerKind: "social", triggerTarget: "lin", minWeek: 1, maxWeek: 44, title: "林素心的索引卡", speaker: "林素心", body: "林素心把一张索引卡推到你面前：旧港税册、失踪粮船、归航名单，三条线在同一周交汇。她没有给结论，只提醒你别急着信任何派系。", sceneId: "library", relationshipTarget: "lin", affectionDelta: 6, axisId: "class", axisDelta: 3, intelligenceDelta: 3, suspicionFaction: "Reformist", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_social_yierde_001", triggerKind: "social", triggerTarget: "yierde", minWeek: 1, maxWeek: 44, title: "边地旧歌", speaker: "伊尔德", body: "伊尔德唱起一段边地旧歌，歌词里有「两岸皆非故乡」的句子。他说这歌常被商队用来记路线，也常被间谍用来传口令。", sceneId: "frontier", relationshipTarget: "yierde", affectionDelta: 6, axisId: "nation", axisDelta: 4, intelligenceDelta: 2, suspicionFaction: "Native", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_activity_drill_001", triggerKind: "activity", triggerTarget: "drill", minWeek: 1, maxWeek: 40, title: "演习里的空缺席位", speaker: "李婉清", body: "战术演练的编组表里多出一个空缺席位，代号「海灯」。李婉清认为这不是笔误，而是有人在演习中预留了一支不存在的部队。", sceneId: "battlefield", relationshipTarget: "li", affectionDelta: 4, axisId: "governance", axisDelta: -2, intelligenceDelta: 2, suspicionFaction: "Imperial", suspicionDelta: 2, nextStoryId: "", once: "true" },
      { id: "nf_activity_salon_001", triggerKind: "activity", triggerTarget: "salon", minWeek: 1, maxWeek: 40, title: "沙龙流言", speaker: "陈敬之", body: "沙龙里有人谈起学院地下室的旧档。陈敬之冷淡地警告你：越接近旧档，越会让讲究秩序的人把你视作麻烦。", sceneId: "council", relationshipTarget: "chen", affectionDelta: 3, axisId: "governance", axisDelta: -2, intelligenceDelta: 1, suspicionFaction: "Imperial", suspicionDelta: 2, nextStoryId: "", once: "true" },
      { id: "nf_rest_secret_letter_001", triggerKind: "rest", triggerTarget: "rest", minWeek: 1, maxWeek: 36, title: "父亲的封信", speaker: "旁白", body: "难得休息时，你整理行囊，摸到父亲临行前塞进夹层的蜡封信。信里只有一句话：若见东渡二字，先查许书院。", sceneId: "academy", relationshipTarget: "", affectionDelta: 0, axisId: "", axisDelta: 0, intelligenceDelta: 4, suspicionFaction: "Imperial", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_study_library_001", triggerKind: "study", triggerTarget: "study", minWeek: 1, maxWeek: 36, title: "许书院旧注", speaker: "旁白", body: "自习到深夜，你在旧书页边角看到「许书院」三字。旁边夹着一片船票残角，日期恰好在主角父亲失踪前七日。", sceneId: "library", relationshipTarget: "", affectionDelta: 0, axisId: "class", axisDelta: 1, intelligenceDelta: 4, suspicionFaction: "Foreign", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_intel_010", triggerKind: "intelligence", triggerTarget: "10", minWeek: 1, maxWeek: 999, title: "密档：第一枚钥匙", speaker: "旁白", body: "情报脉络初成，你终于能把父亲的信、旧港税册和海图刻度放在同一张纸上。它们指向同一个词：东渡。", sceneId: "library", relationshipTarget: "", affectionDelta: 0, axisId: "", axisDelta: 0, intelligenceDelta: 2, suspicionFaction: "Imperial", suspicionDelta: 1, nextStoryId: "", once: "true" },
      { id: "nf_intel_025", triggerKind: "intelligence", triggerTarget: "25", minWeek: 1, maxWeek: 999, title: "密档：旧港仓库", speaker: "旁白", body: "暗线传来回报，旧港仓库近年有一批账目被反复借阅。借阅人身份被涂黑，但签章属于学院军需处。", sceneId: "harbor", relationshipTarget: "", affectionDelta: 0, axisId: "", axisDelta: 0, intelligenceDelta: 3, suspicionFaction: "Foreign", suspicionDelta: 2, nextStoryId: "", once: "true" },
      { id: "nf_intel_050", triggerKind: "intelligence", triggerTarget: "50", minWeek: 1, maxWeek: 999, title: "密档：地下室门牌", speaker: "林素心", body: "林素心确认许书院旧址并非普通藏书楼，地下室门牌上刻着一串军中编号。你的父亲似乎不是旁观者，而是名单上的一员。", sceneId: "library", relationshipTarget: "lin", affectionDelta: 8, axisId: "class", axisDelta: 2, intelligenceDelta: 4, suspicionFaction: "Imperial", suspicionDelta: 3, nextStoryId: "EV002", once: "true" }
    ],
    battleUnitSpawns: defaultBattleUnitSpawns(),
    battleTerrainTiles: battleTerrainTiles(),
    battleRoleDamageRules: defaultBattleRoleDamageRules(),
    healthFactors: [
      { minFormation: 3, maxFormation: 99, minHpPercent: 80, numerator: 7, denominator: 7 },
      { minFormation: 3, maxFormation: 99, minHpPercent: 65, numerator: 6, denominator: 7 },
      { minFormation: 3, maxFormation: 99, minHpPercent: 50, numerator: 5, denominator: 7 },
      { minFormation: 3, maxFormation: 99, minHpPercent: 30, numerator: 4, denominator: 7 },
      { minFormation: 3, maxFormation: 99, minHpPercent: 15, numerator: 3, denominator: 7 },
      { minFormation: 3, maxFormation: 99, minHpPercent: 5, numerator: 2, denominator: 7 },
      { minFormation: 3, maxFormation: 99, minHpPercent: 0, numerator: 1, denominator: 7 },
      { minFormation: 2, maxFormation: 2, minHpPercent: 65, numerator: 6, denominator: 6 },
      { minFormation: 2, maxFormation: 2, minHpPercent: 50, numerator: 5, denominator: 6 },
      { minFormation: 2, maxFormation: 2, minHpPercent: 30, numerator: 4, denominator: 6 },
      { minFormation: 2, maxFormation: 2, minHpPercent: 15, numerator: 3, denominator: 6 },
      { minFormation: 2, maxFormation: 2, minHpPercent: 5, numerator: 2, denominator: 6 },
      { minFormation: 2, maxFormation: 2, minHpPercent: 0, numerator: 1, denominator: 6 },
      { minFormation: 0, maxFormation: 1, minHpPercent: 50, numerator: 5, denominator: 5 },
      { minFormation: 0, maxFormation: 1, minHpPercent: 30, numerator: 4, denominator: 5 },
      { minFormation: 0, maxFormation: 1, minHpPercent: 15, numerator: 3, denominator: 5 },
      { minFormation: 0, maxFormation: 1, minHpPercent: 5, numerator: 2, denominator: 5 },
      { minFormation: 0, maxFormation: 1, minHpPercent: 0, numerator: 1, denominator: 5 }
    ],
    battleCore: {
      hexCols: 9,
      hexRows: 7,
      objectiveQ: 4,
      objectiveR: 3,
      objectiveDefenseBonusPercent: 5,
      playerObjectiveRequiredTurns: 2,
      enemyObjectiveRequiredTurns: 2,
      playerStartMorale: 1,
      enemyStartMorale: 0,
      battleRandomMin: -4,
      battleRandomMaxExclusive: 7,
      aptitudeDamagePerLevel: 5,
      defenderLevelDamagePenalty: 2,
      counterDamagePercent: 55,
      minDamage: 4,
      minCounterDamage: 3,
      lowMoraleHpPercent: 30,
      minMorale: -2,
      maxMorale: 2,
      unitLevelMax: 5,
      unitLevelExpStep: 50,
      unitLevelAttackGain: 2,
      unitLevelHpGain: 8,
      battleExpHit: 12,
      battleExpKill: 24,
      armyLevelMax: 5,
      armyLevelExpStep: 50,
      armyLevelAttackGain: 3,
      armyLevelMaxTroopsGain: 15,
      armyLevelTroopsGain: 10,
      attackerArmyLevelHpPerLevel: 8,
      attackerArmyLevelAttackPerLevel: 2,
      attackerArmyLevelMoveBonusEveryLevels: 3,
      attackerArmyMaxMoveLevelBonusCap: 1,
      victoryArmyExp: 35,
      minTroopsAfterBattle: 20,
      defeatTroopDivisor: 2,
      victoryHighDefenseMerit: 50,
      victoryMidDefenseMerit: 24,
      victoryLowDefenseMerit: 15,
      defeatCommandLockTurns: 2,
      captureChancePercent: 5,
      strategySeasonTurnModulo: 4,
      strategyMissionCycleLength: 3,
      baseSupply: 8,
      supplyPerLogisticsLevel: 2,
      supplyTreasuryDivisor: 2,
      enemyPowerAttackMultiplier: 2,
      defenderPowerAttackMultiplier: 2,
      enemyPowerRandomMin: 0,
      enemyPowerRandomMaxExclusive: 35,
      enemyDefeatMinTroops: 12,
      enemyDefeatTroopLoss: 18,
      defenderVictoryTroopLoss: 12,
      formationDefaultCoefficient: 5,
      formationTwoCoefficient: 6,
      formationThreeCoefficient: 7
    }
  };
}

function mergeObject(defaultObject, currentObject) {
  return { ...(defaultObject || {}), ...(currentObject || {}) };
}

function defaultArmyMaxSupply(faction) {
  return faction === "Player" ? 42 : 36;
}

function defaultAiProfileForFaction(faction) {
  if (faction === "Imperial") return "defensive";
  if (faction === "Reformist") return "tactical";
  if (faction === "Native") return "mobile";
  if (faction === "Foreign") return "aggressive";
  return "balanced";
}

function normalizeArmyRow(row) {
  const maxSupply = Number(row.maxSupply || 0) || defaultArmyMaxSupply(row.faction);
  const supply = Number(row.supply || 0) || maxSupply;
  return {
    ...row,
    supply,
    maxSupply,
    aiProfile: row.aiProfile || defaultAiProfileForFaction(row.faction),
    intelLevel: Number(row.intelLevel || 0) || 0
  };
}

function normalizeConfig(config) {
  const base = defaultConfig();
  const extra = additionalDefaults();
  const normalized = { ...base, ...(config || {}) };
  normalized.uiTexts = mergeByKey([...(base.uiTexts || []), ...(extra.uiTexts || [])], normalized.uiTexts || []);
  const deprecatedUiKeys = new Set([
    "button.random_face",
    "character_create.appearance_changed",
    "character_create.random_face_done",
    "character_create.eyes",
    "character_create.skin",
    "character_create.body",
    "character_create.hair"
  ]);
  normalized.uiTexts = normalized.uiTexts.filter((row) => row && !deprecatedUiKeys.has(row.key));
  normalized.playerDefaults = mergeObject(base.playerDefaults, normalized.playerDefaults);
  delete normalized.playerDefaults.appearanceEyes;
  delete normalized.playerDefaults.appearanceSkin;
  delete normalized.playerDefaults.appearanceBody;
  delete normalized.playerDefaults.appearanceHair;
  normalized.calendar = mergeObject(base.calendar, normalized.calendar);
  normalized.academyCore = mergeObject(extra.academyCore, normalized.academyCore);
  normalized.battleCore = mergeObject(extra.battleCore, normalized.battleCore);
  normalized.traits = mergeByKey(base.traits || [], normalized.traits || [], "id");
  normalized.battleRoles = mergeByKey(base.battleRoles || [], normalized.battleRoles || [], "id");
  normalized.commonUnits = mergeByKey(base.commonUnits || [], normalized.commonUnits || [], "id");

  const defaultSpawns = base.battleUnitSpawns || extra.battleUnitSpawns || [];
  const defaultDamageRules = base.battleRoleDamageRules || extra.battleRoleDamageRules || [];
  const commonUnitNames = new Set((base.commonUnits || []).map((unit) => unit.name));
  if (
    !Array.isArray(normalized.battleUnitSpawns) ||
    normalized.battleUnitSpawns.length === 0 ||
    !normalized.battleUnitSpawns.some((spawn) => commonUnitNames.has(spawn.suffix))
  ) {
    normalized.battleUnitSpawns = defaultSpawns;
  }

  const damageRules = Array.isArray(normalized.battleRoleDamageRules) ? normalized.battleRoleDamageRules : [];
  const damageKeys = new Set(damageRules.map((row) => `${row.attackerRole || ""}->${row.defenderRole || ""}`));
  for (const row of defaultDamageRules) {
    const key = `${row.attackerRole}->${row.defenderRole}`;
    if (!damageKeys.has(key)) {
      damageRules.push(row);
      damageKeys.add(key);
    }
  }
  normalized.battleRoleDamageRules = damageRules;

  for (const key of [
    "traits", "characterOrigins", "creationMemories", "creationTalents", "creationSubjects", "news", "campusActivities", "narrativeFragments", "courses", "moodRules", "academyLevels", "ranks", "relationships", "stances",
    "provinces", "armies", "battleRoles", "terrainRules", "examRewards", "relationshipLevels", "beliefLevels", "factions",
    "ideologyAxes", "politicsOptions", "commonUnits", "battleUnitSpawns", "battleTerrainTiles", "battleRoleDamageRules", "healthFactors",
    "passiveSkills", "quests", "achievements", "titles", "intelligenceActions", "aiProfiles", "supplyRules"
  ]) {
    if (!Array.isArray(normalized[key]) || normalized[key].length === 0) normalized[key] = extra[key] || base[key] || [];
  }
  normalized.armies = (normalized.armies || []).map(normalizeArmyRow);
  return normalized;
}

function loadJson(file, fallback) {
  if (!fs.existsSync(file)) return fallback;
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function writeConfigIfMissing() {
  ensureDir(path.dirname(configPath));
  if (!fs.existsSync(configPath)) {
    fs.writeFileSync(configPath, `${JSON.stringify(defaultConfig(), null, 2)}\n`, "utf8");
  }
}

function flattenObject(obj) {
  return Object.entries(obj || {}).map(([key, value]) => ({ key, value: Array.isArray(value) ? value.join(";") : value }));
}

function main() {
  writeConfigIfMissing();
  ensureDir(outDir);
  const config = normalizeConfig(loadJson(configPath, defaultConfig()));
  fs.writeFileSync(configPath, `${JSON.stringify(config, null, 2)}\n`, "utf8");
  const story = loadJson(storyPath, { events: [], characters: [] });

  writeCsv("ui_texts", config.uiTexts || [], ["key", "value", "note"]);
  writeCsv("player_defaults", flattenObject(config.playerDefaults), ["key", "value"]);
  writeCsv("calendar", flattenObject(config.calendar), ["key", "value"]);
  writeCsv("traits", config.traits || [], ["id", "name", "description", "battleAttack", "battleHp", "battleMove", "socialBonus", "cultivationPercent", "staminaSave"]);
  writeCsv("character_origins", config.characterOrigins || [], ["id", "name", "subtitle", "description", "talentPool", "clueId", "clueName", "infantryExp", "cavalryExp", "artilleryExp", "managementExp", "logisticsExp", "trainingExp", "nationAxis", "classAxis", "governanceAxis", "regionAxis", "stanceHome", "stanceArmy", "stanceNative", "stanceLiberal", "stanceLegal", "relZhao", "relLin", "relYierde", "relChen", "relSu", "relLi"]);
  writeCsv("creation_memories", config.creationMemories || [], ["id", "title", "body", "optionAId", "optionAText", "optionATraitId", "optionANation", "optionAClass", "optionAGovernance", "optionARegion", "optionBId", "optionBText", "optionBTraitId", "optionBNation", "optionBClass", "optionBGovernance", "optionBRegion"]);
  writeCsv("creation_talents", config.creationTalents || [], ["id", "name", "category", "tier", "originTags", "description", "battleAttack", "battleHp", "battleMove", "socialBonus", "cultivationPercent", "staminaSave", "intelligenceBonus"]);
  writeCsv("creation_subjects", config.creationSubjects || [], ["id", "name", "target", "description", "expGain"]);
  writeCsv("passive_skills", config.passiveSkills || [], ["id", "name", "category", "rarity", "slot", "unlockKind", "unlockTarget", "unlockValue", "description", "attackPercent", "defensePercent", "hpPercent", "moveBonus", "moraleBonus", "supplySavePercent", "intelBonus", "expBonusPercent"]);
  writeCsv("quests", config.quests || [], ["id", "type", "name", "description", "unlockKind", "unlockTarget", "unlockValue", "targetKind", "targetId", "targetValue", "rewardMerit", "rewardTreasury", "rewardExpTarget", "rewardExp", "rewardAffectionTarget", "rewardAffection", "rewardAchievement", "nextQuestId"]);
  writeCsv("achievements", config.achievements || [], ["id", "category", "name", "description", "conditionKind", "conditionTarget", "conditionValue", "rewardTitle", "rewardPoints", "rarity"]);
  writeCsv("titles", config.titles || [], ["id", "name", "category", "description", "attackBonus", "hpBonus", "socialBonus", "cultivationBonus", "intelligenceBonus", "supplyBonus"]);
  writeCsv("intelligence_actions", config.intelligenceActions || [], ["id", "name", "type", "description", "cost", "successRate", "risk", "intelGain", "spyNetworkGain", "enemyTroopDamage", "enemySupplyDamage", "targetFaction"]);
  writeCsv("ai_profiles", config.aiProfiles || [], ["id", "name", "aggression", "caution", "focusFire", "retreatHpPercent", "terrainPreference", "objectiveBias", "guardBias", "flankBias", "rangedSpacing", "finishBias", "avoidCounter"]);
  writeCsv("supply_rules", config.supplyRules || [], ["id", "name", "standbyCost", "moveCost", "attackCost", "moveAttackCost", "shortageThreshold", "shortageAttackPenalty", "shortageMoralePenalty"]);
  writeCsv("news", config.news || [], ["id", "unlockWeek", "title", "source", "stanceHint", "body"]);
  writeCsv("campus_activities", config.campusActivities || [], ["id", "name", "description", "moodDelta", "meritDelta", "treasuryDelta", "socialGain", "trainingGain", "axisId", "axisDelta"]);
  writeCsv("narrative_fragments", config.narrativeFragments || [], ["id", "triggerKind", "triggerTarget", "minWeek", "maxWeek", "title", "speaker", "body", "sceneId", "relationshipTarget", "affectionDelta", "axisId", "axisDelta", "intelligenceDelta", "suspicionFaction", "suspicionDelta", "nextStoryId", "once"]);
  writeCsv("courses", config.courses || [], ["id", "label", "target"]);
  writeCsv("mood_rules", config.moodRules || [], ["minMood", "maxMood", "label", "studyMin", "studyMax"]);
  writeCsv("academy_levels", config.academyLevels || [], ["level", "floorExp", "nextExp"]);
  writeCsv("academy_core", flattenObject(config.academyCore), ["key", "value"]);
  writeCsv("exam_rewards", config.examRewards || [], ["minScore", "merit", "treasury"]);
  writeCsv("ranks", config.ranks || [], ["minMerit", "name", "commandLimit"]);
  writeCsv("relationship_levels", config.relationshipLevels || [], ["minAffection", "label", "knownLevel"]);
  writeCsv("belief_levels", config.beliefLevels || [], ["minAbsValue", "label"]);
  writeCsv("factions", config.factions || [], ["id", "displayName"]);
  writeCsv("ideology_axes", config.ideologyAxes || [], ["id", "label", "negativeLabel", "positiveLabel"]);
  writeCsv("politics_options", config.politicsOptions || [], ["id", "label", "stanceId", "stanceValue", "axisId", "axisValue"]);
  writeCsv("relationships", config.relationships || [], ["id", "name", "stance", "affection", "circle", "knownLevel", "lastInteractionWeek", "note"]);
  writeCsv("stances", config.stances || [], ["id", "name", "value"]);
  writeCsv("provinces", config.provinces || [], ["id", "name", "owner", "defense", "income", "x", "y", "roads", "armyId"]);
  writeCsv("armies", config.armies || [], ["id", "name", "faction", "provinceId", "troops", "maxTroops", "move", "maxMove", "level", "exp", "attack", "supply", "maxSupply", "aiProfile", "intelLevel"]);
  writeCsv("battle_roles", config.battleRoles || [], ["id", "displayName", "symbol", "baseHp", "move", "range", "attackBonus", "formation"]);
  writeCsv("common_units", config.commonUnits || [], ["id", "name", "keyword", "role", "asset", "idleFrames", "moveFrames", "attackFrames", "hitFrames"]);
  writeCsv("terrain_rules", config.terrainRules || [], ["id", "name", "defenseInfantry", "defenseCavalry", "defenseArcher", "moveInfantry", "moveCavalry", "moveArcher", "color"]);
  writeCsv("battle_unit_spawns", config.battleUnitSpawns || [], ["side", "suffix", "role", "q", "r", "attackBonus", "troopDivisor"]);
  writeCsv("battle_terrain_tiles", config.battleTerrainTiles || [], ["q", "r", "terrain"]);
  writeCsv("battle_role_damage_rules", config.battleRoleDamageRules || [], ["attackerRole", "defenderRole", "modifier"]);
  writeCsv("health_factors", config.healthFactors || [], ["minFormation", "maxFormation", "minHpPercent", "numerator", "denominator"]);
  writeCsv("battle_core", flattenObject(config.battleCore), ["key", "value"]);

  writeCsv("story_characters", story.characters || [], ["name", "identity", "faction", "kind", "traits", "background", "tasks", "nodes", "portrait", "asset"]);
  writeCsv("story_events", story.events || [], ["id", "type", "chapter", "trigger", "jump", "unlockKind", "unlockTarget", "unlockValue", "unlockHint"]);
  const lines = [];
  const choices = [];
  for (const ev of story.events || []) {
    (ev.lines || []).forEach((line, lineIndex) => {
      lines.push({ eventId: ev.id, lineIndex, speaker: line.speaker, text: line.text, portrait: line.portrait });
    });
    (ev.choices || []).forEach((choice, choiceIndex) => {
      choices.push({
        eventId: ev.id,
        choiceIndex,
        id: choice.id,
        label: choice.label,
        text: choice.text,
        speaker: choice.speaker,
        portrait: choice.portrait,
        nextEventId: choice.nextEventId,
        effectsJson: JSON.stringify(choice.effects || [])
      });
    });
  }
  writeCsv("story_lines", lines, ["eventId", "lineIndex", "speaker", "text", "portrait"]);
  writeCsv("story_choices", choices, ["eventId", "choiceIndex", "id", "label", "text", "speaker", "portrait", "nextEventId", "effectsJson"]);

  console.log(`Exported ${fs.readdirSync(outDir).filter((f) => f.endsWith(".csv")).length} CSV tables to ${outDir}`);
}

main();
