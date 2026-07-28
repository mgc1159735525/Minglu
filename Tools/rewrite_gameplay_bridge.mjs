import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.resolve(__dirname, "..");
const configPath = path.join(projectRoot, "Assets", "Resources", "Data", "MingLuGameConfig.json");
const storyPath = path.join(projectRoot, "Assets", "Resources", "MingLuStoryData.json");

const config = JSON.parse(fs.readFileSync(configPath, "utf8"));
const story = JSON.parse(fs.readFileSync(storyPath, "utf8"));

function upsertUi(key, value) {
  config.uiTexts = config.uiTexts || [];
  const row = config.uiTexts.find((item) => item.key === key);
  if (row) row.value = value;
  else config.uiTexts.push({ key, value });
}

function eventById(id) {
  return (story.events || []).find((event) => event.id === id);
}

function choice(id, label, nextEventId, effects = []) {
  return {
    id,
    label,
    text: label,
    speaker: "主角",
    portrait: "主角",
    nextEventId,
    effects
  };
}

function setEvent(id, patch) {
  const event = eventById(id);
  if (!event) return;
  Object.assign(event, patch);
  if (patch.lines) event.lines = patch.lines;
  if (patch.choices) event.choices = patch.choices;
}

function line(speaker, text, portrait = speaker) {
  return { speaker, text, portrait };
}

function setUnlock(id, unlockKind, unlockTarget = "", unlockValue = 0, unlockHint = "") {
  const event = eventById(id);
  if (!event) return;
  event.unlockKind = unlockKind;
  event.unlockTarget = unlockTarget;
  event.unlockValue = unlockValue;
  event.unlockHint = unlockHint;
}

function quest(row) {
  return {
    unlockTarget: "",
    unlockValue: 0,
    targetId: "",
    rewardMerit: 0,
    rewardTreasury: 0,
    rewardExpTarget: "",
    rewardExp: 0,
    rewardAffectionTarget: "",
    rewardAffection: 0,
    rewardAchievement: "",
    nextQuestId: "",
    ...row
  };
}

function upsertQuest(row) {
  config.quests = config.quests || [];
  const index = config.quests.findIndex((item) => item.id === row.id);
  if (index >= 0) config.quests[index] = quest(row);
  else config.quests.push(quest(row));
}

const mainQuests = [
  quest({
    id: "main_01",
    type: "主线",
    name: "第一周整训",
    description: "完成任意课程。剧情不再自动往前推，先让角色在养成中拿到第一份训练记录。",
    unlockKind: "always",
    targetKind: "anyCourseExp",
    targetValue: 25,
    rewardMerit: 8,
    rewardTreasury: 20,
    rewardExpTarget: "trainingExp",
    rewardExp: 12,
    rewardAchievement: "A01",
    nextQuestId: "main_02"
  }),
  quest({
    id: "main_02",
    type: "主线",
    name: "训练搭档",
    description: "任意同窗好感达到30。战棋前需要可靠搭档，日常社交会直接打开后续剧情。",
    unlockKind: "quest",
    unlockTarget: "main_01",
    unlockValue: 1,
    targetKind: "anyRelationship",
    targetValue: 30,
    rewardMerit: 8,
    rewardTreasury: 10,
    rewardExpTarget: "managementExp",
    rewardExp: 12,
    nextQuestId: "main_03"
  }),
  quest({
    id: "main_03",
    type: "主线",
    name: "兵棋资格",
    description: "任意课程达到2级。课程等级代表角色能读懂阵型、补给和兵种克制。",
    unlockKind: "quest",
    unlockTarget: "main_02",
    unlockValue: 1,
    targetKind: "anyCourseLevel",
    targetValue: 2,
    rewardMerit: 12,
    rewardTreasury: 18,
    rewardExpTarget: "trainingExp",
    rewardExp: 12,
    nextQuestId: "main_04"
  }),
  quest({
    id: "main_04",
    type: "主线",
    name: "战前侦察",
    description: "情报值达到20。侦察结果会解释敌军配置，也决定剧情里哪些风险会提前暴露。",
    unlockKind: "quest",
    unlockTarget: "main_03",
    unlockValue: 1,
    targetKind: "intelligence",
    targetValue: 20,
    rewardMerit: 10,
    rewardTreasury: 12,
    rewardExpTarget: "logisticsExp",
    rewardExp: 12,
    nextQuestId: "main_05"
  }),
  quest({
    id: "main_05",
    type: "主线",
    name: "情报网成形",
    description: "情报网达到2级。后续主线会把战棋、派系压力和日常消息串在一起。",
    unlockKind: "quest",
    unlockTarget: "main_04",
    unlockValue: 1,
    targetKind: "spyNetwork",
    targetValue: 2,
    rewardMerit: 12,
    rewardTreasury: 16,
    rewardExpTarget: "logisticsExp",
    rewardExp: 14,
    rewardAchievement: "I01",
    nextQuestId: "main_06"
  }),
  quest({
    id: "main_06",
    type: "主线",
    name: "首次军演胜利",
    description: "赢得一场战棋战斗。主线只承认玩家在棋盘上打出来的结果。",
    unlockKind: "quest",
    unlockTarget: "main_05",
    unlockValue: 1,
    targetKind: "battleWins",
    targetValue: 1,
    rewardMerit: 20,
    rewardTreasury: 30,
    rewardExpTarget: "managementExp",
    rewardExp: 16,
    rewardAchievement: "B01",
    nextQuestId: "main_07"
  }),
  quest({
    id: "main_07",
    type: "主线",
    name: "连续出阵",
    description: "累计完成2场战棋。胜败都会留下战报，但胜利会让后续评价更高。",
    unlockKind: "quest",
    unlockTarget: "main_06",
    unlockValue: 1,
    targetKind: "battlesFought",
    targetValue: 2,
    rewardMerit: 16,
    rewardTreasury: 24,
    rewardExpTarget: "infantryExp",
    rewardExp: 12,
    nextQuestId: "main_08"
  }),
  quest({
    id: "main_08",
    type: "主线",
    name: "毕业前整备",
    description: "情报值达到45。终盘前先把敌情、补给和同盟线收束清楚。",
    unlockKind: "quest",
    unlockTarget: "main_07",
    unlockValue: 1,
    targetKind: "intelligence",
    targetValue: 45,
    rewardMerit: 20,
    rewardTreasury: 30,
    rewardExpTarget: "logisticsExp",
    rewardExp: 18
  })
];

config.quests = (config.quests || []).filter((item) => !/^main_\d+/.test(item.id));
config.quests.unshift(...mainQuests);

upsertQuest({
  id: "rel_zhao_01",
  type: "角色支线",
  name: "枪阵搭档",
  description: "与赵伯衡好感达到45。他会在步兵与重步阵型上给出额外战棋建议。",
  unlockKind: "story",
  unlockTarget: "EV006",
  unlockValue: 1,
  targetKind: "relationship",
  targetId: "zhao",
  targetValue: 45,
  rewardMerit: 10,
  rewardExpTarget: "infantryExp",
  rewardExp: 12,
  rewardAffectionTarget: "zhao",
  rewardAffection: 8
});
upsertQuest({
  id: "rel_lin_01",
  type: "角色支线",
  name: "军政记录员",
  description: "与林素心好感达到45。她会帮你把课程、任务和战报整理成可用情报。",
  unlockKind: "story",
  unlockTarget: "EV009",
  unlockValue: 1,
  targetKind: "relationship",
  targetId: "lin",
  targetValue: 45,
  rewardMerit: 8,
  rewardTreasury: 6,
  rewardExpTarget: "managementExp",
  rewardExp: 12,
  rewardAffectionTarget: "lin",
  rewardAffection: 8
});
upsertQuest({
  id: "rel_yierde_01",
  type: "角色支线",
  name: "边地向导",
  description: "与伊尔德好感达到45。边地路线会影响战略地图上的侦察与行军判断。",
  unlockKind: "story",
  unlockTarget: "EV008",
  unlockValue: 1,
  targetKind: "relationship",
  targetId: "yierde",
  targetValue: 45,
  rewardMerit: 8,
  rewardExpTarget: "cavalryExp",
  rewardExp: 12,
  rewardAffectionTarget: "yierde",
  rewardAffection: 8
});
upsertQuest({
  id: "rel_chen_01",
  type: "角色支线",
  name: "军法边界",
  description: "与陈敬之好感达到45。他会提醒你哪些战术胜利会引发派系反弹。",
  unlockKind: "story",
  unlockTarget: "EV008",
  unlockValue: 1,
  targetKind: "relationship",
  targetId: "chen",
  targetValue: 45,
  rewardMerit: 8,
  rewardTreasury: 8,
  rewardExpTarget: "managementExp",
  rewardExp: 10,
  rewardAffectionTarget: "chen",
  rewardAffection: 8
});
upsertQuest({
  id: "rel_li_01",
  type: "角色支线",
  name: "炮算校准",
  description: "与李婉清好感达到45。炮兵与重器单位的伤害预判会更可靠。",
  unlockKind: "story",
  unlockTarget: "EV031",
  unlockValue: 1,
  targetKind: "relationship",
  targetId: "li",
  targetValue: 45,
  rewardMerit: 10,
  rewardExpTarget: "artilleryExp",
  rewardExp: 12,
  rewardAffectionTarget: "li",
  rewardAffection: 8
});
upsertQuest({
  id: "faction_liberal_01",
  type: "派系任务",
  name: "议会观察",
  description: "自由派立场达到40。派系任务只在玩家日常选择已经形成倾向后出现。",
  unlockKind: "story",
  unlockTarget: "EV014",
  unlockValue: 1,
  targetKind: "stance",
  targetId: "liberal",
  targetValue: 40,
  rewardMerit: 12,
  rewardTreasury: 8,
  rewardExpTarget: "managementExp",
  rewardExp: 10
});
upsertQuest({
  id: "daily_drill",
  type: "日常",
  name: "晨练操典",
  description: "训练进度达到50。日常任务不推主线，只给玩家稳定成长目标。",
  unlockKind: "always",
  targetKind: "trainingExp",
  targetValue: 50,
  rewardMerit: 6,
  rewardExpTarget: "infantryExp",
  rewardExp: 8
});
upsertQuest({
  id: "intel_scout_01",
  type: "情报",
  name: "战前耳目",
  description: "情报网达到2级。情报任务服务战棋，帮助玩家看清敌军和补给。",
  unlockKind: "quest",
  unlockTarget: "main_04",
  unlockValue: 1,
  targetKind: "spyNetwork",
  targetValue: 2,
  rewardMerit: 8,
  rewardExpTarget: "logisticsExp",
  rewardExp: 8
});

Object.assign(config.campusActivities.find((item) => item.id === "drill") || {}, {
  description: "复盘本周课程，把阵型、兵种克制和补给路线写进演习档案。偏战斗与战功。"
});
Object.assign(config.campusActivities.find((item) => item.id === "salon") || {}, {
  description: "请同窗到校外茶馆交换消息。偏好感与派系信息，能打开角色支线。"
});
Object.assign(config.campusActivities.find((item) => item.id === "lecture") || {}, {
  description: "听讲师解读朝局。偏立场变化，影响派系任务何时出现。"
});
Object.assign(config.campusActivities.find((item) => item.id === "volunteer") || {}, {
  description: "随医务处前往码头棚户区。偏边地见闻，能补足战略地图上的风险判断。"
});

const fragmentRewrites = {
  nf_course_infantry_001: ["枪阵课后的复盘", "赵伯衡", "枪阵课结束后，赵伯衡把步兵、长枪与重步的站位画在沙盘上。你第一次看清：课程经验不是数字，而是战棋里每一次少死人、少掉士气的底气。"],
  nf_course_infantry_002: ["雨夜换防", "赵伯衡", "夜雨中，你们复盘防线换防。赵伯衡指出前排不稳时后排火枪会被骑兵直冲，这条记录会成为下一场战棋前的阵容提醒。"],
  nf_course_cavalry_001: ["马房里的行军课", "伊尔德", "骑训课后，伊尔德让你沿着马蹄印反推道路消耗。你明白战略地图上的一次行军，不只是移动按钮，而是体力、补给和地形的合账。"],
  nf_course_cavalry_002: ["边道复测", "伊尔德", "你随骑队复测边道，发现旧地图漏标了一段浅滩。伊尔德提醒你：战前情报越清楚，开局位置越不容易被动。"],
  nf_course_artillery_001: ["炮尺校准", "李婉清", "炮算课上，李婉清要求你把风向、距离和地形一起算入射击表。下一次使用火枪与重器单位时，这些计算会变成更稳的伤害预判。"],
  nf_course_artillery_002: ["失准的炮表", "李婉清", "你们校准火炮时发现旧炮表误差很大。李婉清把误差写进战前提示：重器强，但没有侦察和保护就会拖慢整支部队。"],
  nf_course_management_001: ["军政课的任务板", "林素心", "军政课讲到军令登记，林素心把主线任务、角色支线和日常目标分成三栏。你终于知道每周该先看目标，再决定课程和周末去向。"],
  nf_course_management_002: ["战报归档", "林素心", "林素心把几份战报排在一起，指出胜负之外还要看击溃数、补给消耗和关系反应。战棋结果会反过来改变任务评价。"],
  nf_course_logistics_001: ["补给线上的红圈", "旁白", "后勤教官让你复盘一次失败转运。地图上那几个红圈不是装饰，而是提醒你：补给不足时，战棋里的攻击、士气和移动都会变差。"],
  nf_course_training_001: ["夜训后的体力账", "旁白", "夜训结束后，你把体力消耗记进周记。高强度养成能换来等级，但疲劳会让社交与战斗状态下降。"],
  nf_social_zhao_001: ["赵伯衡的前排法", "赵伯衡", "邀约归来时，赵伯衡用筷子摆出三层阵。若你愿意听他讲完，下一次编队时会更重视前排承伤和侧翼保护。"],
  nf_social_lin_001: ["林素心的索引卡", "林素心", "林素心把课程、任务和人际消息写成索引卡。她没有替你做决定，只提醒你：所有剧情都应该回答一个问题，本周要练什么、打什么、找谁。"],
  nf_social_yierde_001: ["伊尔德的边地歌", "伊尔德", "伊尔德唱起边地旧歌，歌词里全是道路、渡口和补给点。你意识到文化差异不只是文案，它会影响战略地图上的选择。"],
  nf_activity_drill_001: ["兵棋桌边的空位", "李婉清", "兵棋推演里多出一个空位。李婉清建议你把它留给远程单位，哪怕暂时少一名前排，也能逼敌军改变推进路线。"],
  nf_activity_salon_001: ["茶馆里的派系风向", "陈敬之", "茶馆里有人谈军令，也有人谈名声。陈敬之提醒你：同一场胜利，在不同派系眼中可能是功劳，也可能是威胁。"],
  nf_rest_secret_letter_001: ["休息日的整理", "旁白", "难得休息时，你整理这周的课程记录、战报和邀约。休息不是跳过玩法，而是让下一周的体力与心态回到可控范围。"],
  nf_study_library_001: ["图书馆的克制表", "旁白", "自习到深夜，你在旧书页边看到一张兵种克制表。它很枯燥，却能让下一次攻击前的判断少一点赌运气。"],
  nf_intel_010: ["情报：敌影初现", "旁白", "情报脉络初成，你终于能在战略地图上看出哪支敌军只是虚张声势，哪支部队真的会威胁据点。"],
  nf_intel_025: ["情报：补给破口", "旁白", "暗线传来回报，敌军补给队出现固定路线。若在战前动手，下一场战棋的敌方士气与补给都会更容易被压低。"],
  nf_intel_050: ["情报：决战前夜", "林素心", "林素心把几份战报压在地图角上：课程、关系、情报和战棋结果已经汇合。接下来的剧情只负责把你推向必须亲自打出来的局面。"]
};

for (const fragment of config.narrativeFragments || []) {
  const rewrite = fragmentRewrites[fragment.id];
  if (!rewrite) continue;
  fragment.title = rewrite[0];
  fragment.speaker = rewrite[1];
  fragment.body = rewrite[2];
  if (fragment.id === "nf_intel_050") fragment.nextStoryId = "";
}

const mainEvents = (story.events || [])
  .filter((event) => /^EV\d{3}$/.test(event.id))
  .sort((a, b) => Number(a.id.slice(2)) - Number(b.id.slice(2)));

for (let i = 0; i < mainEvents.length; i += 1) {
  const event = mainEvents[i];
  if (event.id === "EV001") {
    setUnlock(event.id, "always", "", 0, "创建角色后自动开放。");
  } else {
    const prev = mainEvents[i - 1].id;
    setUnlock(event.id, "story", prev, 1, `完成上一段主线 ${prev}。`);
  }
}

setUnlock("EV005", "quest", "main_01", 1, "完成任务「第一周整训」：任意课程进度达到25。");
setUnlock("EV007", "quest", "main_02", 1, "完成任务「训练搭档」：任意同窗好感达到30。");
setUnlock("EV010", "quest", "main_03", 1, "完成任务「兵棋资格」：任意课程达到2级。");
setUnlock("EV013", "quest", "main_04", 1, "完成任务「战前侦察」：情报值达到20。");
setUnlock("EV021", "quest", "main_05", 1, "完成任务「情报网成形」：情报网达到2级。");
setUnlock("EV025", "trainingExp", "", 120, "体能或训练进度达到120，角色才足以参加高压对抗。");
setUnlock("EV031", "anyCourseLevel", "", 3, "任意课程达到3级，取得夏季大演习资格。");
setUnlock("EV038", "quest", "main_06", 1, "完成任务「首次军演胜利」：赢得一场战棋。");
setUnlock("EV050", "quest", "main_08", 1, "完成任务「毕业前整备」：情报值达到45。");
setUnlock("EV059", "battleWins", "", 2, "累计赢得2场战棋，证明你能在实战压力下指挥。");
setUnlock("EV065", "battleWins", "", 3, "累计赢得3场战棋，进入终局决断。");

setEvent("EV001", {
  type: "主线",
  chapter: "序章：入学与第一张任务板",
  lines: [
    line("旁白", "新京军事学院的钟声响起，你以新生身份进入操场。这里不会用一段长剧情决定你的命运，真正决定你道路的是每周课程、同窗关系、情报行动和战棋胜负。"),
    line("方孝先", "学院只认三样东西：训练记录、战报、能让人愿意跟随你的判断。先把名字写上任务板，再用行动证明自己。")
  ],
  choices: []
});

setEvent("EV002", {
  chapter: "序章：入学与第一张任务板",
  lines: [
    line("旁白", "教务处发下三份登记：课程表、同窗名册、军演权限。课程提升角色等级，社交打开角色支线，情报降低战棋风险，战棋结果反过来决定主线评价。"),
    line("林素心", "别急着追问大事。先看任务，缺等级就上课，缺人脉就邀约，缺敌情就做情报。剧情只会在你准备好时继续。")
  ],
  choices: []
});

setEvent("EV003", {
  type: "可选项",
  chapter: "序章：入学与第一张任务板",
  lines: [
    line("旁白", "第一周开始前，你必须选一个入学姿态。它不会替你通关，只会暗中影响你接下来更容易打开哪类玩法入口。")
  ],
  choices: [
    choice("EV003-A", "先去操场报到，熟悉步兵与前排阵型。", "EV004", [{ kind: "好感", target: "zhao", delta: 4 }]),
    choice("EV003-B", "先去图书馆整理课程与任务记录。", "EV004", [{ kind: "好感", target: "lin", delta: 4 }]),
    choice("EV003-C", "先去马房和码头打听道路与补给。", "EV004", [{ kind: "好感", target: "yierde", delta: 4 }])
  ]
});

setEvent("EV004", {
  chapter: "序章：入学与第一张任务板",
  lines: [
    line("赵伯衡", "你想快点出头，就别只看热闹。先把任意一门课练出记录，教官才会让你碰兵棋桌。"),
    line("旁白", "主任务「第一周整训」已成为接下来的推进条件。完成课程后，主线才会继续。")
  ],
  choices: []
});

setEvent("EV005", {
  chapter: "第一章：养成不是过场",
  lines: [
    line("方孝先", "不错，至少有一门课留下了成绩。记住，数值不是墙上的装饰，它们会决定你在战棋里能带什么兵、看懂什么风险、让谁愿意配合。"),
    line("旁白", "你的第一份训练记录被钉上任务板。下一步不再是继续听故事，而是找到一个能在演习中互相补位的同窗。")
  ],
  choices: []
});

setEvent("EV006", {
  type: "可选项",
  chapter: "第一章：养成不是过场",
  lines: [
    line("旁白", "宿舍区、图书馆和马房都有人等你。选择谁作为第一位训练搭档，会暗中改变你更早接触到的支线与战棋建议。")
  ],
  choices: [
    choice("EV006-A", "找赵伯衡练前排推进与守点。", "EV007", [{ kind: "好感", target: "zhao", delta: 6 }]),
    choice("EV006-B", "找林素心整理任务、战报和课程记录。", "EV007", [{ kind: "好感", target: "lin", delta: 6 }]),
    choice("EV006-C", "找伊尔德学习道路、骑兵和补给判断。", "EV007", [{ kind: "好感", target: "yierde", delta: 6 }])
  ]
});

setEvent("EV007", {
  chapter: "第一章：养成不是过场",
  lines: [
    line("旁白", "当第一段稳定关系建立后，学院允许你申请兵棋资格。你已经知道，角色关系不是恋爱摆件，而是战斗建议、情报来源和任务分支。"),
    line("林素心", "下一步很简单：把任意课程练到2级。没有基础等级，兵棋桌只会把你的失误放大。")
  ],
  choices: []
});

setEvent("EV008", {
  type: "可选项",
  chapter: "第一章：养成不是过场",
  lines: [
    line("旁白", "食堂里爆发争执：有人坚持重步压线，有人主张骑兵侧击，也有人认为先摸清敌军补给。争执本身不重要，重要的是你会把哪种思路带进战棋。")
  ],
  choices: [
    choice("EV008-A", "支持稳固前线：先守住据点，再找机会推进。", "EV009", [{ kind: "好感", target: "chen", delta: 4 }]),
    choice("EV008-B", "支持机动包抄：用骑兵和散兵逼敌军分兵。", "EV009", [{ kind: "好感", target: "yierde", delta: 4 }]),
    choice("EV008-C", "支持战前侦察：先知道敌军部署，再谈打法。", "EV009", [{ kind: "好感", target: "lin", delta: 4 }])
  ]
});

setEvent("EV009", {
  chapter: "第一章：养成不是过场",
  lines: [
    line("林素心", "我把你这几周的课程、邀约和周末记录做成索引了。它们不是剧情收藏，而是下一场战棋前的准备清单。"),
    line("旁白", "从现在开始，情报值和情报网会成为主线触发条件。你可以通过自习、周末活动和情报行动补足它。")
  ],
  choices: []
});

setEvent("EV010", {
  chapter: "第一章：兵棋资格",
  lines: [
    line("方孝先", "任意课程达到2级，说明你至少有一个方向能用于实战。现在你可以申请正式兵棋推演。"),
    line("旁白", "接下来的剧情会围绕一次演习准备展开：课程决定基础，情报决定开局，关系决定可用建议。")
  ],
  choices: []
});

setEvent("EV011", {
  type: "可选项",
  chapter: "第一章：兵棋资格",
  lines: [
    line("旁白", "周末只够做一件事。你选择的活动不会显示具体数值影响，但会暗中改变后续任务和支线的开放速度。")
  ],
  choices: [
    choice("EV011-A", "参加兵棋推演，提前熟悉守点与集火。", "EV012", [{ kind: "好感", target: "zhao", delta: 3 }]),
    choice("EV011-B", "参加茶馆沙龙，扩大同窗和派系消息来源。", "EV012", [{ kind: "好感", target: "lin", delta: 3 }]),
    choice("EV011-C", "前往边民救济，了解道路、民心和补给风险。", "EV012", [{ kind: "好感", target: "yierde", delta: 3 }])
  ]
});

setEvent("EV012", {
  chapter: "第一章：兵棋资格",
  lines: [
    line("旁白", "你的周末记录被写进任务板。学院不会评价你选得漂不漂亮，只看这些准备能不能在下一次战棋里减少损失。"),
    line("方孝先", "现在去补情报。看不见敌军部署，就别说自己会指挥。")
  ],
  choices: []
});

setEvent("EV013", {
  type: "主线",
  chapter: "第二章：战前侦察",
  lines: [
    line("旁白", "情报值达到要求后，战略地图上的敌影终于不再是一片黑。你能分辨兵力、补给和可能的推进方向。"),
    line("林素心", "这就是剧情该做的事：告诉你为什么要打这一仗，而不是替你打赢它。")
  ],
  choices: []
});

setEvent("EV014", {
  chapter: "第二章：战前侦察",
  lines: [
    line("旁白", "学院公告宣布夏季前将进行联合军演。派系、同窗和教官都在看你的训练记录。接下来，主线会暂时收束到一个目标：把情报网建起来。"),
    line("陈敬之", "军功能让你升上去，也能让人开始忌惮你。战棋胜负之外，别忘了任务和派系。")
  ],
  choices: []
});

setEvent("EV017", {
  type: "可选项",
  chapter: "第二章：派系压力",
  lines: [
    line("旁白", "年末庆典不是单纯的社交场。你要选择今晚重点交谈的对象，为后续战棋和任务争取支持。")
  ],
  choices: [
    choice("EV017-A", "与赵伯衡和军校生讨论前线打法。", "EV018", [{ kind: "好感", target: "zhao", delta: 6 }]),
    choice("EV017-B", "与林素心和议会派整理战报口径。", "EV018", [{ kind: "好感", target: "lin", delta: 6 }]),
    choice("EV017-C", "与伊尔德和边地代表确认补给道路。", "EV018", [{ kind: "好感", target: "yierde", delta: 6 }])
  ]
});

setEvent("EV021", {
  chapter: "第三章：战略地图打开",
  lines: [
    line("旁白", "情报网成形后，你被允许查看更完整的战略地图。课程、社交和情报终于汇到同一处：选择战场，整备军团，然后亲自下棋。"),
    line("方孝先", "从今天起，你的主线评价不再看你说了什么，而看你在地图上保住了什么、攻下了什么。")
  ],
  choices: []
});

setEvent("EV025", {
  type: "可选项",
  chapter: "第三章：高压对抗",
  lines: [
    line("旁白", "高压对抗课开始。你面对的不再是剧情里的争吵，而是一次会消耗体力、检验训练进度的模拟战。")
  ],
  choices: [
    choice("EV025-A", "稳守中线，让重步和长枪拖住敌方推进。", "EV026", [{ kind: "好感", target: "chen", delta: 4 }]),
    choice("EV025-B", "诱敌深入，用骑兵和散兵从侧翼切断补给。", "EV026", [{ kind: "好感", target: "yierde", delta: 4 }]),
    choice("EV025-C", "集中远程火力，先击溃威胁最大的敌军。", "EV026", [{ kind: "好感", target: "li", delta: 4 }])
  ]
});

setEvent("EV031", {
  chapter: "第四章：夏季大演习",
  lines: [
    line("方孝先", "任意课程达到3级，你才有资格进入夏季大演习。现在上棋盘，胜负由你自己打出来。"),
    line("旁白", "接下来的主线会在你赢得首次战棋后继续。失败可以留下经验，但不会替你打开胜利后的剧情。")
  ],
  choices: []
});

setEvent("EV038", {
  type: "可选项",
  chapter: "第四章：胜利后的选择",
  lines: [
    line("旁白", "首次军演胜利后，各方都递来邀请。你不是在选一段剧情，而是在决定下一轮养成和战棋优先服务哪条路线。")
  ],
  choices: [
    choice("EV038-A", "接受军校派复盘，继续强化正面作战。", "EV039", [{ kind: "好感", target: "zhao", delta: 5 }]),
    choice("EV038-B", "接受议会派约谈，把战报转化为政治筹码。", "EV039", [{ kind: "好感", target: "lin", delta: 5 }]),
    choice("EV038-C", "接受边地代表邀请，扩大战略地图上的情报来源。", "EV039", [{ kind: "好感", target: "yierde", delta: 5 }])
  ]
});

setEvent("EV039", {
  type: "主线",
  chapter: "第五章：战报回流",
  lines: [
    line("旁白", "第一份战报回到学院，胜负、击溃数、补给消耗和同窗评价被写在同一页。你开始明白，战棋不是独立玩法，它会改变接下来所有人的态度。"),
    line("林素心", "继续出阵吧。没有第二份战报，任何派系承诺都只是空话。")
  ],
  choices: []
});

setEvent("EV050", {
  type: "主线",
  chapter: "第六章：毕业前整备",
  lines: [
    line("旁白", "情报值达到45后，终盘前的风险终于被摊开：敌军主力、补给破口、可争取的同盟和会背刺你的派系。"),
    line("方孝先", "毕业不是剧情章节名，是系统把你从养成推向实战的门槛。带着你的等级、关系和战报上路。")
  ],
  choices: []
});

setEvent("EV059", {
  type: "主线",
  chapter: "第七章：烽火连天",
  lines: [
    line("旁白", "累计赢得两场战棋后，内战全面爆发。各派不再听学院里的漂亮话，只看你能不能守住据点、保住补给、打穿敌军阵线。"),
    line("陈敬之", "现在每一次移动都会留下政治后果。胜利能救人，也会让新的敌人记住你。")
  ],
  choices: []
});

setEvent("EV065", {
  type: "可选项",
  chapter: "第七章：终局决断",
  lines: [
    line("旁白", "三场关键战棋之后，所有养成记录、关系路线和情报判断都汇入最后的选择。结局不是凭空分支，而是你一路打出来、练出来、交出来的结果。")
  ]
});

for (const event of story.events || []) {
  if (/^EV\d{3}$/.test(event.id) || /^END-/.test(event.id)) continue;
  const trigger = String(event.trigger || "");
  const match = trigger.match(/EV\d{3}/);
  if (match) {
    event.unlockKind = "story";
    event.unlockTarget = match[0];
    event.unlockValue = 1;
    event.unlockHint = `完成主线 ${match[0]} 后开放。`;
  } else if (!event.unlockKind) {
    event.unlockKind = "always";
    event.unlockTarget = "";
    event.unlockValue = 0;
    event.unlockHint = "达到对应角色或任务条件后开放。";
  }
}

for (const event of story.events || []) {
  event.choices = (event.choices || []).map((item) => {
    const label = String(item.label || item.text || "").replace(/\.{3,}|…+/g, "");
    return { ...item, label, text: item.text || label };
  });
}

upsertUi("story.locked_title", "剧情未触发");
upsertUi("story.locked_body", "触发条件：{0}\n\n先完成对应的养成、情报、任务或战棋目标，再回来推进。");
upsertUi("story_menu.summary", "当前主线：{0}\n推进条件：{1}\n支线会随主线、好感和任务进度开放。");
upsertUi("story_menu.main_ready", "已满足，点击继续主线。");
upsertUi("story.unlock.ready", "已满足，点击继续主线即可推进。");
upsertUi("story.unlock.quest", "完成任务：{0}");
upsertUi("story.unlock.story", "完成剧情：{0}");
upsertUi("story.unlock.battle_wins", "战棋胜利 {0}/{1}");
upsertUi("story.unlock.battles", "完成战棋 {0}/{1}");
upsertUi("story.unlock.enemies", "击溃敌方单位 {0}/{1}");
upsertUi("story.unlock.any_course_exp", "任意课程进度 {0}/{1}");
upsertUi("story.unlock.any_course_level", "任意课程等级 Lv.{0}/Lv.{1}");
upsertUi("story.unlock.any_relationship", "任意同窗好感 {0}/{1}");
upsertUi("story.unlock.relationship", "{0}好感 {1}/{2}");
upsertUi("story.unlock.intelligence", "情报值 {0}/{1}");
upsertUi("story.unlock.spy_network", "情报网 Lv.{0}/Lv.{1}");
upsertUi("story.unlock.merit", "战功 {0}/{1}");
upsertUi("story.unlock.progress", "{0} {1}/{2}");
upsertUi("log.story_locked", "剧情未触发：{0}。");
upsertUi("story_menu.no_side", "支线不会一次性堆出来。先完成主线闸门、提高好感或完成任务。");
upsertUi("quest.body", "任务追踪\n{0}\n\n已完成：{1}");
upsertUi("dossier.summary_none", "当前目标：先看任务板，补课程、好感、情报或战棋条件。");
upsertUi("dossier.summary_low", "当前目标：线索已成形，继续补足主线触发条件。");
upsertUi("dossier.summary_mid", "当前目标：战棋与情报已经接上，准备下一次出阵。");
upsertUi("dossier.summary_full", "当前目标：终盘整备完成，等待关键战棋结果。");

fs.writeFileSync(configPath, `${JSON.stringify(config, null, 2)}\n`, "utf8");
fs.writeFileSync(storyPath, `${JSON.stringify(story, null, 2)}\n`, "utf8");
console.log("Rewrote gameplay-first narrative bridge, quest chain, and story unlock conditions.");
