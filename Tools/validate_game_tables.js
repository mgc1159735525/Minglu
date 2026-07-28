const fs = require("fs");
const path = require("path");

const projectRoot = path.resolve(__dirname, "..");
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
  return rows.filter(r => r.some(c => String(c || "").trim().length > 0));
}

function loadTable(name) {
  const file = path.join(csvDir, `${name}.csv`);
  if (!fs.existsSync(file)) return { headers: [], rows: [], file };
  const parsed = parseCsv(fs.readFileSync(file, "utf8"));
  const headers = parsed.shift() || [];
  const rows = parsed.map((values, index) => {
    const row = { __row: index + 2 };
    headers.forEach((header, i) => {
      row[header] = values[i] || "";
    });
    return row;
  });
  return { headers, rows, file };
}

const errors = [];
const warnings = [];

function issue(list, code, message) {
  list.push(`[${code}] ${message}`);
}

function requireTable(name, requiredHeaders) {
  const table = loadTable(name);
  if (!fs.existsSync(table.file)) {
    issue(errors, "missing_table", `${name}.csv not found`);
    return table;
  }
  for (const header of requiredHeaders) {
    if (!table.headers.includes(header)) {
      issue(errors, "missing_column", `${name}.csv missing column "${header}"`);
    }
  }
  return table;
}

function checkDuplicate(tableName, table, key) {
  if (!table.headers.includes(key)) return;
  const seen = new Map();
  for (const row of table.rows) {
    const value = String(row[key] || "").trim();
    if (!value) {
      issue(errors, "blank_id", `${tableName}.csv row ${row.__row} has blank ${key}`);
      continue;
    }
    if (seen.has(value)) {
      issue(errors, "duplicate_id", `${tableName}.csv duplicate ${key} "${value}" at rows ${seen.get(value)} and ${row.__row}`);
    } else {
      seen.set(value, row.__row);
    }
  }
}

const tableFiles = fs.existsSync(csvDir)
  ? fs.readdirSync(csvDir).filter(file => file.endsWith(".csv"))
  : [];

for (const file of tableFiles) {
  const name = path.basename(file, ".csv");
  const table = loadTable(name);
  if (table.headers.includes("id")) checkDuplicate(name, table, "id");
}

const ui = requireTable("ui_texts", ["key", "value"]);
checkDuplicate("ui_texts", ui, "key");
for (const row of ui.rows) {
  if (!String(row.value || "").trim()) issue(warnings, "blank_ui_text", `ui_texts.csv row ${row.__row} has blank value for "${row.key}"`);
}

const origins = requireTable("character_origins", ["id", "name", "talentPool"]);
const memories = requireTable("creation_memories", ["id", "title", "optionAId", "optionAText", "optionATraitId", "optionBId", "optionBText", "optionBTraitId"]);
const creationTalents = requireTable("creation_talents", ["id", "name", "originTags", "description"]);
const creationSubjects = requireTable("creation_subjects", ["id", "name", "target", "expGain"]);
checkDuplicate("character_origins", origins, "id");
checkDuplicate("creation_memories", memories, "id");
checkDuplicate("creation_talents", creationTalents, "id");
checkDuplicate("creation_subjects", creationSubjects, "id");
const originIds = new Set(origins.rows.map(row => String(row.id || "").trim()).filter(Boolean));
const validSubjectTargets = new Set(["infantryExp", "cavalryExp", "artilleryExp", "managementExp", "logisticsExp", "trainingExp"]);
for (const row of memories.rows) {
  if (!String(row.optionAId || "").trim() || !String(row.optionBId || "").trim()) issue(errors, "bad_memory_option", `creation_memories.csv row ${row.__row} has blank option id`);
  if (!String(row.optionAText || "").trim() || !String(row.optionBText || "").trim()) issue(errors, "blank_memory_option", `creation_memories.csv row ${row.__row} has blank option text`);
}
for (const row of creationTalents.rows) {
  const tags = String(row.originTags || "").split(/[;|]/).map(v => v.trim()).filter(Boolean);
  for (const tag of tags) {
    if (!originIds.has(tag)) issue(warnings, "bad_talent_origin", `creation_talents.csv row ${row.__row} references unknown origin "${tag}"`);
  }
}
for (const row of creationSubjects.rows) {
  const target = String(row.target || "").trim();
  if (!validSubjectTargets.has(target)) issue(errors, "bad_subject_target", `creation_subjects.csv row ${row.__row} has unsupported target "${target}"`);
}

const aiProfiles = requireTable("ai_profiles", ["id", "name", "aggression", "caution", "focusFire", "retreatHpPercent", "terrainPreference", "objectiveBias", "guardBias", "flankBias", "rangedSpacing", "finishBias", "avoidCounter"]);
checkDuplicate("ai_profiles", aiProfiles, "id");
for (const row of aiProfiles.rows) {
  for (const field of ["aggression", "caution", "focusFire", "terrainPreference", "objectiveBias", "guardBias", "flankBias", "finishBias", "avoidCounter"]) {
    const value = Number(row[field]);
    if (!Number.isFinite(value) || value < 0) issue(errors, "bad_ai_weight", `ai_profiles.csv row ${row.__row} field "${field}" must be a non-negative number`);
  }
  const retreat = Number(row.retreatHpPercent);
  if (!Number.isFinite(retreat) || retreat < 0 || retreat > 100) issue(errors, "bad_ai_retreat", `ai_profiles.csv row ${row.__row} retreatHpPercent must be 0-100`);
  const spacing = Number(row.rangedSpacing);
  if (!Number.isFinite(spacing) || spacing < 1 || spacing > 6) issue(errors, "bad_ai_spacing", `ai_profiles.csv row ${row.__row} rangedSpacing must be 1-6`);
}

const quests = requireTable("quests", ["id", "unlockKind", "unlockTarget", "unlockValue", "targetKind", "targetValue"]);
const events = requireTable("story_events", ["id", "type", "jump", "unlockKind", "unlockTarget", "unlockValue", "unlockHint"]);
const lines = requireTable("story_lines", ["eventId", "text"]);
const choices = requireTable("story_choices", ["eventId", "choiceIndex", "label", "nextEventId", "effectsJson"]);
const fragments = requireTable("narrative_fragments", ["id", "triggerKind", "triggerTarget", "title", "body", "sceneId", "once"]);
const eventIds = new Set(events.rows.map(row => String(row.id || "").trim()).filter(Boolean));
const questIds = new Set(quests.rows.map(row => String(row.id || "").trim()).filter(Boolean));
const choicesByEvent = new Map();
const allowedConditionKinds = new Set([
  "", "always", "quest", "story", "skill", "battleWins", "battleLosses", "battlesFought", "enemiesDefeated",
  "questsCompleted", "spySuccesses", "supplyBreaks", "intelligence", "spyNetwork", "storyValue", "suspicion",
  "origin", "trait", "talent", "subject", "memory", "merit", "newGamePlus", "infantryExp", "cavalryExp",
  "artilleryExp", "managementExp", "logisticsExp", "trainingExp", "anyCourseExp", "anyCourseLevel",
  "anyRelationship", "relationship", "stance"
]);

for (const row of quests.rows) {
  const id = String(row.id || "").trim();
  const unlockKind = String(row.unlockKind || "").trim();
  const unlockTarget = String(row.unlockTarget || "").trim();
  const targetKind = String(row.targetKind || "").trim();
  if (!allowedConditionKinds.has(unlockKind)) issue(errors, "bad_quest_unlock", `quests.csv row ${row.__row} "${id}" has unsupported unlockKind "${unlockKind}"`);
  if (!allowedConditionKinds.has(targetKind)) issue(errors, "bad_quest_target", `quests.csv row ${row.__row} "${id}" has unsupported targetKind "${targetKind}"`);
  if (unlockKind === "quest" && (!unlockTarget || !questIds.has(unlockTarget))) issue(errors, "bad_quest_unlock_target", `quests.csv row ${row.__row} "${id}" references missing quest unlockTarget "${unlockTarget}"`);
  if (unlockKind === "story" && (!unlockTarget || !resolvesStoryTarget(unlockTarget, eventIds))) issue(errors, "bad_quest_unlock_target", `quests.csv row ${row.__row} "${id}" references missing story unlockTarget "${unlockTarget}"`);
}

const allowedFragmentTriggers = new Set(["course", "social", "activity", "rest", "study", "intelligence"]);
for (const row of fragments.rows) {
  const id = String(row.id || "").trim();
  const triggerKind = String(row.triggerKind || "").trim();
  if (id && !allowedFragmentTriggers.has(triggerKind)) issue(errors, "bad_fragment_trigger", `narrative_fragments.csv row ${row.__row} "${id}" has unsupported triggerKind "${triggerKind}"`);
  if (id && !String(row.title || "").trim()) issue(errors, "blank_fragment_title", `narrative_fragments.csv row ${row.__row} "${id}" has blank title`);
  if (id && !String(row.body || "").trim()) issue(errors, "blank_fragment_body", `narrative_fragments.csv row ${row.__row} "${id}" has blank body`);
}

for (const row of lines.rows) {
  const eventId = String(row.eventId || "").trim();
  if (eventId && !eventIds.has(eventId)) issue(errors, "bad_story_line", `story_lines.csv row ${row.__row} references missing event "${eventId}"`);
  if (eventId && !String(row.text || "").trim()) issue(warnings, "blank_story_line", `story_lines.csv row ${row.__row} has blank text`);
}

const choiceKeys = new Set();
for (const row of choices.rows) {
  const eventId = String(row.eventId || "").trim();
  const choiceIndex = String(row.choiceIndex || "").trim();
  if (eventId && !eventIds.has(eventId)) issue(errors, "bad_story_choice", `story_choices.csv row ${row.__row} references missing event "${eventId}"`);
  if (!choicesByEvent.has(eventId)) choicesByEvent.set(eventId, []);
  choicesByEvent.get(eventId).push(row);
  const key = `${eventId}:${choiceIndex}`;
  if (choiceKeys.has(key)) issue(errors, "duplicate_choice", `story_choices.csv duplicate eventId/choiceIndex "${key}"`);
  choiceKeys.add(key);
  if (!String(row.label || "").trim()) issue(errors, "blank_choice_label", `story_choices.csv row ${row.__row} has blank label`);
  if (String(row.label || "").length > 90) issue(warnings, "long_choice_label", `story_choices.csv row ${row.__row} label is long; consider moving prose into text`);
  const next = String(row.nextEventId || "").trim();
  if (next && !resolvesStoryTarget(next, eventIds)) issue(warnings, "bad_choice_next", `story_choices.csv row ${row.__row} nextEventId "${next}" not found; runtime will fall back to the next main event`);
  const effects = String(row.effectsJson || "").trim();
  if (effects) {
    try {
      const parsed = JSON.parse(effects);
      if (!Array.isArray(parsed)) issue(errors, "bad_effects_json", `story_choices.csv row ${row.__row} effectsJson must be an array`);
    } catch (error) {
      issue(errors, "bad_effects_json", `story_choices.csv row ${row.__row} effectsJson parse failed: ${error.message}`);
    }
  }
}

for (const row of events.rows) {
  const id = String(row.id || "").trim();
  const type = String(row.type || "");
  const unlockKind = String(row.unlockKind || "").trim();
  const unlockTarget = String(row.unlockTarget || "").trim();
  const hasChoices = (choicesByEvent.get(id) || []).length > 0;
  if (!allowedConditionKinds.has(unlockKind)) issue(errors, "bad_story_unlock", `story_events.csv row ${row.__row} "${id}" has unsupported unlockKind "${unlockKind}"`);
  if (unlockKind === "story" && (!unlockTarget || !resolvesStoryTarget(unlockTarget, eventIds))) issue(errors, "bad_story_unlock_target", `story_events.csv row ${row.__row} "${id}" references missing story unlockTarget "${unlockTarget}"`);
  if (unlockKind === "quest" && (!unlockTarget || !questIds.has(unlockTarget))) issue(errors, "bad_story_unlock_target", `story_events.csv row ${row.__row} "${id}" references missing quest unlockTarget "${unlockTarget}"`);
  if (["skill", "origin", "trait", "talent", "subject", "memory", "relationship", "stance", "storyValue", "suspicion"].includes(unlockKind) && !unlockTarget) issue(warnings, "blank_story_unlock_target", `story_events.csv row ${row.__row} "${id}" uses unlockKind "${unlockKind}" without unlockTarget`);
  if (type.includes("选") && !hasChoices) issue(errors, "choice_event_without_choices", `story_events.csv row ${row.__row} "${id}" is an option event but has no choices`);
  if (!hasChoices && !String(row.jump || "").trim() && !/^EV\d{3}$/.test(id)) issue(warnings, "linear_event", `story_events.csv row ${row.__row} "${id}" has no configured choice; runtime will show Continue/Back only`);
}

const summary = {
  tables: tableFiles.length,
  uiTexts: ui.rows.length,
  characterOrigins: origins.rows.length,
  creationTalents: creationTalents.rows.length,
  storyEvents: events.rows.length,
  storyChoices: choices.rows.length,
  narrativeFragments: fragments.rows.length,
  errors: errors.length,
  warnings: warnings.length,
};

console.log(`MingLu table validation: ${JSON.stringify(summary)}`);
for (const line of errors.slice(0, 80)) console.error(line);
if (errors.length > 80) console.error(`[more_errors] ${errors.length - 80} more errors hidden`);
for (const line of warnings.slice(0, 80)) console.warn(line);
if (warnings.length > 80) console.warn(`[more_warnings] ${warnings.length - 80} more warnings hidden`);

process.exit(errors.length > 0 ? 1 : 0);

function resolvesStoryTarget(raw, ids) {
  if (!raw) return true;
  const id = String(raw).trim();
  if (ids.has(id)) return true;
  if (id.startsWith("END-")) {
    const n = Number.parseInt(id.slice(4), 10);
    if (Number.isFinite(n) && ids.has(`END-${String(n).padStart(2, "0")}`)) return true;
  }
  const dash = id.indexOf("-");
  if (dash > 0 && ids.has(id.slice(0, dash))) return true;
  return false;
}
