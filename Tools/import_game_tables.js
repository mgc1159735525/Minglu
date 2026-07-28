const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "..");
const configPath = path.join(projectRoot, "Assets", "Resources", "Data", "MingLuGameConfig.json");
const storyPath = path.join(projectRoot, "Assets", "Resources", "MingLuStoryData.json");
const csvDir = path.join(projectRoot, "DataTables", "csv");

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
  if (rows.length === 0) return [];
  const headers = rows[0];
  return rows.slice(1).filter((r) => r.some((v) => v !== "")).map((r) => {
    const obj = {};
    headers.forEach((h, i) => {
      obj[h] = r[i] ?? "";
    });
    return obj;
  });
}

function readCsv(name) {
  const file = path.join(csvDir, `${name}.csv`);
  if (!fs.existsSync(file)) return null;
  return parseCsv(fs.readFileSync(file, "utf8"));
}

function asNumber(value) {
  if (value === "" || value === null || value === undefined) return 0;
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function castRows(rows, numericFields) {
  if (!rows) return null;
  const nums = new Set(numericFields);
  return rows.map((row) => {
    const out = {};
    for (const [key, value] of Object.entries(row)) {
      out[key] = nums.has(key) ? asNumber(value) : value;
    }
    return out;
  });
}

function keyValueObject(rows, existing = {}) {
  if (!rows) return existing;
  const out = { ...existing };
  for (const row of rows) {
    if (!row.key) continue;
    let value = row.value;
    if (/^-?\d+(\.\d+)?$/.test(value)) value = Number(value);
    if (["traits", "creationMemoryChoices", "subjectFocusIds", "prisoners", "unlockedSkills", "equippedSkills", "activeQuests", "completedQuests", "unlockedAchievements", "unlockedTitles", "unlockedEndings", "eventReview"].includes(row.key)) {
      value = value ? String(value).split(";").filter(Boolean) : [];
    }
    out[row.key] = value;
  }
  return out;
}

function loadJson(file, fallback) {
  if (!fs.existsSync(file)) return fallback;
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function sortByNumber(rows, field) {
  return rows.sort((a, b) => asNumber(a[field]) - asNumber(b[field]));
}

function main() {
  if (!fs.existsSync(csvDir)) {
    throw new Error(`Missing CSV directory: ${csvDir}. Run export_game_tables.js first.`);
  }

  const config = loadJson(configPath, {});
  const story = loadJson(storyPath, { events: [], characters: [] });

  config.uiTexts = readCsv("ui_texts") || config.uiTexts || [];
  config.playerDefaults = keyValueObject(readCsv("player_defaults"), config.playerDefaults || {});
  config.calendar = keyValueObject(readCsv("calendar"), config.calendar || {});
  config.traits = castRows(readCsv("traits"), ["battleAttack", "battleHp", "battleMove", "socialBonus", "cultivationPercent", "staminaSave"]) || config.traits || [];
  config.characterOrigins = castRows(readCsv("character_origins"), ["infantryExp", "cavalryExp", "artilleryExp", "managementExp", "logisticsExp", "trainingExp", "nationAxis", "classAxis", "governanceAxis", "regionAxis", "stanceHome", "stanceArmy", "stanceNative", "stanceLiberal", "stanceLegal", "relZhao", "relLin", "relYierde", "relChen", "relSu", "relLi"]) || config.characterOrigins || [];
  config.creationMemories = castRows(readCsv("creation_memories"), ["optionANation", "optionAClass", "optionAGovernance", "optionARegion", "optionBNation", "optionBClass", "optionBGovernance", "optionBRegion"]) || config.creationMemories || [];
  config.creationTalents = castRows(readCsv("creation_talents"), ["tier", "battleAttack", "battleHp", "battleMove", "socialBonus", "cultivationPercent", "staminaSave", "intelligenceBonus"]) || config.creationTalents || [];
  config.creationSubjects = castRows(readCsv("creation_subjects"), ["expGain"]) || config.creationSubjects || [];
  config.passiveSkills = castRows(readCsv("passive_skills"), ["unlockValue", "attackPercent", "defensePercent", "hpPercent", "moveBonus", "moraleBonus", "supplySavePercent", "intelBonus", "expBonusPercent"]) || config.passiveSkills || [];
  config.quests = castRows(readCsv("quests"), ["unlockValue", "targetValue", "rewardMerit", "rewardTreasury", "rewardExp", "rewardAffection"]) || config.quests || [];
  config.achievements = castRows(readCsv("achievements"), ["conditionValue", "rewardPoints"]) || config.achievements || [];
  config.titles = castRows(readCsv("titles"), ["attackBonus", "hpBonus", "socialBonus", "cultivationBonus", "intelligenceBonus", "supplyBonus"]) || config.titles || [];
  config.intelligenceActions = castRows(readCsv("intelligence_actions"), ["cost", "successRate", "risk", "intelGain", "spyNetworkGain", "enemyTroopDamage", "enemySupplyDamage"]) || config.intelligenceActions || [];
  config.aiProfiles = castRows(readCsv("ai_profiles"), ["aggression", "caution", "focusFire", "retreatHpPercent", "terrainPreference", "objectiveBias", "guardBias", "flankBias", "rangedSpacing", "finishBias", "avoidCounter"]) || config.aiProfiles || [];
  config.supplyRules = castRows(readCsv("supply_rules"), ["standbyCost", "moveCost", "attackCost", "moveAttackCost", "shortageThreshold", "shortageAttackPenalty", "shortageMoralePenalty"]) || config.supplyRules || [];
  config.news = castRows(readCsv("news"), ["unlockWeek"]) || config.news || [];
  config.campusActivities = castRows(readCsv("campus_activities"), ["moodDelta", "meritDelta", "treasuryDelta", "socialGain", "trainingGain", "axisDelta"]) || config.campusActivities || [];
  config.narrativeFragments = castRows(readCsv("narrative_fragments"), ["minWeek", "maxWeek", "affectionDelta", "axisDelta", "intelligenceDelta", "suspicionDelta"]) || config.narrativeFragments || [];
  config.courses = readCsv("courses") || config.courses || [];
  config.moodRules = castRows(readCsv("mood_rules"), ["minMood", "maxMood", "studyMin", "studyMax"]) || config.moodRules || [];
  config.academyLevels = castRows(readCsv("academy_levels"), ["level", "floorExp", "nextExp"]) || config.academyLevels || [];
  config.academyCore = keyValueObject(readCsv("academy_core"), config.academyCore || {});
  config.examRewards = castRows(readCsv("exam_rewards"), ["minScore", "merit", "treasury"]) || config.examRewards || [];
  config.ranks = castRows(readCsv("ranks"), ["minMerit", "commandLimit"]) || config.ranks || [];
  config.relationshipLevels = castRows(readCsv("relationship_levels"), ["minAffection", "knownLevel"]) || config.relationshipLevels || [];
  config.beliefLevels = castRows(readCsv("belief_levels"), ["minAbsValue"]) || config.beliefLevels || [];
  config.factions = readCsv("factions") || config.factions || [];
  config.ideologyAxes = readCsv("ideology_axes") || config.ideologyAxes || [];
  config.politicsOptions = castRows(readCsv("politics_options"), ["stanceValue", "axisValue"]) || config.politicsOptions || [];
  config.relationships = castRows(readCsv("relationships"), ["affection", "knownLevel", "lastInteractionWeek"]) || config.relationships || [];
  config.stances = castRows(readCsv("stances"), ["value"]) || config.stances || [];
  config.provinces = castRows(readCsv("provinces"), ["defense", "income", "x", "y"]) || config.provinces || [];
  config.armies = castRows(readCsv("armies"), ["troops", "maxTroops", "move", "maxMove", "level", "exp", "attack", "supply", "maxSupply", "intelLevel"]) || config.armies || [];
  config.battleRoles = castRows(readCsv("battle_roles"), ["baseHp", "move", "range", "attackBonus", "formation"]) || config.battleRoles || [];
  config.commonUnits = castRows(readCsv("common_units"), ["idleFrames", "moveFrames", "attackFrames", "hitFrames"]) || config.commonUnits || [];
  config.terrainRules = castRows(readCsv("terrain_rules"), ["defenseInfantry", "defenseCavalry", "defenseArcher", "moveInfantry", "moveCavalry", "moveArcher"]) || config.terrainRules || [];
  config.battleUnitSpawns = castRows(readCsv("battle_unit_spawns"), ["q", "r", "attackBonus", "troopDivisor"]) || config.battleUnitSpawns || [];
  config.battleTerrainTiles = castRows(readCsv("battle_terrain_tiles"), ["q", "r"]) || config.battleTerrainTiles || [];
  config.battleRoleDamageRules = castRows(readCsv("battle_role_damage_rules"), ["modifier"]) || config.battleRoleDamageRules || [];
  config.healthFactors = castRows(readCsv("health_factors"), ["minFormation", "maxFormation", "minHpPercent", "numerator", "denominator"]) || config.healthFactors || [];
  config.battleCore = keyValueObject(readCsv("battle_core"), config.battleCore || {});

  const storyCharacters = readCsv("story_characters");
  if (storyCharacters) story.characters = storyCharacters;

  const eventRows = readCsv("story_events");
  const lineRows = castRows(readCsv("story_lines"), ["lineIndex"]);
  const choiceRows = castRows(readCsv("story_choices"), ["choiceIndex"]);
  if (eventRows) {
    story.events = eventRows.map((ev) => {
      const lines = sortByNumber((lineRows || []).filter((line) => line.eventId === ev.id), "lineIndex")
        .map((line) => ({ speaker: line.speaker, text: line.text, portrait: line.portrait }));
      const choices = sortByNumber((choiceRows || []).filter((choice) => choice.eventId === ev.id), "choiceIndex")
        .map((choice) => {
          let effects = [];
          if (choice.effectsJson) {
            try {
              effects = JSON.parse(choice.effectsJson);
            } catch {
              effects = [];
            }
          }
          return {
            id: choice.id,
            label: choice.label,
            text: choice.text,
            speaker: choice.speaker,
            portrait: choice.portrait,
            nextEventId: choice.nextEventId,
            effects
          };
        });
      return {
        id: ev.id,
        type: ev.type,
        chapter: ev.chapter,
        trigger: ev.trigger,
        jump: ev.jump,
        unlockKind: ev.unlockKind,
        unlockTarget: ev.unlockTarget,
        unlockValue: asNumber(ev.unlockValue),
        unlockHint: ev.unlockHint,
        lines,
        choices
      };
    });
  }

  fs.writeFileSync(configPath, `${JSON.stringify(config, null, 2)}\n`, "utf8");
  fs.writeFileSync(storyPath, `${JSON.stringify(story, null, 2)}\n`, "utf8");
  console.log(`Imported CSV tables from ${csvDir}`);
  console.log(`Updated ${configPath}`);
  console.log(`Updated ${storyPath}`);
}

main();
