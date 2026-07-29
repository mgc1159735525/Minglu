using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MingLuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (UnityEngine.Object.FindObjectOfType<MingLuGame>() != null) return;
        GameObject go = new GameObject("MingLuGame");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<MingLuGame>();
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class HexTileGraphic : MaskableGraphic
{
    public Color strokeColor = Color.black;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width * 0.5f, rect.height * 0.5f) - 1f;
        int centerIndex = vh.currentVertCount;
        Color centerColor = Shift(color, 0.12f);
        Color edgeColor = Shift(color, -0.08f);
        vh.AddVert(center, centerColor, Vector2.zero);
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (30f + i * 60f);
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vh.AddVert(p, edgeColor, Vector2.zero);
        }
        for (int i = 1; i <= 6; i++)
        {
            int next = i == 6 ? 1 : i + 1;
            vh.AddTriangle(centerIndex, i, next);
        }

        AddHexRing(vh, center, radius, Mathf.Max(0f, radius - 3.2f), strokeColor);
        AddHexRing(vh, center, Mathf.Max(0f, radius - 5.2f), Mathf.Max(0f, radius - 6.6f), new Color(1f, 0.92f, 0.72f, 0.22f));
    }

    private static void AddHexRing(VertexHelper vh, Vector2 center, float outerRadius, float innerRadius, Color ringColor)
    {
        int ringStart = vh.currentVertCount;
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (30f + i * 60f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(center + direction * outerRadius, ringColor, Vector2.zero);
            vh.AddVert(center + direction * innerRadius, ringColor, Vector2.zero);
        }
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            int outerA = ringStart + i * 2;
            int innerA = outerA + 1;
            int outerB = ringStart + next * 2;
            int innerB = outerB + 1;
            vh.AddTriangle(outerA, outerB, innerA);
            vh.AddTriangle(innerA, outerB, innerB);
        }
    }

    private static Color Shift(Color c, float amount)
    {
        return new Color(Mathf.Clamp01(c.r + amount), Mathf.Clamp01(c.g + amount), Mathf.Clamp01(c.b + amount), c.a);
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapEllipseGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float rx = rect.width * 0.5f;
        float ry = rect.height * 0.5f;
        int centerIndex = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        const int segments = 40;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 p = center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry);
            vh.AddVert(p, color, Vector2.zero);
        }
        for (int i = 1; i <= segments; i++)
        {
            vh.AddTriangle(centerIndex, i, i + 1);
        }
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class BattleUnitBadgeGraphic : MaskableGraphic
{
    public Color darkColor = new Color(0.08f, 0.08f, 0.08f);
    public Color goldColor = new Color(0.88f, 0.67f, 0.25f);
    public float flash;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Vector2 center = rectTransform.rect.center + new Vector2(0, 8);
        if (flash > 0.01f)
        {
            Color flashColor = new Color(1f, 0.24f, 0.16f, Mathf.Clamp01(flash) * 0.42f);
            AddDisc(vh, center, 35f + flash * 5f, flashColor, 32);
        }
        AddDisc(vh, center, 29f, goldColor, 32);
        AddDisc(vh, center, 25f, darkColor, 32);
        AddDisc(vh, center + new Vector2(0, -1), 18f, color, 32);
        AddRect(vh, center + new Vector2(18, 4), new Vector2(3, 38), new Color(0.11f, 0.09f, 0.07f, 1f));
        AddTriangle(vh, center + new Vector2(20, 21), center + new Vector2(39, 15), center + new Vector2(20, 8), color);
        AddTriangle(vh, center + new Vector2(-14, 18), center + new Vector2(0, 31), center + new Vector2(14, 18), darkColor);
    }

    private static void AddDisc(VertexHelper vh, Vector2 center, float radius, Color color, int segments)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
        }
        for (int i = 1; i <= segments; i++)
        {
            int next = i == segments ? 1 : i + 1;
            vh.AddTriangle(start, start + i, start + next);
        }
    }

    private static void AddRect(VertexHelper vh, Vector2 center, Vector2 size, Color color)
    {
        Vector2 half = size * 0.5f;
        int start = vh.currentVertCount;
        vh.AddVert(center + new Vector2(-half.x, -half.y), color, Vector2.zero);
        vh.AddVert(center + new Vector2(-half.x, half.y), color, Vector2.zero);
        vh.AddVert(center + new Vector2(half.x, half.y), color, Vector2.zero);
        vh.AddVert(center + new Vector2(half.x, -half.y), color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero);
        vh.AddVert(b, color, Vector2.zero);
        vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }
}

public sealed class MingLuGame : MonoBehaviour
{
    private enum ScreenMode { Title, CharacterCreate, Academy, StoryEvent, Strategy, Battle, BattleLab, BattleConfirm, Result, Credits }
    private enum Faction { Player, Imperial, Reformist, Native, Foreign, Neutral }

    [Serializable]
    private sealed class PlayerProfile
    {
        public string name = "夏邑";
        public int age = 16;
        public int year = 1;
        public int week = 1;
        public int mood = 50;
        public int stamina = 80;
        public int merit = 0;
        public int treasury = 120;
        public int infantryExp = 0;
        public int cavalryExp = 0;
        public int artilleryExp = 0;
        public int managementExp = 0;
        public int logisticsExp = 0;
        public int trainingExp = 0;
        public string title = "新京军事学院生";
        public string courtesyName = "";
        public string originId = "";
        public string personalityId = "";
        public List<string> traits = new List<string>();
        public List<string> creationMemoryChoices = new List<string>();
        public List<string> subjectFocusIds = new List<string>();
        public string lastCourse = "";
        public int lastExamScore = 0;
        public int nationAxis = 0;
        public int classAxis = 0;
        public int governanceAxis = 0;
        public int regionAxis = 0;
        public int commandLockTurns = 0;
        public List<string> prisoners = new List<string>();
        public int intelligence = 12;
        public int spyNetwork = 0;
        public int newGamePlus = 0;
        public int achievementPoints = 0;
        public int battlesFought = 0;
        public int battleWins = 0;
        public int battleLosses = 0;
        public int enemiesDefeated = 0;
        public int questsCompleted = 0;
        public int spySuccesses = 0;
        public int supplyBreaks = 0;
        public string equippedTitle = "";
        public List<string> unlockedSkills = new List<string>();
        public List<string> equippedSkills = new List<string>();
        public List<string> activeQuests = new List<string>();
        public List<string> completedQuests = new List<string>();
        public List<string> unlockedAchievements = new List<string>();
        public List<string> unlockedTitles = new List<string>();
        public List<string> unlockedEndings = new List<string>();
        public List<string> eventReview = new List<string>();
    }

    [Serializable]
    private sealed class Relationship
    {
        public string id;
        public string name;
        public string stance;
        public int affection;
        public string note;
        public string circle;
        public int knownLevel;
        public int lastInteractionWeek;
    }

    [Serializable]
    private sealed class StanceScore
    {
        public string id;
        public string name;
        public int value;
    }

    [Serializable]
    private sealed class StoryDatabase
    {
        public string source;
        public List<StoryEventData> events = new List<StoryEventData>();
        public List<StoryCharacterData> characters = new List<StoryCharacterData>();
    }

    [Serializable]
    private sealed class StoryEventData
    {
        public string id;
        public string type;
        public string chapter;
        public string trigger;
        public string jump;
        public string unlockKind;
        public string unlockTarget;
        public int unlockValue;
        public string unlockHint;
        public List<StoryLineData> lines = new List<StoryLineData>();
        public List<StoryChoiceData> choices = new List<StoryChoiceData>();
    }

    [Serializable]
    private sealed class StoryLineData
    {
        public string speaker;
        public string text;
        public string portrait;
    }

    [Serializable]
    private sealed class StoryChoiceData
    {
        public string id;
        public string label;
        public string text;
        public string speaker;
        public string portrait;
        public string nextEventId;
        public List<StoryEffectData> effects = new List<StoryEffectData>();
    }

    [Serializable]
    private sealed class StoryEffectData
    {
        public string kind;
        public string target;
        public int delta;
    }

    [Serializable]
    private sealed class StoryCharacterData
    {
        public string name;
        public string identity;
        public string faction;
        public string kind;
        public string traits;
        public string background;
        public string tasks;
        public string nodes;
        public string portrait;
        public string asset;
    }

    [Serializable]
    private sealed class StoryValue
    {
        public string id;
        public int value;
    }

    [Serializable]
    private sealed class CharacterTrait
    {
        public string id;
        public string name;
        public string description;
        public int battleAttack;
        public int battleHp;
        public int battleMove;
        public int socialBonus;
        public int cultivationPercent;
        public int staminaSave;
    }

    [Serializable]
    private sealed class CharacterOrigin
    {
        public string id;
        public string name;
        public string subtitle;
        public string description;
        public string talentPool;
        public string clueId;
        public string clueName;
        public int infantryExp;
        public int cavalryExp;
        public int artilleryExp;
        public int managementExp;
        public int logisticsExp;
        public int trainingExp;
        public int nationAxis;
        public int classAxis;
        public int governanceAxis;
        public int regionAxis;
        public int stanceHome;
        public int stanceArmy;
        public int stanceNative;
        public int stanceLiberal;
        public int stanceLegal;
        public int relZhao;
        public int relLin;
        public int relYierde;
        public int relChen;
        public int relSu;
        public int relLi;
    }

    [Serializable]
    private sealed class CreationMemory
    {
        public string id;
        public string title;
        public string body;
        public string optionAId;
        public string optionAText;
        public string optionATraitId;
        public int optionANation;
        public int optionAClass;
        public int optionAGovernance;
        public int optionARegion;
        public string optionBId;
        public string optionBText;
        public string optionBTraitId;
        public int optionBNation;
        public int optionBClass;
        public int optionBGovernance;
        public int optionBRegion;
    }

    [Serializable]
    private sealed class CreationTalent
    {
        public string id;
        public string name;
        public string category;
        public int tier;
        public string originTags;
        public string description;
        public int battleAttack;
        public int battleHp;
        public int battleMove;
        public int socialBonus;
        public int cultivationPercent;
        public int staminaSave;
        public int intelligenceBonus;
    }

    [Serializable]
    private sealed class CreationSubject
    {
        public string id;
        public string name;
        public string target;
        public string description;
        public int expGain;
    }

    [Serializable]
    private sealed class PassiveSkillConfig
    {
        public string id;
        public string name;
        public string category;
        public string rarity;
        public string slot;
        public string unlockKind;
        public string unlockTarget;
        public int unlockValue;
        public string description;
        public int attackPercent;
        public int defensePercent;
        public int hpPercent;
        public int moveBonus;
        public int moraleBonus;
        public int supplySavePercent;
        public int intelBonus;
        public int expBonusPercent;
    }

    [Serializable]
    private sealed class QuestConfig
    {
        public string id;
        public string type;
        public string name;
        public string description;
        public string unlockKind;
        public string unlockTarget;
        public int unlockValue;
        public string targetKind;
        public string targetId;
        public int targetValue;
        public int rewardMerit;
        public int rewardTreasury;
        public string rewardExpTarget;
        public int rewardExp;
        public string rewardAffectionTarget;
        public int rewardAffection;
        public string rewardAchievement;
        public string nextQuestId;
    }

    [Serializable]
    private sealed class AchievementConfig
    {
        public string id;
        public string category;
        public string name;
        public string description;
        public string conditionKind;
        public string conditionTarget;
        public int conditionValue;
        public string rewardTitle;
        public int rewardPoints;
        public string rarity;
    }

    [Serializable]
    private sealed class TitleConfig
    {
        public string id;
        public string name;
        public string category;
        public string description;
        public int attackBonus;
        public int hpBonus;
        public int socialBonus;
        public int cultivationBonus;
        public int intelligenceBonus;
        public int supplyBonus;
    }

    [Serializable]
    private sealed class IntelligenceActionConfig
    {
        public string id;
        public string name;
        public string type;
        public string description;
        public int cost;
        public int successRate;
        public int risk;
        public int intelGain;
        public int spyNetworkGain;
        public int enemyTroopDamage;
        public int enemySupplyDamage;
        public string targetFaction;
    }

    [Serializable]
    private sealed class AiProfileConfig
    {
        public string id;
        public string name;
        public int aggression;
        public int caution;
        public int focusFire;
        public int retreatHpPercent;
        public int terrainPreference;
        public int objectiveBias;
        public int guardBias;
        public int flankBias;
        public int rangedSpacing;
        public int finishBias;
        public int avoidCounter;
    }

    [Serializable]
    private sealed class SupplyRuleConfig
    {
        public string id;
        public string name;
        public int standbyCost;
        public int moveCost;
        public int attackCost;
        public int moveAttackCost;
        public int shortageThreshold;
        public int shortageAttackPenalty;
        public int shortageMoralePenalty;
    }

    [Serializable]
    private sealed class NewsArticle
    {
        public string id;
        public string title;
        public string source;
        public int unlockWeek;
        public string stanceHint;
        public string body;
    }

    [Serializable]
    private sealed class CampusActivity
    {
        public string id;
        public string name;
        public string description;
        public int moodDelta;
        public int meritDelta;
        public int treasuryDelta;
        public int socialGain;
        public int trainingGain;
        public string axisId;
        public int axisDelta;
    }

    [Serializable]
    private sealed class NarrativeFragmentConfig
    {
        public string id;
        public string triggerKind;
        public string triggerTarget;
        public int minWeek;
        public int maxWeek;
        public string title;
        public string speaker;
        public string body;
        public string sceneId;
        public string relationshipTarget;
        public int affectionDelta;
        public string axisId;
        public int axisDelta;
        public int intelligenceDelta;
        public string suspicionFaction;
        public int suspicionDelta;
        public string nextStoryId;
        public string once;
    }

    [Serializable]
    private sealed class LocalizedText
    {
        public string key;
        public string value;
        public string note;
    }

    [Serializable]
    private sealed class CalendarConfig
    {
        public string examWeeks = "25;50";
        public string holidayWeeks = "26;27;51;52";
        public int maxWeek = 52;
        public int maxYear = 4;
    }

    [Serializable]
    private sealed class CourseConfig
    {
        public string id;
        public string label;
        public string target;
    }

    [Serializable]
    private sealed class MoodRule
    {
        public int minMood;
        public int maxMood;
        public string label;
        public int studyMin;
        public int studyMax;
    }

    [Serializable]
    private sealed class AcademyLevelRule
    {
        public int level;
        public int floorExp;
        public int nextExp;
    }

    [Serializable]
    private sealed class RankRule
    {
        public int minMerit;
        public string name;
        public int commandLimit;
    }

    [Serializable]
    private sealed class RelationshipLevelRule
    {
        public int minAffection;
        public string label;
        public int knownLevel;
    }

    [Serializable]
    private sealed class BeliefLevelRule
    {
        public int minAbsValue;
        public string label;
    }

    [Serializable]
    private sealed class FactionConfig
    {
        public string id;
        public string displayName;
    }

    [Serializable]
    private sealed class IdeologyAxisConfig
    {
        public string id;
        public string label;
        public string negativeLabel;
        public string positiveLabel;
    }

    [Serializable]
    private sealed class PoliticsOptionConfig
    {
        public string id;
        public string label;
        public string stanceId;
        public int stanceValue;
        public string axisId;
        public int axisValue;
    }

    [Serializable]
    private sealed class ExamRewardRule
    {
        public int minScore;
        public int merit;
        public int treasury;
    }

    [Serializable]
    private sealed class AcademyCoreConfig
    {
        public int studyDays = 6;
        public int studyLowDailyMoodThreshold = 2;
        public int studyLowDailyMoodDelta = 1;
        public int campusWanderMinGain = 2;
        public int campusWanderMaxExclusive = 8;
        public int campusWanderMoodGain = 4;
        public int courseStaminaLossMin = 4;
        public int courseStaminaLossMaxExclusive = 10;
        public int courseMinStaminaLoss = 1;
        public int sundayRestMoodGain = 8;
        public int sundayRestStaminaGain = 18;
        public int sundayStudyBaseGain = 5;
        public int sundayStudyMoodDelta = -2;
        public int inviteGain = 8;
        public int inviteMoodGain = 2;
        public int friendGatheringGain = 3;
        public int friendGatheringTreasuryCost = 8;
        public int politicsMoodGain = 1;
        public int lowStaminaThreshold = 15;
        public int lowStaminaMoodPenalty = 5;
        public int holidayMoodGain = 5;
        public int holidayStaminaGain = 12;
        public int examWrittenMin = 24;
        public int examWrittenMaxExclusive = 61;
        public float examCourseScoreMultiplier = 5.5f;
        public int examHighMoodThreshold = 75;
        public int examHighMoodBonus = 6;
        public int examLowMoodThreshold = 30;
        public int examLowMoodPenalty = -6;
    }

    [Serializable]
    private sealed class ProvinceConfig
    {
        public string id;
        public string name;
        public string city;
        public List<string> cities = new List<string>();
        public string region;
        public string terrain;
        public string description;
        public string owner;
        public int defense;
        public int income;
        public float x;
        public float y;
        public string roads;
        public string armyId;
    }

    [Serializable]
    private sealed class ArmyConfig
    {
        public string id;
        public string name;
        public string faction;
        public string provinceId;
        public int troops;
        public int maxTroops;
        public int move;
        public int maxMove;
        public int level;
        public int exp;
        public int attack;
        public int supply;
        public int maxSupply;
        public string aiProfile;
        public int intelLevel;
    }

    [Serializable]
    private sealed class BattleRoleConfig
    {
        public string id;
        public string displayName;
        public string symbol;
        public int baseHp;
        public int move;
        public int range;
        public int attackBonus;
        public int formation;
    }

    [Serializable]
    private sealed class CommonBattleUnitConfig
    {
        public string id;
        public string name;
        public string keyword;
        public string role;
        public string asset;
        public int idleFrames;
        public int moveFrames;
        public int attackFrames;
        public int hitFrames;
    }

    [Serializable]
    private sealed class TerrainRule
    {
        public string id;
        public string name;
        public int defenseInfantry;
        public int defenseCavalry;
        public int defenseArcher;
        public int moveInfantry;
        public int moveCavalry;
        public int moveArcher;
        public string color;
    }

    [Serializable]
    private sealed class BattleUnitSpawnConfig
    {
        public string side;
        public string suffix;
        public string role;
        public int q;
        public int r;
        public int attackBonus;
        public int troopDivisor;
    }

    [Serializable]
    private sealed class BattleTerrainTileConfig
    {
        public int q;
        public int r;
        public string terrain;
    }

    [Serializable]
    private sealed class BattleLabTriggerConfig
    {
        public string id;
        public string kind;
        public string side;
        public string role;
        public int q;
        public int r;
        public int radius;
        public string title;
        public string body;
        public string action;
        public string actionSide;
        public string actionRole;
        public int actionValue;
        public bool once = true;
    }

    [Serializable]
    private sealed class BattleRoleDamageRule
    {
        public string attackerRole;
        public string defenderRole;
        public int modifier;
    }

    [Serializable]
    private sealed class HealthFactorRule
    {
        public int minFormation;
        public int maxFormation;
        public int minHpPercent;
        public int numerator;
        public int denominator;
    }

    [Serializable]
    private sealed class BattleCoreConfig
    {
        public int hexCols = 9;
        public int hexRows = 7;
        public int objectiveQ = 4;
        public int objectiveR = 3;
        public int objectiveDefenseBonusPercent = 5;
        public int playerObjectiveRequiredTurns = 2;
        public int enemyObjectiveRequiredTurns = 2;
        public int playerStartMorale = 1;
        public int enemyStartMorale = 0;
        public int battleRandomMin = -4;
        public int battleRandomMaxExclusive = 7;
        public int aptitudeDamagePerLevel = 5;
        public int defenderLevelDamagePenalty = 2;
        public int counterDamagePercent = 55;
        public int minDamage = 4;
        public int minCounterDamage = 3;
        public int lowMoraleHpPercent = 30;
        public int minMorale = -2;
        public int maxMorale = 2;
        public int unitLevelMax = 5;
        public int unitLevelExpStep = 50;
        public int unitLevelAttackGain = 2;
        public int unitLevelHpGain = 8;
        public int battleExpHit = 12;
        public int battleExpKill = 24;
        public int armyLevelMax = 5;
        public int armyLevelExpStep = 50;
        public int armyLevelAttackGain = 3;
        public int armyLevelMaxTroopsGain = 15;
        public int armyLevelTroopsGain = 10;
        public int attackerArmyLevelHpPerLevel = 8;
        public int attackerArmyLevelAttackPerLevel = 2;
        public int attackerArmyLevelMoveBonusEveryLevels = 3;
        public int attackerArmyMaxMoveLevelBonusCap = 1;
        public int victoryArmyExp = 35;
        public int minTroopsAfterBattle = 20;
        public int defeatTroopDivisor = 2;
        public int victoryHighDefenseMerit = 50;
        public int victoryMidDefenseMerit = 24;
        public int victoryLowDefenseMerit = 15;
        public int defeatCommandLockTurns = 2;
        public int captureChancePercent = 5;
        public int strategySeasonTurnModulo = 4;
        public int strategyMissionCycleLength = 3;
        public int baseSupply = 8;
        public int supplyPerLogisticsLevel = 2;
        public int supplyTreasuryDivisor = 2;
        public int enemyPowerAttackMultiplier = 2;
        public int defenderPowerAttackMultiplier = 2;
        public int enemyPowerRandomMin = 0;
        public int enemyPowerRandomMaxExclusive = 35;
        public int enemyDefeatMinTroops = 12;
        public int enemyDefeatTroopLoss = 18;
        public int defenderVictoryTroopLoss = 12;
        public int formationDefaultCoefficient = 5;
        public int formationTwoCoefficient = 6;
        public int formationThreeCoefficient = 7;
    }

    [Serializable]
    private sealed class GameConfig
    {
        public string version;
        public List<LocalizedText> uiTexts = new List<LocalizedText>();
        public PlayerProfile playerDefaults = new PlayerProfile();
        public CalendarConfig calendar = new CalendarConfig();
        public List<CharacterTrait> traits = new List<CharacterTrait>();
        public List<CharacterOrigin> characterOrigins = new List<CharacterOrigin>();
        public List<CreationMemory> creationMemories = new List<CreationMemory>();
        public List<CreationTalent> creationTalents = new List<CreationTalent>();
        public List<CreationSubject> creationSubjects = new List<CreationSubject>();
        public List<PassiveSkillConfig> passiveSkills = new List<PassiveSkillConfig>();
        public List<QuestConfig> quests = new List<QuestConfig>();
        public List<AchievementConfig> achievements = new List<AchievementConfig>();
        public List<TitleConfig> titles = new List<TitleConfig>();
        public List<IntelligenceActionConfig> intelligenceActions = new List<IntelligenceActionConfig>();
        public List<AiProfileConfig> aiProfiles = new List<AiProfileConfig>();
        public List<SupplyRuleConfig> supplyRules = new List<SupplyRuleConfig>();
        public List<NewsArticle> news = new List<NewsArticle>();
        public List<CampusActivity> campusActivities = new List<CampusActivity>();
        public List<NarrativeFragmentConfig> narrativeFragments = new List<NarrativeFragmentConfig>();
        public List<CourseConfig> courses = new List<CourseConfig>();
        public List<MoodRule> moodRules = new List<MoodRule>();
        public List<AcademyLevelRule> academyLevels = new List<AcademyLevelRule>();
        public AcademyCoreConfig academyCore = new AcademyCoreConfig();
        public List<ExamRewardRule> examRewards = new List<ExamRewardRule>();
        public List<RankRule> ranks = new List<RankRule>();
        public List<RelationshipLevelRule> relationshipLevels = new List<RelationshipLevelRule>();
        public List<BeliefLevelRule> beliefLevels = new List<BeliefLevelRule>();
        public List<FactionConfig> factions = new List<FactionConfig>();
        public List<IdeologyAxisConfig> ideologyAxes = new List<IdeologyAxisConfig>();
        public List<PoliticsOptionConfig> politicsOptions = new List<PoliticsOptionConfig>();
        public List<Relationship> relationships = new List<Relationship>();
        public List<StanceScore> stances = new List<StanceScore>();
        public List<ProvinceConfig> provinces = new List<ProvinceConfig>();
        public List<ArmyConfig> armies = new List<ArmyConfig>();
        public List<BattleRoleConfig> battleRoles = new List<BattleRoleConfig>();
        public List<CommonBattleUnitConfig> commonUnits = new List<CommonBattleUnitConfig>();
        public List<TerrainRule> terrainRules = new List<TerrainRule>();
        public List<BattleUnitSpawnConfig> battleUnitSpawns = new List<BattleUnitSpawnConfig>();
        public List<BattleTerrainTileConfig> battleTerrainTiles = new List<BattleTerrainTileConfig>();
        public List<BattleRoleDamageRule> battleRoleDamageRules = new List<BattleRoleDamageRule>();
        public List<HealthFactorRule> healthFactors = new List<HealthFactorRule>();
        public BattleCoreConfig battleCore = new BattleCoreConfig();
    }

    [Serializable]
    private sealed class Province
    {
        public string id;
        public string name;
        public string city;
        public List<string> cities = new List<string>();
        public string region;
        public string terrain;
        public string description;
        public Faction owner;
        public int defense;
        public int income;
        public float x;
        public float y;
        public List<string> roads = new List<string>();
        public string armyId;
    }

    [Serializable]
    private sealed class Army
    {
        public string id;
        public string name;
        public Faction faction;
        public string provinceId;
        public int troops;
        public int maxTroops;
        public int move;
        public int maxMove;
        public int level;
        public int exp;
        public int attack;
        public int supply;
        public int maxSupply;
        public string aiProfile;
        public int intelLevel;
    }

    [Serializable]
    private sealed class BattleUnit
    {
        public string id;
        public string name;
        public string role;
        public Faction faction;
        public int startQ;
        public int startR;
        public int q;
        public int r;
        public int hp;
        public int maxHp;
        public int attack;
        public int move;
        public int range;
        public int level;
        public int exp;
        public int morale;
        public int formation;
        public bool moved;
        public bool acted;
        public bool guarding;
        public string armyId;
    }

    [Serializable]
    private sealed class BattleState
    {
        public string attackerArmyId;
        public string defenderArmyId;
        public string provinceId;
        public bool fromStrategy;
        public int turn = 1;
        public Faction activeFaction = Faction.Player;
        public Faction objectiveOwner = Faction.Neutral;
        public int playerObjectiveHold;
        public int enemyObjectiveHold;
        public string outcome = "playing";
        public bool outcomeApplied;
        public List<BattleUnit> units = new List<BattleUnit>();
        public List<string> firedTriggerIds = new List<string>();
    }

    [Serializable]
    private sealed class BattleLevelDesign
    {
        public string name = "工坊测试关";
        public string author = "策划";
        public string description = "使用地图编辑器制作的战棋关卡。";
        public int hexCols = 9;
        public int hexRows = 7;
        public string objectiveType = "capture";
        public int objectiveQ = 4;
        public int objectiveR = 3;
        public int turnLimit = 0;
        public string weather = "clear";
        public string enemyAiProfile = "tactical";
        public int playerTroops = 420;
        public int enemyTroops = 420;
        public int playerAttack = 18;
        public int enemyAttack = 18;
        public List<BattleTerrainTileConfig> terrainTiles = new List<BattleTerrainTileConfig>();
        public List<BattleUnitSpawnConfig> spawns = new List<BattleUnitSpawnConfig>();
        public List<BattleLabTriggerConfig> triggers = new List<BattleLabTriggerConfig>();
    }

    private enum BattleAnimationKind { Move, Attack, Hit }
    private enum AiMoveIntent { Advance, Retreat }

    private sealed class BattleAnimation
    {
        public string unitId;
        public BattleAnimationKind kind;
        public Vector2 from;
        public Vector2 to;
        public float duration;
        public float elapsed;
        public float direction;
    }

    [Serializable]
    private sealed class SaveData
    {
        public PlayerProfile player;
        public List<Relationship> relationships;
        public List<StanceScore> stances;
        public List<Province> provinces;
        public List<Army> armies;
        public int strategyTurn;
        public int season;
        public ScreenMode mode;
        public string log;
        public string currentMainEventId;
        public List<string> completedStoryEvents;
        public List<StoryValue> storyValues;
    }

    private const string SaveKey = "MingLuUnitySaveV1";
    private const string BattleLabSaveKey = "MingLuUnitySaveV1_BattleLab";
    private const string BattleLabAttackerId = "__battle_lab_attacker";
    private const string BattleLabDefenderId = "__battle_lab_defender";
    private const string BattleLabProvinceId = "__battle_lab";
    private const string BattleLabExportFileSuffix = "_battle_level.json";
    private readonly Color bg = new Color(0.84f, 0.78f, 0.66f);
    private readonly Color panel = new Color(0.94f, 0.88f, 0.73f);
    private readonly Color panel2 = new Color(0.37f, 0.54f, 0.44f);
    private readonly Color ink = new Color(0.19f, 0.15f, 0.11f);
    private readonly Color muted = new Color(0.44f, 0.39f, 0.32f);
    private readonly Color playerColor = new Color(0.20f, 0.46f, 0.75f);
    private readonly Color enemyColor = new Color(0.66f, 0.22f, 0.20f);
    private readonly Color neutralColor = new Color(0.38f, 0.38f, 0.35f);
    private readonly Color highlightColor = new Color(0.73f, 0.52f, 0.18f);

    private Canvas canvas;
    private RectTransform root;
    private Font font;
    private GameConfig gameConfig = new GameConfig();
    private readonly Dictionary<string, string> uiTexts = new Dictionary<string, string>();
    private ScreenMode mode = ScreenMode.Title;
    private PlayerProfile player = new PlayerProfile();
    private List<Relationship> relationships = new List<Relationship>();
    private List<StanceScore> stances = new List<StanceScore>();
    private List<Province> provinces = new List<Province>();
    private List<Army> armies = new List<Army>();
    private List<string> logLines = new List<string>();
    private StoryDatabase storyDatabase = new StoryDatabase();
    private string currentMainEventId = "EV001";
    private readonly List<string> completedStoryEvents = new List<string>();
    private List<StoryValue> storyValues = new List<StoryValue>();
    private string selectedProvinceId;
    private string selectedArmyId;
    private int strategyTurn = 1;
    private int season = 1760;
    private BattleState battle;
    private string selectedUnitId;
    private RectTransform battleBoardContent;
    private Vector2 battlePan = new Vector2(0, 12);
    private float battleDragDistance;
    private float battleIgnoreClickUntil;
    private string battleMessage = "选择蓝方军团开始行动。";
    private readonly List<BattleAnimation> battleAnimations = new List<BattleAnimation>();
    private readonly Dictionary<string, RectTransform> battleUnitViews = new Dictionary<string, RectTransform>();
    private readonly Dictionary<string, BattleUnitBadgeGraphic> battleUnitBadges = new Dictionary<string, BattleUnitBadgeGraphic>();
    private readonly Dictionary<string, Image> battleUnitSprites = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> battleLabSpawnSprites = new Dictionary<string, Image>();
    private BattleLevelDesign battleLabDesign;
    private string battleLabBrush = "player";
    private string battleLabTab = "map";
    private string battleLabTerrain = "plain";
    private string battleLabRole = "infantry";
    private string battleLabTriggerAction = "none";
    private int battleLabBrushSize = 1;
    private int battleLabTriggerStoryPreset;
    private string battleLabMessage = "";
    private List<BattleTerrainTileConfig> battleTerrainOverride;
    private string pendingStoryTitle = "";
    private string pendingStoryBody = "";
    private List<Tuple<string, Action>> pendingStoryOptions = new List<Tuple<string, Action>>();
    private Action pendingStoryReturnAction;
    private string pendingStoryPortraitName = "";
    private string pendingStorySceneId = "academy";
    private string activeStoryEventId = "";
    private int activeStoryPageIndex;
    private ScreenMode storyReturnMode = ScreenMode.Academy;
    private int characterArchivePage;
    private readonly List<string> creationTraitIds = new List<string>();
    private readonly List<string> creationMemoryChoiceIds = new List<string>();
    private readonly List<string> creationTalentIds = new List<string>();
    private readonly List<string> creationSubjectIds = new List<string>();
    private int creationStep;
    private string creationNameDraft = "";
    private string creationCourtesyDraft = "";
    private string creationOriginId = "";
    private string characterCreateMessage = "选择 1-3 个特性，确认后进入新京军事学院。";
    private string pendingAttackAttackerId = "";
    private string pendingAttackDefenderId = "";
    private float lastPointerTime = -10f;
    private string lastPointerKey = "";
    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        Application.targetFrameRate = 60;
        EnsureUnityUi();
        LoadStoryDatabase();
        LoadGameConfig();
        ResetGame();
        ShowTitle();
    }

    private void LoadStoryDatabase()
    {
        TextAsset asset = Resources.Load<TextAsset>("MingLuStoryData");
        if (asset == null)
        {
            storyDatabase = new StoryDatabase();
            return;
        }

        storyDatabase = JsonUtility.FromJson<StoryDatabase>(asset.text) ?? new StoryDatabase();
        if (storyDatabase.events == null) storyDatabase.events = new List<StoryEventData>();
        if (storyDatabase.characters == null) storyDatabase.characters = new List<StoryCharacterData>();
    }

    private void LoadGameConfig()
    {
        TextAsset asset = Resources.Load<TextAsset>("Data/MingLuGameConfig");
        gameConfig = asset != null ? JsonUtility.FromJson<GameConfig>(asset.text) : new GameConfig();
        if (gameConfig == null) gameConfig = new GameConfig();
        EnsureGameConfigLists();
        uiTexts.Clear();
        foreach (LocalizedText text in gameConfig.uiTexts)
        {
            if (text != null && !string.IsNullOrEmpty(text.key)) uiTexts[text.key] = text.value ?? "";
        }
    }

    private void EnsureGameConfigLists()
    {
        if (gameConfig.uiTexts == null) gameConfig.uiTexts = new List<LocalizedText>();
        if (gameConfig.playerDefaults == null) gameConfig.playerDefaults = new PlayerProfile();
        if (gameConfig.playerDefaults.traits == null) gameConfig.playerDefaults.traits = new List<string>();
        if (gameConfig.playerDefaults.creationMemoryChoices == null) gameConfig.playerDefaults.creationMemoryChoices = new List<string>();
        if (gameConfig.playerDefaults.subjectFocusIds == null) gameConfig.playerDefaults.subjectFocusIds = new List<string>();
        if (gameConfig.playerDefaults.prisoners == null) gameConfig.playerDefaults.prisoners = new List<string>();
        if (gameConfig.calendar == null) gameConfig.calendar = new CalendarConfig();
        if (gameConfig.traits == null) gameConfig.traits = new List<CharacterTrait>();
        if (gameConfig.characterOrigins == null) gameConfig.characterOrigins = new List<CharacterOrigin>();
        if (gameConfig.creationMemories == null) gameConfig.creationMemories = new List<CreationMemory>();
        if (gameConfig.creationTalents == null) gameConfig.creationTalents = new List<CreationTalent>();
        if (gameConfig.creationSubjects == null) gameConfig.creationSubjects = new List<CreationSubject>();
        if (gameConfig.passiveSkills == null) gameConfig.passiveSkills = new List<PassiveSkillConfig>();
        if (gameConfig.quests == null) gameConfig.quests = new List<QuestConfig>();
        if (gameConfig.achievements == null) gameConfig.achievements = new List<AchievementConfig>();
        if (gameConfig.titles == null) gameConfig.titles = new List<TitleConfig>();
        if (gameConfig.intelligenceActions == null) gameConfig.intelligenceActions = new List<IntelligenceActionConfig>();
        if (gameConfig.aiProfiles == null) gameConfig.aiProfiles = new List<AiProfileConfig>();
        if (gameConfig.supplyRules == null) gameConfig.supplyRules = new List<SupplyRuleConfig>();
        if (gameConfig.news == null) gameConfig.news = new List<NewsArticle>();
        if (gameConfig.campusActivities == null) gameConfig.campusActivities = new List<CampusActivity>();
        if (gameConfig.narrativeFragments == null) gameConfig.narrativeFragments = new List<NarrativeFragmentConfig>();
        if (gameConfig.courses == null) gameConfig.courses = new List<CourseConfig>();
        if (gameConfig.moodRules == null) gameConfig.moodRules = new List<MoodRule>();
        if (gameConfig.academyLevels == null) gameConfig.academyLevels = new List<AcademyLevelRule>();
        if (gameConfig.academyCore == null) gameConfig.academyCore = new AcademyCoreConfig();
        if (gameConfig.examRewards == null) gameConfig.examRewards = new List<ExamRewardRule>();
        if (gameConfig.ranks == null) gameConfig.ranks = new List<RankRule>();
        if (gameConfig.relationshipLevels == null) gameConfig.relationshipLevels = new List<RelationshipLevelRule>();
        if (gameConfig.beliefLevels == null) gameConfig.beliefLevels = new List<BeliefLevelRule>();
        if (gameConfig.factions == null) gameConfig.factions = new List<FactionConfig>();
        if (gameConfig.ideologyAxes == null) gameConfig.ideologyAxes = new List<IdeologyAxisConfig>();
        if (gameConfig.politicsOptions == null) gameConfig.politicsOptions = new List<PoliticsOptionConfig>();
        if (gameConfig.relationships == null) gameConfig.relationships = new List<Relationship>();
        if (gameConfig.stances == null) gameConfig.stances = new List<StanceScore>();
        if (gameConfig.provinces == null) gameConfig.provinces = new List<ProvinceConfig>();
        foreach (ProvinceConfig province in gameConfig.provinces)
        {
            if (province.cities == null) province.cities = new List<string>();
        }
        if (gameConfig.armies == null) gameConfig.armies = new List<ArmyConfig>();
        if (gameConfig.battleRoles == null) gameConfig.battleRoles = new List<BattleRoleConfig>();
        if (gameConfig.commonUnits == null) gameConfig.commonUnits = new List<CommonBattleUnitConfig>();
        if (gameConfig.terrainRules == null) gameConfig.terrainRules = new List<TerrainRule>();
        if (gameConfig.battleUnitSpawns == null) gameConfig.battleUnitSpawns = new List<BattleUnitSpawnConfig>();
        if (gameConfig.battleTerrainTiles == null) gameConfig.battleTerrainTiles = new List<BattleTerrainTileConfig>();
        if (gameConfig.battleRoleDamageRules == null) gameConfig.battleRoleDamageRules = new List<BattleRoleDamageRule>();
        if (gameConfig.healthFactors == null) gameConfig.healthFactors = new List<HealthFactorRule>();
        if (gameConfig.battleCore == null) gameConfig.battleCore = new BattleCoreConfig();
    }

    private string T(string key, string fallback)
    {
        return uiTexts.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }

    private string TF(string key, string fallback, params object[] args)
    {
        string format = T(key, fallback);
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return string.Format(fallback, args);
        }
    }

    private PlayerProfile ConfigPlayerDefaults()
    {
        PlayerProfile clone = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(gameConfig.playerDefaults)) ?? new PlayerProfile();
        EnsurePlayerRuntimeLists(clone);
        return clone;
    }

    private void EnsurePlayerRuntimeLists(PlayerProfile profile)
    {
        if (profile == null) return;
        if (profile.traits == null) profile.traits = new List<string>();
        if (profile.creationMemoryChoices == null) profile.creationMemoryChoices = new List<string>();
        if (profile.subjectFocusIds == null) profile.subjectFocusIds = new List<string>();
        if (profile.prisoners == null) profile.prisoners = new List<string>();
        if (profile.unlockedSkills == null) profile.unlockedSkills = new List<string>();
        if (profile.equippedSkills == null) profile.equippedSkills = new List<string>();
        if (profile.activeQuests == null) profile.activeQuests = new List<string>();
        if (profile.completedQuests == null) profile.completedQuests = new List<string>();
        if (profile.unlockedAchievements == null) profile.unlockedAchievements = new List<string>();
        if (profile.unlockedTitles == null) profile.unlockedTitles = new List<string>();
        if (profile.unlockedEndings == null) profile.unlockedEndings = new List<string>();
        if (profile.eventReview == null) profile.eventReview = new List<string>();
        if (profile.intelligence <= 0) profile.intelligence = 12;
        if (profile.equippedTitle == null) profile.equippedTitle = "";
        if (profile.lastCourse == null) profile.lastCourse = "";
        if (profile.courtesyName == null) profile.courtesyName = "";
        if (profile.originId == null) profile.originId = "";
        if (profile.personalityId == null) profile.personalityId = "";
    }

    private string DefaultPlayerName()
    {
        string configured = gameConfig != null && gameConfig.playerDefaults != null ? gameConfig.playerDefaults.name : "";
        return string.IsNullOrWhiteSpace(configured) ? "夏邑" : configured.Trim();
    }

    private List<Relationship> ConfigRelationships()
    {
        List<Relationship> source = gameConfig.relationships != null && gameConfig.relationships.Count > 0 ? gameConfig.relationships : DefaultRelationships();
        int currentWeek = CurrentCalendarWeek();
        List<Relationship> cloned = source.Select(rel => JsonUtility.FromJson<Relationship>(JsonUtility.ToJson(rel))).Where(rel => rel != null).ToList();
        foreach (Relationship rel in cloned)
        {
            if (rel.lastInteractionWeek <= 0) rel.lastInteractionWeek = currentWeek;
        }
        return cloned;
    }

    private List<StanceScore> ConfigStances()
    {
        List<StanceScore> source = gameConfig.stances != null && gameConfig.stances.Count > 0 ? gameConfig.stances : DefaultStances();
        return source.Select(score => JsonUtility.FromJson<StanceScore>(JsonUtility.ToJson(score))).Where(score => score != null).ToList();
    }

    private List<Relationship> DefaultRelationships()
    {
        return new List<Relationship>
        {
            new Relationship { id = "zhao", name = "赵伯衡", stance = "返乡团", affection = 10, circle = "将门子弟", knownLevel = 1, note = "豪爽热血的将门子弟，常把复国与军功挂在嘴边。" },
            new Relationship { id = "lin", name = "林素心", stance = "自由派", affection = 10, circle = "图书馆", knownLevel = 1, note = "图书馆常客，温雅而坚定，关心民权与共和。" },
            new Relationship { id = "yierde", name = "伊尔德", stance = "印第安乡党", affection = 10, circle = "归化部落", knownLevel = 1, note = "部落首领之子，在两个世界之间寻找自己的道路。" },
            new Relationship { id = "chen", name = "陈敬之", stance = "法治派", affection = 10, circle = "世家子弟", knownLevel = 1, note = "世家子弟，骄矜冷峻，相信秩序与门第。" },
            new Relationship { id = "li", name = "李婉清", stance = "陆军青壮派", affection = 10, circle = "南方军校生", knownLevel = 1, note = "南方转学生，清冷果断，兼具军人气质与改革锋芒。" }
        };
    }

    private List<StanceScore> DefaultStances()
    {
        return new List<StanceScore>
        {
            new StanceScore { id = "home", name = "返乡团", value = 20 },
            new StanceScore { id = "army", name = "陆军青壮派", value = 20 },
            new StanceScore { id = "native", name = "印第安乡党", value = 20 },
            new StanceScore { id = "liberal", name = "自由派", value = 20 },
            new StanceScore { id = "legal", name = "法治派", value = 20 }
        };
    }

    private void EnsureUnityUi()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            if (Application.isPlaying) DontDestroyOnLoad(es);
        }

        GameObject canvasGo = new GameObject("MingLuCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        if (Application.isPlaying) DontDestroyOnLoad(canvasGo);

        root = canvasGo.GetComponent<RectTransform>();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void ResetGame()
    {
        mode = ScreenMode.Title;
        player = ConfigPlayerDefaults();
        creationTraitIds.Clear();
        creationMemoryChoiceIds.Clear();
        creationTalentIds.Clear();
        creationSubjectIds.Clear();
        creationStep = 0;
        creationNameDraft = "";
        creationCourtesyDraft = "";
        creationOriginId = CharacterOriginCatalog().FirstOrDefault()?.id ?? "noble";
        characterCreateMessage = T("character_create.message", "按步骤完成姓名、出身、往事、天赋和学科倾向。");
        relationships = ConfigRelationships();
        stances = ConfigStances();
        currentMainEventId = "EV001";
        completedStoryEvents.Clear();
        storyValues = new List<StoryValue>();
        BuildStrategyMap();
        logLines.Clear();
        AddLog(T("log.game_start", "1760年，你抵达新京军事学院。"));
        UpdatePlayerRank();
        RefreshProgressionSystems(false);
    }

    private void BuildStrategyMap()
    {
        if (gameConfig.provinces != null && gameConfig.provinces.Count > 0 && gameConfig.armies != null && gameConfig.armies.Count > 0)
        {
            provinces = gameConfig.provinces.Select(p => new Province
            {
                id = p.id,
                name = p.name,
                city = p.city ?? "",
                cities = p.cities != null ? p.cities.Where(c => !string.IsNullOrEmpty(c)).ToList() : new List<string>(),
                region = p.region ?? "",
                terrain = p.terrain ?? "",
                description = p.description ?? "",
                owner = ParseFaction(p.owner, Faction.Neutral),
                defense = p.defense,
                income = p.income,
                x = p.x,
                y = p.y,
                armyId = p.armyId ?? "",
                roads = new List<string>()
            }).ToList();

            foreach (ProvinceConfig p in gameConfig.provinces)
            {
                foreach (string road in SplitConfigList(p.roads))
                {
                    Link(p.id, road);
                }
            }

            armies = gameConfig.armies.Select(a => new Army
            {
                id = a.id,
                name = a.name,
                faction = ParseFaction(a.faction, Faction.Neutral),
                provinceId = a.provinceId,
                troops = a.troops,
                maxTroops = a.maxTroops > 0 ? a.maxTroops : a.troops,
                move = a.move,
                maxMove = a.maxMove > 0 ? a.maxMove : a.move,
                level = a.level,
                exp = a.exp,
                attack = a.attack,
                maxSupply = a.maxSupply > 0 ? a.maxSupply : DefaultArmyMaxSupply(a.faction),
                supply = a.supply > 0 ? a.supply : (a.maxSupply > 0 ? a.maxSupply : DefaultArmyMaxSupply(a.faction)),
                aiProfile = string.IsNullOrEmpty(a.aiProfile) ? DefaultAiProfileForFaction(ParseFaction(a.faction, Faction.Neutral)) : a.aiProfile,
                intelLevel = a.intelLevel
            }).ToList();
        }
        else
        {
            provinces = new List<Province>
            {
                NewProvince("xinjing", "新京", Faction.Player, 80, 35, -330, 150),
                NewProvince("linhai", "临海", Faction.Player, 62, 24, -125, 215),
                NewProvince("hegu", "河谷", Faction.Player, 55, 18, -230, -25),
                NewProvince("beiling", "北岭", Faction.Imperial, 58, 16, 40, 80),
                NewProvince("songlin", "松林", Faction.Native, 48, 12, 235, 190),
                NewProvince("shigu", "石谷", Faction.Reformist, 65, 20, 215, -35),
                NewProvince("xigang", "西港", Faction.Foreign, 70, 28, 420, 70),
                NewProvince("nanze", "南泽", Faction.Neutral, 44, 14, -5, -180),
                NewProvince("hongyuan", "红原", Faction.Reformist, 52, 18, 385, -160)
            };

            Link("xinjing", "linhai");
            Link("xinjing", "hegu");
            Link("linhai", "beiling");
            Link("hegu", "beiling");
            Link("hegu", "nanze");
            Link("beiling", "songlin");
            Link("beiling", "shigu");
            Link("songlin", "xigang");
            Link("shigu", "xigang");
            Link("shigu", "hongyuan");
            Link("nanze", "shigu");

            armies = new List<Army>
            {
                NewArmy("a1", "第一军团", Faction.Player, "xinjing", 120, 34),
                NewArmy("a2", "归化骑队", Faction.Player, "hegu", 82, 28),
                NewArmy("e1", "禁卫前锋", Faction.Imperial, "beiling", 96, 30),
                NewArmy("e2", "革故民兵", Faction.Reformist, "shigu", 104, 27),
                NewArmy("e3", "西港殖民队", Faction.Foreign, "xigang", 118, 32)
            };
        }

        foreach (Army army in armies)
        {
            Province province = ProvinceById(army.provinceId);
            if (province != null) province.armyId = army.id;
        }
    }

    private Province NewProvince(string id, string name, Faction owner, int defense, int income, float x, float y, string city = "", string region = "", string terrain = "", string description = "")
    {
        List<string> fallbackCities = string.IsNullOrEmpty(city) ? new List<string>() : new List<string> { city };
        return new Province { id = id, name = name, city = city, cities = fallbackCities, region = region, terrain = terrain, description = description, owner = owner, defense = defense, income = income, x = x, y = y };
    }

    private Army NewArmy(string id, string name, Faction faction, string provinceId, int troops, int attack)
    {
        return new Army
        {
            id = id,
            name = name,
            faction = faction,
            provinceId = provinceId,
            troops = troops,
            maxTroops = troops,
            move = 1,
            maxMove = 1,
            level = 0,
            exp = 0,
            attack = attack,
            supply = DefaultArmyMaxSupply(faction),
            maxSupply = DefaultArmyMaxSupply(faction),
            aiProfile = DefaultAiProfileForFaction(faction),
            intelLevel = faction == Faction.Player ? 3 : 1
        };
    }

    private int DefaultArmyMaxSupply(string faction)
    {
        return DefaultArmyMaxSupply(ParseFaction(faction, Faction.Neutral));
    }

    private int DefaultArmyMaxSupply(Faction faction)
    {
        return faction == Faction.Player ? 42 : 36;
    }

    private string DefaultAiProfileForFaction(Faction faction)
    {
        if (faction == Faction.Imperial) return "defensive";
        if (faction == Faction.Reformist) return "tactical";
        if (faction == Faction.Native) return "mobile";
        if (faction == Faction.Foreign) return "aggressive";
        return "balanced";
    }

    private List<string> SplitConfigList(string value)
    {
        return string.IsNullOrEmpty(value)
            ? new List<string>()
            : value.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
    }

    private Faction ParseFaction(string value, Faction fallback)
    {
        if (string.IsNullOrEmpty(value)) return fallback;
        if (Enum.TryParse(value, true, out Faction parsed)) return parsed;
        FactionConfig configured = FactionConfigs().FirstOrDefault(f => f.displayName == value || f.id == value);
        if (configured != null && Enum.TryParse(configured.id, true, out parsed)) return parsed;
        if (value == "我方") return Faction.Player;
        if (value.Contains("返乡") || value.Contains("朝廷")) return Faction.Imperial;
        if (value.Contains("革故")) return Faction.Reformist;
        if (value.Contains("印第安")) return Faction.Native;
        if (value.Contains("外邦") || value.Contains("殖民")) return Faction.Foreign;
        return fallback;
    }

    private void Link(string a, string b)
    {
        Province pa = ProvinceById(a);
        Province pb = ProvinceById(b);
        if (pa != null && !pa.roads.Contains(b)) pa.roads.Add(b);
        if (pb != null && !pb.roads.Contains(a)) pb.roads.Add(a);
    }

    private void Clear()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static void EnsureCanvasRenderer(GameObject go)
    {
        if (go != null && go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
    }

    private Sprite LoadArtSprite(string resourcePath, Vector4 border = default(Vector4))
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;
        string key = resourcePath + "|" + border;
        if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
            if (loadedSprite == null) return null;
            spriteCache[key] = loadedSprite;
            return loadedSprite;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
        spriteCache[key] = sprite;
        return sprite;
    }

    private RectTransform CreateSpriteRect(string name, Transform parent, Vector2 pos, Vector2 size, string resourcePath, Color fallbackColor, bool sliced = false, bool preserveAspect = false, Vector4 border = default(Vector4))
    {
        RectTransform rt = CreateRect(name, parent, pos, size, fallbackColor);
        Image image = rt.GetComponent<Image>();
        Sprite sprite = LoadArtSprite(resourcePath, border);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = preserveAspect;
        }
        return rt;
    }

    private void DrawSceneBackground(string sceneId)
    {
        string resource = SceneResource(sceneId);
        CreateSpriteRect("BackgroundArt", root, Vector2.zero, new Vector2(1280, 720), resource, bg, false, false);
        bool darkScene = sceneId == "battlefield" || sceneId == "frontier" || sceneId == "strategy";
        Color shade = darkScene ? new Color(0.03f, 0.025f, 0.020f, 0.34f) : new Color(0.78f, 0.68f, 0.48f, 0.24f);
        CreateRect("BackgroundShade", root, Vector2.zero, new Vector2(1400, 820), shade);
    }

    private string SceneResource(string sceneId)
    {
        if (sceneId == "title") return "Art/Scenes/scene_title";
        if (sceneId == "library") return "Art/Scenes/scene_library";
        if (sceneId == "palace") return "Art/Scenes/scene_palace";
        if (sceneId == "council") return "Art/Scenes/scene_council";
        if (sceneId == "strategy") return "Art/Scenes/scene_strategy";
        if (sceneId == "battlefield") return "Art/Scenes/scene_battlefield";
        if (sceneId == "frontier") return "Art/Scenes/scene_frontier";
        if (sceneId == "harbor") return "Art/Scenes/scene_harbor";
        if (sceneId == "street") return "Art/Scenes/scene_street";
        return "Art/Scenes/scene_academy";
    }

    private string SceneForStoryEvent(StoryEventData ev)
    {
        if (ev == null) return "academy";
        string text = (ev.chapter ?? "") + " " + (ev.type ?? "") + " " + (ev.trigger ?? "") + " " + StoryEventShortTitle(ev);
        if (text.Contains("宫") || text.Contains("皇") || text.Contains("太子") || text.Contains("太后")) return "palace";
        if (text.Contains("海") || text.Contains("船") || text.Contains("港") || text.Contains("西班牙") || text.Contains("英国") || text.Contains("法国")) return "harbor";
        if (text.Contains("边") || text.Contains("部落") || text.Contains("印第安") || text.Contains("草原")) return "frontier";
        if (text.Contains("战") || text.Contains("军令") || text.Contains("前线") || text.Contains("据点")) return "battlefield";
        if (text.Contains("议") || text.Contains("朝") || text.Contains("会议")) return "council";
        if (text.Contains("书") || text.Contains("报") || text.Contains("图书")) return "library";
        if (text.Contains("街") || text.Contains("市井") || text.Contains("商")) return "street";
        return "academy";
    }

    private bool ApplyButtonSkin(Image image, Color? requestedColor)
    {
        string skin = ButtonSkinForColor(requestedColor);
        Sprite sprite = LoadArtSprite("Art/UI/" + skin, new Vector4(32, 32, 32, 32));
        if (sprite == null) return false;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        return true;
    }

    private string ButtonSkinForColor(Color? requestedColor)
    {
        if (!requestedColor.HasValue) return "button_clean_jade";
        Color color = requestedColor.Value;
        if (color.r > 0.42f && color.g < 0.32f && color.b < 0.32f) return "button_clean_red";
        if (color.b > color.r && color.b > color.g) return "button_clean_blue";
        if (color.g > color.r && color.g > color.b) return "button_clean_jade";
        return "button_clean_neutral";
    }

    private Color ButtonLabelColor(Color? requestedColor)
    {
        string skin = ButtonSkinForColor(requestedColor);
        if (skin == "button_clean_neutral") return ink;
        return new Color(1.00f, 0.96f, 0.84f);
    }

    private RectTransform AddPortrait(Transform parent, string characterName, Vector2 pos, Vector2 size, bool dimFallback = false)
    {
        string resource = PortraitResource(characterName);
        Color fallback = dimFallback ? new Color(0.18f, 0.18f, 0.20f, 0.75f) : new Color(0.12f, 0.13f, 0.16f, 0.0f);
        RectTransform frame = CreateRect("PortraitFrame_" + SafeText(characterName, "Player"), parent, pos, size + new Vector2(22, 22), new Color(0.86f, 0.72f, 0.44f, 0.42f));
        Image frameImage = frame.GetComponent<Image>();
        Sprite panelSprite = LoadArtSprite("Art/UI/panel_clean_paper", new Vector4(28, 28, 28, 28));
        if (panelSprite != null)
        {
            frameImage.sprite = panelSprite;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = Color.white;
        }
        RectTransform portrait = CreateSpriteRect("Portrait_" + SafeText(characterName, "Player"), frame, Vector2.zero, size, resource, fallback, false, true);
        portrait.GetComponent<Image>().raycastTarget = false;
        return frame;
    }

    private string PortraitResource(string characterName)
    {
        if (string.IsNullOrEmpty(characterName) || characterName == "旁白" || characterName == player.name || characterName == "夏邑" || characterName == "莫明远") return "Art/Portraits/portrait_player_mo_mingyuan";
        StoryCharacterData character = StoryCharacterByName(characterName);
        if (character != null && !string.IsNullOrEmpty(character.asset)) return character.asset;
        Relationship rel = relationships.FirstOrDefault(r => r.name == characterName);
        if (rel != null)
        {
            character = StoryCharacterByName(rel.name);
            if (character != null && !string.IsNullOrEmpty(character.asset)) return character.asset;
        }
        return "Art/Portraits/portrait_player_mo_mingyuan";
    }

    private string CurrentStorySpeaker(StoryEventData ev, int pageCount)
    {
        if (ev == null || ev.lines == null || ev.lines.Count == 0) return "";
        List<StoryLineData> voiced = ev.lines.Where(l => !string.IsNullOrEmpty(l.speaker) && l.speaker != "旁白").ToList();
        if (voiced.Count == 0) return "";
        int index = pageCount <= 1 ? 0 : Mathf.RoundToInt(activeStoryPageIndex * (voiced.Count - 1) / (float)Mathf.Max(1, pageCount - 1));
        return voiced[Mathf.Clamp(index, 0, voiced.Count - 1)].speaker;
    }

    private RectTransform CreateEmptyRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private Text CreateText(string name, Transform parent, string value, int size, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Text AddText(Transform parent, string value, Vector2 pos, Vector2 size, int fontSize = 22, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
    {
        Text text = CreateText("Text", parent, value, fontSize, color ?? ink, anchor);
        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return text;
    }

    private InputField AddInputField(Transform parent, string value, string placeholder, Vector2 pos, Vector2 size, Action<string> onChanged, int characterLimit = 12, int fontSize = 20)
    {
        RectTransform rt = CreateRect("InputField", parent, pos, size, new Color(0.96f, 0.90f, 0.75f, 0.96f));
        Image image = rt.GetComponent<Image>();
        image.sprite = LoadArtSprite("Art/UI/button_clean_neutral", new Vector4(30, 30, 30, 30));
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        InputField input = rt.gameObject.AddComponent<InputField>();
        input.targetGraphic = image;
        input.characterLimit = Mathf.Max(0, characterLimit);
        input.lineType = InputField.LineType.SingleLine;
        input.contentType = InputField.ContentType.Standard;

        Text text = CreateText("InputText", rt, value ?? "", fontSize, ink, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14, 4);
        textRt.offsetMax = new Vector2(-14, -4);

        Text placeholderText = CreateText("Placeholder", rt, placeholder ?? "", Mathf.Max(10, fontSize - 2), muted, TextAnchor.MiddleLeft);
        placeholderText.fontStyle = FontStyle.Italic;
        RectTransform placeholderRt = placeholderText.GetComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = new Vector2(14, 4);
        placeholderRt.offsetMax = new Vector2(-14, -4);

        input.textComponent = text;
        input.placeholder = placeholderText;
        input.text = value ?? "";
        input.onValueChanged.AddListener(v => onChanged?.Invoke(v));
        return input;
    }

    private Button AddButton(Transform parent, string label, Vector2 pos, Vector2 size, Action action, Color? color = null)
    {
        RectTransform rt = CreateRect("Button_" + label, parent, pos, size, color ?? panel2);
        Image image = rt.GetComponent<Image>();
        bool hasSkin = ApplyButtonSkin(image, color);
        Button button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = hasSkin ? Color.white : color ?? panel2;
        colors.highlightedColor = hasSkin ? new Color(1.00f, 0.96f, 0.78f, 1f) : highlightColor;
        colors.pressedColor = hasSkin ? new Color(0.82f, 0.78f, 0.64f, 1f) : new Color(0.10f, 0.36f, 0.55f);
        colors.selectedColor = hasSkin ? new Color(0.96f, 0.86f, 0.55f, 1f) : highlightColor;
        button.colors = colors;
        int buttonFontSize = label.Contains("\n") ? Mathf.RoundToInt(size.y > 54 ? 17 : 15) : Mathf.RoundToInt(size.y > 54 ? 22 : 18);
        if (label.Length > 64) buttonFontSize = Mathf.Min(buttonFontSize, 13);
        else if (label.Length > 42) buttonFontSize = Mathf.Min(buttonFontSize, 15);
        Text text = CreateText("Label", rt, label, buttonFontSize, ButtonLabelColor(color), TextAnchor.MiddleCenter);
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.9f;
        RectTransform trt = text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(4, 2);
        trt.offsetMax = new Vector2(-4, -2);
        button.onClick.AddListener(() => ActivateOnce(label, action));
        return button;
    }

    private Button AddFlatButton(Transform parent, string label, Vector2 pos, Vector2 size, Action action, Color? color = null, int fontSize = 15, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        Color normal = color ?? new Color(0.78f, 0.66f, 0.43f, 0.96f);
        RectTransform rt = CreateRect("FlatButton_" + label, parent, pos, size, normal);
        Image image = rt.GetComponent<Image>();
        Color? skinColor = color ?? normal;
        bool hasSkin = ApplyButtonSkin(image, skinColor);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = hasSkin ? Color.white : normal;
        colors.highlightedColor = hasSkin ? new Color(1.00f, 0.96f, 0.80f, 1f) : Color.Lerp(normal, highlightColor, 0.28f);
        colors.pressedColor = hasSkin ? new Color(0.82f, 0.78f, 0.66f, 1f) : Color.Lerp(normal, Color.black, 0.18f);
        colors.selectedColor = hasSkin ? new Color(0.96f, 0.86f, 0.55f, 1f) : Color.Lerp(normal, highlightColor, 0.20f);
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.40f);
        button.colors = colors;

        int maxFont = fontSize;
        if (label.Contains("\n")) maxFont = Mathf.Min(maxFont, 13);
        if (label.Length > 40) maxFont = Mathf.Min(maxFont, 12);
        Text text = CreateText("Label", rt, label, maxFont, ButtonLabelColor(skinColor), anchor);
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = maxFont;
        text.lineSpacing = 0.92f;
        RectTransform trt = text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10, 4);
        trt.offsetMax = new Vector2(-10, -4);
        button.onClick.AddListener(() => ActivateOnce(label, action));
        return button;
    }

    private Button AddTraitChoiceCard(Transform parent, CharacterTrait trait, bool selected, Vector2 pos, Vector2 size, Action action)
    {
        string key = trait != null && !string.IsNullOrEmpty(trait.id) ? trait.id : "trait";
        RectTransform rt = CreateRect("TraitCard_" + key, parent, pos, size, selected ? new Color(0.95f, 0.84f, 0.57f, 0.97f) : new Color(0.96f, 0.89f, 0.72f, 0.95f));
        Image image = rt.GetComponent<Image>();
        image.sprite = LoadArtSprite("Art/UI/panel_clean_paper", new Vector4(28, 28, 28, 28));
        image.type = Image.Type.Sliced;
        Button button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(1.00f, 0.94f, 0.72f, 0.98f);
        colors.pressedColor = new Color(0.86f, 0.74f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateRect("TraitMark", rt, new Vector2(-size.x * 0.5f + 24f, 0), new Vector2(22, 22), selected ? highlightColor : new Color(0.82f, 0.72f, 0.50f, 0.92f));
        AddText(rt, selected ? "✓" : "", new Vector2(-size.x * 0.5f + 24f, 0), new Vector2(22, 22), 16, TextAnchor.MiddleCenter, selected ? Color.black : muted);

        Text title = AddText(rt, trait != null ? trait.name : "", new Vector2(28, 10), new Vector2(size.x - 76f, 20), 16, TextAnchor.MiddleLeft, selected ? highlightColor : ink);
        title.verticalOverflow = VerticalWrapMode.Truncate;
        Text desc = AddText(rt, trait != null ? trait.description : "", new Vector2(28, -11), new Vector2(size.x - 76f, 18), 12, TextAnchor.MiddleLeft, muted);
        desc.verticalOverflow = VerticalWrapMode.Truncate;
        desc.lineSpacing = 0.9f;

        button.onClick.AddListener(() => ActivateOnce("trait:" + key, action));
        return button;
    }

    private RectTransform CreateStoryDialogFrame(string title, string portraitName, string speakerName, out Vector2 bodyPos, out Vector2 bodySize, out Vector2 optionsCenter, out Vector2 optionsSize)
    {
        bool hasPortrait = !string.IsNullOrEmpty(portraitName);
        CreateRect("DialogBottomShade", root, new Vector2(0, -124), new Vector2(1400, 560), new Color(0.06f, 0.04f, 0.025f, 0.36f));
        RectTransform frame = CreateSpriteRect("DialogFrame", root, new Vector2(0, -72), new Vector2(1180, 560), "Art/UI/panel_clean_paper", panel, true, false, new Vector4(28, 28, 28, 28));
        Image frameImage = frame.GetComponent<Image>();
        frameImage.color = Color.white;

        CreateRect("DialogInnerShade", frame, Vector2.zero, new Vector2(1142, 520), new Color(1.00f, 0.94f, 0.78f, 0.34f));

        float contentX = hasPortrait ? 132f : 0f;
        float contentWidth = hasPortrait ? 800f : 1030f;
        if (hasPortrait)
        {
            CreateRect("DialogPortraitWell", frame, new Vector2(-462, -8), new Vector2(274, 492), new Color(0.96f, 0.88f, 0.68f, 0.46f));
            AddPortrait(frame, portraitName, new Vector2(-462, -14), new Vector2(230, 438), true);
            string label = !string.IsNullOrEmpty(speakerName) ? speakerName : portraitName;
            CreateRect("DialogNameplate", frame, new Vector2(-462, -240), new Vector2(236, 30), new Color(0.36f, 0.53f, 0.43f, 0.92f));
            AddText(frame, label, new Vector2(-462, -240), new Vector2(220, 26), 17, TextAnchor.MiddleCenter, new Color(1.00f, 0.96f, 0.84f));
        }

        Text titleText = AddText(frame, title, new Vector2(contentX, 244), new Vector2(contentWidth - 54f, 42), 16, TextAnchor.MiddleLeft, highlightColor);
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 12;
        titleText.resizeTextMaxSize = 16;
        titleText.lineSpacing = 0.9f;
        AddButton(frame, T("button.close_short", "X"), new Vector2(548, 248), new Vector2(34, 28), CloseStoryDialog, new Color(0.36f, 0.16f, 0.13f));
        CreateRect("DialogTextWell", frame, new Vector2(contentX, 116), new Vector2(contentWidth, 174), new Color(1.00f, 0.95f, 0.82f, 0.56f));
        CreateRect("DialogChoiceWell", frame, new Vector2(contentX, -120), new Vector2(contentWidth, 270), new Color(0.94f, 0.84f, 0.62f, 0.32f));

        bodyPos = new Vector2(contentX, 116);
        bodySize = new Vector2(contentWidth - 38, 144);
        optionsCenter = new Vector2(contentX, -122);
        optionsSize = new Vector2(contentWidth - 38, 244);
        return frame;
    }

    private Button AddDialogChoiceButton(Transform parent, string label, int index, Vector2 pos, Vector2 size, Action action)
    {
        string cleanLabel = CleanDialogChoiceText(label);
        string display = (index + 1).ToString() + ".  " + cleanLabel;
        RectTransform rt = CreateRect("DialogChoice_" + index, parent, pos, size, new Color(0.96f, 0.89f, 0.72f, 0.95f));
        Image image = rt.GetComponent<Image>();
        image.sprite = LoadArtSprite("Art/UI/button_clean_neutral", new Vector4(30, 30, 30, 30));
        image.type = Image.Type.Sliced;
        Button button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.00f, 0.96f, 0.78f, 1f);
        colors.pressedColor = new Color(0.82f, 0.78f, 0.64f, 1f);
        colors.selectedColor = new Color(0.96f, 0.86f, 0.55f, 1f);
        button.colors = colors;
        int fontSize = DialogChoiceFontSize(cleanLabel, size);
        Text text = CreateText("ChoiceLabel", rt, display, fontSize, ink, TextAnchor.MiddleLeft);
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 11;
        text.resizeTextMaxSize = fontSize;
        text.lineSpacing = 0.96f;
        RectTransform trt = text.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(18, 5);
        trt.offsetMax = new Vector2(-14, -5);
        image.raycastTarget = true;
        button.onClick.AddListener(() => ActivateOnce(display, action));
        return button;
    }

    private string CleanDialogChoiceText(string label)
    {
        return (label ?? "").Replace("\r", " ").Replace("\n", "  ").Trim();
    }

    private float DialogChoiceTextUnits(string label)
    {
        float units = 0f;
        string text = CleanDialogChoiceText(label);
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (char.IsWhiteSpace(ch)) units += 0.35f;
            else if (ch < 128) units += 0.55f;
            else units += 1f;
        }
        return units;
    }

    private int DialogChoiceFontSize(string cleanLabel, Vector2 size)
    {
        float units = DialogChoiceTextUnits(cleanLabel);
        int fontSize = size.y < 38f ? 14 : 16;
        if (units > 96f) fontSize = 12;
        else if (units > 68f) fontSize = 13;
        else if (units > 44f) fontSize = 14;
        return fontSize;
    }

    private float EstimateDialogChoiceHeight(string label, float width)
    {
        string cleanLabel = CleanDialogChoiceText(label);
        float units = DialogChoiceTextUnits(cleanLabel) + 4f;
        int fontSize = DialogChoiceFontSize(cleanLabel, new Vector2(width, 48f));
        float usableWidth = Mathf.Max(80f, width - 42f);
        float unitsPerLine = Mathf.Max(8f, usableWidth / (fontSize * 0.98f));
        int lines = Mathf.Max(1, Mathf.CeilToInt(units / unitsPerLine));
        return Mathf.Clamp(20f + lines * fontSize * 1.16f, 36f, 118f);
    }

    private float MeasureDialogOptionsHeight(IList<Tuple<string, Action>> options, int columns, float columnWidth, float gap, out float[] rowHeights)
    {
        int rows = Mathf.CeilToInt(options.Count / (float)columns);
        rowHeights = new float[Mathf.Max(0, rows)];
        for (int i = 0; i < options.Count; i++)
        {
            int row = i / columns;
            rowHeights[row] = Mathf.Max(rowHeights[row], EstimateDialogChoiceHeight(options[i].Item1, columnWidth));
        }

        float total = 0f;
        for (int i = 0; i < rowHeights.Length; i++)
        {
            if (i > 0) total += gap;
            total += rowHeights[i];
        }
        return total;
    }

    private void AddDialogOptions(RectTransform frame, IList<Tuple<string, Action>> options, Vector2 center, Vector2 size)
    {
        AddText(frame, T("story.choice_label", "选择："), new Vector2(center.x, center.y + size.y * 0.5f - 12f), new Vector2(size.x, 24), 15, TextAnchor.MiddleLeft, muted);
        if (options == null || options.Count == 0) return;
        float header = 30f;
        float gap = 6f;
        float availableHeight = Mathf.Max(40f, size.y - header);
        bool hasLongOption = options.Any(option => DialogChoiceTextUnits(option.Item1) > 36f);
        int columns = options.Count > 5 && !hasLongOption ? 2 : 1;
        float columnWidth = columns > 1 ? (size.x - 14f) * 0.5f : size.x;
        float[] rowHeights;
        float totalHeight = MeasureDialogOptionsHeight(options, columns, columnWidth, gap, out rowHeights);
        if (totalHeight > availableHeight && options.Count > 4)
        {
            float twoColumnWidth = (size.x - 14f) * 0.5f;
            float[] twoColumnRows;
            float twoColumnHeight = MeasureDialogOptionsHeight(options, 2, twoColumnWidth, gap, out twoColumnRows);
            if (twoColumnHeight < totalHeight)
            {
                columns = 2;
                columnWidth = twoColumnWidth;
                rowHeights = twoColumnRows;
                totalHeight = twoColumnHeight;
            }
        }

        if (totalHeight > availableHeight)
        {
            RectTransform viewport = CreateRect("DialogChoiceViewport", frame, new Vector2(center.x, center.y - header * 0.5f), new Vector2(size.x, availableHeight), new Color(0f, 0f, 0f, 0.01f));
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            RectTransform content = CreateEmptyRect("DialogChoiceContent", viewport, Vector2.zero, new Vector2(size.x, totalHeight));
            content.anchoredPosition = new Vector2(0f, (totalHeight - availableHeight) * 0.5f);
            scroll.viewport = viewport;
            scroll.content = content;

            float contentY = totalHeight * 0.5f;
            for (int i = 0; i < options.Count; i++)
            {
                Tuple<string, Action> option = options[i];
                int row = i / columns;
                int column = i % columns;
                float x = columns == 1 ? 0f : -columnWidth * 0.5f - 7f + column * (columnWidth + 14f);
                if (column == 0 && row > 0) contentY -= gap;
                AddDialogChoiceButton(content, option.Item1, i, new Vector2(x, contentY - rowHeights[row] * 0.5f), new Vector2(columnWidth, rowHeights[row]), option.Item2);
                if (column == columns - 1 || i == options.Count - 1) contentY -= rowHeights[row];
            }
            return;
        }

        float y = center.y + size.y * 0.5f - header;
        for (int i = 0; i < options.Count; i++)
        {
            Tuple<string, Action> option = options[i];
            int row = i / columns;
            int column = i % columns;
            float x = columns == 1 ? center.x : center.x - columnWidth * 0.5f - 7f + column * (columnWidth + 14f);
            if (column == 0 && row > 0) y -= gap;
            AddDialogChoiceButton(frame, option.Item1, i, new Vector2(x, y - rowHeights[row] * 0.5f), new Vector2(columnWidth, rowHeights[row]), option.Item2);
            if (column == columns - 1 || i == options.Count - 1) y -= rowHeights[row];
        }
    }

    private Text AddDialogBodyText(Transform parent, string value, Vector2 pos, Vector2 size, int baseFontSize)
    {
        string text = value ?? "";
        int fontSize = baseFontSize;
        if (text.Length > 430) fontSize = Mathf.Min(fontSize, 15);
        else if (text.Length > 300) fontSize = Mathf.Min(fontSize, 16);
        else if (text.Length > 210) fontSize = Mathf.Min(fontSize, 17);
        Text body = AddText(parent, text, pos, size, fontSize, TextAnchor.UpperLeft, ink);
        body.verticalOverflow = VerticalWrapMode.Truncate;
        body.lineSpacing = 0.92f;
        return body;
    }

    private void AddDialogAdvanceClick(RectTransform frame, Action action, string hint, Vector2 center, Vector2 size)
    {
        AddText(frame, hint, center, new Vector2(size.x, 30), 16, TextAnchor.MiddleCenter, muted);
        EventTrigger trigger = frame.gameObject.GetComponent<EventTrigger>() ?? frame.gameObject.AddComponent<EventTrigger>();
        AddEventTrigger(trigger, EventTriggerType.PointerClick, _ => ActivateOnce("dialog-advance:" + activeStoryEventId + ":" + activeStoryPageIndex, action));
    }

    private void ActivateOnce(string key, Action action)
    {
        if (Time.unscaledTime - lastPointerTime < 0.08f && lastPointerKey == key) return;
        lastPointerTime = Time.unscaledTime;
        lastPointerKey = key;
        action();
    }

    private void AddTopBar(Transform parent, string title)
    {
        RectTransform bar = CreateSpriteRect("TopBar", parent, new Vector2(0, 322), new Vector2(1180, 52), "Art/UI/topbar_clean_paper", panel, true, false, new Vector4(28, 28, 28, 28));
        AddText(bar, title, new Vector2(-430, -1), new Vector2(300, 36), 22, TextAnchor.MiddleLeft, ink);
        AddTopStat(bar, T("hud.week", "周"), CalendarLabel(), 0);
        AddTopStat(bar, T("hud.mood", "心情"), player.mood.ToString(), 1);
        AddTopStat(bar, T("hud.stamina", "体力"), player.stamina.ToString(), 2);
        AddTopStat(bar, T("hud.merit", "战功"), player.merit.ToString(), 3);
        AddTopStat(bar, T("hud.treasury", "国库"), player.treasury.ToString(), 4);
        AddButton(bar, T("button.settings_short", "设置"), new Vector2(516, 0), new Vector2(76, 30), () => ShowSettingsPanel(mode == ScreenMode.Battle ? ScreenMode.Battle : mode == ScreenMode.BattleLab ? ScreenMode.BattleLab : mode == ScreenMode.Strategy ? ScreenMode.Strategy : ScreenMode.Academy));
        #if false
        AddText(bar, T("top.hint", "鼠标/触摸点击优先，键盘仅作备用"), new Vector2(295, 0), new Vector2(520, 36), 15, TextAnchor.MiddleRight, muted);
        #endif
    }

    private void AddTopStat(Transform parent, string label, string value, int index)
    {
        float x = -150f + index * 122f;
        RectTransform chip = CreateRect("TopStat_" + label, parent, new Vector2(x, -1), new Vector2(108, 30), new Color(0.97f, 0.89f, 0.70f, 0.82f));
        AddText(chip, label, new Vector2(-30, 0), new Vector2(40, 20), 12, TextAnchor.MiddleLeft, muted);
        AddText(chip, value, new Vector2(27, 0), new Vector2(52, 20), 14, TextAnchor.MiddleRight, highlightColor);
    }

    private RectTransform CreateUiPanel(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        RectTransform panelRt = CreateSpriteRect(name, parent, pos, size, "Art/UI/panel_clean_paper", panel, true, false, new Vector4(28, 28, 28, 28));
        Image image = panelRt.GetComponent<Image>();
        image.color = Color.white;
        return panelRt;
    }

    private void AddSectionTitle(Transform parent, string text, Vector2 pos, Vector2 size)
    {
        AddText(parent, text, pos, size, 20, TextAnchor.MiddleLeft, highlightColor);
    }

    private void ShowTitle()
    {
        mode = ScreenMode.Title;
        pendingStoryReturnAction = null;
        battleTerrainOverride = null;
        RemoveBattleLabTempArmies();
        Clear();
        DrawSceneBackground("title");
        AddPortrait(root, player.name, new Vector2(365, -35), new Vector2(245, 365), true);
        AddText(root, T("game.title", "明路"), new Vector2(-260, 205), new Vector2(520, 100), 72, TextAnchor.MiddleCenter);
        AddText(root, T("title.subtitle", "1760，新京军事学院。你的道路，将通向王朝、共和、边疆，或未曾设想的远方。"), new Vector2(-260, 130), new Vector2(720, 60), 21, TextAnchor.MiddleCenter, muted);
        AddButton(root, T("button.new_game", "新的传奇"), new Vector2(-260, 40), new Vector2(280, 56), () =>
        {
            ResetGame();
            ShowCharacterCreate();
        });
        AddButton(root, T("button.strategy", "进入战略地图"), new Vector2(-260, -25), new Vector2(280, 56), () => ShowStrategy());
        AddButton(root, T("button.continue", "继续征途"), new Vector2(-260, -90), new Vector2(280, 56), LoadGame);
        AddButton(root, T("button.credits", "开发团队"), new Vector2(-260, -155), new Vector2(280, 56), ShowCredits);
        AddButton(root, T("button.exit", "退出"), new Vector2(-260, -220), new Vector2(280, 56), () => Application.Quit(), new Color(0.45f, 0.18f, 0.18f));
    }

    private void ShowCredits()
    {
        mode = ScreenMode.Credits;
        Clear();
        DrawSceneBackground("academy");
        AddTopBar(root, T("button.credits", "开发团队"));
        AddText(root, T("credits.body", "《明路》Unity 原型\n设计来源：明路第一版文档\n实现：运行时 UI / 学院养成 / 战略地图 / 六边形战棋\n素材：项目内可配置美术资源。"), new Vector2(0, 60), new Vector2(900, 300), 26, TextAnchor.MiddleCenter);
        AddButton(root, T("button.back_title", "返回标题"), new Vector2(0, -240), new Vector2(260, 56), ShowTitle);
    }

    private void ShowCharacterCreate()
    {
        mode = ScreenMode.CharacterCreate;
        Clear();
        DrawSceneBackground("academy");
        AddTopBar(root, TF("character_create.step_title", "建立角色  {0}/6", creationStep + 1));
        DrawCharacterCreateDashboard();
    }

    private void DrawCharacterCreateDashboard()
    {
        string displayName = string.IsNullOrWhiteSpace(creationNameDraft) ? DefaultPlayerName() : creationNameDraft.Trim();

        RectTransform profile = CreateUiPanel("CreateProfilePanel", root, new Vector2(-465, 0), new Vector2(285, 590));
        AddSectionTitle(profile, T("character_create.section_profile", "档案"), new Vector2(-102, 260), new Vector2(220, 30));
        AddPortrait(profile, displayName, new Vector2(0, 132), new Vector2(154, 222), true);
        AddText(profile, CreationProfileSummary(displayName), new Vector2(0, -84), new Vector2(240, 190), 14, TextAnchor.UpperLeft, muted);

        RectTransform center = CreateUiPanel("CreateMainPanel", root, new Vector2(-25, 0), new Vector2(570, 590));
        AddSectionTitle(center, CreationStepName(creationStep), new Vector2(-235, 260), new Vector2(480, 30));
        AddCreationStepDots(center);
        if (creationStep == 0) DrawCreationProfileStep(center);
        else if (creationStep == 1) DrawCreationOriginStep(center);
        else if (creationStep == 2) DrawCreationMemoryStep(center);
        else if (creationStep == 3) DrawCreationTalentStep(center);
        else if (creationStep == 4) DrawCreationSubjectStep(center);
        else DrawCreationConfirmStep(center);

        RectTransform summary = CreateUiPanel("CreateSummaryPanel", root, new Vector2(420, 0), new Vector2(350, 590));
        AddSectionTitle(summary, T("character_create.section_summary", "总览"), new Vector2(-136, 260), new Vector2(278, 30));
        Text summaryText = AddText(summary, CreationSummaryText(), new Vector2(0, 120), new Vector2(292, 250), 14, TextAnchor.UpperLeft);
        summaryText.verticalOverflow = VerticalWrapMode.Truncate;
        summaryText.lineSpacing = 0.94f;
        AddButton(summary, T("button.details", "查看详情"), new Vector2(0, -36), new Vector2(244, 34), ShowCreationDetailPopup);
        AddText(summary, characterCreateMessage, new Vector2(0, -116), new Vector2(292, 72), 14, TextAnchor.UpperLeft, muted);

        AddButton(summary, T("button.random_all", "全部随机"), new Vector2(0, -178), new Vector2(244, 36), RandomizeCreationAll);
        if (creationStep <= 0) AddButton(summary, T("button.back_title", "返回标题"), new Vector2(-66, -232), new Vector2(118, 38), ShowTitle);
        else AddButton(summary, T("button.prev_step", "上一步"), new Vector2(-66, -232), new Vector2(118, 38), () => { creationStep = Mathf.Max(0, creationStep - 1); ShowCharacterCreate(); });
        string nextLabel = creationStep >= 5 ? T("button.confirm_school", "确认入学") : T("button.next_step", "下一步");
        AddButton(summary, nextLabel, new Vector2(72, -232), new Vector2(126, 38), () =>
        {
            if (creationStep >= 5) ConfirmCharacterCreate();
            else AdvanceCreationStep();
        }, new Color(0.28f, 0.37f, 0.26f));
    }

    private string CreationProfileSummary(string displayName)
    {
        return TF("character_create.profile_card",
            "{0}\n字号：{1}\n新京军事学院预备生\n\n立绘仅作角色预览。\n姓名、出身、往事、天赋和学科会写入存档。",
            displayName,
            string.IsNullOrWhiteSpace(creationCourtesyDraft) ? T("common.none", "无") : creationCourtesyDraft.Trim());
    }

    private string CreationStepName(int step)
    {
        switch (step)
        {
            case 0: return T("character_create.step_profile", "命名登记");
            case 1: return T("character_create.step_origin", "家世出身");
            case 2: return T("character_create.step_memory", "少年往事");
            case 3: return T("character_create.step_talent", "天赋觉醒");
            case 4: return T("character_create.step_subject", "学科倾向");
            default: return T("character_create.step_confirm", "总览确认");
        }
    }

    private void AddCreationStepDots(Transform parent)
    {
        for (int i = 0; i < 6; i++)
        {
            Color color = i == creationStep ? highlightColor : i < creationStep ? new Color(0.30f, 0.38f, 0.28f) : new Color(0.09f, 0.08f, 0.065f);
            CreateRect("CreateStepDot_" + i, parent, new Vector2(166 + i * 24, 260), new Vector2(14, 14), color);
        }
    }

    private void DrawCreationProfileStep(RectTransform panel)
    {
        AddText(panel, T("character_create.profile_hint", "姓名可自由填写；不填则使用默认名。字号为可选项，会出现在正式场合。"), new Vector2(0, 222), new Vector2(496, 42), 14, TextAnchor.UpperLeft, muted);
        AddText(panel, T("character_create.name_label", "姓名"), new Vector2(-210, 144), new Vector2(80, 28), 16, TextAnchor.MiddleRight, muted);
        AddInputField(panel, creationNameDraft, TF("character_create.name_placeholder", "默认：{0}", DefaultPlayerName()), new Vector2(-54, 144), new Vector2(260, 38), value => creationNameDraft = value);
        AddButton(panel, T("button.random_name", "随机姓名"), new Vector2(182, 144), new Vector2(112, 34), RandomizeCreationName);
        AddText(panel, T("character_create.courtesy_label", "字号"), new Vector2(-210, 84), new Vector2(80, 28), 16, TextAnchor.MiddleRight, muted);
        AddInputField(panel, creationCourtesyDraft, T("character_create.courtesy_placeholder", "可不填"), new Vector2(-54, 84), new Vector2(260, 38), value => creationCourtesyDraft = value);
        AddText(panel, T("character_create.profile_plain_note", "外貌自定义暂不开放。当前立绘只代表主角默认形象，避免出现选了外貌但画面不变化的假功能。"), new Vector2(0, -36), new Vector2(496, 132), 16, TextAnchor.UpperLeft, ink);
    }

    private void DrawCreationOriginStep(RectTransform panel)
    {
        AddText(panel, T("character_create.origin_hint", "选择一个家世。出身会影响初始学科、派系关系、立场和可选天赋池。"), new Vector2(0, 220), new Vector2(496, 40), 14, TextAnchor.UpperLeft, muted);
        List<CharacterOrigin> origins = CharacterOriginCatalog();
        for (int i = 0; i < origins.Count; i++)
        {
            CharacterOrigin origin = origins[i];
            int row = i / 2;
            int col = i % 2;
            AddCreationOptionCard(panel, origin.name, origin.subtitle, origin.id == creationOriginId, new Vector2(-132 + col * 264, 150 - row * 82), new Vector2(244, 66), () =>
            {
                creationOriginId = origin.id;
                TrimUnavailableCreationTalents();
                characterCreateMessage = TF("character_create.origin_selected", "已选择出身：{0}。", origin.name);
                ShowCharacterCreate();
            });
        }

        CharacterOrigin selected = CurrentCreationOrigin();
        string body = selected == null ? T("character_create.origin_none", "尚未选择出身。") : selected.description;
        Text text = AddText(panel, body, new Vector2(0, -122), new Vector2(498, 86), 14, TextAnchor.UpperLeft, ink);
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.92f;
        AddButton(panel, T("button.origin_detail", "查看出身影响"), new Vector2(0, -204), new Vector2(220, 36), ShowCreationDetailPopup);
    }

    private void DrawCreationMemoryStep(RectTransform panel)
    {
        AddText(panel, T("character_create.memory_hint", "三段往事会形成你的性格特质，并轻微影响初始立场。"), new Vector2(0, 222), new Vector2(496, 36), 14, TextAnchor.UpperLeft, muted);
        List<CreationMemory> memories = CreationMemoryCatalog();
        for (int i = 0; i < Mathf.Min(3, memories.Count); i++)
        {
            CreationMemory memory = memories[i];
            float y = 142 - i * 132;
            AddText(panel, memory.title, new Vector2(-206, y + 46), new Vector2(120, 24), 15, TextAnchor.MiddleLeft, highlightColor);
            Text body = AddText(panel, memory.body, new Vector2(54, y + 42), new Vector2(394, 38), 12, TextAnchor.UpperLeft, muted);
            body.verticalOverflow = VerticalWrapMode.Truncate;
            string selected = CreationMemoryChoice(memory.id);
            AddCreationOptionCard(panel, memory.optionAText, "", selected == memory.optionAId, new Vector2(-132, y - 18), new Vector2(244, 58), () => SetCreationMemoryChoice(memory, true));
            AddCreationOptionCard(panel, memory.optionBText, "", selected == memory.optionBId, new Vector2(132, y - 18), new Vector2(244, 58), () => SetCreationMemoryChoice(memory, false));
        }
    }

    private void DrawCreationTalentStep(RectTransform panel)
    {
        AddText(panel, T("character_create.talent_hint", "从当前出身的天赋池中选择 2 个初始天赋。天赋会在战斗、社交和养成中长期生效。"), new Vector2(0, 222), new Vector2(496, 40), 14, TextAnchor.UpperLeft, muted);
        List<CreationTalent> talents = CreationTalentCandidates();
        int visible = Mathf.Min(10, talents.Count);
        for (int i = 0; i < visible; i++)
        {
            CreationTalent talent = talents[i];
            int row = i / 2;
            int col = i % 2;
            string subtitle = SafeText(talent.category, T("character_create.talent_default_category", "天赋")) + "  T" + Mathf.Max(1, talent.tier);
            AddCreationOptionCard(panel, talent.name, subtitle + "\n" + talent.description, creationTalentIds.Contains(talent.id), new Vector2(-132 + col * 264, 154 - row * 86), new Vector2(244, 72), () => ToggleCreationTalent(talent.id));
        }
    }

    private void DrawCreationSubjectStep(RectTransform panel)
    {
        AddText(panel, T("character_create.subject_hint", "选择 2 个学科倾向。每个倾向会获得一段初始进度，方便你在入学后更快升级。"), new Vector2(0, 222), new Vector2(496, 40), 14, TextAnchor.UpperLeft, muted);
        List<CreationSubject> subjects = CreationSubjectCatalog();
        for (int i = 0; i < subjects.Count; i++)
        {
            CreationSubject subject = subjects[i];
            int row = i / 2;
            int col = i % 2;
            AddCreationOptionCard(panel, subject.name, subject.description, creationSubjectIds.Contains(subject.id), new Vector2(-132 + col * 264, 150 - row * 86), new Vector2(244, 72), () => ToggleCreationSubject(subject.id));
        }
    }

    private void DrawCreationConfirmStep(RectTransform panel)
    {
        Text text = AddText(panel, CreationSummaryText(), new Vector2(0, 92), new Vector2(500, 260), 15, TextAnchor.UpperLeft);
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.94f;
        AddButton(panel, T("button.details", "查看完整明细"), new Vector2(0, -84), new Vector2(220, 36), ShowCreationDetailPopup);
        AddText(panel, T("character_create.confirm_hint", "确认后进入序章。姓名、出身、往事与初始天赋会写入存档。"), new Vector2(0, -150), new Vector2(498, 42), 14, TextAnchor.UpperLeft, muted);
        AddButton(panel, T("character_create.edit_profile", "改姓名"), new Vector2(-198, -220), new Vector2(100, 34), () => JumpCreationStep(0));
        AddButton(panel, T("character_create.edit_origin", "改出身"), new Vector2(-88, -220), new Vector2(100, 34), () => JumpCreationStep(1));
        AddButton(panel, T("character_create.edit_memory", "改往事"), new Vector2(22, -220), new Vector2(100, 34), () => JumpCreationStep(2));
        AddButton(panel, T("character_create.edit_talent", "改天赋"), new Vector2(132, -220), new Vector2(100, 34), () => JumpCreationStep(3));
        AddButton(panel, T("character_create.edit_subject", "改学科"), new Vector2(242, -220), new Vector2(100, 34), () => JumpCreationStep(4));
    }

    private Button AddCreationOptionCard(Transform parent, string title, string body, bool selected, Vector2 pos, Vector2 size, Action action)
    {
        RectTransform rt = CreateRect("CreationOption_" + SafeText(title, "Option"), parent, pos, size, selected ? new Color(0.95f, 0.84f, 0.57f, 0.97f) : new Color(0.96f, 0.89f, 0.72f, 0.95f));
        Image image = rt.GetComponent<Image>();
        image.sprite = LoadArtSprite("Art/UI/panel_clean_paper", new Vector4(28, 28, 28, 28));
        image.type = Image.Type.Sliced;
        Button button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(1.00f, 0.94f, 0.72f, 0.98f);
        colors.pressedColor = new Color(0.86f, 0.74f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        CreateRect("CreationOptionMark", rt, new Vector2(-size.x * 0.5f + 18f, size.y * 0.5f - 18f), new Vector2(18, 18), selected ? highlightColor : new Color(0.82f, 0.72f, 0.50f, 0.92f));
        AddText(rt, selected ? "✓" : "", new Vector2(-size.x * 0.5f + 18f, size.y * 0.5f - 18f), new Vector2(18, 18), 13, TextAnchor.MiddleCenter, selected ? Color.black : muted);
        Text nameText = AddText(rt, title, new Vector2(14, size.y * 0.5f - 20f), new Vector2(size.x - 54f, 22), title.Length > 14 ? 13 : 15, TextAnchor.MiddleLeft, selected ? highlightColor : ink);
        nameText.verticalOverflow = VerticalWrapMode.Overflow;
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 10;
        nameText.resizeTextMaxSize = title.Length > 14 ? 13 : 15;
        if (!string.IsNullOrWhiteSpace(body))
        {
            Text bodyText = AddText(rt, body, new Vector2(14, -8), new Vector2(size.x - 38f, size.y - 34f), body.Length > 46 ? 11 : 12, TextAnchor.UpperLeft, muted);
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 9;
            bodyText.resizeTextMaxSize = body.Length > 46 ? 11 : 12;
            bodyText.lineSpacing = 0.9f;
        }
        button.onClick.AddListener(() => ActivateOnce("creation:" + title, action));
        return button;
    }

    private void SetCreationMemoryChoice(CreationMemory memory, bool optionA)
    {
        if (memory == null) return;
        string selected = optionA ? memory.optionAId : memory.optionBId;
        creationMemoryChoiceIds.RemoveAll(id => id.StartsWith(memory.id + ":", StringComparison.Ordinal));
        creationMemoryChoiceIds.Add(memory.id + ":" + selected);
        RefreshCreationPersonality();
        characterCreateMessage = TF("character_create.memory_selected", "{0}已记录。", memory.title);
        ShowCharacterCreate();
    }

    private string CreationMemoryChoice(string memoryId)
    {
        string prefix = memoryId + ":";
        string raw = creationMemoryChoiceIds.FirstOrDefault(id => id.StartsWith(prefix, StringComparison.Ordinal));
        return string.IsNullOrEmpty(raw) ? "" : raw.Substring(prefix.Length);
    }

    private void ToggleCreationTalent(string id)
    {
        if (creationTalentIds.Contains(id))
        {
            creationTalentIds.Remove(id);
            characterCreateMessage = T("character_create.talent_removed", "已移除天赋。");
        }
        else if (creationTalentIds.Count >= 2)
        {
            characterCreateMessage = T("character_create.talent_max", "初始天赋最多选择 2 个。");
        }
        else
        {
            creationTalentIds.Add(id);
            characterCreateMessage = TF("character_create.talent_count", "已选择 {0}/2 个天赋。", creationTalentIds.Count);
        }
        ShowCharacterCreate();
    }

    private void ToggleCreationSubject(string id)
    {
        if (creationSubjectIds.Contains(id))
        {
            creationSubjectIds.Remove(id);
            characterCreateMessage = T("character_create.subject_removed", "已移除学科倾向。");
        }
        else if (creationSubjectIds.Count >= 2)
        {
            characterCreateMessage = T("character_create.subject_max", "学科倾向最多选择 2 个。");
        }
        else
        {
            creationSubjectIds.Add(id);
            characterCreateMessage = TF("character_create.subject_count", "已选择 {0}/2 个学科倾向。", creationSubjectIds.Count);
        }
        ShowCharacterCreate();
    }

    private void AdvanceCreationStep()
    {
        if (!CreationStepReady(creationStep))
        {
            ShowCharacterCreate();
            return;
        }
        creationStep = Mathf.Clamp(creationStep + 1, 0, 5);
        characterCreateMessage = T("character_create.message_continue", "继续完成下一项登记。");
        ShowCharacterCreate();
    }

    private bool CreationStepReady(int step)
    {
        if (step == 1 && CurrentCreationOrigin() == null)
        {
            characterCreateMessage = T("character_create.need_origin", "请选择一个家世出身。");
            return false;
        }
        if (step == 2 && creationMemoryChoiceIds.Count < Mathf.Min(3, CreationMemoryCatalog().Count))
        {
            characterCreateMessage = T("character_create.need_memory", "请完成三段少年往事。");
            return false;
        }
        if (step == 3 && creationTalentIds.Count != 2)
        {
            characterCreateMessage = T("character_create.need_talent", "请选择 2 个初始天赋。");
            return false;
        }
        if (step == 4 && creationSubjectIds.Count != 2)
        {
            characterCreateMessage = T("character_create.need_subject", "请选择 2 个学科倾向。");
            return false;
        }
        return true;
    }

    private void JumpCreationStep(int step)
    {
        creationStep = Mathf.Clamp(step, 0, 5);
        ShowCharacterCreate();
    }

    private void RandomizeCreationName()
    {
        string[] names = { "夏邑", "郑怀瑾", "沈砚舟", "陆承霁", "方允衡", "林照野", "赵知微", "陈观澜", "李景行", "苏远岚" };
        string[] courtesy = { "", "怀璧", "景明", "子衡", "望舒", "云旗", "慎初", "照临" };
        creationNameDraft = names[RandomRangeInt(0, names.Length)];
        creationCourtesyDraft = courtesy[RandomRangeInt(0, courtesy.Length)];
        characterCreateMessage = T("character_create.random_name_done", "姓名已随机生成。");
        ShowCharacterCreate();
    }

    private void RandomizeCreationAll()
    {
        RandomizeCreationNameOnly();
        creationOriginId = PickRandom(CharacterOriginCatalog().Select(o => o.id).ToArray());
        creationMemoryChoiceIds.Clear();
        foreach (CreationMemory memory in CreationMemoryCatalog().Take(3))
        {
            bool optionA = RandomRangeInt(0, 2) == 0;
            creationMemoryChoiceIds.Add(memory.id + ":" + (optionA ? memory.optionAId : memory.optionBId));
        }
        RefreshCreationPersonality();
        creationTalentIds.Clear();
        foreach (CreationTalent talent in CreationTalentCandidates().OrderBy(_ => UnityEngine.Random.value).Take(2)) creationTalentIds.Add(talent.id);
        creationSubjectIds.Clear();
        foreach (CreationSubject subject in CreationSubjectCatalog().OrderBy(_ => UnityEngine.Random.value).Take(2)) creationSubjectIds.Add(subject.id);
        characterCreateMessage = T("character_create.random_all_done", "已随机生成完整创建方案。");
        ShowCharacterCreate();
    }

    private void RandomizeCreationNameOnly()
    {
        string[] names = { "夏邑", "郑怀瑾", "沈砚舟", "陆承霁", "方允衡", "林照野", "赵知微", "陈观澜", "李景行", "苏远岚" };
        creationNameDraft = names[RandomRangeInt(0, names.Length)];
        creationCourtesyDraft = PickRandom(new[] { "", "怀璧", "景明", "子衡", "望舒", "云旗", "慎初", "照临" });
    }

    private string PickRandom(string[] values)
    {
        return values == null || values.Length == 0 ? "" : values[RandomRangeInt(0, values.Length)];
    }

    private void RefreshCreationPersonality()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (CreationMemory memory in CreationMemoryCatalog())
        {
            string selected = CreationMemoryChoice(memory.id);
            string trait = selected == memory.optionAId ? memory.optionATraitId : selected == memory.optionBId ? memory.optionBTraitId : "";
            if (string.IsNullOrEmpty(trait)) continue;
            counts[trait] = counts.TryGetValue(trait, out int count) ? count + 1 : 1;
        }

        creationTraitIds.Clear();
        string personality = counts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => pair.Key).FirstOrDefault();
        if (!string.IsNullOrEmpty(personality)) creationTraitIds.Add(personality);
    }

    private void TrimUnavailableCreationTalents()
    {
        HashSet<string> allowed = new HashSet<string>(CreationTalentCandidates().Select(t => t.id));
        creationTalentIds.RemoveAll(id => !allowed.Contains(id));
    }

    private void ShowCreationDetailPopup()
    {
        OpenSystemPopup(T("character_create.detail_title", "创建明细"), CreationDetailedSummary(), new List<Tuple<string, Action>>(), ScreenMode.CharacterCreate, "academy");
    }

    private CharacterOrigin CurrentCreationOrigin()
    {
        return CharacterOriginCatalog().FirstOrDefault(o => o.id == creationOriginId) ?? CharacterOriginCatalog().FirstOrDefault();
    }

    private List<CreationTalent> CreationTalentCandidates()
    {
        string origin = creationOriginId;
        return CreationTalentCatalog().Where(t => TalentAvailableForOrigin(t, origin)).ToList();
    }

    private bool TalentAvailableForOrigin(CreationTalent talent, string originId)
    {
        if (talent == null) return false;
        List<string> tags = SplitConfigList(talent.originTags);
        return tags.Count == 0 || string.IsNullOrEmpty(originId) || tags.Contains(originId);
    }

    private void ConfirmCharacterCreate()
    {
        for (int i = 0; i < 5; i++)
        {
            if (!CreationStepReady(i))
            {
                creationStep = i;
                ShowCharacterCreate();
                return;
            }
        }

        player.name = string.IsNullOrWhiteSpace(creationNameDraft) ? DefaultPlayerName() : creationNameDraft.Trim();
        player.courtesyName = string.IsNullOrWhiteSpace(creationCourtesyDraft) ? "" : creationCourtesyDraft.Trim();
        player.originId = SafeText(creationOriginId, CurrentCreationOrigin()?.id ?? "");
        player.personalityId = creationTraitIds.FirstOrDefault() ?? "";
        player.creationMemoryChoices = creationMemoryChoiceIds.ToList();
        player.subjectFocusIds = creationSubjectIds.ToList();
        player.traits = creationTraitIds.Concat(creationTalentIds).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

        ApplyCharacterCreationEffects();
        AddLog(TF("log.character_create", "入学登记：{0}，出身{1}，特质{2}，天赋{3}。", player.name, CurrentCreationOrigin()?.name ?? T("common.unknown", "未知"), TraitNames(creationTraitIds), TraitNames(creationTalentIds)));
        StartStory("EV001", ScreenMode.Academy);
    }

    private void ApplyCharacterCreationEffects()
    {
        CharacterOrigin origin = CurrentCreationOrigin();
        if (origin != null)
        {
            ApplyCreationExp(origin.infantryExp, origin.cavalryExp, origin.artilleryExp, origin.managementExp, origin.logisticsExp, origin.trainingExp);
            AdjustIdeologyAxis("nation", origin.nationAxis);
            AdjustIdeologyAxis("class", origin.classAxis);
            AdjustIdeologyAxis("governance", origin.governanceAxis);
            AdjustIdeologyAxis("region", origin.regionAxis);
            ApplyCreationStance("home", origin.stanceHome);
            ApplyCreationStance("army", origin.stanceArmy);
            ApplyCreationStance("native", origin.stanceNative);
            ApplyCreationStance("liberal", origin.stanceLiberal);
            ApplyCreationStance("legal", origin.stanceLegal);
            ApplyCreationRelationship("zhao", origin.relZhao);
            ApplyCreationRelationship("lin", origin.relLin);
            ApplyCreationRelationship("yierde", origin.relYierde);
            ApplyCreationRelationship("chen", origin.relChen);
            ApplyCreationRelationship("su", origin.relSu);
            ApplyCreationRelationship("li", origin.relLi);
            SetStoryValue("origin_" + origin.id, 1);
            if (!string.IsNullOrEmpty(origin.clueName))
            {
                AddStoryValue("线索:东渡密档", 1);
                SetStoryValue("线索:" + origin.clueName, 1);
            }
        }

        foreach (CreationMemory memory in CreationMemoryCatalog())
        {
            string selected = CreationMemoryChoice(memory.id);
            if (selected == memory.optionAId)
            {
                AdjustIdeologyAxis("nation", memory.optionANation);
                AdjustIdeologyAxis("class", memory.optionAClass);
                AdjustIdeologyAxis("governance", memory.optionAGovernance);
                AdjustIdeologyAxis("region", memory.optionARegion);
                SetStoryValue("memory_" + memory.optionAId, 1);
            }
            else if (selected == memory.optionBId)
            {
                AdjustIdeologyAxis("nation", memory.optionBNation);
                AdjustIdeologyAxis("class", memory.optionBClass);
                AdjustIdeologyAxis("governance", memory.optionBGovernance);
                AdjustIdeologyAxis("region", memory.optionBRegion);
                SetStoryValue("memory_" + memory.optionBId, 1);
            }
        }

        foreach (CreationSubject subject in CreationSubjectCatalog().Where(s => creationSubjectIds.Contains(s.id)))
        {
            ApplyExpReward(subject.target, Mathf.Max(0, subject.expGain));
            SetStoryValue("subject_" + subject.id, 1);
        }

        foreach (CreationTalent talent in CreationTalentCatalog().Where(t => creationTalentIds.Contains(t.id)))
        {
            if (talent.intelligenceBonus != 0) player.intelligence += talent.intelligenceBonus;
            SetStoryValue("talent_" + talent.id, 1);
        }

        foreach (string traitId in creationTraitIds)
        {
            SetStoryValue(traitId, 1);
        }

        RefreshProgressionSystems(false);
    }

    private void ApplyCreationExp(int infantry, int cavalry, int artillery, int management, int logistics, int training)
    {
        if (infantry != 0) player.infantryExp += infantry;
        if (cavalry != 0) player.cavalryExp += cavalry;
        if (artillery != 0) player.artilleryExp += artillery;
        if (management != 0) player.managementExp += management;
        if (logistics != 0) player.logisticsExp += logistics;
        if (training != 0) player.trainingExp += training;
    }

    private void ApplyCreationStance(string id, int delta)
    {
        if (string.IsNullOrEmpty(id) || delta == 0) return;
        StanceScore score = stances.FirstOrDefault(s => s.id == id || s.name == id);
        if (score == null)
        {
            score = new StanceScore { id = id, name = id, value = 20 };
            stances.Add(score);
        }
        score.value = Mathf.Clamp(score.value + delta, -100, 100);
        AddStoryValue("立场:" + score.name, delta);
    }

    private void ApplyCreationRelationship(string id, int delta)
    {
        if (string.IsNullOrEmpty(id) || delta == 0) return;
        Relationship rel = relationships.FirstOrDefault(r => r.id == id || r.name == id);
        if (rel == null) return;
        GainRelationship(rel, delta);
        AddStoryValue("好感:" + rel.name, delta);
    }

    private string CreationSummaryText()
    {
        CharacterOrigin origin = CurrentCreationOrigin();
        List<string> lines = new List<string>
        {
            TF("character_create.summary_name", "姓名：{0}", string.IsNullOrWhiteSpace(creationNameDraft) ? DefaultPlayerName() : creationNameDraft.Trim()),
            TF("character_create.summary_courtesy", "字号：{0}", string.IsNullOrWhiteSpace(creationCourtesyDraft) ? T("common.none", "无") : creationCourtesyDraft.Trim()),
            TF("character_create.summary_origin", "出身：{0}", origin != null ? origin.name : T("common.none_selected", "未选择")),
            TF("character_create.summary_personality", "性格：{0}", TraitNames(creationTraitIds)),
            TF("character_create.summary_talents", "天赋：{0}", TraitNames(creationTalentIds)),
            TF("character_create.summary_subjects", "学科：{0}", CreationSubjectNames())
        };
        return string.Join("\n", lines.ToArray());
    }

    private string CreationDetailedSummary()
    {
        CharacterOrigin origin = CurrentCreationOrigin();
        List<string> parts = new List<string>
        {
            CreationSummaryText(),
            "",
            TraitBonusSummary(creationTraitIds.Concat(creationTalentIds)),
            "",
            T("character_create.summary_effect_title", "开局影响"),
            origin != null ? CreationOriginEffectText(origin) : "",
            CreationSubjectEffectText(),
            CreationMemoryEffectText()
        };
        return string.Join("\n", parts.Where(p => !string.IsNullOrEmpty(p)).ToArray());
    }

    private string CreationOriginEffectText(CharacterOrigin origin)
    {
        if (origin == null) return "";
        List<string> lines = new List<string>();
        string exp = CreationExpLine(origin.infantryExp, origin.cavalryExp, origin.artilleryExp, origin.managementExp, origin.logisticsExp, origin.trainingExp);
        if (!string.IsNullOrEmpty(exp)) lines.Add(TF("character_create.effect_academy", "学科：{0}", exp));
        string stance = CreationStanceLine(origin.stanceHome, origin.stanceArmy, origin.stanceNative, origin.stanceLiberal, origin.stanceLegal);
        if (!string.IsNullOrEmpty(stance)) lines.Add(TF("character_create.effect_stance", "派系：{0}", stance));
        string axis = CreationAxisLine(origin.nationAxis, origin.classAxis, origin.governanceAxis, origin.regionAxis);
        if (!string.IsNullOrEmpty(axis)) lines.Add(TF("character_create.effect_axis", "立场轴：{0}", axis));
        if (!string.IsNullOrEmpty(origin.clueName)) lines.Add(TF("character_create.effect_clue", "密档线索：{0}", origin.clueName));
        return string.Join("\n", lines.ToArray());
    }

    private string CreationSubjectEffectText()
    {
        List<string> lines = CreationSubjectCatalog()
            .Where(s => creationSubjectIds.Contains(s.id))
            .Select(s => s.name + "+" + Mathf.Max(0, s.expGain))
            .ToList();
        return lines.Count == 0 ? "" : TF("character_create.effect_subjects", "学科倾向：{0}", string.Join(T("common.list_separator", "、"), lines.ToArray()));
    }

    private string CreationMemoryEffectText()
    {
        List<string> traits = creationTraitIds.Select(id => TraitCatalog().FirstOrDefault(t => t.id == id)?.name ?? id).ToList();
        return traits.Count == 0 ? "" : TF("character_create.effect_memory", "往事性格：{0}", string.Join(T("common.list_separator", "、"), traits.ToArray()));
    }

    private string CreationExpLine(int infantry, int cavalry, int artillery, int management, int logistics, int training)
    {
        List<string> lines = new List<string>();
        if (infantry != 0) lines.Add(TF("attribute_gain.infantry", "步兵+{0}", infantry));
        if (cavalry != 0) lines.Add(TF("attribute_gain.cavalry", "骑兵+{0}", cavalry));
        if (artillery != 0) lines.Add(TF("attribute_gain.artillery", "炮兵+{0}", artillery));
        if (management != 0) lines.Add(TF("attribute_gain.management", "管理+{0}", management));
        if (logistics != 0) lines.Add(TF("attribute_gain.logistics", "后勤+{0}", logistics));
        if (training != 0) lines.Add(TF("attribute_gain.training", "训练+{0}", training));
        return string.Join(T("common.list_separator", "、"), lines.ToArray());
    }

    private string CreationStanceLine(int home, int army, int native, int liberal, int legal)
    {
        List<string> lines = new List<string>();
        if (home != 0) lines.Add("返乡团" + SignedValue(home));
        if (army != 0) lines.Add("干城派" + SignedValue(army));
        if (native != 0) lines.Add("乡党" + SignedValue(native));
        if (liberal != 0) lines.Add("自由派" + SignedValue(liberal));
        if (legal != 0) lines.Add("法治派" + SignedValue(legal));
        return string.Join(T("common.list_separator", "、"), lines.ToArray());
    }

    private string CreationAxisLine(int nation, int classValue, int governance, int region)
    {
        List<string> lines = new List<string>();
        if (nation != 0) lines.Add("民族" + SignedValue(nation));
        if (classValue != 0) lines.Add("阶级" + SignedValue(classValue));
        if (governance != 0) lines.Add("治国" + SignedValue(governance));
        if (region != 0) lines.Add("地域" + SignedValue(region));
        return string.Join(T("common.list_separator", "、"), lines.ToArray());
    }

    private string SignedValue(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }

    private string CreationSubjectNames()
    {
        List<string> names = CreationSubjectCatalog().Where(s => creationSubjectIds.Contains(s.id)).Select(s => s.name).ToList();
        return names.Count == 0 ? T("common.none_selected", "未选择") : string.Join(T("common.list_separator", "、"), names.ToArray());
    }

    private List<CharacterTrait> TraitCatalog()
    {
        List<CharacterTrait> rows = new List<CharacterTrait>();
        rows.AddRange(DefaultTraitCatalog());
        if (gameConfig.traits != null) rows.AddRange(gameConfig.traits);
        rows.AddRange(CreationTalentCatalog().Select(CreationTalentAsTrait));
        return rows
            .Where(t => t != null && !string.IsNullOrEmpty(t.id))
            .GroupBy(t => t.id)
            .Select(g => g.Last())
            .ToList();
    }

    private CharacterTrait CreationTalentAsTrait(CreationTalent talent)
    {
        return new CharacterTrait
        {
            id = talent.id,
            name = talent.name,
            description = talent.description,
            battleAttack = talent.battleAttack,
            battleHp = talent.battleHp,
            battleMove = talent.battleMove,
            socialBonus = talent.socialBonus,
            cultivationPercent = talent.cultivationPercent,
            staminaSave = talent.staminaSave
        };
    }

    private List<CharacterTrait> DefaultTraitCatalog()
    {
        return new List<CharacterTrait>
        {
            new CharacterTrait { id = "trait_cautious", name = "审慎", description = "战斗：兵力 +8；养成：体力消耗 -1。", battleHp = 8, staminaSave = 1 },
            new CharacterTrait { id = "trait_kind", name = "仁厚", description = "社交：好感收益 +3。", socialBonus = 3 },
            new CharacterTrait { id = "trait_decisive", name = "果决", description = "战斗：攻击 +2。", battleAttack = 2 },
            new CharacterTrait { id = "trait_sensitive", name = "善感", description = "社交：好感 +1；养成进度 +8%。", socialBonus = 1, cultivationPercent = 8 },
            new CharacterTrait { id = "trait_stoic", name = "隐忍", description = "战斗：兵力 +12；体力消耗 -1。", battleHp = 12, staminaSave = 1 },
            new CharacterTrait { id = "trait_radical", name = "激进", description = "战斗：攻击 +3；养成进度 -5%。", battleAttack = 3, cultivationPercent = -5 },
            new CharacterTrait { id = "field_commander", name = "阵前直觉", description = "战斗：全军攻击 +3。", battleAttack = 3 },
            new CharacterTrait { id = "iron_body", name = "坚韧体魄", description = "战斗：兵力 +15；养成：体力消耗 -2。", battleHp = 15, staminaSave = 2 },
            new CharacterTrait { id = "wild_runner", name = "野外行军", description = "战斗：移动 +1。", battleMove = 1 },
            new CharacterTrait { id = "honor_student", name = "军校优等生", description = "养成：课程进度 +20%。", cultivationPercent = 20 },
            new CharacterTrait { id = "methodical", name = "勤勉自律", description = "养成：课程进度 +12%，体力消耗 -1。", cultivationPercent = 12, staminaSave = 1 },
            new CharacterTrait { id = "silver_tongue", name = "辩才无碍", description = "社交：好感收益 +3。", socialBonus = 3 },
            new CharacterTrait { id = "noble_manners", name = "名门礼仪", description = "社交：好感收益 +2；养成 +8%。", socialBonus = 2, cultivationPercent = 8 },
            new CharacterTrait { id = "balanced", name = "文武兼修", description = "战斗：攻击 +1；社交 +1；养成 +8%。", battleAttack = 1, socialBonus = 1, cultivationPercent = 8 }
        };
    }

    private List<CharacterOrigin> CharacterOriginCatalog()
    {
        return gameConfig.characterOrigins != null && gameConfig.characterOrigins.Count > 0 ? gameConfig.characterOrigins : DefaultCharacterOrigins();
    }

    private List<CreationMemory> CreationMemoryCatalog()
    {
        return gameConfig.creationMemories != null && gameConfig.creationMemories.Count > 0 ? gameConfig.creationMemories : DefaultCreationMemories();
    }

    private List<CreationTalent> CreationTalentCatalog()
    {
        return gameConfig.creationTalents != null && gameConfig.creationTalents.Count > 0 ? gameConfig.creationTalents : DefaultCreationTalents();
    }

    private List<CreationSubject> CreationSubjectCatalog()
    {
        return gameConfig.creationSubjects != null && gameConfig.creationSubjects.Count > 0 ? gameConfig.creationSubjects : DefaultCreationSubjects();
    }

    private List<CharacterOrigin> DefaultCharacterOrigins()
    {
        return new List<CharacterOrigin>
        {
            new CharacterOrigin { id = "noble", name = "勋贵之后", subtitle = "伯爵之子，门第深厚。", description = "你的父亲是宁远伯爵，镇守西境多年。家族与返乡团关系密切，却也背负旧族的沉默。", talentPool = "noble;leader;strategy", clueName = "父亲的沉默", managementExp = 50, logisticsExp = 50, nationAxis = -3, classAxis = -3, governanceAxis = -5, regionAxis = -3, stanceHome = 10, stanceNative = -8, stanceLiberal = -5, stanceLegal = 5, relZhao = 5, relYierde = -5, relLi = 3 },
            new CharacterOrigin { id = "scholar", name = "书香门第", subtitle = "翰林之后，重文重法。", description = "你出身南方文脉，家中藏书与辩论伴随童年。学院里的共和与法理，对你并不陌生。", talentPool = "scholar;strategy;social", trainingExp = 50, nationAxis = 5, classAxis = 3, governanceAxis = 5, stanceHome = -3, stanceNative = 3, stanceLiberal = 12, relLin = 8, relYierde = 3, relChen = -5 },
            new CharacterOrigin { id = "military", name = "将门虎子", subtitle = "总兵之子，少年习武。", description = "你熟悉军营号角，也熟悉军中对软弱的轻蔑。干城派看重你的血脉，文官则未必信任你。", talentPool = "military;leader;bravery", infantryExp = 50, trainingExp = 50, classAxis = -3, governanceAxis = -3, regionAxis = -3, stanceArmy = 12, stanceLiberal = -5, stanceLegal = -5, relZhao = 5, relLin = -3, relChen = -5, relLi = 5 },
            new CharacterOrigin { id = "border", name = "边民出身", subtitle = "边陲小族，熟悉荒原。", description = "你来自边境，见过贸易、冲突和饥荒。你比多数同窗更懂得地图边缘的人如何活下去。", talentPool = "border;bravery;resilience", cavalryExp = 50, logisticsExp = 50, nationAxis = 5, classAxis = 3, regionAxis = 3, stanceHome = -8, stanceArmy = 3, stanceNative = 12, stanceLiberal = 5, stanceLegal = -5, relZhao = -3, relYierde = 10, relChen = -8 },
            new CharacterOrigin { id = "merchant", name = "商贾之家", subtitle = "远洋巨商，消息灵通。", description = "母族经营海贸，银钱与情报在你幼年便是同一种语言。你知道港口的风向，也知道宫廷的价码。", talentPool = "merchant;social;strategy", clueName = "沈家账册", managementExp = 50, logisticsExp = 50, stanceHome = 3, stanceArmy = -3, stanceLiberal = 5, stanceLegal = 5, relZhao = 3, relChen = -3 },
            new CharacterOrigin { id = "tribal", name = "部落血裔", subtitle = "归化部落，双重身份。", description = "你身上流着归化部落的血。有人把这当作污点，也有人把这视为新大陆未来的证明。", talentPool = "tribal;social;resilience", cavalryExp = 50, trainingExp = 50, nationAxis = 8, classAxis = 5, governanceAxis = 3, regionAxis = 5, stanceHome = -10, stanceNative = 15, stanceLiberal = 3, stanceLegal = -8, relYierde = 15, relChen = -10 }
        };
    }

    private List<CreationMemory> DefaultCreationMemories()
    {
        return new List<CreationMemory>
        {
            new CreationMemory { id = "market", title = "往事一：边境集市", body = "老妇人递来一串祈福珠。你看见贫穷，也看见谎言。", optionAId = "market_doubt", optionAText = "拉住父亲，提醒这是骗局。", optionATraitId = "trait_cautious", optionANation = -3, optionAGovernance = -2, optionBId = "market_mercy", optionBText = "把零花钱给她，愿她今日能饱腹。", optionBTraitId = "trait_kind", optionBNation = 4, optionBClass = 2 },
            new CreationMemory { id = "yard", title = "往事二：演武场", body = "同伴被高年级欺辱。你可以忍，也可以立刻冲上去。", optionAId = "yard_endure", optionAText = "先记下对方名字，等机会再还。", optionATraitId = "trait_stoic", optionAGovernance = -2, optionARegion = -1, optionBId = "yard_charge", optionBText = "冲上前，把人从泥地里拉起来。", optionBTraitId = "trait_decisive", optionBNation = 2, optionBGovernance = 2 },
            new CreationMemory { id = "book", title = "往事三：禁书一页", body = "你在书房里发现被撕下的海外手札。父亲沉默，烛火摇晃。", optionAId = "book_hide", optionAText = "藏起残页，等待能看懂它的一天。", optionATraitId = "trait_sensitive", optionAClass = 3, optionAGovernance = 2, optionBId = "book_ask", optionBText = "直接追问父亲，哪怕惹怒他。", optionBTraitId = "trait_radical", optionBNation = 4, optionBClass = 3, optionBGovernance = 3 }
        };
    }

    private List<CreationTalent> DefaultCreationTalents()
    {
        return new List<CreationTalent>
        {
            new CreationTalent { id = "talent_old_blood", name = "贵胄之姿", category = "统率", tier = 1, originTags = "noble", description = "社交好感+2，养成进度+6%。", socialBonus = 2, cultivationPercent = 6 },
            new CreationTalent { id = "talent_family_learning", name = "家学渊源", category = "谋略", tier = 1, originTags = "noble;scholar", description = "养成进度+10%。", cultivationPercent = 10 },
            new CreationTalent { id = "talent_argument", name = "明辨章句", category = "谋略", tier = 1, originTags = "scholar", description = "社交好感+1，养成进度+8%。", socialBonus = 1, cultivationPercent = 8 },
            new CreationTalent { id = "talent_warborn", name = "百战余习", category = "武勇", tier = 1, originTags = "military;border", description = "攻击+3。", battleAttack = 3 },
            new CreationTalent { id = "talent_drillmaster", name = "校场老手", category = "统率", tier = 1, originTags = "military", description = "兵力+12，攻击+1。", battleHp = 12, battleAttack = 1 },
            new CreationTalent { id = "talent_frontier", name = "荒原识途", category = "坚韧", tier = 1, originTags = "border;tribal", description = "移动+1，体力消耗-1。", battleMove = 1, staminaSave = 1 },
            new CreationTalent { id = "talent_tradewind", name = "海贸耳目", category = "通达", tier = 1, originTags = "merchant", description = "情报+4，社交好感+1。", intelligenceBonus = 4, socialBonus = 1 },
            new CreationTalent { id = "talent_abacus", name = "账册心算", category = "谋略", tier = 1, originTags = "merchant;scholar", description = "养成进度+6%，体力消耗-1。", cultivationPercent = 6, staminaSave = 1 },
            new CreationTalent { id = "talent_two_worlds", name = "双界行者", category = "通达", tier = 1, originTags = "tribal;border", description = "社交好感+3。", socialBonus = 3 },
            new CreationTalent { id = "talent_bloodfire", name = "血性激昂", category = "武勇", tier = 1, originTags = "", description = "攻击+2，兵力+8。", battleAttack = 2, battleHp = 8 }
        };
    }

    private List<CreationSubject> DefaultCreationSubjects()
    {
        return new List<CreationSubject>
        {
            new CreationSubject { id = "infantry", name = "步兵", target = "infantryExp", description = "近战、长枪、重步基础。", expGain = 50 },
            new CreationSubject { id = "cavalry", name = "骑兵", target = "cavalryExp", description = "骑兵冲击和战场机动。", expGain = 50 },
            new CreationSubject { id = "artillery", name = "炮兵", target = "artilleryExp", description = "火枪、弓兵和重器输出。", expGain = 50 },
            new CreationSubject { id = "management", name = "管理", target = "managementExp", description = "政务、国库和任务效率。", expGain = 50 },
            new CreationSubject { id = "logistics", name = "后勤", target = "logisticsExp", description = "补给、行军和消耗控制。", expGain = 50 },
            new CreationSubject { id = "training", name = "训练", target = "trainingExp", description = "演训、情报和综合准备。", expGain = 50 }
        };
    }

    private string TraitNames(IEnumerable<string> ids)
    {
        List<string> selectedNames = TraitCatalog()
            .Where(t => ids != null && ids.Contains(t.id))
            .Select(t => t.name)
            .ToList();
        return selectedNames.Count == 0 ? T("common.none_selected", "未选择") : string.Join(T("common.list_separator", "、"), selectedNames.ToArray());
    }

    private string TraitBonusSummary(IEnumerable<string> ids)
    {
        List<CharacterTrait> selected = TraitCatalog().Where(t => ids != null && ids.Contains(t.id)).ToList();
        if (selected.Count == 0) return T("character_create.bonus_none", "加成：未选择");
        int attack = selected.Sum(t => t.battleAttack);
        int hp = selected.Sum(t => t.battleHp);
        int move = selected.Sum(t => t.battleMove);
        int social = selected.Sum(t => t.socialBonus);
        int cultivate = selected.Sum(t => t.cultivationPercent);
        int stamina = selected.Sum(t => t.staminaSave);
        List<string> lines = new List<string>();
        List<string> battle = new List<string>();
        if (attack != 0) battle.Add(TF("character_create.bonus_attack", "攻击 +{0}", attack));
        if (hp != 0) battle.Add(TF("character_create.bonus_hp", "兵力 +{0}", hp));
        if (move != 0) battle.Add(TF("character_create.bonus_move", "移动 +{0}", move));
        if (battle.Count > 0) lines.Add(TF("character_create.bonus_battle", "战斗：{0}", string.Join(T("common.list_separator", "、"), battle.ToArray())));
        if (social != 0) lines.Add(TF("character_create.bonus_social", "社交：好感 +{0}", social));

        List<string> academy = new List<string>();
        if (cultivate != 0) academy.Add(TF("character_create.bonus_cultivate", "进度 {0}{1}%", cultivate > 0 ? "+" : "", cultivate));
        if (stamina != 0) academy.Add(TF("character_create.bonus_stamina", "体力消耗 -{0}", stamina));
        if (academy.Count > 0) lines.Add(TF("character_create.bonus_academy", "养成：{0}", string.Join(T("common.list_separator", "、"), academy.ToArray())));
        return lines.Count == 0 ? T("character_create.bonus_no_numeric", "加成：暂无数值变化") : T("character_create.bonus_header", "加成：") + "\n" + string.Join("\n", lines.ToArray());
    }

    private List<PassiveSkillConfig> PassiveSkills()
    {
        if (gameConfig.passiveSkills != null && gameConfig.passiveSkills.Count > 0) return gameConfig.passiveSkills;
        return new List<PassiveSkillConfig>
        {
            NewPassiveSkill("field_sense", "战场嗅觉", "机动", "普通", "天赋", "always", "", 0, "战斗开始时更容易把握先手。攻击+5%，首发士气+1。", 5, 0, 0, 0, 1, 0, 0, 0),
            NewPassiveSkill("iron_wall", "铁壁之心", "防御", "普通", "学识", "trainingExp", "", 50, "稳定防御姿态，受到伤害-8%。", 0, 8, 0, 0, 0, 0, 0, 0),
            NewPassiveSkill("forced_march", "急掠如风", "机动", "普通", "学识", "logisticsExp", "", 50, "行军和战场机动更流畅。移动+1。", 0, 0, 0, 1, 0, 0, 0, 0),
            NewPassiveSkill("veteran_drill", "身经百战", "指挥", "精锐", "经验", "battleWins", "", 1, "战斗复盘更有效。战斗经验+20%。", 0, 0, 0, 0, 0, 0, 0, 20),
            NewPassiveSkill("supply_master", "补给大师", "生存", "精锐", "指挥", "logisticsExp", "", 150, "降低补给消耗，辎重路线更稳。补给消耗-25%。", 0, 0, 0, 0, 0, 25, 0, 0),
            NewPassiveSkill("precise_fire", "精准打击", "攻击", "精锐", "经验", "artilleryExp", "", 150, "攻击时更容易打出关键杀伤。攻击+12%。", 12, 0, 0, 0, 0, 0, 0, 0),
            NewPassiveSkill("covert_network", "暗线经营", "情报", "精锐", "经验", "intelligence", "", 30, "情报行动成功率提升，情报收益+3。", 0, 0, 0, 0, 0, 0, 3, 0),
            NewPassiveSkill("unyielding", "众志成城", "士气", "传说", "指挥", "merit", "", 300, "战斗中全军最大兵力+10%，士气+1。", 0, 0, 10, 0, 1, 0, 0, 0)
        };
    }

    private PassiveSkillConfig NewPassiveSkill(string id, string name, string category, string rarity, string slot, string unlockKind, string unlockTarget, int unlockValue, string description, int attackPercent, int defensePercent, int hpPercent, int moveBonus, int moraleBonus, int supplySavePercent, int intelBonus, int expBonusPercent)
    {
        return new PassiveSkillConfig
        {
            id = id,
            name = name,
            category = category,
            rarity = rarity,
            slot = slot,
            unlockKind = unlockKind,
            unlockTarget = unlockTarget,
            unlockValue = unlockValue,
            description = description,
            attackPercent = attackPercent,
            defensePercent = defensePercent,
            hpPercent = hpPercent,
            moveBonus = moveBonus,
            moraleBonus = moraleBonus,
            supplySavePercent = supplySavePercent,
            intelBonus = intelBonus,
            expBonusPercent = expBonusPercent
        };
    }

    private List<QuestConfig> QuestCatalog()
    {
        if (gameConfig.quests != null && gameConfig.quests.Count > 0) return gameConfig.quests;
        return new List<QuestConfig>
        {
            NewQuest("main_01", "主线", "第一堂课", "完成任意一门课程，熟悉学院养成节奏。", "always", "", 0, "anyCourseExp", "", 25, 8, 20, "trainingExp", 12, "", 0, "A01", "main_02"),
            NewQuest("main_02", "主线", "夏季大演习", "赢得一场战斗，验证军校课程成果。", "quest", "main_01", 1, "battleWins", "", 1, 18, 30, "managementExp", 15, "", 0, "B01", "main_03"),
            NewQuest("main_03", "主线", "暗线初成", "通过情报行动建立个人情报来源。", "quest", "main_02", 1, "spyNetwork", "", 2, 10, 10, "logisticsExp", 12, "", 0, "I01", ""),
            NewQuest("rel_zhao_01", "角色支线", "将门之后", "与赵伯衡达到朋友关系，解锁更多战场协同。", "always", "", 0, "relationship", "zhao", 50, 10, 0, "infantryExp", 12, "zhao", 8, "", ""),
            NewQuest("faction_liberal_01", "派系任务", "鼎新之光", "通过讲座或剧情让自由派立场达到40。", "always", "", 0, "stance", "liberal", 40, 12, 8, "managementExp", 10, "", 0, "", ""),
            NewQuest("daily_drill", "日常", "晨练操枪", "让训练课程达到2级。", "always", "", 0, "trainingExp", "", 50, 6, 0, "infantryExp", 8, "", 0, "", ""),
            NewQuest("intel_scout_01", "情报", "边境耳目", "累计情报达到30，战前可看到更多敌情。", "always", "", 0, "intelligence", "", 30, 8, 0, "logisticsExp", 8, "", 0, "", "")
        };
    }

    private QuestConfig NewQuest(string id, string type, string name, string description, string unlockKind, string unlockTarget, int unlockValue, string targetKind, string targetId, int targetValue, int rewardMerit, int rewardTreasury, string rewardExpTarget, int rewardExp, string rewardAffectionTarget, int rewardAffection, string rewardAchievement, string nextQuestId)
    {
        return new QuestConfig
        {
            id = id,
            type = type,
            name = name,
            description = description,
            unlockKind = unlockKind,
            unlockTarget = unlockTarget,
            unlockValue = unlockValue,
            targetKind = targetKind,
            targetId = targetId,
            targetValue = targetValue,
            rewardMerit = rewardMerit,
            rewardTreasury = rewardTreasury,
            rewardExpTarget = rewardExpTarget,
            rewardExp = rewardExp,
            rewardAffectionTarget = rewardAffectionTarget,
            rewardAffection = rewardAffection,
            rewardAchievement = rewardAchievement,
            nextQuestId = nextQuestId
        };
    }

    private List<AchievementConfig> AchievementCatalog()
    {
        if (gameConfig.achievements != null && gameConfig.achievements.Count > 0) return gameConfig.achievements;
        return new List<AchievementConfig>
        {
            NewAchievement("B01", "战斗", "初试锋芒", "赢得第一场战斗。", "battleWins", "", 1, "rookie", 10, "铜"),
            NewAchievement("B02", "战斗", "连战连胜", "累计赢得3场战斗。", "battleWins", "", 3, "veteran", 30, "银"),
            NewAchievement("A01", "养成", "军校生涯", "任意学科达到2级。", "anyCourseLevel", "", 2, "honor_student", 10, "铜"),
            NewAchievement("S01", "社交", "朋友之证", "任意角色好感达到50。", "anyRelationship", "", 50, "trusted_friend", 20, "银"),
            NewAchievement("I01", "情报", "暗线初成", "间谍网络达到2。", "spyNetwork", "", 2, "shadow_listener", 20, "银"),
            NewAchievement("Q01", "任务", "有令必达", "完成3个任务。", "questsCompleted", "", 3, "reliable_officer", 20, "银"),
            NewAchievement("NG01", "多周目", "记忆回响", "开启二周目。", "newGamePlus", "", 1, "echo_memory", 50, "金")
        };
    }

    private AchievementConfig NewAchievement(string id, string category, string name, string description, string conditionKind, string conditionTarget, int conditionValue, string rewardTitle, int rewardPoints, string rarity)
    {
        return new AchievementConfig { id = id, category = category, name = name, description = description, conditionKind = conditionKind, conditionTarget = conditionTarget, conditionValue = conditionValue, rewardTitle = rewardTitle, rewardPoints = rewardPoints, rarity = rarity };
    }

    private List<TitleConfig> TitleCatalog()
    {
        if (gameConfig.titles != null && gameConfig.titles.Count > 0) return gameConfig.titles;
        return new List<TitleConfig>
        {
            NewTitle("rookie", "新兵", "军事", "第一次胜利的纪念。攻击+1。", 1, 0, 0, 0, 0, 0),
            NewTitle("veteran", "老兵", "军事", "连战积累出的沉稳。攻击+2，兵力+5。", 2, 5, 0, 0, 0, 0),
            NewTitle("honor_student", "优等生", "学术", "学院课程表现优秀。养成+5%。", 0, 0, 0, 5, 0, 0),
            NewTitle("trusted_friend", "挚友", "社交", "值得托付的人。社交+2。", 0, 0, 2, 0, 0, 0),
            NewTitle("shadow_listener", "听影者", "情报", "善于捕捉看不见的动静。情报收益+2。", 0, 0, 0, 0, 2, 0),
            NewTitle("reliable_officer", "可靠军官", "任务", "稳定完成上级军令。补给上限+4。", 0, 0, 0, 0, 0, 4),
            NewTitle("echo_memory", "记忆回响", "多周目", "你隐约记得另一个结局。全局小幅加成。", 1, 4, 1, 3, 1, 2)
        };
    }

    private TitleConfig NewTitle(string id, string name, string category, string description, int attackBonus, int hpBonus, int socialBonus, int cultivationBonus, int intelligenceBonus, int supplyBonus)
    {
        return new TitleConfig { id = id, name = name, category = category, description = description, attackBonus = attackBonus, hpBonus = hpBonus, socialBonus = socialBonus, cultivationBonus = cultivationBonus, intelligenceBonus = intelligenceBonus, supplyBonus = supplyBonus };
    }

    private List<IntelligenceActionConfig> IntelligenceActions()
    {
        if (gameConfig.intelligenceActions != null && gameConfig.intelligenceActions.Count > 0) return gameConfig.intelligenceActions;
        return new List<IntelligenceActionConfig>
        {
            NewIntelAction("scout", "刺探军情", "侦察", "派斥候和线人确认敌方兵力。成功后情报+10。", 8, 78, 10, 10, 1, 0, 0, ""),
            NewIntelAction("sabotage_supply", "破坏补给", "破坏", "袭扰敌方辎重，降低敌军补给并有小幅兵力损失。", 14, 62, 22, 5, 1, 4, 10, ""),
            NewIntelAction("rumor", "散布谣言", "扰乱", "制造敌后谣言，降低敌军战前士气。", 10, 70, 18, 6, 1, 0, 4, ""),
            NewIntelAction("counter_spy", "反间清查", "反间", "清理可疑线索，安全地提升情报网络。", 6, 84, 6, 4, 2, 0, 0, "")
        };
    }

    private IntelligenceActionConfig NewIntelAction(string id, string name, string type, string description, int cost, int successRate, int risk, int intelGain, int spyNetworkGain, int enemyTroopDamage, int enemySupplyDamage, string targetFaction)
    {
        return new IntelligenceActionConfig { id = id, name = name, type = type, description = description, cost = cost, successRate = successRate, risk = risk, intelGain = intelGain, spyNetworkGain = spyNetworkGain, enemyTroopDamage = enemyTroopDamage, enemySupplyDamage = enemySupplyDamage, targetFaction = targetFaction };
    }

    private List<AiProfileConfig> AiProfiles()
    {
        if (gameConfig.aiProfiles != null && gameConfig.aiProfiles.Count > 0) return gameConfig.aiProfiles;
        return new List<AiProfileConfig>
        {
            NewAiProfile("balanced", "均衡型", 100, 100, 100, 25, 100, 100, 90, 80, 1, 110, 80),
            NewAiProfile("aggressive", "激进型", 165, 55, 95, 12, 65, 70, 35, 90, 1, 155, 35),
            NewAiProfile("tactical", "智将型", 115, 105, 165, 24, 135, 105, 75, 110, 2, 145, 115),
            NewAiProfile("defensive", "防守型", 65, 165, 120, 32, 190, 180, 170, 45, 2, 95, 145),
            NewAiProfile("mobile", "机动型", 130, 85, 105, 20, 100, 120, 65, 180, 2, 125, 75),
            NewAiProfile("siege", "攻坚型", 120, 115, 135, 22, 120, 190, 105, 70, 3, 135, 95),
            NewAiProfile("skirmish", "游击型", 105, 135, 120, 35, 130, 85, 85, 170, 3, 115, 160),
            NewAiProfile("berserker", "狂热型", 190, 30, 70, 5, 45, 55, 15, 120, 1, 180, 15)
        };
    }

    private AiProfileConfig NewAiProfile(string id, string name, int aggression, int caution, int focusFire, int retreatHpPercent, int terrainPreference, int objectiveBias, int guardBias, int flankBias, int rangedSpacing, int finishBias, int avoidCounter)
    {
        return new AiProfileConfig
        {
            id = id,
            name = name,
            aggression = aggression,
            caution = caution,
            focusFire = focusFire,
            retreatHpPercent = retreatHpPercent,
            terrainPreference = terrainPreference,
            objectiveBias = objectiveBias,
            guardBias = guardBias,
            flankBias = flankBias,
            rangedSpacing = rangedSpacing,
            finishBias = finishBias,
            avoidCounter = avoidCounter
        };
    }

    private SupplyRuleConfig SupplyRule()
    {
        if (gameConfig.supplyRules != null && gameConfig.supplyRules.Count > 0) return gameConfig.supplyRules[0];
        return new SupplyRuleConfig
        {
            id = "core",
            name = "标准补给",
            standbyCost = 2,
            moveCost = 3,
            attackCost = 5,
            moveAttackCost = 6,
            shortageThreshold = 8,
            shortageAttackPenalty = 5,
            shortageMoralePenalty = 1
        };
    }

    private void OpenSystemPopup(string title, string body, List<Tuple<string, Action>> options, ScreenMode returnMode, string sceneId = "library")
    {
        activeStoryEventId = "";
        pendingStoryTitle = title;
        pendingStorySceneId = sceneId;
        pendingStoryPortraitName = player.name;
        pendingStoryBody = body;
        pendingStoryOptions = options ?? new List<Tuple<string, Action>>();
        pendingStoryReturnAction = () => ReturnToMode(returnMode);
        ShowStoryEvent();
    }

    private void ReturnToMode(ScreenMode returnMode)
    {
        if (returnMode == ScreenMode.CharacterCreate) ShowCharacterCreate();
        else
        if (returnMode == ScreenMode.BattleLab) ShowBattleLabEditor();
        else
        if (returnMode == ScreenMode.Strategy) ShowStrategy();
        else if (returnMode == ScreenMode.Battle) ShowBattle();
        else ShowAcademy();
    }

    private void CloseStoryDialog()
    {
        if (pendingStoryReturnAction != null)
        {
            Action back = pendingStoryReturnAction;
            pendingStoryReturnAction = null;
            activeStoryEventId = "";
            back();
            return;
        }
        if (!string.IsNullOrEmpty(activeStoryEventId))
        {
            ReturnToStoryCaller();
            return;
        }
        ShowAcademy();
    }

    private void RefreshProgressionSystems(bool announce = true)
    {
        EnsurePlayerRuntimeLists(player);
        RefreshSkillUnlocks(announce);
        RefreshQuestState(announce);
        CheckQuestCompletions(announce);
        RefreshAchievements(announce);
        UpdatePlayerRank();
    }

    private void RefreshSkillUnlocks(bool announce)
    {
        foreach (PassiveSkillConfig skill in PassiveSkills())
        {
            if (string.IsNullOrEmpty(skill.id) || player.unlockedSkills.Contains(skill.id)) continue;
            if (!ConditionMet(skill.unlockKind, skill.unlockTarget, skill.unlockValue)) continue;
            player.unlockedSkills.Add(skill.id);
            if (announce) AddLog(TF("log.skill_unlock", "解锁被动技能：{0}。", skill.name));
        }
        if (player.equippedSkills.Count == 0)
        {
            PassiveSkillConfig first = PassiveSkills().FirstOrDefault(s => player.unlockedSkills.Contains(s.id));
            if (first != null) player.equippedSkills.Add(first.id);
        }
        while (player.equippedSkills.Count > SkillSlotLimit()) player.equippedSkills.RemoveAt(player.equippedSkills.Count - 1);
    }

    private int SkillSlotLimit()
    {
        if (player.merit >= 1500) return 4;
        if (player.merit >= 300) return 3;
        if (player.merit >= 100) return 2;
        return 1;
    }

    private IEnumerable<PassiveSkillConfig> EquippedPassiveSkills()
    {
        EnsurePlayerRuntimeLists(player);
        return PassiveSkills().Where(skill => player.equippedSkills.Contains(skill.id));
    }

    private int PassiveSkillSum(Func<PassiveSkillConfig, int> selector)
    {
        return EquippedPassiveSkills().Sum(selector);
    }

    private TitleConfig EquippedTitle()
    {
        if (string.IsNullOrEmpty(player.equippedTitle)) return null;
        return TitleCatalog().FirstOrDefault(t => t.id == player.equippedTitle);
    }

    private void ShowSkillPanel(ScreenMode returnMode)
    {
        RefreshProgressionSystems(false);
        List<PassiveSkillConfig> unlocked = PassiveSkills().Where(s => player.unlockedSkills.Contains(s.id)).ToList();
        string equipped = player.equippedSkills.Count == 0
            ? T("common.none", "无")
            : string.Join(T("common.list_separator", "、"), PassiveSkills().Where(s => player.equippedSkills.Contains(s.id)).Select(s => s.name).ToArray());
        string body = TF("skill.body", "被动槽位：{0}/{1}\n已装备：{2}\n\n已解锁：\n{3}",
            player.equippedSkills.Count,
            SkillSlotLimit(),
            equipped,
            unlocked.Count == 0 ? T("skill.none", "暂无。通过课程、战斗、情报和战功解锁。") : string.Join("\n", unlocked.Select(s => "· " + s.name + " [" + s.rarity + "/" + s.category + "] " + s.description).Take(7).ToArray()));
        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>();
        foreach (PassiveSkillConfig skill in unlocked.Where(s => !player.equippedSkills.Contains(s.id)).Take(5))
        {
            options.Add(Tuple.Create(TF("skill.equip", "装备 {0}", skill.name), (Action)(() => EquipSkill(skill.id, returnMode))));
        }
        foreach (PassiveSkillConfig skill in unlocked.Where(s => player.equippedSkills.Contains(s.id)).Take(3))
        {
            options.Add(Tuple.Create(TF("skill.unequip", "卸下 {0}", skill.name), (Action)(() => UnequipSkill(skill.id, returnMode))));
        }
        OpenSystemPopup(T("skill.title", "被动技能"), body, options, returnMode, "council");
    }

    private void EquipSkill(string id, ScreenMode returnMode)
    {
        if (!player.unlockedSkills.Contains(id)) return;
        if (player.equippedSkills.Contains(id)) return;
        while (player.equippedSkills.Count >= SkillSlotLimit() && player.equippedSkills.Count > 0) player.equippedSkills.RemoveAt(player.equippedSkills.Count - 1);
        player.equippedSkills.Add(id);
        PassiveSkillConfig skill = PassiveSkills().FirstOrDefault(s => s.id == id);
        AddLog(TF("log.skill_equip", "已装备被动：{0}。", skill != null ? skill.name : id));
        ShowSkillPanel(returnMode);
    }

    private void UnequipSkill(string id, ScreenMode returnMode)
    {
        player.equippedSkills.Remove(id);
        ShowSkillPanel(returnMode);
    }

    private void RefreshQuestState(bool announce)
    {
        foreach (QuestConfig quest in QuestCatalog())
        {
            if (string.IsNullOrEmpty(quest.id) || player.completedQuests.Contains(quest.id) || player.activeQuests.Contains(quest.id)) continue;
            if (!ConditionMet(quest.unlockKind, quest.unlockTarget, quest.unlockValue)) continue;
            if (player.activeQuests.Count >= 6 && quest.type != "主线") continue;
            player.activeQuests.Add(quest.id);
            if (announce) AddLog(TF("log.quest_unlock", "新任务：{0}。", quest.name));
        }
    }

    private void CheckQuestCompletions(bool announce)
    {
        foreach (string questId in player.activeQuests.ToList())
        {
            QuestConfig quest = QuestCatalog().FirstOrDefault(q => q.id == questId);
            if (quest == null || QuestProgress(quest) < Mathf.Max(1, quest.targetValue)) continue;
            CompleteQuest(quest, announce);
        }
    }

    private void CompleteQuest(QuestConfig quest, bool announce)
    {
        if (quest == null || player.completedQuests.Contains(quest.id)) return;
        player.activeQuests.Remove(quest.id);
        player.completedQuests.Add(quest.id);
        player.questsCompleted += 1;
        player.merit += quest.rewardMerit;
        player.treasury += quest.rewardTreasury;
        if (!string.IsNullOrEmpty(quest.rewardExpTarget) && quest.rewardExp > 0) ApplyExpReward(quest.rewardExpTarget, quest.rewardExp);
        if (!string.IsNullOrEmpty(quest.rewardAffectionTarget) && quest.rewardAffection != 0)
        {
            Relationship rel = relationships.FirstOrDefault(r => r.id == quest.rewardAffectionTarget || r.name == quest.rewardAffectionTarget);
            if (rel != null) GainRelationship(rel, quest.rewardAffection);
        }
        if (!string.IsNullOrEmpty(quest.rewardAchievement)) UnlockAchievement(quest.rewardAchievement, announce);
        if (!string.IsNullOrEmpty(quest.nextQuestId) && !player.activeQuests.Contains(quest.nextQuestId) && !player.completedQuests.Contains(quest.nextQuestId)) player.activeQuests.Add(quest.nextQuestId);
        if (announce) AddLog(TF("log.quest_complete", "任务完成：{0}。", quest.name));
    }

    private void ApplyExpReward(string target, int exp)
    {
        if (target == "infantryExp") player.infantryExp += exp;
        else if (target == "cavalryExp") player.cavalryExp += exp;
        else if (target == "artilleryExp") player.artilleryExp += exp;
        else if (target == "managementExp") player.managementExp += exp;
        else if (target == "logisticsExp") player.logisticsExp += exp;
        else player.trainingExp += exp;
    }

    private int QuestProgress(QuestConfig quest)
    {
        return ProgressValue(quest.targetKind, quest.targetId);
    }

    private bool ConditionMet(string kind, string target, int value)
    {
        if (string.IsNullOrEmpty(kind) || kind == "always") return true;
        if (kind == "quest") return player.completedQuests.Contains(target);
        if (kind == "story") return completedStoryEvents.Contains(target);
        if (kind == "skill") return player.unlockedSkills.Contains(target);
        return ProgressValue(kind, target) >= value;
    }

    private int ProgressValue(string kind, string target)
    {
        if (kind == "battleWins") return player.battleWins;
        if (kind == "battleLosses") return player.battleLosses;
        if (kind == "battlesFought") return player.battlesFought;
        if (kind == "enemiesDefeated") return player.enemiesDefeated;
        if (kind == "questsCompleted") return player.questsCompleted;
        if (kind == "spySuccesses") return player.spySuccesses;
        if (kind == "supplyBreaks") return player.supplyBreaks;
        if (kind == "intelligence") return player.intelligence;
        if (kind == "spyNetwork") return player.spyNetwork;
        if (kind == "storyValue") return GetStoryValue(target);
        if (kind == "suspicion") return GetStoryValue("警觉:" + target);
        if (kind == "origin") return player.originId == target || GetStoryValue("origin_" + target) > 0 ? 1 : 0;
        if (kind == "trait") return player.traits != null && player.traits.Contains(target) ? 1 : 0;
        if (kind == "talent") return player.traits != null && player.traits.Contains(target) ? 1 : 0;
        if (kind == "subject") return player.subjectFocusIds != null && player.subjectFocusIds.Contains(target) ? 1 : 0;
        if (kind == "memory") return player.creationMemoryChoices != null && player.creationMemoryChoices.Any(id => id.EndsWith(":" + target, StringComparison.Ordinal)) ? 1 : 0;
        if (kind == "merit") return player.merit;
        if (kind == "newGamePlus") return player.newGamePlus;
        if (kind == "infantryExp") return player.infantryExp;
        if (kind == "cavalryExp") return player.cavalryExp;
        if (kind == "artilleryExp") return player.artilleryExp;
        if (kind == "managementExp") return player.managementExp;
        if (kind == "logisticsExp") return player.logisticsExp;
        if (kind == "trainingExp") return player.trainingExp;
        if (kind == "anyCourseExp") return Mathf.Max(player.infantryExp, player.cavalryExp, player.artilleryExp, player.managementExp, player.logisticsExp, player.trainingExp);
        if (kind == "anyCourseLevel") return Mathf.Max(AcademyDisplayLevel(player.infantryExp), AcademyDisplayLevel(player.cavalryExp), AcademyDisplayLevel(player.artilleryExp), AcademyDisplayLevel(player.managementExp), AcademyDisplayLevel(player.logisticsExp), AcademyDisplayLevel(player.trainingExp));
        if (kind == "anyRelationship") return relationships.Count == 0 ? 0 : relationships.Max(r => r.affection);
        if (kind == "relationship")
        {
            Relationship rel = relationships.FirstOrDefault(r => r.id == target || r.name == target);
            return rel != null ? rel.affection : 0;
        }
        if (kind == "stance")
        {
            StanceScore score = stances.FirstOrDefault(s => s.id == target || s.name == target);
            return score != null ? score.value : 0;
        }
        return 0;
    }

    private void ShowQuestLog(ScreenMode returnMode)
    {
        RefreshProgressionSystems(false);
        List<QuestConfig> active = player.activeQuests.Select(id => QuestCatalog().FirstOrDefault(q => q.id == id)).Where(q => q != null).ToList();
        string activeText = active.Count == 0
            ? T("quest.no_active", "暂无进行中的任务。")
            : string.Join("\n", active.Select(q => TF("quest.line", "· [{0}] {1}  {2}/{3}\n  {4}", q.type, q.name, QuestProgress(q), q.targetValue, q.description)).Take(6).ToArray());
        string body = TF("quest.body", "任务追踪\n{0}\n\n已完成：{1}", activeText, player.completedQuests.Count);
        List<Tuple<string, Action>> options = active
            .Where(q => QuestProgress(q) >= Mathf.Max(1, q.targetValue))
            .Select<QuestConfig, Tuple<string, Action>>(q => Tuple.Create(TF("quest.claim", "领取 {0}", q.name), (Action)(() =>
            {
                CompleteQuest(q, true);
                ShowQuestLog(returnMode);
            }))).ToList();
        OpenSystemPopup(T("quest.title", "任务日志"), body, options, returnMode, "library");
    }

    private void RefreshAchievements(bool announce)
    {
        foreach (AchievementConfig achievement in AchievementCatalog())
        {
            if (string.IsNullOrEmpty(achievement.id) || player.unlockedAchievements.Contains(achievement.id)) continue;
            if (ProgressValue(achievement.conditionKind, achievement.conditionTarget) < achievement.conditionValue) continue;
            UnlockAchievement(achievement.id, announce);
        }
    }

    private void UnlockAchievement(string id, bool announce)
    {
        AchievementConfig achievement = AchievementCatalog().FirstOrDefault(a => a.id == id);
        if (achievement == null || player.unlockedAchievements.Contains(id)) return;
        player.unlockedAchievements.Add(id);
        player.achievementPoints += Mathf.Max(0, achievement.rewardPoints);
        if (!string.IsNullOrEmpty(achievement.rewardTitle) && !player.unlockedTitles.Contains(achievement.rewardTitle))
        {
            player.unlockedTitles.Add(achievement.rewardTitle);
            if (string.IsNullOrEmpty(player.equippedTitle)) player.equippedTitle = achievement.rewardTitle;
        }
        if (announce) AddLog(TF("log.achievement_unlock", "成就解锁：{0}。", achievement.name));
    }

    private void ShowAchievementPanel(ScreenMode returnMode)
    {
        RefreshProgressionSystems(false);
        List<AchievementConfig> achievements = AchievementCatalog();
        string unlocked = string.Join("\n", achievements.Where(a => player.unlockedAchievements.Contains(a.id)).Select(a => "· " + a.name + " [" + a.rarity + "]").Take(6).ToArray());
        if (string.IsNullOrEmpty(unlocked)) unlocked = T("achievement.none", "暂无成就。");
        string titles = player.unlockedTitles.Count == 0
            ? T("common.none", "无")
            : string.Join(T("common.list_separator", "、"), TitleCatalog().Where(t => player.unlockedTitles.Contains(t.id)).Select(t => t.name).ToArray());
        TitleConfig equipped = EquippedTitle();
        string body = TF("achievement.body", "成就点：{0}\n已解锁：{1}/{2}\n当前称号：{3}\n\n成就：\n{4}\n\n可用称号：{5}",
            player.achievementPoints,
            player.unlockedAchievements.Count,
            achievements.Count,
            equipped != null ? equipped.name : SafeText(player.title, T("common.none", "无")),
            unlocked,
            titles);
        List<Tuple<string, Action>> options = TitleCatalog()
            .Where(t => player.unlockedTitles.Contains(t.id) && player.equippedTitle != t.id)
            .Take(6)
            .Select<TitleConfig, Tuple<string, Action>>(title => Tuple.Create(TF("title.equip", "装备称号 {0}", title.name), (Action)(() =>
            {
                player.equippedTitle = title.id;
                UpdatePlayerRank();
                ShowAchievementPanel(returnMode);
            }))).ToList();
        OpenSystemPopup(T("achievement.title", "成就与称号"), body, options, returnMode, "library");
    }

    private void ShowIntelligencePanel(ScreenMode returnMode)
    {
        RefreshProgressionSystems(false);
        string knownEnemy = string.Join("\n", armies.Where(a => a.faction != Faction.Player).Select(a => "· " + KnownArmyIntelText(a)).Take(5).ToArray());
        string body = TF("intel.body", "情报值：{0}  间谍网络：{1}\n训练等级会提高行动成功率；失败会损失心情或国库。\n\n敌情摘要：\n{2}",
            player.intelligence,
            player.spyNetwork,
            string.IsNullOrEmpty(knownEnemy) ? T("intel.no_enemy", "暂无敌情。") : knownEnemy);
        List<Tuple<string, Action>> options = IntelligenceActions()
            .Take(7)
            .Select<IntelligenceActionConfig, Tuple<string, Action>>(action => Tuple.Create(TF("intel.action_button", "{0} - 花费{1}", action.name, action.cost), (Action)(() => RunIntelligenceAction(action, returnMode))))
            .ToList();
        OpenSystemPopup(T("intel.title", "情报与间谍"), body, options, returnMode, "council");
    }

    private string KnownArmyIntelText(Army army)
    {
        if (army == null) return "";
        int level = Mathf.Clamp(army.intelLevel + player.intelligence / 30, 0, 3);
        Province p = ProvinceById(army.provinceId);
        if (level <= 0) return TF("intel.army_unknown", "{0}方向有不明敌影", p != null ? p.name : T("common.unknown", "未知"));
        if (level == 1) return TF("intel.army_rough", "{0}：{1}，兵力约略可见", p != null ? p.name : "", army.name);
        if (level == 2) return TF("intel.army_clear", "{0}：{1}  兵力{2}  攻击{3}", p != null ? p.name : "", army.name, army.troops, army.attack);
        return TF("intel.army_full", "{0}：{1}  兵力{2}/{3}  攻击{4}  补给{5}/{6}  AI:{7}", p != null ? p.name : "", army.name, army.troops, army.maxTroops, army.attack, army.supply, army.maxSupply, AiProfileForArmy(army).name);
    }

    private void RunIntelligenceAction(IntelligenceActionConfig action, ScreenMode returnMode)
    {
        if (action == null) return;
        if (player.treasury < action.cost)
        {
            AddLog(TF("log.intel_no_money", "情报行动「{0}」资金不足。", action.name));
            ShowIntelligencePanel(returnMode);
            return;
        }
        player.treasury -= Mathf.Max(0, action.cost);
        int chance = Mathf.Clamp(action.successRate + ExpLevel(player.trainingExp) * 3 + PassiveSkillSum(s => s.intelBonus) * 3 + (EquippedTitle()?.intelligenceBonus ?? 0) * 3, 5, 95);
        bool success = UnityEngine.Random.Range(0, 100) < chance;
        if (success)
        {
            int gain = Mathf.Max(0, action.intelGain + PassiveSkillSum(s => s.intelBonus) + (EquippedTitle()?.intelligenceBonus ?? 0));
            player.intelligence += gain;
            player.spyNetwork += Mathf.Max(0, action.spyNetworkGain);
            player.spySuccesses += 1;
            foreach (Army enemy in armies.Where(a => a.faction != Faction.Player && (string.IsNullOrEmpty(action.targetFaction) || a.faction.ToString() == action.targetFaction)))
            {
                enemy.intelLevel = Mathf.Clamp(enemy.intelLevel + 1, 0, 3);
                enemy.troops = Mathf.Max(1, enemy.troops - Mathf.Max(0, action.enemyTroopDamage));
                if (action.enemySupplyDamage > 0)
                {
                    enemy.supply = Mathf.Max(0, enemy.supply - action.enemySupplyDamage);
                    if (enemy.supply == 0) player.supplyBreaks += 1;
                }
            }
            AddLog(TF("log.intel_success", "情报行动「{0}」成功，情报 +{1}。", action.name, gain));
        }
        else
        {
            int loss = Mathf.Max(1, action.risk / 4);
            player.mood = Mathf.Clamp(player.mood - loss, 0, 100);
            AddLog(TF("log.intel_fail", "情报行动「{0}」失败，心情 -{1}。", action.name, loss));
        }
        RefreshProgressionSystems(true);
        ShowIntelligencePanel(returnMode);
    }

    private void ShowSavePanel(ScreenMode returnMode)
    {
        string body = TF("save.body", "多槽位存档\n自动存档：每周推进、战斗开始、战斗结算时写入。\n当前周目：{0}\n成就点：{1}\n已解锁结局：{2}\n\n选择槽位进行保存或读取。",
            player.newGamePlus + 1,
            player.achievementPoints,
            player.unlockedEndings.Count);
        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>();
        for (int slot = 1; slot <= 3; slot++)
        {
            int captured = slot;
            options.Add(Tuple.Create(TF("save.manual_save", "保存到手动槽 {0}", captured), (Action)(() =>
            {
                SaveGameSlot("MANUAL_" + captured, returnMode, true);
            })));
            options.Add(Tuple.Create(TF("save.manual_load", "读取手动槽 {0}", captured), (Action)(() =>
            {
                LoadGameSlot("MANUAL_" + captured);
            })));
        }
        options.Add(Tuple.Create(T("save.new_game_plus", "开启新周目"), (Action)(() => StartNewGamePlus(returnMode))));
        OpenSystemPopup(T("save.title", "存档与多周目"), body, options, returnMode, "library");
    }

    private void ShowSettingsPanel(ScreenMode returnMode)
    {
        string body = TF("system.body", "系统\n当前界面：{0}\n自动存档：每周、剧情、战斗关键节点\n手动槽：3 个\n\n存档读档、标题界面和成就称号都收在这里。", ModeLabel(returnMode));
        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("button.save_system", "存档"), (Action)(() => ShowSavePanel(returnMode))),
            Tuple.Create(T("button.achievements", "成就"), (Action)(() => ShowAchievementPanel(returnMode))),
            Tuple.Create(T("button.battle_lab", "战棋工坊"), (Action)ShowBattleLabEditor),
            Tuple.Create(T("button.story_menu", "剧情目录"), (Action)(() =>
            {
                storyReturnMode = returnMode;
                ShowStoryMenu();
            })),
            Tuple.Create(T("button.back_title", "返回标题"), (Action)ShowTitle)
        };
        OpenSystemPopup(T("system.title", "设置"), body, options, returnMode, "library");
    }

    private string ModeLabel(ScreenMode screenMode)
    {
        if (screenMode == ScreenMode.Strategy) return T("mode.strategy", "战略地图");
        if (screenMode == ScreenMode.Battle) return T("mode.battle", "战斗");
        if (screenMode == ScreenMode.BattleLab) return T("mode.battle_lab", "战棋工坊");
        return T("mode.academy", "学院");
    }

    private void ShowFormationPanel(ScreenMode returnMode)
    {
        string localArmies = string.Join("\n", armies
            .Where(a => a.faction == Faction.Player)
            .Select(a => "· " + TF("formation.army_line", "{0}  Lv.{1}  兵力{2}/{3}  攻击{4}  补给{5}", a.name, a.level, a.troops, a.maxTroops, a.attack, SupplyStatus(a)))
            .Take(5)
            .ToArray());
        if (string.IsNullOrEmpty(localArmies)) localArmies = T("formation.no_army", "暂无可编队军团。");

        string unitPreview = string.Join("\n", CommonUnits()
            .Take(8)
            .Select(u => "· " + TF("formation.unit_line", "{0}｜{1}｜{2}", u.name, u.keyword, BattleRoleName(u.role)))
            .ToArray());

        string body = TF("formation.body", "直属军团\n{0}\n\n常见单位\n{1}\n\n军团数值来自战略军团表，棋子序列帧来自常见单位表。", localArmies, unitPreview);
        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("button.strategy_short", "战略"), (Action)ShowStrategy),
            Tuple.Create(T("button.mission", "军令"), (Action)ShowMissionBrief)
        };
        OpenSystemPopup(T("formation.title", "编队"), body, options, returnMode, "battlefield");
    }

    private string BattleRoleName(string roleId)
    {
        return RoleName(roleId);
    }

    private void ShowCharacterStatusPanel(ScreenMode returnMode)
    {
        string body = TF("character_status.body", "{0}\n\n属性等级\n{1}\n\n心态与性格\n心情：{2}（{3}）  体力：{4}\n特性：{5}\n\n立场\n{6}",
            AcademySummary(),
            AttributeSummary(),
            player.mood,
            MoodLabel(player.mood),
            player.stamina,
            TraitDetailSummary(),
            IdeologySummary());
        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("button.attribute_guide", "属性说明"), (Action)(() => ShowAttributeGuide(returnMode))),
            Tuple.Create(T("button.character_archive", "角色档案"), (Action)(() =>
            {
                storyReturnMode = returnMode;
                ShowCharacterArchive();
            })),
            Tuple.Create(T("button.achievements", "成就"), (Action)(() => ShowAchievementPanel(returnMode)))
        };
        OpenSystemPopup(T("character_status.title", "角色状态"), body, options, returnMode, "academy");
    }

    private string AttributeSummary()
    {
        return string.Join("\n", new[]
        {
            AttributeLine(T("attribute.infantry", "步兵"), player.infantryExp),
            AttributeLine(T("attribute.cavalry", "骑兵"), player.cavalryExp),
            AttributeLine(T("attribute.artillery", "炮兵"), player.artilleryExp),
            AttributeLine(T("attribute.management", "管理"), player.managementExp),
            AttributeLine(T("attribute.logistics", "后勤"), player.logisticsExp),
            AttributeLine(T("attribute.training", "训练"), player.trainingExp)
        });
    }

    private string AttributeLine(string label, int exp)
    {
        return "· " + label + " Lv." + AcademyDisplayLevel(exp) + "  " + AcademyProgressLabel(exp);
    }

    private string TraitDetailSummary()
    {
        List<CharacterTrait> selected = SelectedPlayerTraits();
        if (selected.Count == 0) return T("common.none", "无");
        return string.Join(T("common.list_separator", "、"), selected.Select(t => t.name).ToArray());
    }

    private string IdeologySummary()
    {
        return string.Join("\n", IdeologyAxes().Select(axis => "· " + axis.label + "：" + AxisLabel(axis.negativeLabel, axis.positiveLabel, AxisValue(axis.id))).ToArray());
    }

    private void ShowDiplomacyPanel(ScreenMode returnMode)
    {
        string stancesText = stances.Count == 0
            ? T("diplomacy.no_stance", "暂无派系接触。")
            : string.Join("\n", stances.OrderByDescending(s => s.value).Select(s => "· " + TF("diplomacy.stance_line", "{0}  倾向{1}", s.name, s.value)).Take(6).ToArray());
        string body = TF("diplomacy.body", "派系倾向\n{0}\n\n警觉\n{1}\n\n意识形态\n{2}", stancesText, FactionAlertSummary(), IdeologySummary());
        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("ideology.option_politics", "参加时事讲座"), (Action)ShowPoliticsEvent),
            Tuple.Create(T("button.intelligence", "情报"), (Action)(() => ShowIntelligencePanel(returnMode))),
            Tuple.Create(T("button.newspaper", "报纸"), (Action)ShowNewspaperMenu)
        };
        OpenSystemPopup(T("diplomacy.title", "外交与立场"), body, options, returnMode, "council");
    }

    private string FactionAlertSummary()
    {
        List<string> rows = FactionConfigs()
            .Where(f => f.id != "Player" && f.id != "Neutral")
            .Select(f => "· " + TF("diplomacy.alert_line", "{0}  警觉{1}", f.displayName, GetStoryValue("警觉:" + f.id)))
            .ToList();
        return rows.Count == 0 ? T("diplomacy.no_alert", "暂无明显警觉。") : string.Join("\n", rows.ToArray());
    }

    private string SecretDossierSummary()
    {
        int clue = GetStoryValue("线索:东渡密档");
        if (clue >= 50) return TF("dossier.summary_full", "东渡密档：{0}  地下室门牌已浮现。", clue);
        if (clue >= 25) return TF("dossier.summary_mid", "东渡密档：{0}  旧港仓库值得追查。", clue);
        if (clue >= 10) return TF("dossier.summary_low", "东渡密档：{0}  线索正在成形。", clue);
        return TF("dossier.summary_none", "东渡密档：{0}  仍是零散疑点。", clue);
    }

    private AiProfileConfig AiProfileForArmy(Army army)
    {
        string id = army != null && !string.IsNullOrEmpty(army.aiProfile) ? army.aiProfile : DefaultAiProfileForFaction(army != null ? army.faction : Faction.Neutral);
        return AiProfiles().FirstOrDefault(p => p.id == id) ?? AiProfiles().First();
    }

    private int ArmySupplyMaxWithBonuses(Army army)
    {
        if (army == null) return 0;
        int bonus = army.faction == Faction.Player ? (EquippedTitle()?.supplyBonus ?? 0) : 0;
        return Mathf.Max(1, army.maxSupply + bonus);
    }

    private string SupplyStatus(Army army)
    {
        if (army == null) return T("common.none", "无");
        SupplyRuleConfig rule = SupplyRule();
        int max = ArmySupplyMaxWithBonuses(army);
        string label = army.supply <= 0 ? T("supply.empty", "断补") : army.supply < rule.shortageThreshold ? T("supply.low", "缺补") : T("supply.good", "充足");
        return TF("supply.status", "{0}/{1}（{2}）", Mathf.Clamp(army.supply, 0, max), max, label);
    }

    private bool IsSupplyShort(Army army)
    {
        return army != null && army.supply < SupplyRule().shortageThreshold;
    }

    private int SupplyCostForAction(string action)
    {
        SupplyRuleConfig rule = SupplyRule();
        if (action == "moveAttack") return rule.moveAttackCost;
        if (action == "attack") return rule.attackCost;
        if (action == "move") return rule.moveCost;
        return rule.standbyCost;
    }

    private int ApplySupplySave(int cost, Army army)
    {
        if (army == null || army.faction != Faction.Player) return cost;
        int percent = Mathf.Clamp(PassiveSkillSum(s => s.supplySavePercent), 0, 75);
        return Mathf.Max(1, Mathf.RoundToInt(cost * (100 - percent) / 100f));
    }

    private void ConsumeArmySupply(Army army, string action)
    {
        if (army == null) return;
        int cost = ApplySupplySave(SupplyCostForAction(action), army);
        army.supply = Mathf.Clamp(army.supply - cost, 0, ArmySupplyMaxWithBonuses(army));
        if (army.supply == 0 && army.faction == Faction.Player) player.supplyBreaks += 1;
    }

    private void ConsumeBattleSupply(BattleUnit unit, string action)
    {
        if (unit == null) return;
        Army army = ArmyById(unit.armyId);
        ConsumeArmySupply(army, action);
    }

    private void RestoreArmySupply(Army army, int amount)
    {
        if (army == null) return;
        army.supply = Mathf.Clamp(army.supply + Mathf.Max(0, amount), 0, ArmySupplyMaxWithBonuses(army));
    }

    private void ApplySupplyToBattleUnit(BattleUnit unit, Army army)
    {
        if (unit == null || army == null || !IsSupplyShort(army)) return;
        SupplyRuleConfig rule = SupplyRule();
        unit.attack = Mathf.Max(1, unit.attack - rule.shortageAttackPenalty);
        unit.morale = Mathf.Clamp(unit.morale - rule.shortageMoralePenalty, BattleCore().minMorale, BattleCore().maxMorale);
    }

    private List<NewsArticle> NewsCatalog()
    {
        if (gameConfig.news != null && gameConfig.news.Count > 0) return gameConfig.news;
        return new List<NewsArticle>
        {
            new NewsArticle { id = "N001", unlockWeek = 1, title = "新京学报：新生入校", source = "新京学报", stanceHint = "温和官报", body = "军事学院迎来新一届学生。学报称，王朝需要既懂战术又懂人心的年轻军官。" },
            new NewsArticle { id = "N002", unlockWeek = 4, title = "海潮报：远航派争论", source = "海潮报", stanceHint = "返乡团", body = "返乡团再次推动远航预算，反对者认为大陆防线更需要兵员和补给。" },
            new NewsArticle { id = "N003", unlockWeek = 8, title = "民声小报：议会呼声", source = "民声小报", stanceHint = "自由派", body = "地方士绅与商人联名要求设立民间代表团。文章认为，军国政治无法回答所有民生问题。" },
            new NewsArticle { id = "N004", unlockWeek = 13, title = "红林周刊：归化部落", source = "红林周刊", stanceHint = "印第安乡党", body = "归化部落在边境贸易中承担了越来越多的运输与侦察职责，但他们在朝堂中的声音仍然微弱。" },
            new NewsArticle { id = "N005", unlockWeek = 20, title = "律令汇编：法治与皇权", source = "律令汇编", stanceHint = "法治派", body = "法治派主张重整贵族和地方豪强秩序，以严密法令削弱商团与宗教组织的影响。" },
            new NewsArticle { id = "N006", unlockWeek = 32, title = "前线简讯：北岭摩擦", source = "前线简讯", stanceHint = "陆军青壮派", body = "北岭附近出现小规模冲突。年轻军官要求增兵，海军派则担心这会拖慢远航计划。" }
        };
    }

    private List<CampusActivity> ActivityCatalog()
    {
        if (gameConfig.campusActivities != null && gameConfig.campusActivities.Count > 0) return gameConfig.campusActivities;
        return new List<CampusActivity>
        {
            new CampusActivity { id = "drill", name = "战术演练", description = "参加六日战术演练。训练成长，并获得少量战功。", moodDelta = -3, meritDelta = 6, trainingGain = 12 },
            new CampusActivity { id = "salon", name = "同窗沙龙", description = "加入朋友圈闲谈。全员好感提升，心情恢复。", moodDelta = 5, socialGain = 4, treasuryDelta = -6 },
            new CampusActivity { id = "lecture", name = "公共讲座", description = "聆听一场政治讲座。选择后会推动立场轴。", moodDelta = 1, axisId = "governance", axisDelta = 6 },
            new CampusActivity { id = "volunteer", name = "边民救济", description = "协助学院救济边民。共治与民主倾向上升。", moodDelta = 2, socialGain = 2, treasuryDelta = -10, axisId = "nation", axisDelta = 5 }
        };
    }

    private List<NarrativeFragmentConfig> NarrativeFragments()
    {
        if (gameConfig.narrativeFragments != null && gameConfig.narrativeFragments.Count > 0) return gameConfig.narrativeFragments;
        return new List<NarrativeFragmentConfig>
        {
            NewNarrativeFragment("nf_course_infantry_001", "course", "infantry", 1, 18, "操场边的旧口令", "赵伯衡", "步兵课结束后，赵伯衡低声纠正你的枪阵步点。他提到父辈口中的旧军号令，又突然收住话头：那套口令据说只在东渡前夜用过。", "academy", "zhao", 4, "region", -2, 1, "Imperial", 1, ""),
            NewNarrativeFragment("nf_course_cavalry_001", "course", "cavalry", 2, 24, "马房里的铜印", "伊尔德", "马房角落的旧鞍袋里夹着一枚磨损铜印。伊尔德认出边地商路的纹样，说它不像军需官的东西，更像某个远航商团的信物。", "academy", "yierde", 4, "nation", 2, 2, "Foreign", 1, ""),
            NewNarrativeFragment("nf_course_artillery_001", "course", "artillery", 3, 28, "靶场上的异式刻度", "李婉清", "炮兵靶场的旧火炮上刻着一组异式刻度。李婉清认为这是海图修正用的记号，若真如此，学院早在数年前就有人筹备过跨洋航线。", "battlefield", "li", 4, "governance", -2, 2, "Foreign", 2, ""),
            NewNarrativeFragment("nf_course_management_001", "course", "management", 4, 30, "账册空页", "林素心", "管理课的账册范本中少了一整页。林素心在页缝里找到淡墨痕迹：一串粮船编号被人为刮去，末尾留下「东」字残笔。", "library", "lin", 5, "class", 2, 3, "Reformist", 1, ""),
            NewNarrativeFragment("nf_course_logistics_001", "course", "logistics", 5, 34, "被改写的补给线", "旁白", "后勤教官让你复盘一次失败转运。图上某条补给线被人用朱砂改过，改线后的终点不是前线，而是旧港仓库。", "library", "", 0, "region", -1, 3, "Foreign", 2, ""),
            NewNarrativeFragment("nf_course_training_001", "course", "training", 6, 36, "夜训后的暗号", "旁白", "夜训结束时，你在靶壕边听见两名低年级学生交换暗号。他们谈起一份「父辈留下的名单」，并在看见你后匆匆离开。", "academy", "", 0, "governance", 1, 2, "Imperial", 2, ""),
            NewNarrativeFragment("nf_social_zhao_001", "social", "zhao", 1, 44, "梦话里的海门", "赵伯衡", "邀约归来时，赵伯衡半醉半醒，说父亲曾把一封信藏在「海门之后」。醒来后他矢口否认，却答应帮你打听旧将门的传闻。", "street", "zhao", 6, "region", -3, 2, "Imperial", 2, ""),
            NewNarrativeFragment("nf_social_lin_001", "social", "lin", 1, 44, "林素心的索引卡", "林素心", "林素心把一张索引卡推到你面前：旧港税册、失踪粮船、归航名单，三条线在同一周交汇。她没有给结论，只提醒你别急着信任何派系。", "library", "lin", 6, "class", 3, 3, "Reformist", 1, ""),
            NewNarrativeFragment("nf_social_yierde_001", "social", "yierde", 1, 44, "边地旧歌", "伊尔德", "伊尔德唱起一段边地旧歌，歌词里有「两岸皆非故乡」的句子。他说这歌常被商队用来记路线，也常被间谍用来传口令。", "frontier", "yierde", 6, "nation", 4, 2, "Native", 1, ""),
            NewNarrativeFragment("nf_activity_drill_001", "activity", "drill", 1, 40, "演习里的空缺席位", "李婉清", "战术演练的编组表里多出一个空缺席位，代号「海灯」。李婉清认为这不是笔误，而是有人在演习中预留了一支不存在的部队。", "battlefield", "li", 4, "governance", -2, 2, "Imperial", 2, ""),
            NewNarrativeFragment("nf_activity_salon_001", "activity", "salon", 1, 40, "沙龙流言", "陈敬之", "沙龙里有人谈起学院地下室的旧档。陈敬之冷淡地警告你：越接近旧档，越会让讲究秩序的人把你视作麻烦。", "council", "chen", 3, "governance", -2, 1, "Imperial", 2, ""),
            NewNarrativeFragment("nf_rest_secret_letter_001", "rest", "rest", 1, 36, "父亲的封信", "旁白", "难得休息时，你整理行囊，摸到父亲临行前塞进夹层的蜡封信。信里只有一句话：若见东渡二字，先查许书院。", "academy", "", 0, "", 0, 4, "Imperial", 1, ""),
            NewNarrativeFragment("nf_study_library_001", "study", "study", 1, 36, "许书院旧注", "旁白", "自习到深夜，你在旧书页边角看到「许书院」三字。旁边夹着一片船票残角，日期恰好在主角父亲失踪前七日。", "library", "", 0, "class", 1, 4, "Foreign", 1, ""),
            NewNarrativeFragment("nf_intel_010", "intelligence", "10", 1, 999, "密档：第一枚钥匙", "旁白", "情报脉络初成，你终于能把父亲的信、旧港税册和海图刻度放在同一张纸上。它们指向同一个词：东渡。", "library", "", 0, "", 0, 2, "Imperial", 1, ""),
            NewNarrativeFragment("nf_intel_025", "intelligence", "25", 1, 999, "密档：旧港仓库", "旁白", "暗线传来回报，旧港仓库近年有一批账目被反复借阅。借阅人身份被涂黑，但签章属于学院军需处。", "harbor", "", 0, "", 0, 3, "Foreign", 2, ""),
            NewNarrativeFragment("nf_intel_050", "intelligence", "50", 1, 999, "密档：地下室门牌", "林素心", "林素心确认许书院旧址并非普通藏书楼，地下室门牌上刻着一串军中编号。你的父亲似乎不是旁观者，而是名单上的一员。", "library", "lin", 8, "class", 2, 4, "Imperial", 3, "EV002")
        };
    }

    private NarrativeFragmentConfig NewNarrativeFragment(string id, string triggerKind, string triggerTarget, int minWeek, int maxWeek, string title, string speaker, string body, string sceneId, string relationshipTarget, int affectionDelta, string axisId, int axisDelta, int intelligenceDelta, string suspicionFaction, int suspicionDelta, string nextStoryId)
    {
        return new NarrativeFragmentConfig
        {
            id = id,
            triggerKind = triggerKind,
            triggerTarget = triggerTarget,
            minWeek = minWeek,
            maxWeek = maxWeek,
            title = title,
            speaker = speaker,
            body = body,
            sceneId = sceneId,
            relationshipTarget = relationshipTarget,
            affectionDelta = affectionDelta,
            axisId = axisId,
            axisDelta = axisDelta,
            intelligenceDelta = intelligenceDelta,
            suspicionFaction = suspicionFaction,
            suspicionDelta = suspicionDelta,
            nextStoryId = nextStoryId,
            once = "true"
        };
    }

    private List<CourseConfig> CourseCatalog()
    {
        if (gameConfig.courses != null && gameConfig.courses.Count > 0) return gameConfig.courses;
        return new List<CourseConfig>
        {
            new CourseConfig { id = "infantry", label = "步兵课程", target = "infantryExp" },
            new CourseConfig { id = "cavalry", label = "骑兵课程", target = "cavalryExp" },
            new CourseConfig { id = "artillery", label = "炮兵课程", target = "artilleryExp" },
            new CourseConfig { id = "management", label = "管理课程", target = "managementExp" },
            new CourseConfig { id = "logistics", label = "后勤课程", target = "logisticsExp" },
            new CourseConfig { id = "training", label = "训练课程", target = "trainingExp" },
            new CourseConfig { id = "wander", label = "校园闲逛", target = "social" }
        };
    }

    private CourseConfig CourseByLabel(string label)
    {
        return CourseCatalog().FirstOrDefault(c => c.label == label || c.id == label);
    }

    private AcademyCoreConfig AcademyCore()
    {
        return gameConfig.academyCore ?? new AcademyCoreConfig();
    }

    private BattleCoreConfig BattleCore()
    {
        return gameConfig.battleCore ?? new BattleCoreConfig();
    }

    private List<ExamRewardRule> ExamRewards()
    {
        if (gameConfig.examRewards != null && gameConfig.examRewards.Count > 0) return gameConfig.examRewards;
        return new List<ExamRewardRule>
        {
            new ExamRewardRule { minScore = 85, merit = 24, treasury = 35 },
            new ExamRewardRule { minScore = 70, merit = 16, treasury = 22 },
            new ExamRewardRule { minScore = 50, merit = 8, treasury = 10 },
            new ExamRewardRule { minScore = 0, merit = 2, treasury = 0 }
        };
    }

    private List<RelationshipLevelRule> RelationshipLevelRules()
    {
        if (gameConfig.relationshipLevels != null && gameConfig.relationshipLevels.Count > 0) return gameConfig.relationshipLevels;
        return new List<RelationshipLevelRule>
        {
            new RelationshipLevelRule { minAffection = 90, label = "莫逆", knownLevel = 4 },
            new RelationshipLevelRule { minAffection = 75, label = "亲密", knownLevel = 3 },
            new RelationshipLevelRule { minAffection = 50, label = "朋友", knownLevel = 2 },
            new RelationshipLevelRule { minAffection = 10, label = "熟人", knownLevel = 1 },
            new RelationshipLevelRule { minAffection = -30, label = "冷漠", knownLevel = 1 },
            new RelationshipLevelRule { minAffection = -70, label = "敌对", knownLevel = 1 },
            new RelationshipLevelRule { minAffection = -90, label = "仇视", knownLevel = 1 },
            new RelationshipLevelRule { minAffection = -100, label = "死敌", knownLevel = 1 }
        };
    }

    private List<BeliefLevelRule> BeliefLevelRules()
    {
        if (gameConfig.beliefLevels != null && gameConfig.beliefLevels.Count > 0) return gameConfig.beliefLevels;
        return new List<BeliefLevelRule>
        {
            new BeliefLevelRule { minAbsValue = 80, label = "狂热" },
            new BeliefLevelRule { minAbsValue = 60, label = "忠诚" },
            new BeliefLevelRule { minAbsValue = 40, label = "坚定" },
            new BeliefLevelRule { minAbsValue = 20, label = "倾向" },
            new BeliefLevelRule { minAbsValue = 1, label = "认可" },
            new BeliefLevelRule { minAbsValue = 0, label = "中立" }
        };
    }

    private List<FactionConfig> FactionConfigs()
    {
        if (gameConfig.factions != null && gameConfig.factions.Count > 0) return gameConfig.factions;
        return new List<FactionConfig>
        {
            new FactionConfig { id = "Player", displayName = "我方" },
            new FactionConfig { id = "Imperial", displayName = "返乡团/朝廷" },
            new FactionConfig { id = "Reformist", displayName = "革故派" },
            new FactionConfig { id = "Native", displayName = "印第安乡党" },
            new FactionConfig { id = "Foreign", displayName = "外邦势力" },
            new FactionConfig { id = "Neutral", displayName = "中立" }
        };
    }

    private List<IdeologyAxisConfig> IdeologyAxes()
    {
        if (gameConfig.ideologyAxes != null && gameConfig.ideologyAxes.Count > 0) return gameConfig.ideologyAxes;
        return new List<IdeologyAxisConfig>
        {
            new IdeologyAxisConfig { id = "nation", label = "民族", negativeLabel = "皇汉", positiveLabel = "共治" },
            new IdeologyAxisConfig { id = "class", label = "阶级", negativeLabel = "君主", positiveLabel = "民主" },
            new IdeologyAxisConfig { id = "governance", label = "治国", negativeLabel = "独裁", positiveLabel = "共和" },
            new IdeologyAxisConfig { id = "region", label = "地域", negativeLabel = "统一", positiveLabel = "分裂" }
        };
    }

    private List<PoliticsOptionConfig> PoliticsOptions()
    {
        if (gameConfig.politicsOptions != null && gameConfig.politicsOptions.Count > 0) return gameConfig.politicsOptions;
        return new List<PoliticsOptionConfig>
        {
            new PoliticsOptionConfig { id = "home_voyage", label = "支持远航光复", stanceId = "home", stanceValue = 8, axisId = "region", axisValue = -5 },
            new PoliticsOptionConfig { id = "army_reform", label = "支持大陆军改革", stanceId = "army", stanceValue = 8, axisId = "governance", axisValue = -5 },
            new PoliticsOptionConfig { id = "native_co_rule", label = "主张族群共治", stanceId = "native", stanceValue = 8, axisId = "nation", axisValue = 6 },
            new PoliticsOptionConfig { id = "liberal_constitution", label = "主张议会立宪", stanceId = "liberal", stanceValue = 8, axisId = "class", axisValue = 6 },
            new PoliticsOptionConfig { id = "legal_order", label = "主张严法强国", stanceId = "legal", stanceValue = 8, axisId = "governance", axisValue = -6 }
        };
    }

    private List<BattleUnitSpawnConfig> BattleUnitSpawns()
    {
        if (gameConfig.battleUnitSpawns != null && gameConfig.battleUnitSpawns.Count > 0) return gameConfig.battleUnitSpawns;
        return new List<BattleUnitSpawnConfig>
        {
            new BattleUnitSpawnConfig { side = "attacker", suffix = "剑士队", role = "infantry", q = 1, r = 6, attackBonus = 0, troopDivisor = 4 },
            new BattleUnitSpawnConfig { side = "attacker", suffix = "火绳枪队", role = "musket", q = 3, r = 6, attackBonus = 2, troopDivisor = 5 },
            new BattleUnitSpawnConfig { side = "attacker", suffix = "先锋骑军", role = "cavalry", q = 5, r = 6, attackBonus = 4, troopDivisor = 5 },
            new BattleUnitSpawnConfig { side = "attacker", suffix = "精锐弓兵队", role = "archer", q = 2, r = 5, attackBonus = 1, troopDivisor = 5 },
            new BattleUnitSpawnConfig { side = "attacker", suffix = "钢盔军", role = "heavy_infantry", q = 4, r = 5, attackBonus = 2, troopDivisor = 4 },
            new BattleUnitSpawnConfig { side = "defender", suffix = "禁卫长戟队", role = "heavy_spear", q = 7, r = 0, attackBonus = 2, troopDivisor = 4 },
            new BattleUnitSpawnConfig { side = "defender", suffix = "具装铁骑军", role = "heavy_cavalry", q = 5, r = 1, attackBonus = 6, troopDivisor = 5 },
            new BattleUnitSpawnConfig { side = "defender", suffix = "禁军长弓兵", role = "heavy_archer", q = 3, r = 0, attackBonus = 2, troopDivisor = 5 },
            new BattleUnitSpawnConfig { side = "defender", suffix = "重甲禁卫军", role = "heavy_infantry", q = 6, r = 1, attackBonus = 4, troopDivisor = 4 },
            new BattleUnitSpawnConfig { side = "defender", suffix = "禁军神机队", role = "artillery", q = 2, r = 1, attackBonus = 5, troopDivisor = 6 }
        };
    }

    private List<CommonBattleUnitConfig> CommonUnits()
    {
        if (gameConfig.commonUnits != null && gameConfig.commonUnits.Count > 0) return gameConfig.commonUnits;
        return DefaultCommonUnits();
    }

    private List<CommonBattleUnitConfig> DefaultCommonUnits()
    {
        return new List<CommonBattleUnitConfig>
        {
            NewCommonUnit("swordsmen_volunteers", "剑士队", "义勇军", "infantry"),
            NewCommonUnit("matchlock_volunteers", "火绳枪队", "义勇军", "musket"),
            NewCommonUnit("militia_volunteers", "民兵团", "义勇军", "skirmisher"),
            NewCommonUnit("outlaw_skirmishers", "亡徒军", "贼徒", "skirmisher"),
            NewCommonUnit("imperial_halberdiers", "禁卫长戟队", "禁军", "heavy_spear"),
            NewCommonUnit("armored_iron_cavalry", "具装铁骑军", "禁军", "heavy_cavalry"),
            NewCommonUnit("steel_helmet_heavy_infantry", "钢盔军", "义勇军", "heavy_infantry"),
            NewCommonUnit("imperial_longbowmen", "禁军长弓兵", "禁军", "heavy_archer"),
            NewCommonUnit("sword_guard_corps", "剑卫军团", "义勇军", "infantry"),
            NewCommonUnit("imperial_axe_guard", "禁军斧卫", "禁军", "heavy_brute"),
            NewCommonUnit("vanguard_cavalry", "先锋骑军", "义勇军", "cavalry"),
            NewCommonUnit("solemn_guard_matchlocks", "肃卫火枪队", "义勇军", "musket"),
            NewCommonUnit("raiders", "掠杀军", "贼徒", "skirmisher"),
            NewCommonUnit("imperial_heavy_guard", "重甲禁卫军", "禁军", "heavy_infantry"),
            NewCommonUnit("warhammer_volunteers", "重锤军", "义勇军", "heavy_brute"),
            NewCommonUnit("imperial_shenji_artillery", "禁军神机队", "禁军", "artillery"),
            NewCommonUnit("zealot_believers", "狂热信众", "信徒", "skirmisher"),
            NewCommonUnit("zealot_mob", "狂热暴徒", "信徒", "brute"),
            NewCommonUnit("leader_guard", "领袖卫队", "信徒", "heavy_infantry"),
            NewCommonUnit("elite_archers", "精锐弓兵队", "义勇军", "archer"),
            NewCommonUnit("bandits", "土匪", "贼徒", "skirmisher"),
            NewCommonUnit("great_axe_warriors", "巨斧军", "义勇军", "brute"),
            NewCommonUnit("believer_elites", "信徒精锐", "信徒", "infantry")
        };
    }

    private CommonBattleUnitConfig NewCommonUnit(string id, string name, string keyword, string role)
    {
        return new CommonBattleUnitConfig
        {
            id = id,
            name = name,
            keyword = keyword,
            role = role,
            asset = "Art/BattleUnits/" + id,
            idleFrames = 4,
            moveFrames = 6,
            attackFrames = 6,
            hitFrames = 6
        };
    }

    private CommonBattleUnitConfig CommonUnitByName(string nameOrId)
    {
        if (string.IsNullOrEmpty(nameOrId)) return null;
        return CommonUnits().FirstOrDefault(u =>
            (!string.IsNullOrEmpty(u.name) && (u.name == nameOrId || nameOrId.EndsWith(u.name))) ||
            (!string.IsNullOrEmpty(u.id) && u.id == nameOrId));
    }

    private CommonBattleUnitConfig CommonUnitForBattleUnit(BattleUnit unit)
    {
        if (unit == null) return null;
        CommonBattleUnitConfig byName = CommonUnitByName(ShortBattleUnitName(unit));
        if (byName != null) return byName;
        return CommonUnits().FirstOrDefault(u => u.role == unit.role);
    }

    private CommonBattleUnitConfig CommonUnitForSpawn(BattleUnitSpawnConfig spawn)
    {
        if (spawn == null) return null;
        CommonBattleUnitConfig byName = CommonUnitByName(spawn.suffix);
        if (byName != null) return byName;
        return CommonUnits().FirstOrDefault(u => u.role == spawn.role);
    }

    private string BattleUnitSpriteResource(BattleUnit unit)
    {
        CommonBattleUnitConfig config = CommonUnitForBattleUnit(unit);
        if (config == null || string.IsNullOrEmpty(config.asset)) return "";

        string animName = "idle";
        int frame = 0;
        BattleAnimation anim = BattleAnimationForUnit(unit.id);
        if (anim != null)
        {
            float p = Mathf.Clamp01(anim.elapsed / Mathf.Max(0.01f, anim.duration));
            if (anim.kind == BattleAnimationKind.Move) animName = "move";
            else if (anim.kind == BattleAnimationKind.Attack) animName = "attack";
            else if (anim.kind == BattleAnimationKind.Hit) animName = "hit";
            int count = BattleUnitAnimationFrameCount(config, animName);
            frame = Mathf.Clamp(Mathf.FloorToInt(p * count), 0, Mathf.Max(0, count - 1));
        }
        else
        {
            frame = 0;
        }

        return BattleUnitSpriteResource(config, animName, frame);
    }

    private string BattleUnitSpriteResource(CommonBattleUnitConfig config, string animName, int frame)
    {
        if (config == null || string.IsNullOrEmpty(config.asset)) return "";
        return config.asset + "/" + animName + "_" + frame;
    }

    private Sprite LoadBattleUnitSprite(BattleUnit unit)
    {
        CommonBattleUnitConfig config = CommonUnitForBattleUnit(unit);
        if (config == null) return null;

        string animName = "idle";
        int frame = 0;
        BattleAnimation anim = BattleAnimationForUnit(unit.id);
        if (anim != null)
        {
            float p = Mathf.Clamp01(anim.elapsed / Mathf.Max(0.01f, anim.duration));
            if (anim.kind == BattleAnimationKind.Move) animName = "move";
            else if (anim.kind == BattleAnimationKind.Attack) animName = "attack";
            else if (anim.kind == BattleAnimationKind.Hit) animName = "hit";
            int count = BattleUnitAnimationFrameCount(config, animName);
            frame = Mathf.Clamp(Mathf.FloorToInt(p * count), 0, Mathf.Max(0, count - 1));
        }
        else
        {
            frame = 0;
        }

        return LoadBattleUnitSprite(config, animName, frame);
    }

    private Sprite LoadBattleUnitSprite(CommonBattleUnitConfig config, string animName, int frame)
    {
        Sprite sprite = LoadArtSprite(BattleUnitSpriteResource(config, animName, frame));
        if (sprite != null) return sprite;
        return animName == "idle" ? null : LoadArtSprite(BattleUnitSpriteResource(config, "idle", 0));
    }

    private int BattleUnitAnimationFrameCount(CommonBattleUnitConfig config, string animName)
    {
        if (config == null) return animName == "idle" ? 4 : 6;
        if (animName == "move") return Mathf.Max(1, config.moveFrames);
        if (animName == "attack") return Mathf.Max(1, config.attackFrames);
        if (animName == "hit") return Mathf.Max(1, config.hitFrames);
        return Mathf.Max(1, config.idleFrames);
    }

    private List<BattleRoleDamageRule> BattleRoleDamageRules()
    {
        if (gameConfig.battleRoleDamageRules != null && gameConfig.battleRoleDamageRules.Count > 0) return gameConfig.battleRoleDamageRules;
        return new List<BattleRoleDamageRule>
        {
            new BattleRoleDamageRule { attackerRole = "cavalry", defenderRole = "archer", modifier = 12 },
            new BattleRoleDamageRule { attackerRole = "archer", defenderRole = "cavalry", modifier = -5 },
            new BattleRoleDamageRule { attackerRole = "infantry", defenderRole = "cavalry", modifier = 3 },
            new BattleRoleDamageRule { attackerRole = "heavy_cavalry", defenderRole = "archer", modifier = 14 },
            new BattleRoleDamageRule { attackerRole = "heavy_cavalry", defenderRole = "heavy_archer", modifier = 10 },
            new BattleRoleDamageRule { attackerRole = "heavy_spear", defenderRole = "cavalry", modifier = 14 },
            new BattleRoleDamageRule { attackerRole = "heavy_spear", defenderRole = "heavy_cavalry", modifier = 18 },
            new BattleRoleDamageRule { attackerRole = "musket", defenderRole = "heavy_infantry", modifier = 8 },
            new BattleRoleDamageRule { attackerRole = "musket", defenderRole = "heavy_spear", modifier = 8 },
            new BattleRoleDamageRule { attackerRole = "skirmisher", defenderRole = "artillery", modifier = 10 },
            new BattleRoleDamageRule { attackerRole = "artillery", defenderRole = "heavy_infantry", modifier = 12 },
            new BattleRoleDamageRule { attackerRole = "artillery", defenderRole = "heavy_spear", modifier = 12 },
            new BattleRoleDamageRule { attackerRole = "heavy_archer", defenderRole = "brute", modifier = 7 },
            new BattleRoleDamageRule { attackerRole = "heavy_archer", defenderRole = "heavy_brute", modifier = 5 }
        };
    }

    private List<HealthFactorRule> HealthFactorRules()
    {
        if (gameConfig.healthFactors != null && gameConfig.healthFactors.Count > 0) return gameConfig.healthFactors;
        return new List<HealthFactorRule>
        {
            new HealthFactorRule { minFormation = 3, maxFormation = 99, minHpPercent = 80, numerator = 7, denominator = 7 },
            new HealthFactorRule { minFormation = 3, maxFormation = 99, minHpPercent = 65, numerator = 6, denominator = 7 },
            new HealthFactorRule { minFormation = 3, maxFormation = 99, minHpPercent = 50, numerator = 5, denominator = 7 },
            new HealthFactorRule { minFormation = 3, maxFormation = 99, minHpPercent = 30, numerator = 4, denominator = 7 },
            new HealthFactorRule { minFormation = 3, maxFormation = 99, minHpPercent = 15, numerator = 3, denominator = 7 },
            new HealthFactorRule { minFormation = 3, maxFormation = 99, minHpPercent = 5, numerator = 2, denominator = 7 },
            new HealthFactorRule { minFormation = 3, maxFormation = 99, minHpPercent = 0, numerator = 1, denominator = 7 },
            new HealthFactorRule { minFormation = 2, maxFormation = 2, minHpPercent = 65, numerator = 6, denominator = 6 },
            new HealthFactorRule { minFormation = 2, maxFormation = 2, minHpPercent = 50, numerator = 5, denominator = 6 },
            new HealthFactorRule { minFormation = 2, maxFormation = 2, minHpPercent = 30, numerator = 4, denominator = 6 },
            new HealthFactorRule { minFormation = 2, maxFormation = 2, minHpPercent = 15, numerator = 3, denominator = 6 },
            new HealthFactorRule { minFormation = 2, maxFormation = 2, minHpPercent = 5, numerator = 2, denominator = 6 },
            new HealthFactorRule { minFormation = 2, maxFormation = 2, minHpPercent = 0, numerator = 1, denominator = 6 },
            new HealthFactorRule { minFormation = 0, maxFormation = 1, minHpPercent = 50, numerator = 5, denominator = 5 },
            new HealthFactorRule { minFormation = 0, maxFormation = 1, minHpPercent = 30, numerator = 4, denominator = 5 },
            new HealthFactorRule { minFormation = 0, maxFormation = 1, minHpPercent = 15, numerator = 3, denominator = 5 },
            new HealthFactorRule { minFormation = 0, maxFormation = 1, minHpPercent = 5, numerator = 2, denominator = 5 },
            new HealthFactorRule { minFormation = 0, maxFormation = 1, minHpPercent = 0, numerator = 1, denominator = 5 }
        };
    }

    private List<BattleTerrainTileConfig> BattleTerrainTiles()
    {
        if (battleTerrainOverride != null) return battleTerrainOverride;
        if (gameConfig.battleTerrainTiles != null && gameConfig.battleTerrainTiles.Count > 0) return gameConfig.battleTerrainTiles;
        List<BattleTerrainTileConfig> tiles = new List<BattleTerrainTileConfig>();
        int cols = BattleHexCols();
        int rows = BattleHexRows();
        for (int r = 0; r < rows; r++)
        {
            for (int q = 0; q < cols; q++)
            {
                string terrain = "plain";
                if (q == BattleObjectiveQ() && r == BattleObjectiveR()) terrain = "city";
                else if ((q == 2 && r == 1) || (q == 6 && r == 4) || (q == 7 && r == 2)) terrain = "mountain";
                else if ((q + r) % 5 == 0 || (q == 1 && r == 4) || (q == 5 && r == 2)) terrain = "forest";
                else if ((r == 5 && q > 1 && q < 8) || (q == 3 && r == 3)) terrain = "river";
                tiles.Add(new BattleTerrainTileConfig { q = q, r = r, terrain = terrain });
            }
        }
        return tiles;
    }

    private int BattleHexCols()
    {
        if (UseBattleLabLayout())
        {
            int cols = battleLabDesign.hexCols <= 0 ? BattleCore().hexCols : battleLabDesign.hexCols;
            return Mathf.Clamp(cols, BattleLabMinCols(), BattleLabMaxCols());
        }
        return Mathf.Max(1, BattleCore().hexCols);
    }

    private int BattleHexRows()
    {
        if (UseBattleLabLayout())
        {
            int rows = battleLabDesign.hexRows <= 0 ? BattleCore().hexRows : battleLabDesign.hexRows;
            return Mathf.Clamp(rows, BattleLabMinRows(), BattleLabMaxRows());
        }
        return Mathf.Max(1, BattleCore().hexRows);
    }

    private int BattleObjectiveQ()
    {
        if (UseBattleLabObjective()) return Mathf.Clamp(battleLabDesign.objectiveQ, 0, BattleHexCols() - 1);
        return Mathf.Clamp(BattleCore().objectiveQ, 0, BattleHexCols() - 1);
    }

    private int BattleObjectiveR()
    {
        if (UseBattleLabObjective()) return Mathf.Clamp(battleLabDesign.objectiveR, 0, BattleHexRows() - 1);
        return Mathf.Clamp(BattleCore().objectiveR, 0, BattleHexRows() - 1);
    }

    private bool UseBattleLabObjective()
    {
        return UseBattleLabLayout();
    }

    private bool UseBattleLabLayout()
    {
        return battleLabDesign != null && (mode == ScreenMode.BattleLab || (battle != null && !battle.fromStrategy));
    }

    private int BattleLabMinCols()
    {
        return 7;
    }

    private int BattleLabMaxCols()
    {
        return 13;
    }

    private int BattleLabMinRows()
    {
        return 6;
    }

    private int BattleLabMaxRows()
    {
        return 10;
    }

    private int RandomRangeInt(int minInclusive, int maxExclusive)
    {
        return UnityEngine.Random.Range(minInclusive, Mathf.Max(minInclusive + 1, maxExclusive));
    }

    private void ShowNewspaperMenu()
    {
        pendingStoryTitle = T("newspaper.title", "报纸刊物");
        pendingStorySceneId = "library";
        pendingStoryPortraitName = "";
        List<NewsArticle> unlocked = NewsCatalog().Where(n => n.unlockWeek <= CurrentCalendarWeek()).OrderByDescending(n => n.unlockWeek).Take(4).ToList();
        pendingStoryBody = T("newspaper.body", "按日期解锁的短文章。不同刊物有自己的立场，读完不会直接给出标准答案。");
        pendingStoryOptions = unlocked.Select<NewsArticle, Tuple<string, Action>>(article => Tuple.Create(article.title, (Action)(() => ShowNewsArticle(article)))).ToList();
        if (pendingStoryOptions.Count == 0)
        {
            pendingStoryOptions.Add(Tuple.Create(T("newspaper.empty", "暂无可读刊物"), (Action)ShowAcademy));
        }
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void ShowNewsArticle(NewsArticle article)
    {
        pendingStoryTitle = article.source;
        pendingStorySceneId = "library";
        pendingStoryPortraitName = "";
        pendingStoryBody = TF("newspaper.article_body", "{0}\n\n立场倾向：{1}\n\n{2}", article.title, article.stanceHint, article.body);
        pendingStoryOptions = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("newspaper.option_clip", "记入摘录"), (Action)(() =>
            {
                AddLog(TF("log.newspaper_clip", "摘录：{0}。", article.title));
                player.mood = Mathf.Clamp(player.mood + 1, 0, 100);
                ShowAcademy();
            })),
            Tuple.Create(T("newspaper.option_continue", "继续读报"), (Action)ShowNewspaperMenu)
        };
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void ShowCampusActivity()
    {
        pendingStoryTitle = T("campus_activity.title", "周末活动");
        pendingStorySceneId = "academy";
        pendingStoryPortraitName = player.name;
        pendingStoryBody = T("campus_activity.body", "选择一项活动。战术演练偏战斗，沙龙偏社交，讲座偏立场，救济偏道德与关系。");
        pendingStoryOptions = ActivityCatalog().Select<CampusActivity, Tuple<string, Action>>(activity => Tuple.Create(activity.name, (Action)(() => RunCampusActivity(activity)))).ToList();
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void RunCampusActivity(CampusActivity activity)
    {
        player.mood = Mathf.Clamp(player.mood + activity.moodDelta, 0, 100);
        player.merit += activity.meritDelta;
        player.treasury = Mathf.Max(0, player.treasury + activity.treasuryDelta);
        if (activity.trainingGain > 0)
        {
            int gain = ApplyCultivationGain(activity.trainingGain);
            player.trainingExp += gain;
            AddLog(TF("log.activity_training", "{0}：训练修习进度 +{1}。", activity.name, gain));
        }
        if (activity.socialGain > 0)
        {
            int gain = ApplySocialGain(activity.socialGain);
            foreach (Relationship rel in relationships) GainRelationship(rel, gain);
            AddLog(TF("log.activity_social", "{0}：朋友圈好感 +{1}。", activity.name, gain));
        }
        if (!string.IsNullOrEmpty(activity.axisId))
        {
            AdjustIdeologyAxis(activity.axisId, activity.axisDelta);
        }
        UpdatePlayerRank();
        AdvanceWeek();
        ShowPostWeekNarrative("activity", activity.id, ScreenMode.Academy, ShowAcademy);
    }

    private void ShowIdeologyPanel()
    {
        pendingStoryTitle = T("ideology.title", "立场偏向");
        pendingStorySceneId = "council";
        pendingStoryPortraitName = player.name;
        pendingStoryBody = string.Join("\n", IdeologyAxes().Select(axis => axis.label + "：" + AxisLabel(axis.negativeLabel, axis.positiveLabel, AxisValue(axis.id))).ToArray()) +
            "\n\n" + TF("ideology.strength_hint", "绝对值代表信念强度：{0}。", BeliefLevel(MaxIdeologyAbs()));
        pendingStoryOptions = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("ideology.option_politics", "参加时事讲座"), (Action)ShowPoliticsEvent)
        };
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void ShowRelationshipDetail(string id)
    {
        Relationship rel = relationships.FirstOrDefault(r => r.id == id || r.name == id);
        if (rel == null)
        {
            ShowAcademy();
            return;
        }
        rel.knownLevel = Mathf.Max(rel.knownLevel, RelationshipKnownLevel(rel.affection));
        string note = rel.knownLevel >= 2 ? rel.note : T("relationship.locked_note", "了解尚浅。继续邀约或共同活动可解锁更多词条。");
        string battle = rel.affection >= 75 ? T("relationship.battle_support_high", "战场：作为友军时会优先支援主角。") :
            rel.affection >= 50 ? T("relationship.battle_support_mid", "战场：作为友军时更愿意靠近主角。") :
            rel.affection <= -31 ? T("relationship.battle_enemy", "战场：作为敌军时更可能追击主角。") : T("relationship.battle_none", "战场：暂无特殊倾向。");
        pendingStoryTitle = rel.name;
        pendingStorySceneId = "academy";
        pendingStoryPortraitName = rel.name;
        pendingStoryBody = TF("relationship.detail_body", "朋友圈：{0}\n立场：{1}\n好感：{2}（{3}）\n\n{4}\n\n{5}", SafeText(rel.circle, T("relationship.no_circle", "未加入")), rel.stance, rel.affection, RelationshipLevel(rel.affection), note, battle);
        pendingStoryOptions = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("relationship.option_invite", "邀约"), (Action)(() =>
            {
                int gain = ApplySocialGain(AcademyCore().inviteGain);
                GainRelationship(rel, gain);
                player.mood = Mathf.Clamp(player.mood + AcademyCore().inviteMoodGain, 0, 100);
                AddLog(TF("log.invite_one", "你邀约了{0}，好感 +{1}。", rel.name, gain));
                AdvanceWeek();
                ShowPostWeekNarrative("social", rel.id, ScreenMode.Academy, ShowAcademy);
            }))
        };
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void ShowAcademy()
    {
        mode = ScreenMode.Academy;
        pendingStoryReturnAction = null;
        battleTerrainOverride = null;
        RemoveBattleLabTempArmies();
        Clear();
        DrawSceneBackground("academy");
        AddTopBar(root, T("academy.title", "新京军事学院"));
        DrawAcademyDashboard();
        DrawSystemDock(root, ScreenMode.Academy);
    }

    private void DrawAcademyDashboard()
    {
        RectTransform profile = CreateUiPanel("AcademyProfilePanel", root, new Vector2(-470, -8), new Vector2(290, 536));
        DrawAcademyProfileColumn(profile);

        RectTransform schedule = CreateUiPanel("AcademySchedulePanel", root, new Vector2(-74, -8), new Vector2(506, 536));
        DrawAcademyScheduleColumn(schedule);

        RectTransform inspector = CreateUiPanel("AcademyInspectorPanel", root, new Vector2(414, -8), new Vector2(350, 536));
        DrawAcademyInspectorColumn(inspector);
    }

    private void DrawAcademyProfileColumn(Transform profile)
    {
        AddSectionTitle(profile, T("academy.section_profile", "学籍档案"), new Vector2(-104, 246), new Vector2(220, 28));
        AddPortrait(profile, player.name, new Vector2(0, 118), new Vector2(142, 190), true);

        RectTransform nameBand = CreateRect("AcademyProfileNameBand", profile, new Vector2(0, -2), new Vector2(224, 48), new Color(0.92f, 0.80f, 0.55f, 0.42f));
        AddText(nameBand, player.name, new Vector2(0, 10), new Vector2(204, 22), 18, TextAnchor.MiddleCenter, highlightColor);
        AddText(nameBand, player.title, new Vector2(0, -11), new Vector2(204, 18), 12, TextAnchor.MiddleCenter, muted);

        string traits = TraitNames(player.traits);
        if (traits.Length > 18) traits = traits.Substring(0, 18) + "...";
        Text brief = AddText(profile, TF("academy.profile_brief_clean", "{0}岁  {1}年级  第{2}周\n军衔：{3}    特性：{4}", player.age, player.year, player.week, CurrentMilitaryRank(), traits), new Vector2(0, -47), new Vector2(232, 38), 12, TextAnchor.UpperLeft, muted);
        brief.lineSpacing = 0.9f;
        brief.verticalOverflow = VerticalWrapMode.Truncate;

        DrawCompactAcademyAttributeBars(profile);

        AddFlatButton(profile, T("button.character", "角色"), new Vector2(-62, -244), new Vector2(104, 28), () => ShowCharacterStatusPanel(ScreenMode.Academy), new Color(0.43f, 0.58f, 0.48f, 0.96f), 13);
        AddFlatButton(profile, T("button.attribute_guide", "属性说明"), new Vector2(62, -244), new Vector2(104, 28), () => ShowAttributeGuide(ScreenMode.Academy), new Color(0.72f, 0.57f, 0.28f, 0.96f), 13);
    }

    private void DrawAcademyScheduleColumn(Transform schedule)
    {
        AddSectionTitle(schedule, T("academy.section_schedule", "本周安排"), new Vector2(-208, 246), new Vector2(360, 30));
        DrawCurrentGoalCard(schedule, new Vector2(0, 196), new Vector2(440, 84));

        AddText(schedule, T("academy.group_courses", "课程训练"), new Vector2(-202, 134), new Vector2(160, 24), 15, TextAnchor.MiddleLeft, highlightColor);
        List<CourseConfig> courses = CourseCatalog().Where(c => c.target != "social").Take(6).ToList();
        for (int i = 0; i < courses.Count; i++)
        {
            int index = i;
            AddFlatButton(schedule, courses[i].label, new Vector2(-112 + (i % 2) * 224, 102 - (i / 2) * 38), new Vector2(194, 30), () => RunAcademyAction(courses[index].label), new Color(0.38f, 0.52f, 0.64f, 0.96f), 14);
        }

        AddText(schedule, T("academy.group_weekend", "休整与社交"), new Vector2(-202, -28), new Vector2(160, 24), 15, TextAnchor.MiddleLeft, highlightColor);
        AddFlatButton(schedule, T("button.rest", "休息"), new Vector2(-166, -62), new Vector2(130, 30), () => RunSundayAction("rest"), new Color(0.43f, 0.58f, 0.48f, 0.96f), 14);
        AddFlatButton(schedule, T("button.study", "自习"), new Vector2(-28, -62), new Vector2(130, 30), () => RunSundayAction("study"), new Color(0.43f, 0.58f, 0.48f, 0.96f), 14);
        AddFlatButton(schedule, T("button.invite", "邀约同窗"), new Vector2(110, -62), new Vector2(130, 30), ShowInviteEvent, new Color(0.38f, 0.52f, 0.64f, 0.96f), 14);
        AddFlatButton(schedule, T("button.campus_activity", "周末活动"), new Vector2(-98, -100), new Vector2(194, 30), ShowCampusActivity, new Color(0.38f, 0.52f, 0.64f, 0.96f), 14);
        AddFlatButton(schedule, T("button.battle_lab", "战棋工坊"), new Vector2(126, -100), new Vector2(194, 30), ShowBattleLabEditor, new Color(0.52f, 0.42f, 0.62f, 0.96f), 14);

        RectTransform logCard = CreateRect("AcademyLogCard", schedule, new Vector2(0, -194), new Vector2(440, 114), new Color(0.96f, 0.88f, 0.68f, 0.48f));
        AddText(logCard, T("label.log", "日志："), new Vector2(-198, 42), new Vector2(120, 20), 14, TextAnchor.MiddleLeft, highlightColor);
        Text logText = AddText(logCard, LatestLog(4), new Vector2(0, -10), new Vector2(398, 82), 12, TextAnchor.UpperLeft, muted);
        logText.lineSpacing = 0.9f;
        logText.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void DrawAcademyInspectorColumn(Transform inspector)
    {
        AddSectionTitle(inspector, T("academy.section_action_preview", "行动预览"), new Vector2(-144, 246), new Vector2(292, 30));

        RectTransform preview = CreateRect("AcademyPreviewBand", inspector, new Vector2(0, 184), new Vector2(292, 92), new Color(0.96f, 0.88f, 0.68f, 0.48f));
        AddText(preview, T("academy.preview_main_check", "主线检定"), new Vector2(-126, 34), new Vector2(120, 20), 14, TextAnchor.MiddleLeft, highlightColor);
        Text previewText = AddText(preview, RecommendedActionSummary() + "\n" + SecretDossierSummary(), new Vector2(0, -8), new Vector2(254, 58), 12, TextAnchor.UpperLeft, muted);
        previewText.lineSpacing = 0.9f;
        previewText.verticalOverflow = VerticalWrapMode.Truncate;

        AddButton(inspector, T("button.continue_main_story", "继续主线"), new Vector2(-78, 120), new Vector2(136, 32), () => StartStory(currentMainEventId, ScreenMode.Academy), new Color(0.43f, 0.58f, 0.48f, 0.96f));
        AddButton(inspector, T("button.strategy_short", "战略"), new Vector2(78, 120), new Vector2(136, 32), ShowStrategy, new Color(0.38f, 0.52f, 0.64f, 0.96f));

        AddText(inspector, T("label.relationships", "同窗关系"), new Vector2(-144, 78), new Vector2(292, 24), 15, TextAnchor.MiddleLeft, highlightColor);
        int visibleRelationships = Mathf.Min(relationships.Count, 3);
        for (int i = 0; i < visibleRelationships; i++)
        {
            Relationship rel = relationships[i];
            string itemLabel = TF("relationship.button_label_compact", "{0}  {1}  好感{2}", rel.name, RelationshipLevel(rel.affection), rel.affection);
            AddFlatButton(inspector, itemLabel, new Vector2(0, 46 - i * 36), new Vector2(292, 28), () => ShowRelationshipDetail(rel.id), i % 2 == 0 ? new Color(0.38f, 0.52f, 0.64f, 0.96f) : new Color(0.43f, 0.58f, 0.48f, 0.96f), 12, TextAnchor.MiddleLeft);
        }

        AddFlatButton(inspector, T("relationship.more_button", "更多人物：角色档案"), new Vector2(0, -70), new Vector2(292, 28), () =>
        {
            storyReturnMode = ScreenMode.Academy;
            ShowCharacterArchive();
        }, new Color(0.72f, 0.57f, 0.28f, 0.96f), 12);

        AddText(inspector, T("academy.section_events", "事件入口"), new Vector2(-144, -112), new Vector2(292, 24), 15, TextAnchor.MiddleLeft, highlightColor);
        Text dossier = AddText(inspector, SecretDossierSummary(), new Vector2(0, -136), new Vector2(292, 22), 12, TextAnchor.MiddleLeft, muted);
        dossier.verticalOverflow = VerticalWrapMode.Truncate;
        AddFlatButton(inspector, T("button.newspaper", "报纸"), new Vector2(-78, -170), new Vector2(136, 28), ShowNewspaperMenu, new Color(0.72f, 0.57f, 0.28f, 0.96f), 13);
        AddFlatButton(inspector, T("button.ideology", "立场"), new Vector2(78, -170), new Vector2(136, 28), ShowIdeologyPanel, new Color(0.72f, 0.57f, 0.28f, 0.96f), 13);
        AddFlatButton(inspector, T("button.story_menu", "剧情目录"), new Vector2(-78, -206), new Vector2(136, 28), () =>
        {
            storyReturnMode = ScreenMode.Academy;
            ShowStoryMenu();
        }, new Color(0.52f, 0.42f, 0.62f, 0.96f), 13);
        AddFlatButton(inspector, T("button.character_archive", "角色档案"), new Vector2(78, -206), new Vector2(136, 28), () =>
        {
            storyReturnMode = ScreenMode.Academy;
            ShowCharacterArchive();
        }, new Color(0.52f, 0.42f, 0.62f, 0.96f), 13);
    }

    private void DrawCurrentGoalCard(Transform parent, Vector2 pos, Vector2 size)
    {
        RectTransform goal = CreateRect("CurrentGoalCard", parent, pos, size, new Color(0.96f, 0.88f, 0.68f, 0.48f));
        AddText(goal, T("goal.current_title", "当前目标"), new Vector2(-size.x * 0.5f + 62f, size.y * 0.5f - 18f), new Vector2(120, 24), 16, TextAnchor.MiddleLeft, highlightColor);
        AddText(goal, CurrentGoalSummary() + "\n" + RecommendedActionSummary(), new Vector2(8, -10), new Vector2(size.x - 34f, size.y - 32f), 13, TextAnchor.UpperLeft, muted);
    }

    private string CurrentGoalSummary()
    {
        RefreshProgressionSystems(false);
        QuestConfig quest = QuestCatalog()
            .Where(q => player.activeQuests.Contains(q.id))
            .OrderBy(q => q.type == "主线" ? 0 : 1)
            .ThenBy(q => q.id)
            .FirstOrDefault();
        if (quest == null)
        {
            return T("goal.no_active", "暂无追踪任务。推进主线或完成一周行动来解锁新目标。");
        }
        int target = Mathf.Max(1, quest.targetValue);
        int progress = Mathf.Clamp(QuestProgress(quest), 0, target);
        return TF("goal.quest_progress", "{0}：{1}  {2}/{3}", SafeText(quest.type, T("quest.type_default", "任务")), quest.name, progress, target);
    }

    private string RecommendedActionSummary()
    {
        QuestConfig quest = QuestCatalog()
            .Where(q => player.activeQuests.Contains(q.id))
            .OrderBy(q => q.type == "主线" ? 0 : 1)
            .ThenBy(q => q.id)
            .FirstOrDefault();
        if (player.stamina <= 25) return T("goal.recommend_rest", "推荐：体力偏低，先休息，避免课程收益下降。");
        if (quest == null) return T("goal.recommend_story", "推荐：查看主线或周末活动，寻找新的推进点。");
        if (quest.targetKind == "battleWins" || quest.targetKind == "battlesFought" || quest.targetKind == "enemiesDefeated")
        {
            return T("goal.recommend_strategy", "推荐：进入战略地图，挑选相邻敌方省份开战。");
        }
        if (quest.targetKind == "relationship")
        {
            return T("goal.recommend_relationship", "推荐：使用邀约同窗或周末活动提升好感。");
        }
        if (quest.targetKind == "stance")
        {
            return T("goal.recommend_ideology", "推荐：参加公共讲座或剧情选择来调整立场。");
        }
        if (quest.targetKind == "intelligence" || quest.targetKind == "spyNetwork")
        {
            return T("goal.recommend_intel", "推荐：打开情报入口，执行低风险侦察行动。");
        }
        string course = CourseLabelForTarget(quest.targetKind);
        if (!string.IsNullOrEmpty(course))
        {
            return TF("goal.recommend_course", "推荐：优先安排「{0}」。", course);
        }
        return T("goal.recommend_general", "推荐：选择一项课程推进本周，保持成长节奏。");
    }

    private string CourseLabelForTarget(string target)
    {
        CourseConfig course = CourseCatalog().FirstOrDefault(c => c.target == target || c.id == target);
        return course != null ? course.label : "";
    }

    private string AcademySummary()
    {
        RefreshProgressionSystems(false);
        return TF("academy.summary",
            "姓名：{0}  年龄：{1}  {2}\n{3}  称号：{4}\n心情：{5}({6})  体力：{7}  战功：{8}\n军衔：{9}  国库：{10}  情报：{11}\n特性：{12}",
            player.name, player.age, CalendarLabel(), player.year + "年级 第" + player.week + "周", player.title,
            player.mood, MoodLabel(player.mood), player.stamina, player.merit, CurrentMilitaryRank(), player.treasury, player.intelligence,
            TraitNames(player.traits));
    }

    private string AcademyProfileBrief()
    {
        string traits = TraitNames(player.traits);
        if (traits.Length > 20) traits = traits.Substring(0, 20) + "...";
        return TF("academy.profile_brief", "{0}  {1}岁\n{2}  第{3}周\n{4}  战功{5}\n特性：{6}",
            player.name, player.age, player.year + "年级", player.week, CurrentMilitaryRank(), player.merit, traits);
    }

    private void DrawSystemDock(Transform parent, ScreenMode returnMode)
    {
        if (returnMode == ScreenMode.Academy)
        {
            RectTransform compactDock = CreateSpriteRect("SystemDockCompact", parent, new Vector2(0, -322), new Vector2(720, 52), "Art/UI/topbar_clean_paper", panel, true, false, new Vector4(28, 28, 28, 28));
            Vector2 compactButtonSize = new Vector2(118, 20);
            float compactStartX = -180f;
            float compactGap = 126f;
            AddFlatButton(compactDock, T("button.map_tab", "地图"), new Vector2(compactStartX + compactGap * 0, 10), compactButtonSize, () => ReturnToMode(returnMode), null, 11);
            AddFlatButton(compactDock, T("button.formation", "编队"), new Vector2(compactStartX + compactGap * 1, 10), compactButtonSize, () => ShowFormationPanel(returnMode), null, 11);
            AddFlatButton(compactDock, T("button.character", "角色"), new Vector2(compactStartX + compactGap * 2, 10), compactButtonSize, () => ShowCharacterStatusPanel(returnMode), null, 11);
            AddFlatButton(compactDock, T("button.skills", "技能"), new Vector2(compactStartX + compactGap * 3, 10), compactButtonSize, () => ShowSkillPanel(returnMode), null, 11);
            AddFlatButton(compactDock, T("button.quests", "任务"), new Vector2(compactStartX + compactGap * 0, -10), compactButtonSize, () => ShowQuestLog(returnMode), null, 11);
            AddFlatButton(compactDock, T("button.diplomacy", "外交"), new Vector2(compactStartX + compactGap * 1, -10), compactButtonSize, () => ShowDiplomacyPanel(returnMode), null, 11);
            AddFlatButton(compactDock, T("button.intelligence", "情报"), new Vector2(compactStartX + compactGap * 2, -10), compactButtonSize, () => ShowIntelligencePanel(returnMode), new Color(0.38f, 0.52f, 0.64f, 0.96f), 11);
            AddFlatButton(compactDock, T("button.settings", "设置"), new Vector2(compactStartX + compactGap * 3, -10), compactButtonSize, () => ShowSettingsPanel(returnMode), null, 11);
            return;
        }

        RectTransform dock = CreateUiPanel("SystemDock", parent, new Vector2(0, -336), new Vector2(1040, 46));
        Vector2 size = new Vector2(92, 30);
        float startX = -424f;
        float gap = 112f;
        AddButton(dock, T("button.map_tab", "地图"), new Vector2(startX + gap * 0, 0), size, () => ReturnToMode(returnMode));
        AddButton(dock, T("button.formation", "编队"), new Vector2(startX + gap * 1, 0), size, () => ShowFormationPanel(returnMode));
        AddButton(dock, T("button.character", "角色"), new Vector2(startX + gap * 2, 0), size, () => ShowCharacterStatusPanel(returnMode));
        AddButton(dock, T("button.skills", "技能"), new Vector2(startX + gap * 3, 0), size, () => ShowSkillPanel(returnMode));
        AddButton(dock, T("button.quests", "任务"), new Vector2(startX + gap * 4, 0), size, () => ShowQuestLog(returnMode));
        AddButton(dock, T("button.diplomacy", "外交"), new Vector2(startX + gap * 5, 0), size, () => ShowDiplomacyPanel(returnMode));
        AddButton(dock, T("button.intelligence", "情报"), new Vector2(startX + gap * 6, 0), size, () => ShowIntelligencePanel(returnMode), new Color(0.18f, 0.24f, 0.38f));
        AddButton(dock, T("button.settings", "设置"), new Vector2(startX + gap * 7, 0), size, () => ShowSettingsPanel(returnMode));
    }

    private void DrawRelationships(Transform parent)
    {
        RectTransform relPanel = CreateUiPanel("RelationshipPanel", parent, new Vector2(405, 108), new Vector2(350, 374));
        AddSectionTitle(relPanel, T("label.relationships", "同窗关系"), new Vector2(-144, 158), new Vector2(292, 30));
        AddText(relPanel, T("relationship.panel_hint", "点击人物查看档案和邀约。"), new Vector2(0, 130), new Vector2(292, 22), 12, TextAnchor.MiddleLeft, muted);
        int visibleRelationships = Mathf.Min(relationships.Count, 5);
        for (int i = 0; i < visibleRelationships; i++)
        {
            Relationship rel = relationships[i];
            string itemLabel = TF("relationship.button_label", "{0}  {1}\n{2}  好感 {3}", rel.name, RelationshipLevel(rel.affection), SafeText(rel.circle, rel.stance), rel.affection);
            Color itemColor = i % 2 == 0 ? new Color(0.15f, 0.17f, 0.21f, 0.96f) : new Color(0.13f, 0.15f, 0.18f, 0.96f);
            AddFlatButton(relPanel, itemLabel, new Vector2(0, 91 - i * 46), new Vector2(292, 38), () => ShowRelationshipDetail(rel.id), itemColor, 13);
        }
        if (relationships.Count > visibleRelationships)
        {
            AddFlatButton(relPanel, T("relationship.more_button", "更多人物：角色档案"), new Vector2(0, -154), new Vector2(292, 28), () =>
            {
                storyReturnMode = ScreenMode.Academy;
                ShowCharacterArchive();
            }, new Color(0.18f, 0.15f, 0.12f, 0.96f), 12);
        }
        return;
        #if false
        AddText(parent, T("label.relationships", "同窗关系"), new Vector2(-20, 244), new Vector2(300, 34), 24, TextAnchor.MiddleLeft);
        for (int i = 0; i < relationships.Count; i++)
        {
            Relationship rel = relationships[i];
            string label = TF("relationship.button_label", "{0}  {1}\n{2}  好感 {3}", rel.name, RelationshipLevel(rel.affection), SafeText(rel.circle, rel.stance), rel.affection);
            AddButton(parent, label, new Vector2(160 + (i % 2) * 205, 192 - (i / 2) * 62), new Vector2(190, 50), () => ShowRelationshipDetail(rel.id), i % 2 == 0 ? panel2 : new Color(0.18f, 0.20f, 0.24f));
        }
        #endif
    }

    private void ShowAttributeGuide(ScreenMode returnMode)
    {
        string body = T("attribute_guide.body",
            "步兵：提升步兵、长枪、重步等近战单位的伤害稳定性。\n骑兵：提升骑兵、重骑的冲击伤害和机动价值。\n炮兵：提升火枪、弓兵、火炮等远程单位的输出。\n管理：影响国库、任务处理和学院评价。\n后勤：影响补给恢复、行军续航和缺补给惩罚。\n训练：影响情报行动成功率与综合战斗准备。");
        OpenSystemPopup(T("attribute_guide.title", "属性影响说明"), body, new List<Tuple<string, Action>>(), returnMode, "library");
    }

    private string SafeText(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private int CurrentCalendarWeek()
    {
        int maxWeek = gameConfig.calendar != null && gameConfig.calendar.maxWeek > 0 ? gameConfig.calendar.maxWeek : 52;
        return Mathf.Max(1, (player.year - 1) * maxWeek + player.week);
    }

    private string CalendarLabel()
    {
        if (IsExamWeek()) return T("calendar.exam_week", "校考周");
        if (IsHolidayWeek()) return T("calendar.holiday", "假期");
        return T("calendar.normal", "校历");
    }

    private bool IsExamWeek()
    {
        return WeekInConfigList(gameConfig.calendar != null ? gameConfig.calendar.examWeeks : "", player.week, new[] { 25, 50 });
    }

    private bool IsHolidayWeek()
    {
        return WeekInConfigList(gameConfig.calendar != null ? gameConfig.calendar.holidayWeeks : "", player.week, new[] { 26, 27, 51, 52 });
    }

    private bool WeekInConfigList(string values, int week, int[] fallback)
    {
        List<int> weeks = SplitConfigList(values).Select(v => int.TryParse(v, out int parsed) ? parsed : -1).Where(v => v > 0).ToList();
        if (weeks.Count == 0) weeks = fallback.ToList();
        return weeks.Contains(week);
    }

    private string MoodLabel(int mood)
    {
        MoodRule rule = MoodRuleFor(mood);
        return rule != null && !string.IsNullOrEmpty(rule.label) ? rule.label : "一般";
    }

    private int MoodStudyMin(int mood)
    {
        MoodRule rule = MoodRuleFor(mood);
        return rule != null ? Mathf.Max(1, rule.studyMin) : 1;
    }

    private int MoodStudyMax(int mood)
    {
        MoodRule rule = MoodRuleFor(mood);
        return rule != null ? Mathf.Max(MoodStudyMin(mood), rule.studyMax) : 2;
    }

    private MoodRule MoodRuleFor(int mood)
    {
        if (gameConfig.moodRules != null && gameConfig.moodRules.Count > 0)
        {
            MoodRule rule = gameConfig.moodRules.FirstOrDefault(r => mood >= r.minMood && mood <= r.maxMood);
            if (rule != null) return rule;
        }
        if (mood >= 90) return new MoodRule { label = "超好", studyMin = 3, studyMax = 5 };
        if (mood >= 75) return new MoodRule { label = "好", studyMin = 2, studyMax = 5 };
        if (mood >= 50) return new MoodRule { label = "一般", studyMin = 2, studyMax = 4 };
        if (mood >= 30) return new MoodRule { label = "低落", studyMin = 1, studyMax = 4 };
        if (mood >= 15) return new MoodRule { label = "难过", studyMin = 1, studyMax = 3 };
        return new MoodRule { label = "痛苦", studyMin = 1, studyMax = 2 };
    }

    private string RelationshipLevel(int affection)
    {
        RelationshipLevelRule rule = RelationshipLevelRules().OrderByDescending(r => r.minAffection).FirstOrDefault(r => affection >= r.minAffection);
        return rule != null && !string.IsNullOrEmpty(rule.label) ? rule.label : T("relationship.level_default", "冷漠");
    }

    private int RelationshipKnownLevel(int affection)
    {
        RelationshipLevelRule rule = RelationshipLevelRules().OrderByDescending(r => r.minAffection).FirstOrDefault(r => affection >= r.minAffection);
        return rule != null ? Mathf.Max(1, rule.knownLevel) : 1;
    }

    private void GainRelationship(Relationship rel, int delta)
    {
        if (rel == null) return;
        rel.affection = Mathf.Clamp(rel.affection + delta, -100, 100);
        if (delta > 0) rel.lastInteractionWeek = CurrentCalendarWeek();
        int known = RelationshipKnownLevel(rel.affection);
        if (known > rel.knownLevel)
        {
            rel.knownLevel = known;
            AddLog(TF("log.relationship_unlock", "{0}解锁了新的角色词条。", rel.name));
        }
    }

    private void ApplyRelationshipDecay()
    {
        int now = CurrentCalendarWeek();
        List<string> cooled = new List<string>();
        foreach (Relationship rel in relationships)
        {
            if (rel == null) continue;
            if (rel.lastInteractionWeek <= 0)
            {
                rel.lastInteractionWeek = now;
                continue;
            }
            int idleWeeks = now - rel.lastInteractionWeek;
            int decay = idleWeeks >= 8 ? 3 : idleWeeks >= 4 ? 1 : 0;
            if (decay <= 0) continue;
            int before = rel.affection;
            rel.affection = Mathf.Clamp(rel.affection - decay, -100, 100);
            rel.lastInteractionWeek = now;
            if (before != rel.affection) cooled.Add(rel.name);
        }
        if (cooled.Count > 0)
        {
            AddLog(TF("log.relationship_decay", "久未联络：{0}的好感略有下降。", string.Join(T("common.list_separator", "、"), cooled.Take(3).ToArray())));
        }
    }

    private void AdjustIdeologyAxis(string axisId, int delta)
    {
        if (axisId == "nation") player.nationAxis = Mathf.Clamp(player.nationAxis + delta, -100, 100);
        else if (axisId == "class") player.classAxis = Mathf.Clamp(player.classAxis + delta, -100, 100);
        else if (axisId == "governance") player.governanceAxis = Mathf.Clamp(player.governanceAxis + delta, -100, 100);
        else if (axisId == "region") player.regionAxis = Mathf.Clamp(player.regionAxis + delta, -100, 100);
    }

    private string AxisLabel(string negative, string positive, int value)
    {
        string side = value < 0 ? negative : value > 0 ? positive : T("common.neutral", "中立");
        return TF("ideology.axis_label", "{0} {1}（{2}）", side, Mathf.Abs(value), BeliefLevel(Mathf.Abs(value)));
    }

    private int AxisValue(string axisId)
    {
        if (axisId == "nation") return player.nationAxis;
        if (axisId == "class") return player.classAxis;
        if (axisId == "governance") return player.governanceAxis;
        if (axisId == "region") return player.regionAxis;
        return 0;
    }

    private string BeliefLevel(int absValue)
    {
        BeliefLevelRule rule = BeliefLevelRules().OrderByDescending(r => r.minAbsValue).FirstOrDefault(r => absValue >= r.minAbsValue);
        return rule != null && !string.IsNullOrEmpty(rule.label) ? rule.label : T("common.neutral", "中立");
    }

    private int MaxIdeologyAbs()
    {
        int max = Mathf.Max(Mathf.Abs(player.nationAxis), Mathf.Abs(player.classAxis));
        max = Mathf.Max(max, Mathf.Abs(player.governanceAxis));
        return Mathf.Max(max, Mathf.Abs(player.regionAxis));
    }

    private string CurrentMilitaryRank()
    {
        int merit = player.merit;
        RankRule rank = RankForMerit(merit);
        return rank != null && !string.IsNullOrEmpty(rank.name) ? rank.name : "士";
    }

    private int CommandLimit()
    {
        RankRule rank = RankForMerit(player.merit);
        return rank != null ? Mathf.Max(1, rank.commandLimit) : 2;
    }

    private RankRule RankForMerit(int merit)
    {
        if (gameConfig.ranks != null && gameConfig.ranks.Count > 0)
        {
            RankRule rank = gameConfig.ranks.OrderByDescending(r => r.minMerit).FirstOrDefault(r => merit >= r.minMerit);
            if (rank != null) return rank;
        }
        if (merit >= 10000) return new RankRule { name = "元帅", commandLimit = 4 };
        if (merit >= 5000) return new RankRule { name = "上将", commandLimit = 6 };
        if (merit >= 2500) return new RankRule { name = "中将", commandLimit = 5 };
        if (merit >= 1500) return new RankRule { name = "少将", commandLimit = 4 };
        if (merit >= 1000) return new RankRule { name = "上校", commandLimit = 6 };
        if (merit >= 600) return new RankRule { name = "中校", commandLimit = 5 };
        if (merit >= 300) return new RankRule { name = "少校", commandLimit = 4 };
        return new RankRule { name = "士", commandLimit = 2 };
    }

    private void UpdatePlayerRank()
    {
        string rank = CurrentMilitaryRank();
        player.title = player.year >= 4 ? rank + "军官" : "新京军事学院生";
    }

    private int ExpLevel(int exp)
    {
        return Mathf.Max(0, AcademyDisplayLevel(exp) - 1);
    }

    private void DrawCompactAcademyAttributeBars(Transform parent)
    {
        AddText(parent, T("label.character_attributes", "角色属性"), new Vector2(-94, -78), new Vector2(160, 22), 15, TextAnchor.MiddleLeft, highlightColor);
        DrawCompactAcademyAttributeBar(parent, T("attribute.infantry", "步兵"), player.infantryExp, 0);
        DrawCompactAcademyAttributeBar(parent, T("attribute.cavalry", "骑兵"), player.cavalryExp, 1);
        DrawCompactAcademyAttributeBar(parent, T("attribute.artillery", "炮兵"), player.artilleryExp, 2);
        DrawCompactAcademyAttributeBar(parent, T("attribute.management", "管理"), player.managementExp, 3);
        DrawCompactAcademyAttributeBar(parent, T("attribute.logistics", "后勤"), player.logisticsExp, 4);
        DrawCompactAcademyAttributeBar(parent, T("attribute.training", "训练"), player.trainingExp, 5);
    }

    private void DrawCompactAcademyAttributeBar(Transform parent, string label, int exp, int index)
    {
        float y = -104 - index * 23;
        int level = AcademyDisplayLevel(exp);
        AddText(parent, label + " Lv." + level, new Vector2(-80, y), new Vector2(92, 20), 12, TextAnchor.MiddleLeft);
        float barWidth = 126f;
        CreateRect("CompactBarBack_" + label, parent, new Vector2(32, y - 1), new Vector2(barWidth, 8), new Color(0.79f, 0.70f, 0.52f, 0.72f));
        float fillWidth = Mathf.Max(2f, barWidth * AcademyProgress01(exp));
        CreateRect("CompactBarFill_" + label, parent, new Vector2(32 - barWidth * 0.5f + fillWidth * 0.5f, y - 1), new Vector2(fillWidth, 8), new Color(0.33f, 0.55f, 0.43f, 0.96f));
        AddText(parent, AcademyProgressLabel(exp), new Vector2(108, y), new Vector2(48, 18), 10, TextAnchor.MiddleRight, muted);
    }

    private void DrawAcademyAttributeBars(Transform parent)
    {
        AddText(parent, T("label.character_attributes", "角色属性"), new Vector2(0, 68), new Vector2(405, 26), 18, TextAnchor.MiddleLeft, highlightColor);
        DrawAcademyAttributeBar(parent, T("attribute.infantry", "步兵"), player.infantryExp, 0);
        DrawAcademyAttributeBar(parent, T("attribute.cavalry", "骑兵"), player.cavalryExp, 1);
        DrawAcademyAttributeBar(parent, T("attribute.artillery", "炮兵"), player.artilleryExp, 2);
        DrawAcademyAttributeBar(parent, T("attribute.management", "管理"), player.managementExp, 3);
        DrawAcademyAttributeBar(parent, T("attribute.logistics", "后勤"), player.logisticsExp, 4);
        DrawAcademyAttributeBar(parent, T("attribute.training", "训练"), player.trainingExp, 5);
    }

    private void DrawAcademyAttributeBar(Transform parent, string label, int exp, int index)
    {
        float y = 36 - index * 34;
        int level = AcademyDisplayLevel(exp);
        AddText(parent, label + "  Lv." + level, new Vector2(-128, y), new Vector2(140, 24), 15, TextAnchor.MiddleLeft);
        AddText(parent, AcademyProgressLabel(exp), new Vector2(157, y), new Vector2(110, 24), 13, TextAnchor.MiddleRight, muted);
        float barWidth = 210f;
        CreateRect("BarBack_" + label, parent, new Vector2(35, y - 14), new Vector2(barWidth, 9), new Color(0.79f, 0.70f, 0.52f, 0.72f));
        float fillWidth = Mathf.Max(2f, barWidth * AcademyProgress01(exp));
        CreateRect("BarFill_" + label, parent, new Vector2(35 - barWidth * 0.5f + fillWidth * 0.5f, y - 14), new Vector2(fillWidth, 9), new Color(0.33f, 0.55f, 0.43f, 0.96f));
    }

    private int AcademyDisplayLevel(int exp)
    {
        List<AcademyLevelRule> levels = AcademyLevelRules();
        AcademyLevelRule rule = levels.OrderByDescending(l => l.floorExp).FirstOrDefault(l => exp >= l.floorExp);
        return rule != null ? Mathf.Max(1, rule.level) : 1;
    }

    private int AcademyLevelFloorExp(int level)
    {
        AcademyLevelRule rule = AcademyLevelRules().FirstOrDefault(l => l.level == level);
        return rule != null ? rule.floorExp : 0;
    }

    private int AcademyLevelNextExp(int level)
    {
        AcademyLevelRule rule = AcademyLevelRules().FirstOrDefault(l => l.level == level);
        return rule != null ? rule.nextExp : -1;
    }

    private List<AcademyLevelRule> AcademyLevelRules()
    {
        if (gameConfig.academyLevels != null && gameConfig.academyLevels.Count > 0) return gameConfig.academyLevels;
        return new List<AcademyLevelRule>
        {
            new AcademyLevelRule { level = 1, floorExp = 0, nextExp = 50 },
            new AcademyLevelRule { level = 2, floorExp = 50, nextExp = 150 },
            new AcademyLevelRule { level = 3, floorExp = 150, nextExp = 400 },
            new AcademyLevelRule { level = 4, floorExp = 400, nextExp = 1000 },
            new AcademyLevelRule { level = 5, floorExp = 1000, nextExp = 2000 },
            new AcademyLevelRule { level = 6, floorExp = 2000, nextExp = -1 }
        };
    }

    private float AcademyProgress01(int exp)
    {
        int level = AcademyDisplayLevel(exp);
        int next = AcademyLevelNextExp(level);
        if (next < 0) return 1f;
        int floor = AcademyLevelFloorExp(level);
        return Mathf.Clamp01((float)(exp - floor) / Mathf.Max(1, next - floor));
    }

    private string AcademyProgressLabel(int exp)
    {
        int level = AcademyDisplayLevel(exp);
        int next = AcademyLevelNextExp(level);
        if (next < 0) return "MAX";
        int floor = AcademyLevelFloorExp(level);
        return Mathf.Max(0, exp - floor) + "/" + Mathf.Max(1, next - floor);
    }

    private List<CharacterTrait> SelectedPlayerTraits()
    {
        return TraitCatalog().Where(t => player.traits != null && player.traits.Contains(t.id)).ToList();
    }

    private int ApplyCultivationGain(int baseGain)
    {
        int percent = SelectedPlayerTraits().Sum(t => t.cultivationPercent) + (EquippedTitle()?.cultivationBonus ?? 0) + PassiveSkillSum(s => s.expBonusPercent);
        return Mathf.Max(1, Mathf.RoundToInt(baseGain * (100 + percent) / 100f));
    }

    private int ApplySocialGain(int baseGain)
    {
        int bonus = SelectedPlayerTraits().Sum(t => t.socialBonus) + (EquippedTitle()?.socialBonus ?? 0);
        return Mathf.Max(0, baseGain + bonus);
    }

    private int PlayerStaminaSave()
    {
        return SelectedPlayerTraits().Sum(t => t.staminaSave) + Mathf.Max(0, PassiveSkillSum(s => s.supplySavePercent) / 25);
    }

    private int PlayerBattleAttackBonus()
    {
        return SelectedPlayerTraits().Sum(t => t.battleAttack) + (EquippedTitle()?.attackBonus ?? 0);
    }

    private int PlayerBattleHpBonus()
    {
        return SelectedPlayerTraits().Sum(t => t.battleHp) + (EquippedTitle()?.hpBonus ?? 0);
    }

    private int PlayerBattleMoveBonus()
    {
        return SelectedPlayerTraits().Sum(t => t.battleMove) + PassiveSkillSum(s => s.moveBonus);
    }

    private void RunAcademyAction(string course)
    {
        if (IsHolidayWeek())
        {
            ShowHolidayEvent();
            return;
        }

        int rawGain = 0;
        int min = MoodStudyMin(player.mood);
        int max = MoodStudyMax(player.mood);
        AcademyCoreConfig core = AcademyCore();
        for (int day = 0; day < Mathf.Max(1, core.studyDays); day++)
        {
            int daily = UnityEngine.Random.Range(min, max + 1);
            rawGain += daily;
            if (daily <= core.studyLowDailyMoodThreshold) player.mood = Mathf.Clamp(player.mood + core.studyLowDailyMoodDelta, 0, 100);
        }
        int gain = rawGain;
        gain = ApplyCultivationGain(gain);

        CourseConfig config = CourseByLabel(course);
        if (config == null || config.target == "social")
        {
            gain = ApplySocialGain(RandomRangeInt(core.campusWanderMinGain, core.campusWanderMaxExclusive));
            if (relationships.Count == 0)
            {
                AddLog(T("log.campus_wander_empty", "校园闲逛：今日无人同行。"));
                AdvanceWeek();
                ShowPostWeekNarrative("social", "wander", ScreenMode.Academy, ShowAcademy);
                return;
            }
            Relationship rel = relationships[UnityEngine.Random.Range(0, relationships.Count)];
            GainRelationship(rel, gain);
            player.mood = Mathf.Clamp(player.mood + core.campusWanderMoodGain, 0, 100);
            AddLog(TF("log.campus_wander", "校园闲逛：与{0}同行，好感 +{1}。", rel.name, gain));
            AdvanceWeek();
            ShowPostWeekNarrative("social", rel.id, ScreenMode.Academy, ShowAcademy);
            return;
        }

        ApplyCourseGain(course, gain);

        player.lastCourse = course;
        int staminaLoss = Mathf.Max(core.courseMinStaminaLoss, RandomRangeInt(core.courseStaminaLossMin, core.courseStaminaLossMaxExclusive) - PlayerStaminaSave());
        player.stamina = Mathf.Clamp(player.stamina - staminaLoss, 0, 100);
        AddLog(TF("log.course_gain", "{0}：修习进度 +{1}。", course, gain));
        AdvanceWeek();
        ShowPostWeekNarrative("course", config != null ? config.id : course, ScreenMode.Academy, ShowAcademy);
    }

    private void RunSundayAction(string action)
    {
        AcademyCoreConfig core = AcademyCore();
        if (action == "rest")
        {
            player.mood = Mathf.Clamp(player.mood + core.sundayRestMoodGain, 0, 100);
            player.stamina = Mathf.Clamp(player.stamina + core.sundayRestStaminaGain, 0, 100);
            AddLog(T("log.sunday_rest", "周日休息：心情和体力恢复。"));
        }
        else
        {
            int studyGain = ApplyCultivationGain(core.sundayStudyBaseGain);
            string fallbackCourse = CourseCatalog().FirstOrDefault(c => c.target == "trainingExp")?.label ?? T("course.training_fallback", "训练课程");
            ApplyCourseGain(string.IsNullOrEmpty(player.lastCourse) ? fallbackCourse : player.lastCourse, studyGain);
            player.mood = Mathf.Clamp(player.mood + core.sundayStudyMoodDelta, 0, 100);
            AddLog(TF("log.sunday_study", "周日自习：{0}修习进度 +{1}。", SafeText(player.lastCourse, fallbackCourse), studyGain));
        }
        AdvanceWeek();
        ShowPostWeekNarrative(action, action, ScreenMode.Academy, ShowAcademy);
    }

    private void ApplyCourseGain(string course, int gain)
    {
        CourseConfig config = CourseByLabel(course);
        string target = config != null ? config.target : "";
        if (target == "infantryExp" || course == "步兵课程") player.infantryExp += gain;
        else if (target == "cavalryExp" || course == "骑兵课程") player.cavalryExp += gain;
        else if (target == "artilleryExp" || course == "炮兵课程") player.artilleryExp += gain;
        else if (target == "managementExp" || course == "管理课程") player.managementExp += gain;
        else if (target == "logisticsExp" || course == "后勤课程") player.logisticsExp += gain;
        else player.trainingExp += gain;
    }

    private void ShowInviteEvent()
    {
        pendingStoryTitle = T("invite.title", "邀约同窗");
        pendingStorySceneId = "academy";
        pendingStoryPortraitName = player.name;
        pendingStoryBody = T("invite.body", "你准备邀请同窗共度周日。选择一人深入交流，或组织一次朋友圈聚会。");
        AcademyCoreConfig core = AcademyCore();
        pendingStoryOptions = relationships.Select<Relationship, Tuple<string, Action>>(rel => Tuple.Create(TF("invite.option_person", "邀约 {0}", rel.name), (Action)(() =>
        {
            int gain = ApplySocialGain(core.inviteGain);
            GainRelationship(rel, gain);
            player.mood += core.inviteMoodGain;
            AddLog(TF("log.invite_one", "你邀约了{0}，好感 +{1}。", rel.name, gain));
            AdvanceWeek();
            ShowPostWeekNarrative("social", rel.id, ScreenMode.Academy, ShowAcademy);
        }))).ToList();
        pendingStoryOptions.Add(Tuple.Create(T("invite.option_group", "朋友圈聚会"), (Action)(() =>
        {
            int gain = ApplySocialGain(core.friendGatheringGain);
            foreach (Relationship rel in relationships) GainRelationship(rel, gain);
            player.treasury -= core.friendGatheringTreasuryCost;
            AddLog(TF("log.invite_group", "你组织了一次朋友圈聚会，全员好感 +{0}。", gain));
            AdvanceWeek();
            ShowPostWeekNarrative("social", "group", ScreenMode.Academy, ShowAcademy);
        })));
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void ShowPoliticsEvent()
    {
        pendingStoryTitle = T("politics.title", "时事讲座");
        pendingStorySceneId = "council";
        pendingStoryPortraitName = player.name;
        pendingStoryBody = T("politics.body", "讲座围绕王朝改革、军权和族群议题展开。你的发言会改变政治倾向。");
        pendingStoryOptions = PoliticsOptions().Select<PoliticsOptionConfig, Tuple<string, Action>>(option =>
            Tuple.Create(option.label, (Action)(() => AdjustStance(option.stanceId, option.stanceValue, option.axisId, option.axisValue)))).ToList();
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void AdjustStance(string id, int value, string axisId, int axisValue)
    {
        StanceScore score = stances.First(s => s.id == id);
        score.value += value;
        AdjustIdeologyAxis(axisId, axisValue);
        player.mood = Mathf.Clamp(player.mood + AcademyCore().politicsMoodGain, 0, 100);
        AddLog(TF("log.politics_stance", "你在讲座中靠近了{0}。", score.name));
        AdvanceWeek();
        ShowPostWeekNarrative("activity", "lecture", ScreenMode.Academy, ShowAcademy);
    }

    private void AdvanceWeek()
    {
        player.week += 1;
        int maxWeek = gameConfig.calendar != null && gameConfig.calendar.maxWeek > 0 ? gameConfig.calendar.maxWeek : 52;
        if (player.week > maxWeek)
        {
            player.week = 1;
            player.year += 1;
            player.age += 1;
            player.title = player.year >= 4 ? T("title.graduate_officer", "毕业军官") : T("title.academy_student", "新京军事学院生");
            AddLog(T("log.new_school_year", "新学年开始。"));
        }
        AcademyCoreConfig core = AcademyCore();
        if (player.stamina < core.lowStaminaThreshold)
        {
            player.mood = Mathf.Clamp(player.mood - core.lowStaminaMoodPenalty, 0, 100);
            AddLog(T("log.low_stamina", "体力透支影响了心情。"));
        }
        if (IsExamWeek()) ResolveAcademyExam();
        if (IsHolidayWeek()) ApplyHolidayWeek();
        ApplyRelationshipDecay();
        UpdatePlayerRank();
        RefreshProgressionSystems(true);
        AutoSave("AUTO_WEEKLY_SUN");
    }

    private void ResolveAcademyExam()
    {
        AcademyCoreConfig core = AcademyCore();
        int courseScore = Mathf.RoundToInt((ExpLevel(player.infantryExp) + ExpLevel(player.cavalryExp) + ExpLevel(player.artilleryExp) + ExpLevel(player.managementExp) + ExpLevel(player.logisticsExp) + ExpLevel(player.trainingExp)) * core.examCourseScoreMultiplier);
        int writtenScore = RandomRangeInt(core.examWrittenMin, core.examWrittenMaxExclusive);
        int stateScore = player.mood >= core.examHighMoodThreshold ? core.examHighMoodBonus : player.mood < core.examLowMoodThreshold ? core.examLowMoodPenalty : 0;
        int score = Mathf.Clamp(courseScore + writtenScore + stateScore, 0, 100);
        player.lastExamScore = score;
        ExamRewardRule reward = ExamRewards().OrderByDescending(r => r.minScore).FirstOrDefault(r => score >= r.minScore) ?? new ExamRewardRule();
        int merit = reward.merit;
        int treasury = reward.treasury;
        player.merit += merit;
        player.treasury += treasury;
        AddLog(TF("log.exam_result", "校考结算：{0}分，战功 +{1}，国库 +{2}。", score, merit, treasury));
    }

    private void ApplyHolidayWeek()
    {
        AcademyCoreConfig core = AcademyCore();
        player.mood = Mathf.Clamp(player.mood + core.holidayMoodGain, 0, 100);
        player.stamina = Mathf.Clamp(player.stamina + core.holidayStaminaGain, 0, 100);
        AddLog(T("log.holiday_apply", "假期：课程暂停，心情与体力恢复。"));
    }

    private void ShowHolidayEvent()
    {
        pendingStoryTitle = T("holiday.title", "假期");
        pendingStorySceneId = "street";
        pendingStoryPortraitName = player.name;
        pendingStoryBody = T("holiday.body", "本周是学院假期。课程暂停，你可以休整、读报或推进剧情。");
        pendingStoryOptions = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("holiday.option_rest", "休整"), (Action)(() =>
            {
                AcademyCoreConfig c = AcademyCore();
                player.mood = Mathf.Clamp(player.mood + c.sundayRestMoodGain, 0, 100);
                player.stamina = Mathf.Clamp(player.stamina + c.sundayRestStaminaGain, 0, 100);
                AdvanceWeek();
                ShowPostWeekNarrative("rest", "rest", ScreenMode.Academy, ShowAcademy);
            })),
            Tuple.Create(T("holiday.option_newspaper", "读报"), (Action)ShowNewspaperMenu)
        };
        pendingStoryReturnAction = ShowAcademy;
        ShowStoryEvent();
    }

    private void ShowStoryEvent()
    {
        mode = ScreenMode.StoryEvent;
        Clear();
        DrawSceneBackground(pendingStorySceneId);
        Vector2 bodyPos;
        Vector2 bodySize;
        Vector2 optionsCenter;
        Vector2 optionsSize;
        RectTransform frame = CreateStoryDialogFrame(pendingStoryTitle, pendingStoryPortraitName, pendingStoryPortraitName, out bodyPos, out bodySize, out optionsCenter, out optionsSize);
        AddDialogBodyText(frame, pendingStoryBody, bodyPos, bodySize, 19);
        List<Tuple<string, Action>> options = DialogOptionsWithReturn(pendingStoryOptions);
        AddDialogOptions(frame, options, optionsCenter, optionsSize);
    }

    private List<Tuple<string, Action>> DialogOptionsWithReturn(IEnumerable<Tuple<string, Action>> sourceOptions)
    {
        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>();
        HashSet<string> seenLabels = new HashSet<string>(StringComparer.Ordinal);
        if (sourceOptions != null)
        {
            foreach (Tuple<string, Action> option in sourceOptions)
            {
                if (option == null) continue;
                string key = CleanDialogChoiceText(option.Item1);
                if (!seenLabels.Add(key)) continue;
                options.Add(option);
            }
        }

        string backLabel = T("button.back", "返回");
        string backKey = CleanDialogChoiceText(backLabel);
        if (!seenLabels.Contains(backKey))
        {
            options.Add(Tuple.Create(backLabel, (Action)(() =>
            {
                Action back = pendingStoryReturnAction ?? (Action)ShowAcademy;
                pendingStoryReturnAction = null;
                back();
            })));
        }

        return options;
    }

    private void StartStory(string eventId, ScreenMode returnMode)
    {
        pendingStoryReturnAction = null;
        storyReturnMode = returnMode;
        activeStoryEventId = ResolveStoryTarget(eventId);
        activeStoryPageIndex = 0;
        StoryEventData ev = StoryEventById(activeStoryEventId);
        if (ev == null)
        {
            AddLog(TF("log.story_missing", "剧情：未找到事件 {0}。", eventId));
            ReturnToStoryCaller();
            return;
        }
        if (!IsStoryConditionMet(ev))
        {
            ShowStoryLocked(ev, returnMode);
            return;
        }
        ShowStoryDataEvent();
    }

    private bool IsStoryConditionMet(StoryEventData ev)
    {
        if (ev == null) return false;
        return ConditionMet(ev.unlockKind, ev.unlockTarget, ev.unlockValue);
    }

    private void ShowStoryLocked(StoryEventData ev, ScreenMode returnMode)
    {
        string requirement = StoryUnlockRequirement(ev);
        string body = TF("story.locked_body", "触发条件：{0}\n\n先完成对应的养成、情报、任务或战棋目标，再回来推进。", requirement);
        AddLog(TF("log.story_locked", "剧情未触发：{0}。", requirement));
        OpenSystemPopup(T("story.locked_title", "剧情未触发"), body, null, returnMode, "library");
    }

    private string StoryUnlockRequirement(StoryEventData ev)
    {
        if (ev == null) return T("story.unlock.unknown", "条件未配置");
        if (!string.IsNullOrWhiteSpace(ev.unlockHint)) return ev.unlockHint;
        string kind = SafeText(ev.unlockKind, "").Trim();
        string target = SafeText(ev.unlockTarget, "").Trim();
        int value = Mathf.Max(1, ev.unlockValue);
        if (string.IsNullOrEmpty(kind) || kind == "always") return T("story.unlock.ready", "已满足，点击继续主线即可推进。");
        if (kind == "quest")
        {
            QuestConfig quest = QuestCatalog().FirstOrDefault(q => q.id == target);
            return TF("story.unlock.quest", "完成任务：{0}", quest != null ? quest.name : target);
        }
        if (kind == "story") return TF("story.unlock.story", "完成剧情：{0}", target);
        if (kind == "battleWins") return TF("story.unlock.battle_wins", "战棋胜利 {0}/{1}", player.battleWins, value);
        if (kind == "battlesFought") return TF("story.unlock.battles", "完成战棋 {0}/{1}", player.battlesFought, value);
        if (kind == "enemiesDefeated") return TF("story.unlock.enemies", "击溃敌方单位 {0}/{1}", player.enemiesDefeated, value);
        if (kind == "anyCourseExp") return TF("story.unlock.any_course_exp", "任意课程进度 {0}/{1}", ProgressValue(kind, target), value);
        if (kind == "anyCourseLevel") return TF("story.unlock.any_course_level", "任意课程等级 Lv.{0}/Lv.{1}", ProgressValue(kind, target), value);
        if (kind == "anyRelationship") return TF("story.unlock.any_relationship", "任意同窗好感 {0}/{1}", ProgressValue(kind, target), value);
        if (kind == "relationship")
        {
            Relationship rel = relationships.FirstOrDefault(r => r.id == target || r.name == target);
            return TF("story.unlock.relationship", "{0}好感 {1}/{2}", rel != null ? rel.name : target, ProgressValue(kind, target), value);
        }
        if (kind == "intelligence") return TF("story.unlock.intelligence", "情报值 {0}/{1}", player.intelligence, value);
        if (kind == "spyNetwork") return TF("story.unlock.spy_network", "情报网 Lv.{0}/Lv.{1}", player.spyNetwork, value);
        if (kind == "merit") return TF("story.unlock.merit", "战功 {0}/{1}", player.merit, value);
        return TF("story.unlock.progress", "{0} {1}/{2}", kind, ProgressValue(kind, target), value);
    }

    private void ShowStoryDataEvent()
    {
        mode = ScreenMode.StoryEvent;
        Clear();
        StoryEventData ev = StoryEventById(activeStoryEventId);
        DrawSceneBackground(SceneForStoryEvent(ev));
        if (ev == null)
        {
            AddTopBar(root, T("story.missing_title", "剧情缺失"));
            AddText(root, TF("story.missing", "未找到剧情事件：{0}", activeStoryEventId), Vector2.zero, new Vector2(820, 100), 22, TextAnchor.MiddleCenter);
            AddButton(root, T("button.back", "返回"), new Vector2(0, -260), new Vector2(180, 48), ReturnToStoryCaller);
            return;
        }

        List<string> pages = BuildStoryPages(ev);
        activeStoryPageIndex = Mathf.Clamp(activeStoryPageIndex, 0, Mathf.Max(0, pages.Count - 1));
        string speakerName = CurrentStorySpeaker(ev, pages.Count);
        Vector2 bodyPos;
        Vector2 bodySize;
        Vector2 optionsCenter;
        Vector2 optionsSize;
        string shortTitle = StoryEventShortTitle(ev);
        string dialogTitle = (string.IsNullOrEmpty(ev.chapter) ? ev.id : ev.chapter + "  " + ev.id) + (string.IsNullOrEmpty(shortTitle) ? "" : " · " + shortTitle);
        RectTransform frame = CreateStoryDialogFrame(dialogTitle, speakerName, speakerName, out bodyPos, out bodySize, out optionsCenter, out optionsSize);
        AddDialogBodyText(frame, pages[activeStoryPageIndex], bodyPos, bodySize, 18);
        AddText(frame, TF("story.page_progress", "第 {0}/{1} 页", activeStoryPageIndex + 1, pages.Count), new Vector2(bodyPos.x + bodySize.x * 0.5f - 72f, bodyPos.y - bodySize.y * 0.5f - 12f), new Vector2(140, 22), 14, TextAnchor.MiddleRight, muted);

        List<Tuple<string, Action>> options = new List<Tuple<string, Action>>();
        bool hasChoicesNow = activeStoryPageIndex >= pages.Count - 1 && ev.choices != null && ev.choices.Count > 0;
        if (!hasChoicesNow)
        {
            AddDialogAdvanceClick(frame, () => AdvanceStoryReading(ev, pages), T("story.click_to_continue", "点击任意处继续"), optionsCenter, optionsSize);
            return;
        }

        for (int i = 0; i < ev.choices.Count; i++)
        {
            StoryChoiceData choice = ev.choices[i];
            options.Add(Tuple.Create(StoryChoiceDisplayLabel(choice), (Action)(() => ApplyStoryChoice(choice))));
        }

        options.Add(Tuple.Create(T("button.story_menu", "剧情目录"), (Action)ShowStoryMenu));
        AddDialogOptions(frame, options, optionsCenter, optionsSize);
    }

    private void AdvanceStoryReading(StoryEventData ev, List<string> pages)
    {
        if (ev == null) return;
        if (activeStoryPageIndex < pages.Count - 1)
        {
            activeStoryPageIndex += 1;
            ShowStoryDataEvent();
            return;
        }

        string next = NextMainEventAfter(ev.id);
        CompleteStoryEvent(ev);
        if (!string.IsNullOrEmpty(next)) StartStory(next, storyReturnMode);
        else ReturnToStoryCaller();
    }

    private List<string> BuildStoryPages(StoryEventData ev)
    {
        List<string> pages = new List<string>();
        string current = "";
        foreach (StoryLineData line in ev.lines ?? new List<StoryLineData>())
        {
            string speaker = string.IsNullOrEmpty(line.speaker) ? "旁白" : line.speaker;
            string prefix = speaker == "旁白" ? "" : "【" + speaker + "】\n";
            string block = prefix + (line.text ?? "") + "\n\n";
            foreach (string chunk in SplitStoryBlock(block, 320))
            {
                if (current.Length + chunk.Length > 390 && current.Length > 0)
                {
                    pages.Add(current.TrimEnd());
                    current = "";
                }
                current += chunk;
            }
        }
        if (!string.IsNullOrWhiteSpace(current)) pages.Add(current.TrimEnd());
        if (pages.Count == 0) pages.Add(T("story.empty_text", "（暂无剧情文本）"));
        return pages;
    }

    private IEnumerable<string> SplitStoryBlock(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }
        for (int i = 0; i < text.Length; i += maxChars)
        {
            int length = Mathf.Min(maxChars, text.Length - i);
            yield return text.Substring(i, length);
        }
    }

    private void ApplyStoryChoice(StoryChoiceData choice)
    {
        StoryEventData ev = StoryEventById(activeStoryEventId);
        if (ev == null) return;
        ApplyStoryEffects(choice);
        CompleteStoryEvent(ev);
        AddLog(TF("log.story_choice", "剧情选择：{0}。", choice.label));

        string next = ResolveStoryTarget(choice.nextEventId);
        if (string.IsNullOrEmpty(next)) next = NextMainEventAfter(ev.id);
        if (!string.IsNullOrEmpty(next))
        {
            StartStory(next, storyReturnMode);
        }
        else
        {
            ReturnToStoryCaller();
        }
    }

    private string StoryChoiceDisplayLabel(StoryChoiceData choice)
    {
        if (choice == null) return "";
        string label = choice.label ?? "";
        string full = choice.text ?? "";
        if (!string.IsNullOrWhiteSpace(full) && (label.Contains("...") || label.Contains("…"))) return full;
        return label;
    }

    private void ApplyStoryEffects(StoryChoiceData choice)
    {
        foreach (StoryEffectData effect in choice.effects ?? new List<StoryEffectData>())
        {
            if (effect.kind == "立场")
            {
                AdjustStoryStance(effect.target, effect.delta);
            }
            else if (effect.kind == "好感")
            {
                AdjustStoryAffection(effect.target, effect.delta);
            }
            else
            {
                AddStoryValue(effect.kind + ":" + effect.target, effect.delta);
            }
        }
    }

    private void AdjustStoryStance(string target, int delta)
    {
        string name = NormalizeStanceName(target);
        StanceScore score = stances.FirstOrDefault(s => s.name == name);
        if (score == null)
        {
            score = new StanceScore { id = name, name = name, value = 0 };
            stances.Add(score);
        }
        score.value += delta;
        if (name == "印第安乡党") AdjustIdeologyAxis("nation", delta > 0 ? 3 : -3);
        else if (name == "自由派") AdjustIdeologyAxis("class", delta > 0 ? 3 : -3);
        else if (name == "陆军青壮派") AdjustIdeologyAxis("governance", delta > 0 ? -3 : 3);
        else if (name == "返乡团") AdjustIdeologyAxis("region", delta > 0 ? -3 : 3);
        else if (name == "法治派") AdjustIdeologyAxis("governance", delta > 0 ? -3 : 3);
        AddStoryValue("立场:" + name, delta);
    }

    private string NormalizeStanceName(string target)
    {
        if (string.IsNullOrEmpty(target)) return T("stance.unknown", "未知立场");
        if (target == T("stance.alias_home", "返乡")) return "返乡团";
        if (target == T("stance.alias_army", "干城派")) return "陆军青壮派";
        return target;
    }

    private void AdjustStoryAffection(string target, int delta)
    {
        if (string.IsNullOrEmpty(target)) return;
        Relationship rel = relationships.FirstOrDefault(r => r.id == target || r.name == target);
        if (rel == null)
        {
            StoryCharacterData character = StoryCharacterByName(target);
            rel = new Relationship
            {
                id = target,
                name = target,
                stance = character != null && !string.IsNullOrEmpty(character.faction) ? character.faction : T("relationship.story_character", "剧情人物"),
                affection = 0,
                note = character != null && !string.IsNullOrEmpty(character.identity) ? character.identity : T("relationship.story_character_note", "剧情中结识的人物。"),
                lastInteractionWeek = CurrentCalendarWeek()
            };
            relationships.Add(rel);
        }
        int actualDelta = delta > 0 ? ApplySocialGain(delta) : delta;
        GainRelationship(rel, actualDelta);
        AddStoryValue("好感:" + target, actualDelta);
    }

    private int GetStoryValue(string id)
    {
        StoryValue value = storyValues.FirstOrDefault(v => v.id == id);
        return value != null ? value.value : 0;
    }

    private void SetStoryValue(string id, int value)
    {
        StoryValue existing = storyValues.FirstOrDefault(v => v.id == id);
        if (existing == null)
        {
            storyValues.Add(new StoryValue { id = id, value = value });
        }
        else
        {
            existing.value = value;
        }
    }

    private void AddStoryValue(string id, int delta)
    {
        StoryValue value = storyValues.FirstOrDefault(v => v.id == id);
        if (value == null)
        {
            value = new StoryValue { id = id, value = 0 };
            storyValues.Add(value);
        }
        value.value += delta;
    }

    private void ShowPostWeekNarrative(string triggerKind, string triggerTarget, ScreenMode returnMode, Action fallback)
    {
        if (TryShowNarrativeFragment(triggerKind, triggerTarget, returnMode, fallback)) return;
        if (TryShowIntelligenceNarrative(returnMode, fallback)) return;
        fallback();
    }

    private bool TryShowNarrativeFragment(string triggerKind, string triggerTarget, ScreenMode returnMode, Action fallback)
    {
        NarrativeFragmentConfig fragment = NarrativeFragments()
            .Where(f => NarrativeFragmentMatches(f, triggerKind, triggerTarget))
            .OrderBy(f => Mathf.Max(0, f.minWeek))
            .FirstOrDefault();
        if (fragment == null) return false;
        OpenNarrativeFragment(fragment, returnMode, fallback);
        return true;
    }

    private bool TryShowIntelligenceNarrative(ScreenMode returnMode, Action fallback)
    {
        NarrativeFragmentConfig fragment = NarrativeFragments()
            .Where(f => f != null && string.Equals(f.triggerKind, "intelligence", StringComparison.OrdinalIgnoreCase))
            .Where(f =>
            {
                int threshold = 0;
                int.TryParse(f.triggerTarget, out threshold);
                return threshold > 0 && player.intelligence >= threshold && NarrativeWeekMatches(f) && !NarrativeFragmentSeen(f);
            })
            .OrderBy(f =>
            {
                int threshold = 0;
                int.TryParse(f.triggerTarget, out threshold);
                return threshold;
            })
            .FirstOrDefault();
        if (fragment == null) return false;
        OpenNarrativeFragment(fragment, returnMode, fallback);
        return true;
    }

    private bool NarrativeFragmentMatches(NarrativeFragmentConfig fragment, string triggerKind, string triggerTarget)
    {
        if (fragment == null) return false;
        if (!string.Equals(SafeText(fragment.triggerKind, ""), SafeText(triggerKind, ""), StringComparison.OrdinalIgnoreCase)) return false;
        if (!NarrativeWeekMatches(fragment)) return false;
        if (NarrativeFragmentSeen(fragment)) return false;
        string configuredTarget = SafeText(fragment.triggerTarget, "").Trim();
        string actualTarget = SafeText(triggerTarget, "").Trim();
        return string.IsNullOrEmpty(configuredTarget) || configuredTarget == "*" || string.Equals(configuredTarget, actualTarget, StringComparison.OrdinalIgnoreCase);
    }

    private bool NarrativeWeekMatches(NarrativeFragmentConfig fragment)
    {
        int week = CurrentCalendarWeek();
        if (fragment.minWeek > 0 && week < fragment.minWeek) return false;
        if (fragment.maxWeek > 0 && week > fragment.maxWeek) return false;
        return true;
    }

    private bool NarrativeFragmentSeen(NarrativeFragmentConfig fragment)
    {
        if (fragment == null || string.IsNullOrEmpty(fragment.id)) return true;
        bool once = string.IsNullOrEmpty(fragment.once) || !string.Equals(fragment.once, "false", StringComparison.OrdinalIgnoreCase);
        return once && GetStoryValue("fragment:" + fragment.id) > 0;
    }

    private void MarkNarrativeFragmentSeen(NarrativeFragmentConfig fragment)
    {
        if (fragment != null && !string.IsNullOrEmpty(fragment.id)) SetStoryValue("fragment:" + fragment.id, 1);
    }

    private void OpenNarrativeFragment(NarrativeFragmentConfig fragment, ScreenMode returnMode, Action fallback)
    {
        activeStoryEventId = "";
        pendingStoryTitle = SafeText(fragment.title, T("narrative.default_title", "学院异闻"));
        pendingStorySceneId = SafeText(fragment.sceneId, "academy");
        pendingStoryPortraitName = SafeText(fragment.speaker, player.name);
        pendingStoryBody = fragment.body ?? "";
        pendingStoryOptions = new List<Tuple<string, Action>>
        {
            Tuple.Create(NarrativeChoiceLabel(fragment, true), (Action)(() =>
            {
                ApplyNarrativeFragment(fragment, true);
                MarkNarrativeFragmentSeen(fragment);
                RefreshProgressionSystems(true);
                if (!string.IsNullOrEmpty(fragment.nextStoryId) && StoryEventById(ResolveStoryTarget(fragment.nextStoryId)) != null)
                {
                    StartStory(fragment.nextStoryId, returnMode);
                }
                else
                {
                    fallback();
                }
            })),
            Tuple.Create(NarrativeChoiceLabel(fragment, false), (Action)(() =>
            {
                ApplyNarrativeFragment(fragment, false);
                MarkNarrativeFragmentSeen(fragment);
                RefreshProgressionSystems(true);
                fallback();
            }))
        };
        pendingStoryReturnAction = fallback;
        ShowStoryEvent();
    }

    private string NarrativeChoiceLabel(NarrativeFragmentConfig fragment, bool pursue)
    {
        return pursue ? T("narrative.choice_pursue", "追问线索") : T("narrative.choice_record", "暂且记下");
    }

    private int ScaledNarrativeDelta(int value, bool pursue)
    {
        if (value == 0) return 0;
        if (pursue) return value;
        int scaled = Mathf.RoundToInt(value * 0.5f);
        if (scaled == 0) scaled = value > 0 ? 1 : -1;
        return scaled;
    }

    private int ScaledNarrativePositive(int value, bool pursue)
    {
        if (value <= 0) return 0;
        return pursue ? value : Mathf.Max(1, value / 2);
    }

    private void ApplyNarrativeFragment(NarrativeFragmentConfig fragment, bool pursue)
    {
        if (fragment == null) return;
        int affection = ScaledNarrativeDelta(fragment.affectionDelta, pursue);
        if (!string.IsNullOrEmpty(fragment.relationshipTarget) && affection != 0) AdjustStoryAffection(fragment.relationshipTarget, affection);

        int axis = ScaledNarrativeDelta(fragment.axisDelta, pursue);
        if (!string.IsNullOrEmpty(fragment.axisId) && axis != 0)
        {
            AdjustIdeologyAxis(fragment.axisId, axis);
            AddStoryValue("立场轴:" + fragment.axisId, axis);
        }

        int intelligence = ScaledNarrativePositive(fragment.intelligenceDelta, pursue);
        if (intelligence != 0)
        {
            player.intelligence += intelligence;
            AddStoryValue("线索:东渡密档", intelligence);
        }

        int suspicion = ScaledNarrativePositive(fragment.suspicionDelta, pursue);
        if (!string.IsNullOrEmpty(fragment.suspicionFaction) && suspicion != 0)
        {
            AddStoryValue("警觉:" + fragment.suspicionFaction, suspicion);
        }

        AddLog(TF("log.narrative_fragment", "事件：{0}（{1}）。", SafeText(fragment.title, T("narrative.default_title", "学院异闻")), pursue ? T("narrative.pursued", "追问") : T("narrative.recorded", "记录")));
    }


    private void CompleteStoryEvent(StoryEventData ev)
    {
        if (ev == null) return;
        if (!completedStoryEvents.Contains(ev.id)) completedStoryEvents.Add(ev.id);
        if (player.eventReview != null && !player.eventReview.Contains(ev.id)) player.eventReview.Add(ev.id);
        if (ev.id.StartsWith("END", StringComparison.OrdinalIgnoreCase) && player.unlockedEndings != null && !player.unlockedEndings.Contains(ev.id))
        {
            player.unlockedEndings.Add(ev.id);
            AutoSave("AUTO_ENDING_" + ev.id);
        }
        if (ev.id == currentMainEventId)
        {
            string next = NextMainEventAfter(ev.id);
            if (!string.IsNullOrEmpty(next)) currentMainEventId = next;
        }
        RefreshProgressionSystems(true);
        AutoSave("AUTO_STORY");
    }

    private void ReturnToStoryCaller()
    {
        if (storyReturnMode == ScreenMode.Strategy) ShowStrategy();
        else ShowAcademy();
    }

    private StoryEventData StoryEventById(string id)
    {
        if (storyDatabase == null || storyDatabase.events == null || string.IsNullOrEmpty(id)) return null;
        return storyDatabase.events.FirstOrDefault(e => e.id == id);
    }

    private StoryCharacterData StoryCharacterByName(string name)
    {
        if (storyDatabase == null || storyDatabase.characters == null || string.IsNullOrEmpty(name)) return null;
        return storyDatabase.characters.FirstOrDefault(c => c.name == name);
    }

    private string ResolveStoryTarget(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string id = raw.Trim();
        if (StoryEventById(id) != null) return id;
        if (id.StartsWith("END-"))
        {
            string tail = id.Substring(4);
            if (int.TryParse(tail, out int endNo))
            {
                string normalized = "END-" + endNo.ToString("00");
                if (StoryEventById(normalized) != null) return normalized;
            }
        }
        int dash = id.IndexOf('-');
        if (dash > 0)
        {
            string rootId = id.Substring(0, dash);
            if (StoryEventById(rootId) != null) return rootId;
        }
        return "";
    }

    private string NextMainEventAfter(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.StartsWith("EV") || id.Length < 5) return "";
        if (!int.TryParse(id.Substring(2, 3), out int number)) return "";
        for (int next = number + 1; next <= 65; next++)
        {
            string nextId = "EV" + next.ToString("000");
            if (StoryEventById(nextId) != null) return nextId;
        }
        return "";
    }

    private bool IsStoryUnlocked(StoryEventData ev)
    {
        if (ev == null || completedStoryEvents.Contains(ev.id)) return false;
        if (!IsStoryConditionMet(ev)) return false;
        if (ev.id.StartsWith("EV")) return ev.id == currentMainEventId;
        if (string.IsNullOrEmpty(ev.trigger)) return false;
        foreach (string id in completedStoryEvents)
        {
            if (ev.trigger.Contains(id)) return true;
        }
        return ev.trigger.Contains("游戏开始") || ev.trigger.Contains("遊戲開始");
    }

    private void ShowStoryMenu()
    {
        mode = ScreenMode.StoryEvent;
        Clear();
        DrawSceneBackground("library");
        AddTopBar(root, T("story_menu.title", "剧情目录"));
        StoryEventData current = StoryEventById(currentMainEventId);
        string requirement = current != null && IsStoryConditionMet(current)
            ? T("story_menu.main_ready", "已满足，点击继续主线。")
            : StoryUnlockRequirement(current);
        AddText(root, TF("story_menu.summary", "当前主线：{0}\n推进条件：{1}\n支线会随主线、好感和任务进度开放。", currentMainEventId, requirement), new Vector2(-360, 210), new Vector2(420, 130), 18, TextAnchor.UpperLeft);
        AddButton(root, T("button.continue_main_story", "继续主线"), new Vector2(-390, 95), new Vector2(220, 50), () => StartStory(currentMainEventId, storyReturnMode), new Color(0.28f, 0.37f, 0.26f));

        List<StoryEventData> side = storyDatabase.events
            .Where(e => !e.id.StartsWith("EV") && !e.id.StartsWith("END-") && IsStoryUnlocked(e))
            .Take(10)
            .ToList();
        AddText(root, T("story_menu.unlocked_side", "已解锁支线"), new Vector2(150, 245), new Vector2(520, 36), 24, TextAnchor.MiddleLeft);
        if (side.Count == 0)
        {
            AddText(root, T("story_menu.no_side", "继续推进主线后会解锁角色支线。"), new Vector2(150, 175), new Vector2(520, 80), 18, TextAnchor.MiddleLeft, muted);
        }
        for (int i = 0; i < side.Count; i++)
        {
            StoryEventData ev = side[i];
            AddButton(root, ev.id + "  " + StoryEventShortTitle(ev), new Vector2(165, 180 - i * 50), new Vector2(560, 42), () => StartStory(ev.id, storyReturnMode));
        }
        AddButton(root, T("button.character_archive", "角色档案"), new Vector2(-390, 25), new Vector2(220, 50), ShowCharacterArchive);
        AddButton(root, T("button.back", "返回"), new Vector2(-390, -300), new Vector2(160, 46), ReturnToStoryCaller);
    }

    private string StoryEventShortTitle(StoryEventData ev)
    {
        if (ev == null || ev.lines == null || ev.lines.Count == 0) return ev != null ? ev.type : "";
        string text = ev.lines[0].text ?? "";
        return text.Replace("\n", " ");
    }

    private void ShowCharacterArchive()
    {
        mode = ScreenMode.StoryEvent;
        Clear();
        DrawSceneBackground("library");
        AddTopBar(root, T("character_archive.title", "角色档案"));
        List<StoryCharacterData> chars = storyDatabase.characters ?? new List<StoryCharacterData>();
        int pageSize = 4;
        int maxPage = Mathf.Max(0, Mathf.CeilToInt(chars.Count / (float)pageSize) - 1);
        characterArchivePage = Mathf.Clamp(characterArchivePage, 0, maxPage);
        AddText(root, TF("character_archive.page", "角色 {0}/{1} 页", characterArchivePage + 1, maxPage + 1), new Vector2(-430, 250), new Vector2(300, 40), 22, TextAnchor.MiddleLeft);
        for (int i = 0; i < pageSize; i++)
        {
            int index = characterArchivePage * pageSize + i;
            if (index >= chars.Count) break;
            StoryCharacterData c = chars[index];
            string text = TF("character_archive.card", "{0}｜{1}\n{2}\n性格：{3}\n{4}", c.name, c.faction, c.identity, c.traits, c.background);
            AddPortrait(root, c.name, new Vector2(-455, 176 - i * 125), new Vector2(76, 112), true);
            AddText(root, text, new Vector2(90, 175 - i * 125), new Vector2(860, 105), 15, TextAnchor.UpperLeft, i % 2 == 0 ? ink : muted);
        }
        if (characterArchivePage > 0)
        {
            AddButton(root, T("button.prev_page", "上一页"), new Vector2(-180, -300), new Vector2(140, 46), () =>
            {
                characterArchivePage -= 1;
                ShowCharacterArchive();
            });
        }
        if (characterArchivePage < maxPage)
        {
            AddButton(root, T("button.next_page", "下一页"), new Vector2(0, -300), new Vector2(140, 46), () =>
            {
                characterArchivePage += 1;
                ShowCharacterArchive();
            }, new Color(0.28f, 0.37f, 0.26f));
        }
        AddButton(root, T("button.story_menu", "剧情目录"), new Vector2(180, -300), new Vector2(140, 46), ShowStoryMenu);
        AddButton(root, T("button.back", "返回"), new Vector2(360, -300), new Vector2(140, 46), ReturnToStoryCaller);
    }

    private void ShowStrategy()
    {
        mode = ScreenMode.Strategy;
        pendingStoryReturnAction = null;
        selectedUnitId = null;
        battle = null;
        battleTerrainOverride = null;
        RemoveBattleLabTempArmies();
        Clear();
        DrawSceneBackground("strategy");
        AddTopBar(root, TF("strategy.title", "战略地图  {0}年  第{1}回合", season, strategyTurn));
        DrawSystemDock(root, ScreenMode.Strategy);
        DrawStrategyDashboard();
    }

    private void DrawStrategyDashboard()
    {
        RectTransform map = CreateRect("Map", root, new Vector2(-178, 20), new Vector2(830, 520), new Color(0.10f, 0.075f, 0.045f, 0.96f));
        DrawStrategyMapTerrain(map);
        foreach (Province p in provinces)
        {
            foreach (string road in p.roads)
            {
                Province target = ProvinceById(road);
                if (target == null || string.CompareOrdinal(p.id, target.id) > 0) continue;
                DrawLine(map, StrategyMapPosition(p), StrategyMapPosition(target), new Color(0.13f, 0.09f, 0.045f, 0.82f), 3);
            }
        }
        foreach (Province p in provinces) DrawProvince(map, p);
        DrawStrategyLegend(map);
        DrawStrategySidePanel(root);

        RectTransform commands = CreateUiPanel("StrategyCommands", root, new Vector2(-126, -286), new Vector2(805, 48));
        AddButton(commands, T("button.mission", "军令"), new Vector2(-338, 0), new Vector2(88, 32), ShowMissionBrief);
        AddButton(commands, T("button.story_menu", "剧情"), new Vector2(-240, 0), new Vector2(88, 32), () =>
        {
            storyReturnMode = ScreenMode.Strategy;
            ShowStoryMenu();
        });
        AddButton(commands, T("button.continue_main_story", "主线"), new Vector2(-142, 0), new Vector2(88, 32), () => StartStory(currentMainEventId, ScreenMode.Strategy), new Color(0.43f, 0.58f, 0.48f, 0.96f));
        AddButton(commands, T("button.battle_lab", "工坊"), new Vector2(-44, 0), new Vector2(88, 32), ShowBattleLabEditor, new Color(0.52f, 0.42f, 0.62f, 0.96f));
        AddButton(commands, T("button.academy_review", "学院"), new Vector2(54, 0), new Vector2(88, 32), ShowAcademy);
        AddButton(commands, T("button.system", "系统"), new Vector2(152, 0), new Vector2(88, 32), () => ShowSettingsPanel(ScreenMode.Strategy));
        AddButton(commands, T("button.end_turn", "结束回合"), new Vector2(282, 0), new Vector2(126, 32), EndStrategyTurn, new Color(0.43f, 0.58f, 0.48f, 0.96f));
    }

    private Vector2 StrategyMapPosition(Province province)
    {
        return province == null ? Vector2.zero : new Vector2(province.x, province.y);
    }

    private void DrawStrategyMapTerrain(Transform map)
    {
        RectTransform mapArt = CreateSpriteRect("NorthAmericaStrategyMap", map, Vector2.zero, new Vector2(806, 500), "Art/Scenes/scene_strategy", new Color(0.48f, 0.42f, 0.31f, 0.96f), false, false);
        Image mapImage = mapArt.GetComponent<Image>();
        if (mapImage != null) mapImage.raycastTarget = false;

        RectTransform shade = CreateRect("MapReadabilityShade", map, Vector2.zero, new Vector2(806, 500), new Color(0.02f, 0.016f, 0.010f, 0.10f));
        Image shadeImage = shade.GetComponent<Image>();
        if (shadeImage != null) shadeImage.raycastTarget = false;

        RectTransform border = CreateRect("MapInnerBorder", map, Vector2.zero, new Vector2(814, 508), new Color(0.96f, 0.74f, 0.34f, 0.12f));
        Image borderImage = border.GetComponent<Image>();
        if (borderImage != null) borderImage.raycastTarget = false;
    }

    private void DrawMapPatch(Transform parent, string name, Vector2 pos, Vector2 size, Color color, string label)
    {
        RectTransform patch = CreateRect(name, parent, pos, size, color);
        patch.GetComponent<Image>().raycastTarget = false;
        Text text = AddText(patch, label, Vector2.zero, new Vector2(size.x - 8f, 28), 12, TextAnchor.MiddleCenter, new Color(0.25f, 0.22f, 0.17f, 0.62f));
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 8;
        text.resizeTextMaxSize = 12;
    }

    private RectTransform CreateEllipse(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        MapEllipseGraphic ellipse = go.AddComponent<MapEllipseGraphic>();
        ellipse.color = color;
        ellipse.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private void DrawStrategyRiver(Transform parent, params Vector2[] points)
    {
        for (int i = 1; i < points.Length; i++)
        {
            DrawLine(parent, points[i - 1], points[i], new Color(0.24f, 0.47f, 0.62f, 0.56f), 4f);
        }
    }

    private void DrawStrategyLegend(Transform map)
    {
        RectTransform legend = CreateRect("StrategyLegend", map, new Vector2(-18, 238), new Vector2(560, 28), new Color(0.08f, 0.055f, 0.030f, 0.58f));
        AddText(legend, T("strategy.legend_title", "势力"), new Vector2(-254, 0), new Vector2(44, 18), 11, TextAnchor.MiddleLeft, highlightColor);
        DrawLegendItem(legend, Faction.Player, new Vector2(-188, 0));
        DrawLegendItem(legend, Faction.Imperial, new Vector2(-96, 0));
        DrawLegendItem(legend, Faction.Reformist, new Vector2(2, 0));
        DrawLegendItem(legend, Faction.Native, new Vector2(98, 0));
        DrawLegendItem(legend, Faction.Foreign, new Vector2(190, 0));
        DrawLegendItem(legend, Faction.Neutral, new Vector2(282, 0));
    }

    private void DrawLegendItem(Transform parent, Faction faction, Vector2 pos)
    {
        CreateRect("LegendColor_" + faction, parent, pos + new Vector2(-31, 0), new Vector2(12, 12), ProvinceColor(faction));
        Text label = AddText(parent, FactionName(faction), pos + new Vector2(18, 0), new Vector2(72, 18), 9, TextAnchor.MiddleLeft, new Color(0.88f, 0.80f, 0.62f, 0.96f));
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 7;
        label.resizeTextMaxSize = 9;
    }

    private void DrawLine(Transform parent, Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 mid = (a + b) * 0.5f;
        float length = Vector2.Distance(a, b);
        RectTransform line = CreateRect("Road", parent, mid, new Vector2(length, width), color);
        line.GetComponent<Image>().raycastTarget = false;
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
    }

    private void DrawProvince(Transform map, Province province)
    {
        Color color = ProvinceColor(province.owner);
        if (province.id == selectedProvinceId) color = highlightColor;
        Button button = AddButton(map, province.name, StrategyMapPosition(province), new Vector2(62, 30), () => OnProvinceClicked(province.id), color);
        Text label = button.GetComponentInChildren<Text>();
        Army army = ArmyById(province.armyId);
        label.text = SafeText(province.city, province.name) + (army != null ? "\n" + T("strategy.army_badge", "驻") + army.troops : "");
        label.fontSize = 9;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 7;
        label.resizeTextMaxSize = 9;
        Army selectedArmy = ArmyById(selectedArmyId);
        if (selectedArmy != null && IsAdjacent(selectedArmy.provinceId, province.id) && province.id != selectedArmy.provinceId)
        {
            Image image = button.GetComponent<Image>();
            image.color = province.owner == Faction.Player ? new Color(0.23f, 0.48f, 0.30f) : new Color(0.64f, 0.34f, 0.21f);
        }
        AddText(map, TerrainShortLabel(province), StrategyMapPosition(province) + new Vector2(0, -22), new Vector2(68, 12), 8, TextAnchor.MiddleCenter, new Color(0.12f, 0.095f, 0.065f, 0.82f)).raycastTarget = false;
    }

    private string TerrainShortLabel(Province province)
    {
        if (province == null) return "";
        if (!string.IsNullOrEmpty(province.terrain))
        {
            string terrain = province.terrain;
            int split = terrain.IndexOfAny(new[] { '/', '、', '，', ' ' });
            return split > 0 ? terrain.Substring(0, split) : terrain;
        }
        return !string.IsNullOrEmpty(province.region) ? province.region : "";
    }

    private List<string> ProvinceCities(Province province)
    {
        List<string> result = new List<string>();
        if (province == null) return result;
        if (province.cities != null)
        {
            foreach (string cityName in province.cities)
            {
                if (!string.IsNullOrEmpty(cityName) && !result.Contains(cityName)) result.Add(cityName);
            }
        }
        string mainCity = SafeText(province.city, province.name);
        if (!string.IsNullOrEmpty(mainCity) && !result.Contains(mainCity)) result.Insert(0, mainCity);
        return result;
    }

    private string ProvinceCitySummary(Province province)
    {
        List<string> cities = ProvinceCities(province);
        if (cities.Count == 0) return T("common.none", "无");
        string visible = string.Join(T("common.list_separator", "、"), cities.Take(4).ToArray());
        return cities.Count > 4 ? TF("strategy.city_summary_more", "{0} 等{1}城", visible, cities.Count) : visible;
    }

    private void ShowProvinceCitiesPanel()
    {
        Province province = ProvinceById(selectedProvinceId) ?? provinces.FirstOrDefault();
        if (province == null)
        {
            ShowStrategy();
            return;
        }

        List<string> cities = ProvinceCities(province);
        string roster = string.Join(T("common.list_separator", "、"), cities.ToArray());
        string body = TF("strategy.city_roster_body", "{0}\n区域：{1}\n地形：{2}\n辖城（{3}座）：{4}",
            province.name,
            SafeText(province.region, T("common.unknown", "未知")),
            SafeText(province.terrain, T("common.unknown", "未知")),
            cities.Count,
            roster);
        OpenSystemPopup(TF("strategy.city_roster_title", "{0} 城池名册", province.name), body, new List<Tuple<string, Action>>
        {
            Tuple.Create(T("button.back", "返回"), (Action)ShowStrategy)
        }, ScreenMode.Strategy, "strategy");
    }

    private void DrawStrategySidePanel(Transform parent)
    {
        RectTransform side = CreateUiPanel("Side", parent, new Vector2(430, 0), new Vector2(360, 570));
        Province selectedProvince = ProvinceById(selectedProvinceId) ?? provinces[0];
        Army selectedArmy = ArmyById(selectedArmyId);
        string roads = string.Join(T("common.list_separator", "、"), selectedProvince.roads.Select(id => ProvinceById(id)).Where(p => p != null).Select(p => SafeText(p.city, p.name)).ToArray());
        string citySummary = ProvinceCitySummary(selectedProvince);
        string provinceText = TF("strategy.province_text_v4", "主城：{0}\n辖城：{1}\n区域：{2}\n地形：{3}\n势力：{4}\n城防：{5}  收入：{6}\n驻军：{7}\n道路：{8}\n\n{9}",
            SafeText(selectedProvince.city, selectedProvince.name),
            citySummary,
            SafeText(selectedProvince.region, T("common.unknown", "未知")),
            SafeText(selectedProvince.terrain, T("common.unknown", "未知")),
            FactionName(selectedProvince.owner),
            selectedProvince.defense,
            selectedProvince.income,
            ArmyById(selectedProvince.armyId) != null ? ArmyById(selectedProvince.armyId).name : T("common.none", "无"),
            roads,
            SafeText(selectedProvince.description, T("strategy.no_province_desc", "暂无详细情报。")));
        AddSectionTitle(side, T("strategy.section_theater", "北美战区"), new Vector2(-144, 246), new Vector2(292, 30));
        AddText(side, TF("strategy.command_text", "军衔：{0}  指挥上限：{1}\n{2}", CurrentMilitaryRank(), CommandLimit(), CurrentMissionSummary()), new Vector2(0, 203), new Vector2(320, 58), 14, TextAnchor.UpperLeft, highlightColor);
        AddText(side, CurrentGoalSummary(), new Vector2(0, 150), new Vector2(320, 40), 12, TextAnchor.UpperLeft, muted);

        RectTransform provinceCard = CreateRect("StrategyProvinceCard", side, new Vector2(0, 22), new Vector2(318, 206), new Color(0.96f, 0.88f, 0.68f, 0.46f));
        AddText(provinceCard, selectedProvince.name, new Vector2(-132, 84), new Vector2(200, 24), 16, TextAnchor.MiddleLeft, highlightColor);
        AddButton(provinceCard, T("button.city_roster", "名册"), new Vector2(112, 84), new Vector2(72, 24), ShowProvinceCitiesPanel, new Color(0.43f, 0.58f, 0.48f, 0.96f));
        Text provinceInfo = AddText(provinceCard, provinceText, new Vector2(0, -14), new Vector2(286, 168), 12, TextAnchor.UpperLeft, muted);
        provinceInfo.verticalOverflow = VerticalWrapMode.Truncate;
        provinceInfo.resizeTextForBestFit = true;
        provinceInfo.resizeTextMinSize = 9;
        provinceInfo.resizeTextMaxSize = 12;
        provinceInfo.lineSpacing = 0.9f;

        string armyText = selectedArmy != null
            ? TF("strategy.army_text", "军团：{0}\n兵力：{1}/{2}\n等级：{3}\n经验：{4}\n攻击：{5}\n行军力：{6}/{7}", selectedArmy.name, selectedArmy.troops, selectedArmy.maxTroops, selectedArmy.level, selectedArmy.exp, selectedArmy.attack, selectedArmy.move, selectedArmy.maxMove)
            : T("strategy.no_army_hint", "点击有我方军团的城池可选择军团。\n点击相邻己方城池行军。\n点击相邻敌方城池发起六边形战棋。");
        if (selectedArmy != null)
        {
            armyText += "\n" + TF("strategy.army_extra_text_v2", "补给：{0}\n战术：{1}\n情报：{2}", SupplyStatus(selectedArmy), AiProfileForArmy(selectedArmy).name, selectedArmy.faction == Faction.Player ? player.intelligence.ToString() : KnownArmyIntelText(selectedArmy));
        }
        RectTransform armyCard = CreateRect("StrategyArmyCard", side, new Vector2(0, -162), new Vector2(318, 138), new Color(0.96f, 0.88f, 0.68f, 0.36f));
        AddText(armyCard, T("strategy.section_army", "军团行动"), new Vector2(-136, 52), new Vector2(160, 22), 14, TextAnchor.MiddleLeft, highlightColor);
        Text armyInfo = AddText(armyCard, armyText, new Vector2(0, -10), new Vector2(286, 104), 12, TextAnchor.UpperLeft, selectedArmy != null ? ink : muted);
        armyInfo.verticalOverflow = VerticalWrapMode.Truncate;
        armyInfo.resizeTextForBestFit = true;
        armyInfo.resizeTextMinSize = 9;
        armyInfo.resizeTextMaxSize = 12;
        armyInfo.lineSpacing = 0.9f;

        Text logText = AddText(side, T("label.recent_news", "最近消息：") + "\n" + LatestLog(3), new Vector2(0, -254), new Vector2(320, 72), 11, TextAnchor.UpperLeft, muted);
        logText.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private string CurrentMissionSummary()
    {
        if (player.commandLockTurns > 0) return TF("mission.locked", "军令：指挥权暂停 {0} 回合", player.commandLockTurns);
        int cycle = Mathf.Max(1, BattleCore().strategyMissionCycleLength);
        if (strategyTurn % cycle == 1) return T("mission.capture_enemy", "军令：夺取相邻敌省");
        if (strategyTurn % cycle == 2) return T("mission.supply_line", "军令：稳固防线并补给");
        return T("mission.scout_neutral", "军令：侦察中立地带");
    }

    private void ShowMissionBrief()
    {
        pendingStoryTitle = T("mission.title", "上级军令");
        pendingStorySceneId = "council";
        pendingStoryPortraitName = player.name;
        pendingStoryBody =
            TF("mission.body", "当前军衔：{0}\n可指挥直属部队：{1}\n\n{2}\n\n攻占敌方据点会获得战功。未达成命令或战败时，会短暂失去直接指挥权。", CurrentMilitaryRank(), CommandLimit(), CurrentMissionSummary());
        pendingStoryOptions = new List<Tuple<string, Action>>
        {
            Tuple.Create(T("mission.option_strategy", "查看战略地图"), (Action)ShowStrategy)
        };
        pendingStoryReturnAction = ShowStrategy;
        ShowStoryEvent();
    }

    private void OnProvinceClicked(string provinceId)
    {
        Province province = ProvinceById(provinceId);
        if (province == null) return;
        selectedProvinceId = provinceId;

        if (selectedArmyId != null)
        {
            Army army = ArmyById(selectedArmyId);
            if (army != null && army.provinceId == provinceId)
            {
                selectedArmyId = null;
                AddLog(T("log.cancel_army_selection", "取消军团选择。"));
                ShowStrategy();
                return;
            }
            if (player.commandLockTurns > 0)
            {
                AddLog(TF("log.command_locked", "你暂时被解除指挥权，还需等待 {0} 回合。", player.commandLockTurns));
                ShowStrategy();
                return;
            }
            if (army != null && !IsAdjacent(army.provinceId, provinceId))
            {
                AddLog(T("log.army_not_adjacent", "军团只能沿道路进入相邻省份。"));
                ShowStrategy();
                return;
            }
            if (army != null && army.move <= 0)
            {
                AddLog(T("log.army_no_move", "该军团本回合已经用尽行军力。"));
                ShowStrategy();
                return;
            }
            if (army != null && province.owner == Faction.Player)
            {
                MoveArmyToProvince(army, province);
                selectedArmyId = null;
                ShowStrategy();
                return;
            }
            if (army != null && province.owner != Faction.Player)
            {
                StartBattle(army, province);
                return;
            }
        }

        Army localArmy = ArmyById(province.armyId);
        if (localArmy != null && localArmy.faction == Faction.Player)
        {
            selectedArmyId = localArmy.id;
            AddLog(TF("log.army_selected", "选中{0}。", localArmy.name));
        }
        else if (localArmy == null && province.owner == Faction.Player)
        {
            AddLog(T("log.no_local_army", "这座省份没有可指挥的我方军团。"));
        }
        ShowStrategy();
    }

    private void MoveArmyToProvince(Army army, Province province)
    {
        Province from = ProvinceById(army.provinceId);
        if (from != null) from.armyId = "";
        army.provinceId = province.id;
        army.move -= 1;
        ConsumeArmySupply(army, "move");
        province.armyId = army.id;
        AddLog(TF("log.army_moved", "{0}沿道路进驻{1}。", army.name, province.name));
    }

    private void ShowBattleLabEditor()
    {
        mode = ScreenMode.BattleLab;
        pendingStoryReturnAction = null;
        battle = null;
        selectedUnitId = null;
        battleAnimations.Clear();
        battleUnitViews.Clear();
        battleUnitBadges.Clear();
        battleUnitSprites.Clear();
        battleLabSpawnSprites.Clear();
        RemoveBattleLabTempArmies();
        EnsureBattleLabDesign();
        battleTerrainOverride = battleLabDesign.terrainTiles;
        Clear();
        DrawSceneBackground("battlefield");
        AddTopBar(root, T("battle_lab.title", "战棋工坊"));
        RectTransform board = CreateRect("BattleLabBoard", root, new Vector2(-160, 20), new Vector2(800, 560), new Color(0.46f, 0.47f, 0.40f, 0.96f));
        battleBoardContent = CreateEmptyRect("BattleLabMapContent", board, new Vector2(0, 12), new Vector2(900, 570));
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                DrawBattleLabHexTile(battleBoardContent, q, r);
            }
        }
        foreach (BattleUnitSpawnConfig spawn in battleLabDesign.spawns.ToList())
        {
            DrawBattleLabSpawnBadge(battleBoardContent, spawn);
        }
        DrawBattleLabSidePanel(root);
    }

    private void EnsureBattleLabDesign()
    {
        if (battleLabDesign == null)
        {
            string raw = PlayerPrefs.GetString(BattleLabSaveKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    battleLabDesign = JsonUtility.FromJson<BattleLevelDesign>(raw);
                }
                catch
                {
                    battleLabDesign = null;
                }
            }
            if (battleLabDesign == null) battleLabDesign = DefaultBattleLabDesign();
        }
        NormalizeBattleLabDesign();
    }

    private BattleLevelDesign DefaultBattleLabDesign()
    {
        BattleLevelDesign design = new BattleLevelDesign
        {
            name = T("battle_lab.default_name", "工坊测试关"),
            author = player != null && !string.IsNullOrEmpty(player.name) ? player.name : T("battle_lab.default_author", "策划"),
            description = T("battle_lab.default_description", "演示占点、歼灭、抵达和剧情触发的测试关卡。"),
            hexCols = BattleCore().hexCols,
            hexRows = BattleCore().hexRows,
            objectiveType = "capture",
            objectiveQ = BattleCore().objectiveQ,
            objectiveR = BattleCore().objectiveR,
            turnLimit = 0,
            weather = "clear",
            enemyAiProfile = "tactical",
            playerTroops = 420,
            enemyTroops = 420,
            playerAttack = 18,
            enemyAttack = 18,
            terrainTiles = DefaultBattleLabTerrainTiles(),
            spawns = BattleUnitSpawns().Select(CopyBattleSpawn).ToList(),
            triggers = new List<BattleLabTriggerConfig>()
        };
        return design;
    }

    private List<BattleTerrainTileConfig> DefaultBattleLabTerrainTiles()
    {
        List<BattleTerrainTileConfig> source = gameConfig.battleTerrainTiles != null && gameConfig.battleTerrainTiles.Count > 0
            ? gameConfig.battleTerrainTiles
            : null;
        if (source != null)
        {
            return source.Select(CopyBattleTerrain).ToList();
        }

        List<BattleTerrainTileConfig> tiles = new List<BattleTerrainTileConfig>();
        int cols = BattleHexCols();
        int rows = BattleHexRows();
        for (int r = 0; r < rows; r++)
        {
            for (int q = 0; q < cols; q++)
            {
                string terrain = "plain";
                if (q == BattleCore().objectiveQ && r == BattleCore().objectiveR) terrain = "city";
                else if ((q == 2 && r == 1) || (q == 6 && r == 4) || (q == 7 && r == 2)) terrain = "mountain";
                else if ((q + r) % 5 == 0 || (q == 1 && r == 4) || (q == 5 && r == 2)) terrain = "forest";
                else if ((r == 5 && q > 1 && q < 8) || (q == 3 && r == 3)) terrain = "river";
                tiles.Add(new BattleTerrainTileConfig { q = q, r = r, terrain = terrain });
            }
        }
        return tiles;
    }

    private BattleTerrainTileConfig CopyBattleTerrain(BattleTerrainTileConfig tile)
    {
        return new BattleTerrainTileConfig
        {
            q = tile.q,
            r = tile.r,
            terrain = string.IsNullOrEmpty(tile.terrain) ? "plain" : tile.terrain
        };
    }

    private BattleUnitSpawnConfig CopyBattleSpawn(BattleUnitSpawnConfig spawn)
    {
        return new BattleUnitSpawnConfig
        {
            side = string.IsNullOrEmpty(spawn.side) ? "attacker" : spawn.side,
            suffix = spawn.suffix,
            role = string.IsNullOrEmpty(spawn.role) ? "infantry" : spawn.role,
            q = spawn.q,
            r = spawn.r,
            attackBonus = spawn.attackBonus,
            troopDivisor = spawn.troopDivisor <= 0 ? 4 : spawn.troopDivisor
        };
    }

    private BattleLabTriggerConfig CopyBattleLabTrigger(BattleLabTriggerConfig trigger)
    {
        return new BattleLabTriggerConfig
        {
            id = string.IsNullOrEmpty(trigger.id) ? NewBattleLabTriggerId() : trigger.id,
            kind = string.IsNullOrEmpty(trigger.kind) ? "reach" : trigger.kind,
            side = string.IsNullOrEmpty(trigger.side) ? "attacker" : trigger.side,
            role = string.IsNullOrEmpty(trigger.role) ? "any" : trigger.role,
            q = trigger.q,
            r = trigger.r,
            radius = Mathf.Clamp(trigger.radius, 0, 4),
            title = string.IsNullOrEmpty(trigger.title) ? BattleLabTriggerPresetTitle(battleLabTriggerStoryPreset) : trigger.title,
            body = string.IsNullOrEmpty(trigger.body) ? BattleLabTriggerPresetBody(battleLabTriggerStoryPreset) : trigger.body,
            action = string.IsNullOrEmpty(trigger.action) ? "none" : trigger.action,
            actionSide = string.IsNullOrEmpty(trigger.actionSide) ? (trigger.kind == "reach" ? "attacker" : "defender") : trigger.actionSide,
            actionRole = string.IsNullOrEmpty(trigger.actionRole) ? (string.IsNullOrEmpty(trigger.role) ? "infantry" : trigger.role) : trigger.actionRole,
            actionValue = trigger.actionValue == 0 ? 1 : trigger.actionValue,
            once = trigger.once
        };
    }

    private void NormalizeBattleLabDesign()
    {
        if (battleLabDesign.terrainTiles == null) battleLabDesign.terrainTiles = new List<BattleTerrainTileConfig>();
        if (battleLabDesign.spawns == null) battleLabDesign.spawns = new List<BattleUnitSpawnConfig>();
        if (battleLabDesign.triggers == null) battleLabDesign.triggers = new List<BattleLabTriggerConfig>();
        if (string.IsNullOrEmpty(battleLabDesign.name)) battleLabDesign.name = T("battle_lab.default_name", "工坊测试关");
        if (string.IsNullOrEmpty(battleLabDesign.author)) battleLabDesign.author = T("battle_lab.default_author", "策划");
        if (string.IsNullOrEmpty(battleLabDesign.description)) battleLabDesign.description = T("battle_lab.default_description", "使用地图编辑器制作的战棋关卡。");
        if (string.IsNullOrEmpty(battleLabDesign.weather)) battleLabDesign.weather = "clear";
        if (string.IsNullOrEmpty(battleLabDesign.enemyAiProfile)) battleLabDesign.enemyAiProfile = "tactical";
        if (battleLabDesign.playerTroops <= 0) battleLabDesign.playerTroops = 420;
        if (battleLabDesign.enemyTroops <= 0) battleLabDesign.enemyTroops = 420;
        if (battleLabDesign.playerAttack <= 0) battleLabDesign.playerAttack = 18;
        if (battleLabDesign.enemyAttack <= 0) battleLabDesign.enemyAttack = 18;
        battleLabDesign.turnLimit = Mathf.Clamp(battleLabDesign.turnLimit, 0, 99);
        if (battleLabDesign.hexCols <= 0) battleLabDesign.hexCols = BattleCore().hexCols;
        if (battleLabDesign.hexRows <= 0) battleLabDesign.hexRows = BattleCore().hexRows;
        battleLabDesign.hexCols = Mathf.Clamp(battleLabDesign.hexCols, BattleLabMinCols(), BattleLabMaxCols());
        battleLabDesign.hexRows = Mathf.Clamp(battleLabDesign.hexRows, BattleLabMinRows(), BattleLabMaxRows());
        if (string.IsNullOrEmpty(battleLabDesign.objectiveType)) battleLabDesign.objectiveType = "capture";
        battleLabDesign.objectiveQ = Mathf.Clamp(battleLabDesign.objectiveQ, 0, BattleHexCols() - 1);
        battleLabDesign.objectiveR = Mathf.Clamp(battleLabDesign.objectiveR, 0, BattleHexRows() - 1);

        Dictionary<string, BattleTerrainTileConfig> terrainByCell = new Dictionary<string, BattleTerrainTileConfig>();
        foreach (BattleTerrainTileConfig tile in battleLabDesign.terrainTiles)
        {
            if (tile == null || !InsideHex(tile.q, tile.r)) continue;
            string key = tile.q + ":" + tile.r;
            terrainByCell[key] = new BattleTerrainTileConfig { q = tile.q, r = tile.r, terrain = string.IsNullOrEmpty(tile.terrain) ? "plain" : tile.terrain };
        }
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                string key = q + ":" + r;
                if (!terrainByCell.ContainsKey(key)) terrainByCell[key] = new BattleTerrainTileConfig { q = q, r = r, terrain = "plain" };
            }
        }
        battleLabDesign.terrainTiles = terrainByCell.Values.OrderBy(t => t.r).ThenBy(t => t.q).ToList();

        Dictionary<string, BattleUnitSpawnConfig> spawnByCell = new Dictionary<string, BattleUnitSpawnConfig>();
        foreach (BattleUnitSpawnConfig spawn in battleLabDesign.spawns)
        {
            if (spawn == null || !InsideHex(spawn.q, spawn.r)) continue;
            string key = spawn.q + ":" + spawn.r;
            spawnByCell[key] = CopyBattleSpawn(spawn);
        }
        battleLabDesign.spawns = spawnByCell.Values.OrderBy(s => s.side == "defender" ? 1 : 0).ThenBy(s => s.r).ThenBy(s => s.q).ToList();

        List<BattleLabTriggerConfig> triggers = new List<BattleLabTriggerConfig>();
        HashSet<string> usedIds = new HashSet<string>();
        foreach (BattleLabTriggerConfig trigger in battleLabDesign.triggers)
        {
            if (trigger == null || !InsideHex(trigger.q, trigger.r)) continue;
            BattleLabTriggerConfig copy = CopyBattleLabTrigger(trigger);
            if (usedIds.Contains(copy.id)) copy.id = NewBattleLabTriggerId();
            usedIds.Add(copy.id);
            triggers.Add(copy);
        }
        battleLabDesign.triggers = triggers.OrderBy(t => t.r).ThenBy(t => t.q).ThenBy(t => t.kind).ToList();
    }

    private void DrawBattleLabHexTile(Transform board, int q, int r)
    {
        Vector2 pos = HexScreen(q, r);
        Color color = TerrainColor(q, r);
        if (q == BattleObjectiveQ() && r == BattleObjectiveR()) color = new Color(0.66f, 0.52f, 0.22f);
        BattleLabTriggerConfig trigger = BattleLabTriggerAt(q, r);
        BattleLabTriggerConfig triggerArea = trigger ?? BattleLabTriggerCoveringCell(q, r);
        GameObject go = new GameObject("BattleLabHex_" + q + "_" + r);
        go.transform.SetParent(board, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        EnsureCanvasRenderer(go);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(78, 68);
        HexTileGraphic graphic = go.AddComponent<HexTileGraphic>();
        graphic.color = color;
        graphic.strokeColor = trigger != null ? new Color(0.72f, 0.32f, 0.82f, 0.96f) : triggerArea != null ? new Color(0.54f, 0.28f, 0.68f, 0.68f) : BattleLabSpawnAt(q, r) != null ? highlightColor : new Color(0.18f, 0.19f, 0.17f, 0.92f);
        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = graphic;
        button.onClick.AddListener(() => ActivateOnce("battle_lab_hex_" + q + "_" + r, () => OnBattleLabHexClicked(q, r)));

        string label = q == BattleObjectiveQ() && r == BattleObjectiveR()
            ? T("battle.objective", "据点")
            : TerrainName(q, r);
        string triggerLabel = trigger == null ? "" : "\n" + BattleLabTriggerShortLabel(trigger);
        Text text = CreateText("BattleLabTileLabel", rt, label + "\n" + q + "," + r + triggerLabel, trigger == null ? 10 : 9, ink, TextAnchor.MiddleCenter);
        text.raycastTarget = false;
        RectTransform labelRt = text.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
    }

    private void DrawBattleLabSpawnBadge(Transform board, BattleUnitSpawnConfig spawn)
    {
        if (spawn == null || !InsideHex(spawn.q, spawn.r)) return;
        bool defender = string.Equals(spawn.side, "defender", StringComparison.OrdinalIgnoreCase);
        RectTransform rt = CreateRect("BattleLabSpawn_" + spawn.q + "_" + spawn.r, board, HexScreen(spawn.q, spawn.r) + new Vector2(0, 5), new Vector2(84, 84), defender ? enemyColor : playerColor);
        Image image = rt.GetComponent<Image>();
        image.color = defender ? new Color(0.58f, 0.16f, 0.14f, 0.28f) : new Color(0.13f, 0.33f, 0.60f, 0.28f);
        Button button = rt.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(() => ActivateOnce("battle_lab_unit_" + spawn.q + "_" + spawn.r, () => OnBattleLabHexClicked(spawn.q, spawn.r)));

        CommonBattleUnitConfig unitConfig = CommonUnitForSpawn(spawn);
        Sprite unitSprite = LoadBattleUnitSprite(unitConfig, "idle", BattleLabIdleFrame(unitConfig));
        if (unitSprite != null)
        {
            RectTransform spriteRt = CreateRect("BattleLabUnitSprite", rt, new Vector2(0, 4), new Vector2(82, 82), new Color(1f, 1f, 1f, 0f));
            Image spriteImage = spriteRt.GetComponent<Image>();
            spriteImage.sprite = unitSprite;
            spriteImage.color = Color.white;
            spriteImage.preserveAspect = true;
            spriteImage.raycastTarget = false;
            battleLabSpawnSprites[BattleLabCellKey(spawn.q, spawn.r)] = spriteImage;
        }

        RectTransform labelBack = CreateRect("BattleLabUnitLabelBack", rt, new Vector2(0, -34), new Vector2(72, 18), new Color(0.04f, 0.03f, 0.025f, 0.66f));
        labelBack.GetComponent<Image>().raycastTarget = false;
        Text text = AddText(rt, RoleName(spawn.role), new Vector2(0, -34), new Vector2(68, 16), 10, TextAnchor.MiddleCenter, Color.white);
        text.raycastTarget = false;
    }

    private string BattleLabCellKey(int q, int r)
    {
        return q + ":" + r;
    }

    private int BattleLabIdleFrame(CommonBattleUnitConfig config)
    {
        return 0;
    }

    private void RefreshBattleLabSpawnSprites()
    {
        if (mode != ScreenMode.BattleLab || battleLabSpawnSprites.Count == 0 || battleLabDesign == null) return;
        foreach (KeyValuePair<string, Image> pair in battleLabSpawnSprites.ToList())
        {
            if (pair.Value == null)
            {
                battleLabSpawnSprites.Remove(pair.Key);
                continue;
            }

            BattleUnitSpawnConfig spawn = battleLabDesign.spawns.FirstOrDefault(s => s != null && BattleLabCellKey(s.q, s.r) == pair.Key);
            CommonBattleUnitConfig config = CommonUnitForSpawn(spawn);
            Sprite sprite = LoadBattleUnitSprite(config, "idle", BattleLabIdleFrame(config));
            if (sprite != null) pair.Value.sprite = sprite;
        }
    }

    private void DrawBattleLabSidePanel(Transform parent)
    {
        RectTransform side = CreateUiPanel("BattleLabSide", parent, new Vector2(430, -2), new Vector2(360, 610));
        if (!new[] { "map", "brush", "trigger", "file" }.Contains(battleLabTab)) battleLabTab = "map";
        int playerCount = battleLabDesign.spawns.Count(s => !string.Equals(s.side, "defender", StringComparison.OrdinalIgnoreCase));
        int enemyCount = battleLabDesign.spawns.Count(s => string.Equals(s.side, "defender", StringComparison.OrdinalIgnoreCase));
        int triggerCount = battleLabDesign.triggers.Count;
        string body = TF("battle_lab.editor_summary", "{0}\n{1} x {2}｜目标：{3}｜AI：{4}\n画笔：{5}｜地形：{6}｜兵种：{7}\n蓝/红：{8}/{9}｜触发器：{10}",
            battleLabDesign.name,
            BattleHexCols(),
            BattleHexRows(),
            BattleLabObjectiveTypeLabel(),
            AiProfileName(battleLabDesign.enemyAiProfile),
            BattleLabBrushLabel(),
            TerrainDisplayName(battleLabTerrain),
            RoleName(battleLabRole),
            playerCount,
            enemyCount,
            triggerCount);
        AddText(side, body, new Vector2(0, 246), new Vector2(328, 86), 12, TextAnchor.UpperLeft);

        DrawBattleLabTabButton(side, "map", T("battle_lab.tab_map", "地图"), -123);
        DrawBattleLabTabButton(side, "brush", T("battle_lab.tab_brush", "画笔"), -41);
        DrawBattleLabTabButton(side, "trigger", T("battle_lab.tab_trigger", "触发"), 41);
        DrawBattleLabTabButton(side, "file", T("battle_lab.tab_file", "文件"), 123);

        if (battleLabTab == "map") DrawBattleLabMapTab(side);
        else if (battleLabTab == "brush") DrawBattleLabBrushTab(side);
        else if (battleLabTab == "trigger") DrawBattleLabTriggerTab(side);
        else DrawBattleLabFileTab(side);

        AddText(side, string.IsNullOrEmpty(battleLabMessage) ? T("battle_lab.message_default", "像地图编辑器一样先定地图属性，再刷地形、摆单位、加触发器，最后开始测试。") : battleLabMessage, new Vector2(0, -254), new Vector2(328, 52), 12, TextAnchor.UpperLeft, muted);
        AddFlatButton(side, T("battle_lab.test", "开始测试"), new Vector2(-82, -292), new Vector2(148, 28), StartBattleLabTest, new Color(0.24f, 0.36f, 0.24f, 0.96f), 12);
        AddFlatButton(side, T("button.back_strategy", "返回战略"), new Vector2(82, -292), new Vector2(148, 28), ShowStrategy, null, 12);
    }

    private void DrawBattleLabTabButton(Transform side, string tab, string label, float x)
    {
        Color? color = battleLabTab == tab ? (Color?)highlightColor : null;
        AddFlatButton(side, label, new Vector2(x, 183), new Vector2(72, 28), () => SetBattleLabTab(tab), color, 12);
    }

    private void SetBattleLabTab(string tab)
    {
        battleLabTab = tab;
        ShowBattleLabEditor();
    }

    private void DrawBattleLabMapTab(Transform side)
    {
        AddText(side, T("battle_lab.map_section", "地图属性"), new Vector2(0, 149), new Vector2(320, 24), 16, TextAnchor.MiddleLeft, highlightColor);
        AddText(side, T("battle_lab.name_label", "名称"), new Vector2(-132, 117), new Vector2(58, 24), 12, TextAnchor.MiddleLeft, muted);
        AddInputField(side, battleLabDesign.name, T("battle_lab.name_placeholder", "关卡名"), new Vector2(48, 117), new Vector2(248, 28), value => battleLabDesign.name = SafeText(value, T("battle_lab.default_name", "工坊测试关")), 18, 14);
        AddText(side, T("battle_lab.author_label", "作者"), new Vector2(-132, 83), new Vector2(58, 24), 12, TextAnchor.MiddleLeft, muted);
        AddInputField(side, battleLabDesign.author, T("battle_lab.author_placeholder", "策划名"), new Vector2(48, 83), new Vector2(248, 28), value => battleLabDesign.author = SafeText(value, T("battle_lab.default_author", "策划")), 12, 14);
        AddText(side, T("battle_lab.desc_label", "说明"), new Vector2(-132, 49), new Vector2(58, 24), 12, TextAnchor.MiddleLeft, muted);
        AddInputField(side, battleLabDesign.description, T("battle_lab.desc_placeholder", "一句话说明目标"), new Vector2(48, 49), new Vector2(248, 28), value => battleLabDesign.description = SafeText(value, T("battle_lab.default_description", "战棋测试关卡")), 48, 13);

        AddFlatButton(side, T("battle_lab.cols_minus", "列 -"), new Vector2(-123, 10), new Vector2(72, 26), () => ResizeBattleLabMap(-1, 0), null, 11);
        AddFlatButton(side, T("battle_lab.cols_plus", "列 +"), new Vector2(-41, 10), new Vector2(72, 26), () => ResizeBattleLabMap(1, 0), null, 11);
        AddFlatButton(side, T("battle_lab.rows_minus", "行 -"), new Vector2(41, 10), new Vector2(72, 26), () => ResizeBattleLabMap(0, -1), null, 11);
        AddFlatButton(side, T("battle_lab.rows_plus", "行 +"), new Vector2(123, 10), new Vector2(72, 26), () => ResizeBattleLabMap(0, 1), null, 11);

        AddFlatButton(side, TF("battle_lab.objective_type_button", "目标：{0}", BattleLabObjectiveTypeLabel()), new Vector2(-82, -28), new Vector2(148, 26), CycleBattleLabObjectiveType, new Color(0.20f, 0.18f, 0.12f, 0.96f), 11);
        AddFlatButton(side, TF("battle_lab.turn_limit_button", "回合：{0}", battleLabDesign.turnLimit <= 0 ? T("common.unlimited", "不限") : battleLabDesign.turnLimit.ToString()), new Vector2(82, -28), new Vector2(148, 26), () => AdjustBattleLabTurnLimit(1), null, 11);
        AddFlatButton(side, T("battle_lab.turn_minus", "回合 -"), new Vector2(-82, -64), new Vector2(148, 26), () => AdjustBattleLabTurnLimit(-1), null, 11);
        AddFlatButton(side, TF("battle_lab.weather_button", "天气：{0}", BattleLabWeatherLabel()), new Vector2(82, -64), new Vector2(148, 26), CycleBattleLabWeather, null, 11);
        AddFlatButton(side, TF("battle_lab.enemy_ai_button", "敌AI：{0}", AiProfileName(battleLabDesign.enemyAiProfile)), new Vector2(0, -100), new Vector2(310, 26), CycleBattleLabEnemyAi, new Color(0.18f, 0.16f, 0.22f, 0.96f), 11);
        AddFlatButton(side, T("battle_lab.player_power_minus", "蓝军 -"), new Vector2(-123, -137), new Vector2(72, 26), () => AdjustBattleLabTestPower("player", -20), null, 11);
        AddFlatButton(side, T("battle_lab.player_power_plus", "蓝军 +"), new Vector2(-41, -137), new Vector2(72, 26), () => AdjustBattleLabTestPower("player", 20), null, 11);
        AddFlatButton(side, T("battle_lab.enemy_power_minus", "红军 -"), new Vector2(41, -137), new Vector2(72, 26), () => AdjustBattleLabTestPower("enemy", -20), null, 11);
        AddFlatButton(side, T("battle_lab.enemy_power_plus", "红军 +"), new Vector2(123, -137), new Vector2(72, 26), () => AdjustBattleLabTestPower("enemy", 20), null, 11);
        AddText(side, TF("battle_lab.power_line", "测试兵力：蓝{0}/红{1}  攻击：蓝{2}/红{3}", battleLabDesign.playerTroops, battleLabDesign.enemyTroops, battleLabDesign.playerAttack, battleLabDesign.enemyAttack), new Vector2(0, -174), new Vector2(318, 28), 12, TextAnchor.MiddleLeft, muted);
    }

    private void DrawBattleLabBrushTab(Transform side)
    {
        AddText(side, T("battle_lab.brush_section", "画笔与对象"), new Vector2(0, 149), new Vector2(320, 24), 16, TextAnchor.MiddleLeft, highlightColor);
        AddFlatButton(side, T("battle_lab.brush_terrain", "地形"), new Vector2(-110, 112), new Vector2(96, 28), () => SetBattleLabBrush("terrain"), battleLabBrush == "terrain" ? (Color?)highlightColor : null, 12);
        AddFlatButton(side, T("battle_lab.brush_player", "蓝方"), new Vector2(0, 112), new Vector2(96, 28), () => SetBattleLabBrush("player"), battleLabBrush == "player" ? (Color?)highlightColor : null, 12);
        AddFlatButton(side, T("battle_lab.brush_enemy", "红方"), new Vector2(110, 112), new Vector2(96, 28), () => SetBattleLabBrush("enemy"), battleLabBrush == "enemy" ? (Color?)highlightColor : null, 12);
        AddFlatButton(side, T("battle_lab.brush_objective", "据点"), new Vector2(-110, 76), new Vector2(96, 28), () => SetBattleLabBrush("objective"), battleLabBrush == "objective" ? (Color?)highlightColor : null, 12);
        AddFlatButton(side, T("battle_lab.brush_erase", "擦除"), new Vector2(0, 76), new Vector2(96, 28), () => SetBattleLabBrush("erase"), battleLabBrush == "erase" ? (Color?)new Color(0.45f, 0.18f, 0.18f) : null, 12);
        AddFlatButton(side, TF("battle_lab.brush_size", "尺寸 {0}", battleLabBrushSize), new Vector2(110, 76), new Vector2(96, 28), CycleBattleLabBrushSize, null, 12);

        AddFlatButton(side, T("battle_lab.prev_terrain", "上一地形"), new Vector2(-82, 32), new Vector2(148, 28), () => CycleBattleLabTerrain(-1), null, 12);
        AddFlatButton(side, T("battle_lab.next_terrain", "下一地形"), new Vector2(82, 32), new Vector2(148, 28), () => CycleBattleLabTerrain(1), null, 12);
        AddFlatButton(side, T("battle_lab.prev_role", "上一兵种"), new Vector2(-82, -4), new Vector2(148, 28), () => CycleBattleLabRole(-1), null, 12);
        AddFlatButton(side, T("battle_lab.next_role", "下一兵种"), new Vector2(82, -4), new Vector2(148, 28), () => CycleBattleLabRole(1), null, 12);
        AddFlatButton(side, T("battle_lab.fill_terrain", "填充当前地形"), new Vector2(-82, -48), new Vector2(148, 28), FillBattleLabTerrain, new Color(0.18f, 0.22f, 0.16f, 0.96f), 12);
        AddFlatButton(side, T("battle_lab.mirror_terrain", "镜像地形"), new Vector2(82, -48), new Vector2(148, 28), MirrorBattleLabTerrain, null, 12);
        AddFlatButton(side, T("battle_lab.clear_units", "清空单位"), new Vector2(-82, -84), new Vector2(148, 28), ClearBattleLabUnits, new Color(0.34f, 0.16f, 0.16f, 0.96f), 12);
        AddFlatButton(side, T("battle_lab.clear_map", "清空地图"), new Vector2(82, -84), new Vector2(148, 28), ClearBattleLabMap, new Color(0.34f, 0.16f, 0.16f, 0.96f), 12);
        AddText(side, TF("battle_lab.brush_hint", "当前：{0} / {1} / {2}\n地形和擦除支持尺寸画笔；单位、据点和触发器按单格放置。", BattleLabBrushLabel(), TerrainDisplayName(battleLabTerrain), RoleName(battleLabRole)), new Vector2(0, -145), new Vector2(318, 72), 12, TextAnchor.UpperLeft, muted);
    }

    private void DrawBattleLabTriggerTab(Transform side)
    {
        AddText(side, T("battle_lab.trigger_section", "触发器"), new Vector2(0, 149), new Vector2(320, 24), 16, TextAnchor.MiddleLeft, highlightColor);
        AddFlatButton(side, T("battle_lab.brush_trigger_reach", "抵达剧情"), new Vector2(-82, 112), new Vector2(148, 28), () => SetBattleLabBrush("trigger_reach"), battleLabBrush == "trigger_reach" ? (Color?)highlightColor : null, 12);
        AddFlatButton(side, T("battle_lab.brush_trigger_defeat", "击败剧情"), new Vector2(82, 112), new Vector2(148, 28), () => SetBattleLabBrush("trigger_defeat"), battleLabBrush == "trigger_defeat" ? (Color?)highlightColor : null, 12);
        AddFlatButton(side, TF("battle_lab.trigger_story_button", "剧情：{0}", BattleLabTriggerPresetTitle(battleLabTriggerStoryPreset)), new Vector2(-82, 76), new Vector2(148, 28), CycleBattleLabTriggerStory, new Color(0.18f, 0.16f, 0.22f, 0.96f), 12);
        AddFlatButton(side, TF("battle_lab.trigger_action_button", "动作：{0}", BattleLabTriggerActionLabel(battleLabTriggerAction)), new Vector2(82, 76), new Vector2(148, 28), CycleBattleLabTriggerAction, new Color(0.16f, 0.20f, 0.18f, 0.96f), 12);
        AddFlatButton(side, T("battle_lab.clear_triggers", "清空触发"), new Vector2(0, 40), new Vector2(310, 28), ClearBattleLabTriggers, new Color(0.34f, 0.16f, 0.16f, 0.96f), 12);

        string triggerLines = battleLabDesign.triggers.Count == 0
            ? T("battle_lab.no_triggers", "暂无触发器。")
            : string.Join("\n", battleLabDesign.triggers.Take(6).Select((trigger, index) => TF("battle_lab.trigger_list_row", "{0}. {1}({2},{3}) R{4} {5} -> {6}",
                index + 1,
                BattleLabTriggerShortLabel(trigger),
                trigger.r + 1,
                trigger.q + 1,
                Mathf.Max(0, trigger.radius),
                RoleName(trigger.role),
                BattleLabTriggerActionLabel(trigger.action))).ToArray());
        AddText(side, triggerLines, new Vector2(0, -62), new Vector2(318, 144), 12, TextAnchor.UpperLeft);
        AddText(side, T("battle_lab.trigger_hint", "触发器目前支持：抵达、击败、刷援军、加减士气、直接胜负。后续可继续扩成条件树。"), new Vector2(0, -168), new Vector2(318, 54), 12, TextAnchor.UpperLeft, muted);
    }

    private void DrawBattleLabFileTab(Transform side)
    {
        AddText(side, T("battle_lab.file_section", "文件与测试"), new Vector2(0, 149), new Vector2(320, 24), 16, TextAnchor.MiddleLeft, highlightColor);
        AddFlatButton(side, T("battle_lab.save", "保存草稿"), new Vector2(-82, 112), new Vector2(148, 30), SaveBattleLabDesign, null, 13);
        AddFlatButton(side, T("battle_lab.load", "载入草稿"), new Vector2(82, 112), new Vector2(148, 30), LoadBattleLabDesign, null, 13);
        AddFlatButton(side, T("battle_lab.export_json", "导出JSON"), new Vector2(-82, 72), new Vector2(148, 30), ExportBattleLabDesign, new Color(0.20f, 0.23f, 0.30f, 0.96f), 13);
        AddFlatButton(side, T("battle_lab.import_json", "导入最新JSON"), new Vector2(82, 72), new Vector2(148, 30), ImportLatestBattleLabDesign, new Color(0.20f, 0.23f, 0.30f, 0.96f), 13);
        AddFlatButton(side, T("battle_lab.reset", "重置"), new Vector2(-82, 32), new Vector2(148, 28), ResetBattleLabDesign, new Color(0.45f, 0.18f, 0.18f, 0.96f), 12);
        AddFlatButton(side, T("battle_lab.open_export_folder", "导出目录"), new Vector2(82, 32), new Vector2(148, 28), ShowBattleLabExportFolder, null, 12);
        AddText(side, TF("battle_lab.file_hint", "导出目录：\n{0}\n\n导出的 JSON 可以提交到 Git，用于团队共享关卡。运行包里会改用持久化目录。", BattleLabExportDirectory()), new Vector2(0, -68), new Vector2(318, 178), 11, TextAnchor.UpperLeft, muted);
    }

    private string AiProfileName(string id)
    {
        AiProfileConfig profile = AiProfiles().FirstOrDefault(p => p.id == id);
        return profile != null ? profile.name : T("ai.default", "均衡型");
    }

    private void AdjustBattleLabTurnLimit(int delta)
    {
        EnsureBattleLabDesign();
        battleLabDesign.turnLimit = Mathf.Clamp(battleLabDesign.turnLimit + delta, 0, 99);
        battleLabMessage = battleLabDesign.turnLimit <= 0
            ? T("battle_lab.turn_unlimited", "回合限制已关闭。")
            : TF("battle_lab.turn_limited", "回合限制：{0} 回合。", battleLabDesign.turnLimit);
        ShowBattleLabEditor();
    }

    private List<string> BattleLabWeatherIds()
    {
        return new List<string> { "clear", "rain", "fog", "night" };
    }

    private string BattleLabWeatherLabel()
    {
        string weather = SafeText(battleLabDesign.weather, "clear");
        if (weather == "rain") return T("battle_lab.weather_rain", "雨");
        if (weather == "fog") return T("battle_lab.weather_fog", "雾");
        if (weather == "night") return T("battle_lab.weather_night", "夜");
        return T("battle_lab.weather_clear", "晴");
    }

    private void CycleBattleLabWeather()
    {
        EnsureBattleLabDesign();
        List<string> ids = BattleLabWeatherIds();
        int index = Mathf.Max(0, ids.IndexOf(SafeText(battleLabDesign.weather, "clear")));
        battleLabDesign.weather = ids[(index + 1) % ids.Count];
        battleLabMessage = TF("battle_lab.weather_changed", "天气已改为：{0}。雨天削弱远程，雾天压低射程，夜战利于散兵。", BattleLabWeatherLabel());
        ShowBattleLabEditor();
    }

    private void CycleBattleLabEnemyAi()
    {
        EnsureBattleLabDesign();
        List<string> ids = AiProfiles().Select(p => p.id).Where(id => !string.IsNullOrEmpty(id)).ToList();
        if (ids.Count == 0) ids.Add("balanced");
        int index = Mathf.Max(0, ids.IndexOf(SafeText(battleLabDesign.enemyAiProfile, ids[0])));
        battleLabDesign.enemyAiProfile = ids[(index + 1) % ids.Count];
        battleLabMessage = TF("battle_lab.enemy_ai_changed", "敌方 AI 已切换为：{0}。", AiProfileName(battleLabDesign.enemyAiProfile));
        ShowBattleLabEditor();
    }

    private void AdjustBattleLabTestPower(string side, int delta)
    {
        EnsureBattleLabDesign();
        bool enemy = side == "enemy";
        if (enemy)
        {
            battleLabDesign.enemyTroops = Mathf.Clamp(battleLabDesign.enemyTroops + delta, 80, 1200);
            battleLabDesign.enemyAttack = Mathf.Clamp(battleLabDesign.enemyAttack + delta / 20, 4, 60);
        }
        else
        {
            battleLabDesign.playerTroops = Mathf.Clamp(battleLabDesign.playerTroops + delta, 80, 1200);
            battleLabDesign.playerAttack = Mathf.Clamp(battleLabDesign.playerAttack + delta / 20, 4, 60);
        }
        battleLabMessage = TF("battle_lab.power_changed", "测试军力已调整：蓝{0}/红{1}。", battleLabDesign.playerTroops, battleLabDesign.enemyTroops);
        ShowBattleLabEditor();
    }

    private void CycleBattleLabBrushSize()
    {
        battleLabBrushSize = battleLabBrushSize >= 3 ? 1 : battleLabBrushSize + 1;
        battleLabMessage = TF("battle_lab.brush_size_changed", "画笔尺寸：{0}。", battleLabBrushSize);
        ShowBattleLabEditor();
    }

    private IEnumerable<Vector2Int> BattleLabBrushCells(int centerQ, int centerR)
    {
        int radius = Mathf.Max(0, battleLabBrushSize - 1);
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                if (HexDistance(centerQ, centerR, q, r) <= radius) yield return new Vector2Int(q, r);
            }
        }
    }

    private void FillBattleLabTerrain()
    {
        EnsureBattleLabDesign();
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                SetBattleLabTerrain(q, r, battleLabTerrain);
            }
        }
        SetBattleLabTerrain(battleLabDesign.objectiveQ, battleLabDesign.objectiveR, "city");
        battleLabMessage = TF("battle_lab.filled", "全图已填充为：{0}。", TerrainDisplayName(battleLabTerrain));
        ShowBattleLabEditor();
    }

    private void MirrorBattleLabTerrain()
    {
        EnsureBattleLabDesign();
        Dictionary<string, string> source = battleLabDesign.terrainTiles.ToDictionary(t => t.q + ":" + t.r, t => t.terrain);
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                int mirrorQ = BattleHexCols() - 1 - q;
                string key = q + ":" + r;
                if (source.TryGetValue(key, out string terrain)) SetBattleLabTerrain(mirrorQ, r, terrain);
            }
        }
        SetBattleLabTerrain(battleLabDesign.objectiveQ, battleLabDesign.objectiveR, "city");
        battleLabMessage = T("battle_lab.mirrored", "已按左右镜像复制地形。");
        ShowBattleLabEditor();
    }

    private void ClearBattleLabUnits()
    {
        EnsureBattleLabDesign();
        battleLabDesign.spawns.Clear();
        battleLabMessage = T("battle_lab.units_cleared", "已清空所有蓝方和红方单位。");
        ShowBattleLabEditor();
    }

    private void ClearBattleLabMap()
    {
        EnsureBattleLabDesign();
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                SetBattleLabTerrain(q, r, "plain");
            }
        }
        SetBattleLabTerrain(battleLabDesign.objectiveQ, battleLabDesign.objectiveR, "city");
        battleLabDesign.spawns.Clear();
        battleLabDesign.triggers.Clear();
        battleLabMessage = T("battle_lab.map_cleared", "已清空地形、单位和触发器，并保留当前据点。");
        ShowBattleLabEditor();
    }

    private List<string> BattleLabTriggerActions()
    {
        return new List<string> { "none", "spawn_enemy", "spawn_player", "morale_enemy", "morale_player", "victory", "defeat" };
    }

    private string BattleLabTriggerActionLabel(string action)
    {
        if (action == "spawn_enemy") return T("battle_lab.action_spawn_enemy", "刷红方");
        if (action == "spawn_player") return T("battle_lab.action_spawn_player", "刷蓝方");
        if (action == "morale_enemy") return T("battle_lab.action_morale_enemy", "红方士气");
        if (action == "morale_player") return T("battle_lab.action_morale_player", "蓝方士气");
        if (action == "victory") return T("battle_lab.action_victory", "直接胜利");
        if (action == "defeat") return T("battle_lab.action_defeat", "直接失败");
        return T("battle_lab.action_none", "仅剧情");
    }

    private void CycleBattleLabTriggerAction()
    {
        List<string> ids = BattleLabTriggerActions();
        int index = Mathf.Max(0, ids.IndexOf(battleLabTriggerAction));
        battleLabTriggerAction = ids[(index + 1) % ids.Count];
        battleLabMessage = TF("battle_lab.trigger_action_changed", "新触发器动作：{0}。", BattleLabTriggerActionLabel(battleLabTriggerAction));
        ShowBattleLabEditor();
    }

    private string BattleLabTriggerActionSide(string action, string triggerKind)
    {
        if (action == "spawn_player" || action == "morale_player") return "attacker";
        if (action == "spawn_enemy" || action == "morale_enemy") return "defender";
        return triggerKind == "reach" ? "attacker" : "defender";
    }

    private string BattleLabExportDirectory()
    {
#if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "Resources", "Data", "BattleLevels");
#else
        return Path.Combine(Application.persistentDataPath, "BattleLevels");
#endif
    }

    private string SafeFileName(string value)
    {
        string raw = SafeText(value, "battle_lab").Trim();
        foreach (char ch in Path.GetInvalidFileNameChars()) raw = raw.Replace(ch, '_');
        raw = raw.Replace(' ', '_');
        return string.IsNullOrEmpty(raw) ? "battle_lab" : raw;
    }

    private string BattleLabExportPath()
    {
        return Path.Combine(BattleLabExportDirectory(), SafeFileName(battleLabDesign.name) + BattleLabExportFileSuffix);
    }

    private void ExportBattleLabDesign()
    {
        EnsureBattleLabDesign();
        try
        {
            string dir = BattleLabExportDirectory();
            Directory.CreateDirectory(dir);
            string path = BattleLabExportPath();
            File.WriteAllText(path, JsonUtility.ToJson(battleLabDesign, true), Encoding.UTF8);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            battleLabMessage = TF("battle_lab.exported", "已导出关卡 JSON：{0}", path);
        }
        catch (Exception ex)
        {
            battleLabMessage = TF("battle_lab.export_failed", "导出失败：{0}", ex.Message);
        }
        ShowBattleLabEditor();
    }

    private void ImportLatestBattleLabDesign()
    {
        try
        {
            string dir = BattleLabExportDirectory();
            if (!Directory.Exists(dir))
            {
                battleLabMessage = TF("battle_lab.import_no_dir", "还没有导出目录：{0}", dir);
                ShowBattleLabEditor();
                return;
            }
            string path = Directory.GetFiles(dir, "*" + BattleLabExportFileSuffix)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(path))
            {
                battleLabMessage = T("battle_lab.import_no_file", "导出目录里没有可导入的关卡 JSON。");
                ShowBattleLabEditor();
                return;
            }
            string raw = File.ReadAllText(path, Encoding.UTF8);
            BattleLevelDesign loaded = JsonUtility.FromJson<BattleLevelDesign>(raw);
            if (loaded == null)
            {
                battleLabMessage = T("battle_lab.import_bad_file", "关卡 JSON 读取失败。");
                ShowBattleLabEditor();
                return;
            }
            battleLabDesign = loaded;
            NormalizeBattleLabDesign();
            battleLabMessage = TF("battle_lab.imported", "已导入最新关卡：{0}", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            battleLabMessage = TF("battle_lab.import_failed", "导入失败：{0}", ex.Message);
        }
        ShowBattleLabEditor();
    }

    private void ShowBattleLabExportFolder()
    {
        string dir = BattleLabExportDirectory();
        Directory.CreateDirectory(dir);
        battleLabMessage = TF("battle_lab.export_folder", "导出目录：{0}", dir);
        Application.OpenURL("file:///" + dir.Replace("\\", "/"));
        ShowBattleLabEditor();
    }

    private void OnBattleLabHexClicked(int q, int r)
    {
        EnsureBattleLabDesign();
        if (battleLabBrush == "terrain")
        {
            foreach (Vector2Int cell in BattleLabBrushCells(q, r)) SetBattleLabTerrain(cell.x, cell.y, battleLabTerrain);
            if (BattleLabBrushCells(q, r).Any(cell => cell.x == battleLabDesign.objectiveQ && cell.y == battleLabDesign.objectiveR))
            {
                SetBattleLabTerrain(battleLabDesign.objectiveQ, battleLabDesign.objectiveR, "city");
            }
            battleLabMessage = TF("battle_lab.msg_terrain_brush", "已用 {0} 格画笔设置地形：{1}。", battleLabBrushSize, TerrainDisplayName(battleLabTerrain));
        }
        else if (battleLabBrush == "player")
        {
            PlaceBattleLabUnit(q, r, "attacker");
            battleLabMessage = TF("battle_lab.msg_player", "已放置蓝方：{0}。", RoleName(battleLabRole));
        }
        else if (battleLabBrush == "enemy")
        {
            PlaceBattleLabUnit(q, r, "defender");
            battleLabMessage = TF("battle_lab.msg_enemy", "已放置红方：{0}。", RoleName(battleLabRole));
        }
        else if (battleLabBrush == "objective")
        {
            battleLabDesign.objectiveQ = q;
            battleLabDesign.objectiveR = r;
            SetBattleLabTerrain(q, r, "city");
            battleLabMessage = T("battle_lab.msg_objective", "已移动中央据点。");
        }
        else if (battleLabBrush == "trigger_reach")
        {
            PlaceBattleLabTrigger(q, r, "reach");
            battleLabMessage = TF("battle_lab.msg_trigger_reach", "已设置抵达剧情：{0}抵达第{1}行第{2}列时触发。", RoleName(battleLabRole), r + 1, q + 1);
        }
        else if (battleLabBrush == "trigger_defeat")
        {
            PlaceBattleLabTrigger(q, r, "defeat");
            battleLabMessage = T("battle_lab.msg_trigger_defeat", "已设置击败剧情：目标单位被击溃时触发。");
        }
        else
        {
            foreach (Vector2Int cell in BattleLabBrushCells(q, r)) EraseBattleLabCell(cell.x, cell.y);
            SetBattleLabTerrain(battleLabDesign.objectiveQ, battleLabDesign.objectiveR, "city");
            battleLabMessage = TF("battle_lab.msg_erase_brush", "已用 {0} 格画笔擦除。", battleLabBrushSize);
        }
        NormalizeBattleLabDesign();
        ShowBattleLabEditor();
    }

    private void SetBattleLabTerrain(int q, int r, string terrain)
    {
        BattleTerrainTileConfig tile = battleLabDesign.terrainTiles.FirstOrDefault(t => t.q == q && t.r == r);
        if (tile == null)
        {
            battleLabDesign.terrainTiles.Add(new BattleTerrainTileConfig { q = q, r = r, terrain = terrain });
        }
        else
        {
            tile.terrain = terrain;
        }
    }

    private void PlaceBattleLabUnit(int q, int r, string side)
    {
        battleLabDesign.spawns.RemoveAll(s => s.q == q && s.r == r);
        battleLabDesign.spawns.Add(new BattleUnitSpawnConfig
        {
            side = side,
            suffix = BattleLabSpawnSuffix(battleLabRole, side),
            role = battleLabRole,
            q = q,
            r = r,
            attackBonus = string.Equals(side, "defender", StringComparison.OrdinalIgnoreCase) ? 2 : 0,
            troopDivisor = 4
        });
    }

    private void EraseBattleLabCell(int q, int r)
    {
        int removed = battleLabDesign.spawns.RemoveAll(s => s.q == q && s.r == r);
        if (battleLabDesign.triggers != null) battleLabDesign.triggers.RemoveAll(t => t.q == q && t.r == r);
        if (removed == 0) SetBattleLabTerrain(q, r, "plain");
    }

    private BattleUnitSpawnConfig BattleLabSpawnAt(int q, int r)
    {
        if (battleLabDesign == null || battleLabDesign.spawns == null) return null;
        return battleLabDesign.spawns.FirstOrDefault(s => s.q == q && s.r == r);
    }

    private BattleLabTriggerConfig BattleLabTriggerAt(int q, int r)
    {
        if (battleLabDesign == null || battleLabDesign.triggers == null) return null;
        return battleLabDesign.triggers.FirstOrDefault(t => t.q == q && t.r == r);
    }

    private BattleLabTriggerConfig BattleLabTriggerCoveringCell(int q, int r)
    {
        if (battleLabDesign == null || battleLabDesign.triggers == null) return null;
        return battleLabDesign.triggers.FirstOrDefault(t => BattleLabTriggerCoversCell(t, q, r));
    }

    private bool BattleLabTriggerCoversCell(BattleLabTriggerConfig trigger, int q, int r)
    {
        return trigger != null && HexDistance(trigger.q, trigger.r, q, r) <= Mathf.Max(0, trigger.radius);
    }

    private void PlaceBattleLabTrigger(int q, int r, string kind)
    {
        if (battleLabDesign.triggers == null) battleLabDesign.triggers = new List<BattleLabTriggerConfig>();
        string triggerKind = kind == "defeat" ? "defeat" : "reach";
        BattleUnitSpawnConfig spawn = BattleLabSpawnAt(q, r);
        string side = triggerKind == "defeat" ? "defender" : "attacker";
        string role = battleLabRole;
        if (triggerKind == "defeat" && spawn != null && string.Equals(spawn.side, "defender", StringComparison.OrdinalIgnoreCase))
        {
            role = string.IsNullOrEmpty(spawn.role) ? battleLabRole : spawn.role;
        }

        battleLabDesign.triggers.RemoveAll(t => t != null && t.q == q && t.r == r && t.kind == triggerKind);
        battleLabDesign.triggers.Add(new BattleLabTriggerConfig
        {
            id = NewBattleLabTriggerId(),
            kind = triggerKind,
            side = side,
            role = role,
            q = q,
            r = r,
            radius = Mathf.Max(0, battleLabBrushSize - 1),
            title = BattleLabTriggerPresetTitle(battleLabTriggerStoryPreset),
            body = BattleLabTriggerPresetBody(battleLabTriggerStoryPreset),
            action = battleLabTriggerAction,
            actionSide = BattleLabTriggerActionSide(battleLabTriggerAction, triggerKind),
            actionRole = battleLabRole,
            actionValue = 1,
            once = true
        });
    }

    private void ClearBattleLabTriggers()
    {
        EnsureBattleLabDesign();
        battleLabDesign.triggers.Clear();
        battleLabMessage = T("battle_lab.triggers_cleared", "已清空本关所有剧情触发器。");
        ShowBattleLabEditor();
    }

    private string NewBattleLabTriggerId()
    {
        return "TRG_" + DateTime.UtcNow.Ticks + "_" + UnityEngine.Random.Range(100, 999);
    }

    private string BattleLabTriggerShortLabel(BattleLabTriggerConfig trigger)
    {
        if (trigger == null) return "";
        return trigger.kind == "defeat" ? T("battle_lab.trigger_defeat_short", "击败") : T("battle_lab.trigger_reach_short", "抵达");
    }

    private string BattleLabSpawnSuffix(string role, string side)
    {
        CommonBattleUnitConfig unit = CommonUnits().FirstOrDefault(u => u.role == role);
        if (unit != null && !string.IsNullOrEmpty(unit.name)) return unit.name;
        return string.Equals(side, "defender", StringComparison.OrdinalIgnoreCase)
            ? T("battle_lab.enemy_unit_suffix", "红方部队")
            : T("battle_lab.player_unit_suffix", "蓝方部队");
    }

    private string BattleLabBrushLabel()
    {
        if (battleLabBrush == "terrain") return T("battle_lab.brush_terrain", "地形");
        if (battleLabBrush == "player") return T("battle_lab.brush_player", "蓝方");
        if (battleLabBrush == "enemy") return T("battle_lab.brush_enemy", "红方");
        if (battleLabBrush == "objective") return T("battle_lab.brush_objective", "据点");
        if (battleLabBrush == "trigger_reach") return T("battle_lab.brush_trigger_reach", "抵达剧情");
        if (battleLabBrush == "trigger_defeat") return T("battle_lab.brush_trigger_defeat", "击败剧情");
        return T("battle_lab.brush_erase", "擦除");
    }

    private void SetBattleLabBrush(string brush)
    {
        battleLabBrush = brush;
        ShowBattleLabEditor();
    }

    private List<string> BattleLabTerrainIds()
    {
        List<string> ids = gameConfig.terrainRules != null
            ? gameConfig.terrainRules.Select(t => t.id).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList()
            : new List<string>();
        foreach (string id in new[] { "plain", "forest", "mountain", "river", "city" })
        {
            if (!ids.Contains(id)) ids.Add(id);
        }
        return ids;
    }

    private List<string> BattleLabRoleIds()
    {
        List<string> roles = CommonUnits().Select(u => u.role).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        foreach (string role in new[] { "infantry", "cavalry", "archer", "musket", "skirmisher", "heavy_infantry", "heavy_spear", "heavy_cavalry", "heavy_archer", "artillery", "brute", "heavy_brute" })
        {
            if (!roles.Contains(role)) roles.Add(role);
        }
        return roles;
    }

    private void CycleBattleLabTerrain(int direction)
    {
        List<string> ids = BattleLabTerrainIds();
        int index = Mathf.Max(0, ids.IndexOf(battleLabTerrain));
        battleLabTerrain = ids[(index + direction + ids.Count) % ids.Count];
        battleLabBrush = "terrain";
        ShowBattleLabEditor();
    }

    private void CycleBattleLabRole(int direction)
    {
        List<string> ids = BattleLabRoleIds();
        int index = Mathf.Max(0, ids.IndexOf(battleLabRole));
        battleLabRole = ids[(index + direction + ids.Count) % ids.Count];
        if (battleLabBrush != "enemy" && battleLabBrush != "trigger_reach" && battleLabBrush != "trigger_defeat") battleLabBrush = "player";
        ShowBattleLabEditor();
    }

    private List<string> BattleLabObjectiveTypes()
    {
        return new List<string> { "capture", "reach", "rout" };
    }

    private string BattleLabObjectiveType()
    {
        if (battleLabDesign == null || string.IsNullOrEmpty(battleLabDesign.objectiveType)) return "capture";
        return BattleLabObjectiveTypes().Contains(battleLabDesign.objectiveType) ? battleLabDesign.objectiveType : "capture";
    }

    private string BattleLabObjectiveTypeLabel()
    {
        string type = BattleLabObjectiveType();
        if (type == "reach") return T("battle_lab.objective_reach", "抵达据点");
        if (type == "rout") return T("battle_lab.objective_rout", "击溃敌军");
        return T("battle_lab.objective_capture", "占据据点");
    }

    private void CycleBattleLabObjectiveType()
    {
        EnsureBattleLabDesign();
        List<string> ids = BattleLabObjectiveTypes();
        int index = Mathf.Max(0, ids.IndexOf(BattleLabObjectiveType()));
        battleLabDesign.objectiveType = ids[(index + 1) % ids.Count];
        battleLabMessage = TF("battle_lab.objective_changed", "战争目标已改为：{0}。", BattleLabObjectiveTypeLabel());
        ShowBattleLabEditor();
    }

    private int BattleLabTriggerPresetCount()
    {
        return 4;
    }

    private string BattleLabTriggerPresetTitle(int index)
    {
        int safe = Mathf.Abs(index) % BattleLabTriggerPresetCount();
        if (safe == 1) return T("battle_lab.trigger_title_reinforce", "援军信号");
        if (safe == 2) return T("battle_lab.trigger_title_duel", "阵前交锋");
        if (safe == 3) return T("battle_lab.trigger_title_route", "军心动摇");
        return T("battle_lab.trigger_title_ambush", "战场变故");
    }

    private string BattleLabTriggerPresetBody(int index)
    {
        int safe = Mathf.Abs(index) % BattleLabTriggerPresetCount();
        if (safe == 1) return T("battle_lab.trigger_body_reinforce", "{actor}抵达指定区域，远处响起回应的号声，新的战机已经出现。");
        if (safe == 2) return T("battle_lab.trigger_body_duel", "{actor}与{target}的交锋打乱了敌军节奏，主将立刻下令调整战线。");
        if (safe == 3) return T("battle_lab.trigger_body_route", "{target}被击溃后，周围部队开始迟疑，战场主动权正在转移。");
        return T("battle_lab.trigger_body_ambush", "{actor}踏入关键区域，烟尘后传来急促军号，隐藏的剧情节点被触发。");
    }

    private void CycleBattleLabTriggerStory()
    {
        battleLabTriggerStoryPreset = (battleLabTriggerStoryPreset + 1) % BattleLabTriggerPresetCount();
        battleLabMessage = TF("battle_lab.trigger_story_changed", "剧情模板已切换为：{0}。", BattleLabTriggerPresetTitle(battleLabTriggerStoryPreset));
        ShowBattleLabEditor();
    }

    private void ResizeBattleLabMap(int colDelta, int rowDelta)
    {
        EnsureBattleLabDesign();
        battleLabDesign.hexCols = Mathf.Clamp(battleLabDesign.hexCols + colDelta, BattleLabMinCols(), BattleLabMaxCols());
        battleLabDesign.hexRows = Mathf.Clamp(battleLabDesign.hexRows + rowDelta, BattleLabMinRows(), BattleLabMaxRows());
        NormalizeBattleLabDesign();
        battleLabMessage = TF("battle_lab.map_resized", "地图已调整为 {0} x {1}。缩小地图会自动移除越界单位和触发器。", battleLabDesign.hexCols, battleLabDesign.hexRows);
        ShowBattleLabEditor();
    }

    private void SaveBattleLabDesign()
    {
        EnsureBattleLabDesign();
        PlayerPrefs.SetString(BattleLabSaveKey, JsonUtility.ToJson(battleLabDesign));
        PlayerPrefs.Save();
        battleLabMessage = T("battle_lab.saved", "关卡草稿已保存。");
        ShowBattleLabEditor();
    }

    private void LoadBattleLabDesign()
    {
        string raw = PlayerPrefs.GetString(BattleLabSaveKey, "");
        if (string.IsNullOrEmpty(raw))
        {
            battleLabMessage = T("battle_lab.no_save", "还没有保存过关卡草稿。");
            ShowBattleLabEditor();
            return;
        }
        battleLabDesign = JsonUtility.FromJson<BattleLevelDesign>(raw);
        battleLabMessage = T("battle_lab.loaded", "关卡草稿已载入。");
        ShowBattleLabEditor();
    }

    private void ResetBattleLabDesign()
    {
        battleLabDesign = DefaultBattleLabDesign();
        battleLabTab = "map";
        battleLabBrush = "player";
        battleLabTerrain = "plain";
        battleLabRole = "infantry";
        battleLabTriggerAction = "none";
        battleLabBrushSize = 1;
        battleLabTriggerStoryPreset = 0;
        battleLabMessage = T("battle_lab.reset_done", "已恢复默认演习关卡。");
        ShowBattleLabEditor();
    }

    private void StartBattleLabTest()
    {
        EnsureBattleLabDesign();
        bool hasPlayer = battleLabDesign.spawns.Any(s => !string.Equals(s.side, "defender", StringComparison.OrdinalIgnoreCase));
        bool hasEnemy = battleLabDesign.spawns.Any(s => string.Equals(s.side, "defender", StringComparison.OrdinalIgnoreCase));
        if (!hasPlayer || !hasEnemy)
        {
            battleLabMessage = T("battle_lab.need_both_sides", "测试前需要至少1个蓝方单位和1个红方单位。");
            ShowBattleLabEditor();
            return;
        }

        RemoveBattleLabTempArmies();
        Army playerArmy = NewBattleLabArmy(BattleLabAttackerId, T("battle_lab.player_army", "蓝方测试军"), Faction.Player, battleLabDesign.playerTroops, battleLabDesign.playerAttack);
        Army enemyArmy = NewBattleLabArmy(BattleLabDefenderId, T("battle_lab.enemy_army", "红方测试军"), Faction.Imperial, battleLabDesign.enemyTroops, battleLabDesign.enemyAttack);
        armies.Add(playerArmy);
        armies.Add(enemyArmy);
        battleTerrainOverride = battleLabDesign.terrainTiles;
        battle = new BattleState
        {
            attackerArmyId = playerArmy.id,
            defenderArmyId = enemyArmy.id,
            provinceId = BattleLabProvinceId,
            fromStrategy = false,
            activeFaction = Faction.Player,
            firedTriggerIds = new List<string>()
        };
        BuildBattleLabUnits(playerArmy, enemyArmy);
        selectedUnitId = null;
        battlePan = new Vector2(0, 12);
        battleMessage = T("battle_lab.test_start", "工坊测试开始。选择蓝方单位行动，胜负不会写入正式存档。");
        battleAnimations.Clear();
        ShowBattle();
    }

    private Army NewBattleLabArmy(string id, string name, Faction faction, int troops, int attack)
    {
        return new Army
        {
            id = id,
            name = name,
            faction = faction,
            provinceId = BattleLabProvinceId,
            troops = troops,
            maxTroops = troops,
            move = 1,
            maxMove = 1,
            level = 1,
            exp = 0,
            attack = attack,
            supply = 99,
            maxSupply = 99,
            aiProfile = faction == Faction.Player ? "balanced" : SafeText(battleLabDesign != null ? battleLabDesign.enemyAiProfile : "", "tactical"),
            intelLevel = 3
        };
    }

    private void BuildBattleLabUnits(Army playerArmy, Army enemyArmy)
    {
        battle.units.Clear();
        foreach (BattleUnitSpawnConfig spawn in battleLabDesign.spawns)
        {
            if (!InsideHex(spawn.q, spawn.r)) continue;
            Army army = string.Equals(spawn.side, "defender", StringComparison.OrdinalIgnoreCase) ? enemyArmy : playerArmy;
            string role = string.IsNullOrEmpty(spawn.role) ? "infantry" : spawn.role;
            int divisor = Mathf.Max(1, spawn.troopDivisor <= 0 ? 4 : spawn.troopDivisor);
            battle.units.Add(NewBattleUnit(army, BattleLabSpawnSuffix(role, spawn.side), role, spawn.q, spawn.r, army.attack + spawn.attackBonus, army.troops / divisor));
        }
        UpdateObjectiveOwner();
    }

    private void ReturnToBattleLabAfterTest()
    {
        battle = null;
        battleAnimations.Clear();
        battleUnitViews.Clear();
        battleUnitBadges.Clear();
        battleUnitSprites.Clear();
        selectedUnitId = null;
        RemoveBattleLabTempArmies();
        battleLabMessage = T("battle_lab.test_return", "已返回工坊，可继续调整并再次测试。");
        ShowBattleLabEditor();
    }

    private void RemoveBattleLabTempArmies()
    {
        armies.RemoveAll(IsBattleLabTempArmy);
    }

    private bool IsBattleLabTempArmy(Army army)
    {
        return army != null && (army.id == BattleLabAttackerId || army.id == BattleLabDefenderId);
    }

    private void StartBattle(Army attacker, Province targetProvince)
    {
        battleTerrainOverride = null;
        RemoveBattleLabTempArmies();
        Army defender = ArmyById(targetProvince.armyId);
        if (defender == null)
        {
            targetProvince.owner = Faction.Player;
            MoveArmyToProvince(attacker, targetProvince);
            AddLog(TF("log.unoccupied_capture", "目标无驻军，{0}接管了{1}。", attacker.name, targetProvince.name));
            RefreshProgressionSystems(true);
            ShowStrategy();
            return;
        }

        ConsumeArmySupply(attacker, "attack");
        AutoSave("AUTO_BAT_" + targetProvince.id);
        battle = new BattleState
        {
            attackerArmyId = attacker.id,
            defenderArmyId = defender.id,
            provinceId = targetProvince.id,
            fromStrategy = true,
            activeFaction = Faction.Player
        };
        BuildBattleUnits(attacker, defender);
        selectedUnitId = null;
        battlePan = new Vector2(0, 12);
        battleMessage = T("battle.message.start", "选择蓝方军团开始行动。拖动地图可以调整视野。");
        battleAnimations.Clear();
        ShowBattle();
    }

    private void BuildBattleUnits(Army attacker, Army defender)
    {
        battle.units.Clear();
        foreach (BattleUnitSpawnConfig spawn in BattleUnitSpawns())
        {
            Army army = string.Equals(spawn.side, "defender", StringComparison.OrdinalIgnoreCase) ? defender : attacker;
            if (army == null) continue;
            int divisor = Mathf.Max(1, spawn.troopDivisor);
            string role = string.IsNullOrEmpty(spawn.role) ? CommonUnitByName(spawn.suffix)?.role ?? "infantry" : spawn.role;
            battle.units.Add(NewBattleUnit(army, spawn.suffix, role, spawn.q, spawn.r, army.attack + spawn.attackBonus, army.troops / divisor));
        }
        UpdateObjectiveOwner();
    }

    private BattleUnit NewBattleUnit(Army army, string suffix, string role, int q, int r, int attack, int hp)
    {
        BattleCoreConfig core = BattleCore();
        int maxHp = Mathf.Max(RoleBaseHp(role), hp + army.level * core.attackerArmyLevelHpPerLevel);
        int attackBonus = 0;
        int hpBonus = 0;
        int moveBonus = 0;
        if (army.faction == Faction.Player)
        {
            attackBonus = PlayerBattleAttackBonus();
            hpBonus = PlayerBattleHpBonus();
            moveBonus = PlayerBattleMoveBonus();
        }
        int hpPercentBonus = army.faction == Faction.Player ? PassiveSkillSum(s => s.hpPercent) : 0;
        if (hpPercentBonus != 0) maxHp = Mathf.RoundToInt(maxHp * (100 + hpPercentBonus) / 100f);
        maxHp += hpBonus;
        BattleUnit unit = new BattleUnit
        {
            id = army.id + "_" + role + "_" + q + "_" + r,
            armyId = army.id,
            name = army.name + "·" + suffix,
            role = role,
            faction = army.faction,
            startQ = q,
            startR = r,
            q = q,
            r = r,
            hp = maxHp,
            maxHp = maxHp,
            attack = attack + RoleAttackBonus(role) + army.level * core.attackerArmyLevelAttackPerLevel + attackBonus,
            move = RoleMove(role) + Mathf.Min(core.attackerArmyMaxMoveLevelBonusCap, army.level / Mathf.Max(1, core.attackerArmyLevelMoveBonusEveryLevels)) + moveBonus,
            range = RoleRange(role),
            level = army.level,
            exp = army.exp,
            morale = (army.faction == Faction.Player ? core.playerStartMorale : core.enemyStartMorale) + (army.faction == Faction.Player ? PassiveSkillSum(s => s.moraleBonus) : 0),
            formation = RoleFormation(role)
        };
        ApplySupplyToBattleUnit(unit, army);
        unit.morale = Mathf.Clamp(unit.morale, core.minMorale, core.maxMorale);
        return unit;
    }

    private int RoleBaseHp(string role)
    {
        BattleRoleConfig config = BattleRole(role);
        return config != null && config.baseHp > 0 ? config.baseHp : role == "cavalry" ? 115 : role == "archer" ? 78 : 100;
    }

    private int RoleMove(string role)
    {
        BattleRoleConfig config = BattleRole(role);
        return config != null && config.move > 0 ? config.move : role == "cavalry" ? 3 : 2;
    }

    private int RoleRange(string role)
    {
        BattleRoleConfig config = BattleRole(role);
        return config != null && config.range > 0 ? config.range : role == "archer" ? 3 : 1;
    }

    private int RoleFormation(string role)
    {
        BattleRoleConfig config = BattleRole(role);
        if (config != null && config.formation > 0) return config.formation;
        if (role == "infantry") return 3;
        if (role == "cavalry") return 2;
        return 1;
    }

    private int RoleAttackBonus(string role)
    {
        BattleRoleConfig config = BattleRole(role);
        if (config != null) return config.attackBonus;
        if (role == "cavalry") return 6;
        if (role == "archer") return -3;
        return 0;
    }

    private string RoleSymbol(string role)
    {
        BattleRoleConfig config = BattleRole(role);
        if (config != null && !string.IsNullOrEmpty(config.symbol)) return config.symbol;
        if (role == "cavalry") return "骑";
        if (role == "archer") return "弓";
        return "步";
    }

    private string RoleName(string role)
    {
        BattleRoleConfig config = BattleRole(role);
        if (config != null && !string.IsNullOrEmpty(config.displayName)) return config.displayName;
        if (role == "cavalry") return "骑兵";
        if (role == "archer") return "弓兵";
        return "步兵";
    }

    private BattleRoleConfig BattleRole(string role)
    {
        if (gameConfig.battleRoles != null && gameConfig.battleRoles.Count > 0)
        {
            BattleRoleConfig config = gameConfig.battleRoles.FirstOrDefault(r => r.id == role);
            if (config != null) return config;
        }
        if (role == "musket") return NewBattleRole("musket", "火枪", "铳", 82, 2, 3, 2, 1);
        if (role == "skirmisher") return NewBattleRole("skirmisher", "散兵", "散", 72, 3, 2, -1, 1);
        if (role == "heavy_spear") return NewBattleRole("heavy_spear", "重枪", "枪", 128, 2, 1, 4, 4);
        if (role == "heavy_cavalry") return NewBattleRole("heavy_cavalry", "重骑", "骑", 145, 3, 1, 8, 3);
        if (role == "heavy_infantry") return NewBattleRole("heavy_infantry", "重步", "甲", 135, 2, 1, 3, 4);
        if (role == "heavy_archer") return NewBattleRole("heavy_archer", "重弓", "弩", 92, 2, 4, 1, 2);
        if (role == "heavy_brute") return NewBattleRole("heavy_brute", "重猛", "猛", 130, 2, 1, 7, 3);
        if (role == "artillery") return NewBattleRole("artillery", "重器", "器", 105, 1, 4, 10, 1);
        if (role == "brute") return NewBattleRole("brute", "猛士", "斧", 105, 2, 1, 5, 2);
        if (role == "archer") return NewBattleRole("archer", "弓兵", "弓", 78, 2, 3, -3, 1);
        if (role == "cavalry") return NewBattleRole("cavalry", "骑兵", "骑", 115, 3, 1, 6, 2);
        return NewBattleRole("infantry", "步兵", "步", 100, 2, 1, 0, 3);
    }

    private BattleRoleConfig NewBattleRole(string id, string displayName, string symbol, int baseHp, int move, int range, int attackBonus, int formation)
    {
        return new BattleRoleConfig
        {
            id = id,
            displayName = displayName,
            symbol = symbol,
            baseHp = baseHp,
            move = move,
            range = range,
            attackBonus = attackBonus,
            formation = formation
        };
    }

    private void ShowBattle()
    {
        mode = ScreenMode.Battle;
        Clear();
        battleUnitViews.Clear();
        battleUnitBadges.Clear();
        battleUnitSprites.Clear();
        DrawSceneBackground("battlefield");
        Province province = ProvinceById(battle.provinceId);
        string battleTitleName = province != null ? province.name : battleLabDesign != null ? SafeText(battleLabDesign.name, T("battle_lab.test_battle_name", "工坊测试关")) : T("battle_lab.test_battle_name", "工坊测试关");
        AddTopBar(root, TF("battle.title", "边境军令：{0}  第{1}回合  当前：{2}", battleTitleName, battle.turn, FactionName(battle.activeFaction)));
        RectTransform board = CreateRect("Board", root, new Vector2(-160, 20), new Vector2(800, 560), new Color(0.46f, 0.47f, 0.40f, 0.96f));
        board.gameObject.AddComponent<RectMask2D>();
        AddBattleDragEvents(board.gameObject);
        battleBoardContent = CreateEmptyRect("BattleMapContent", board, battlePan, new Vector2(900, 570));
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                DrawHexTile(battleBoardContent, q, r);
            }
        }
        foreach (BattleUnit unit in battle.units.Where(u => u.hp > 0 || HasBattleAnimation(u.id)).ToList())
        {
            DrawBattleUnit(battleBoardContent, unit);
        }
        DrawBattlePanel(root);
        DrawBattleCommandBar(root);
        if (battle.outcome != "playing") DrawBattleOutcomeOverlay();
        RefreshBattleUnitViews();
    }

    private void DrawBattleCommandBar(Transform parent)
    {
        RectTransform commands = CreateUiPanel("BattleCommands", parent, new Vector2(-78, -318), new Vector2(910, 56));
        string hint = selectedUnitId == null
            ? T("battle.command_hint_select", "选择蓝方单位，随后在棋盘上移动或攻击。")
            : T("battle.command_hint_ready", "绿色格可移动，红色格可攻击；右侧可防御或待命。");
        AddText(commands, hint, new Vector2(-228, 0), new Vector2(430, 34), 14, TextAnchor.MiddleLeft, muted);
        AddButton(commands, T("button.cancel_selection", "取消选择"), new Vector2(188, 0), new Vector2(132, 38), () => { selectedUnitId = null; ShowBattle(); });
        AddButton(commands, T("button.end_turn", "结束回合"), new Vector2(346, 0), new Vector2(142, 38), EndBattleTurn, new Color(0.28f, 0.37f, 0.26f));
    }

    private void DrawHexTile(Transform board, int q, int r)
    {
        Vector2 pos = HexScreen(q, r);
        BattleUnit unit = UnitAt(q, r);
        BattleUnit selected = UnitById(selectedUnitId);
        bool moveRange = selected != null && selected.faction == Faction.Player && !selected.moved && unit == null && CanMoveTo(selected, q, r);
        bool attackRange = selected != null && selected.faction == Faction.Player && !selected.acted && unit != null && unit.faction != Faction.Player && HexDistance(selected.q, selected.r, q, r) <= AttackRange(selected);
        Color color = TerrainColor(q, r);
        if (q == BattleObjectiveQ() && r == BattleObjectiveR()) color = new Color(0.66f, 0.52f, 0.22f);
        if (moveRange) color = new Color(0.18f, 0.48f, 0.36f);
        if (attackRange) color = new Color(0.68f, 0.24f, 0.20f);
        if (unit != null && unit.id == selectedUnitId) color = highlightColor;

        GameObject go = new GameObject("Hex_" + q + "_" + r);
        go.transform.SetParent(board, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        EnsureCanvasRenderer(go);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(78, 68);
        HexTileGraphic graphic = go.AddComponent<HexTileGraphic>();
        graphic.color = color;
        graphic.strokeColor = new Color(0.18f, 0.19f, 0.17f, 0.92f);
        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = graphic;
        button.onClick.AddListener(() => ActivateOnce("hex_" + q + "_" + r, () => OnHexClicked(q, r)));
        AddBattleDragEvents(go);

        string terrainIcon = TerrainIconResource(q, r);
        if (!string.IsNullOrEmpty(terrainIcon))
        {
            RectTransform iconRt = CreateSpriteRect("TerrainIcon", rt, new Vector2(0, 4), new Vector2(48, 42), terrainIcon, new Color(1f, 1f, 1f, 0f), false, true);
            Image iconImage = iconRt.GetComponent<Image>();
            iconImage.color = new Color(1f, 1f, 1f, 0.88f);
            iconImage.raycastTarget = false;
        }

        Text label = CreateText("TileLabel", rt, TileLabel(q, r, unit), 12, ink, TextAnchor.MiddleCenter);
        label.raycastTarget = false;
        label.fontSize = !string.IsNullOrEmpty(terrainIcon) ? 9 : 11;
        label.color = new Color(0.11f, 0.10f, 0.08f, 0.84f);
        RectTransform labelRt = label.GetComponent<RectTransform>();
        if (!string.IsNullOrEmpty(terrainIcon))
        {
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.offsetMin = new Vector2(3, 2);
            labelRt.offsetMax = new Vector2(-3, 18);
        }
        else
        {
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
        }
    }

    private string TerrainIconResource(int q, int r)
    {
        if (q == BattleObjectiveQ() && r == BattleObjectiveR()) return "Art/Terrain/terrain_objective";
        string t = TerrainName(q, r);
        if (t == TerrainDisplayName("mountain")) return "Art/Terrain/terrain_mountain";
        if (t == TerrainDisplayName("forest")) return "Art/Terrain/terrain_forest";
        if (t == TerrainDisplayName("river")) return "Art/Terrain/terrain_river";
        if (t == TerrainDisplayName("city")) return "Art/Terrain/terrain_city";
        return "Art/Terrain/terrain_plain";
    }

    private string TileLabel(int q, int r, BattleUnit unit)
    {
        string terrain = TerrainName(q, r);
        if (q == BattleObjectiveQ() && r == BattleObjectiveR()) return terrain + "\n" + T("battle.objective", "据点");
        return terrain;
    }

    private void DrawBattleUnit(Transform board, BattleUnit unit)
    {
        RectTransform rt = CreateEmptyRect("Unit_" + unit.id, board, UnitRenderPosition(unit), new Vector2(76, 80));
        EnsureCanvasRenderer(rt.gameObject);
        BattleUnitBadgeGraphic badge = rt.gameObject.AddComponent<BattleUnitBadgeGraphic>();
        badge.color = unit.faction == Faction.Player ? playerColor : enemyColor;
        badge.darkColor = unit.faction == Faction.Player ? new Color(0.08f, 0.18f, 0.32f) : new Color(0.31f, 0.08f, 0.08f);
        badge.goldColor = highlightColor;
        badge.raycastTarget = false;
        AddBattleUnitHitArea(rt, unit);

        RectTransform statusLayer = CreateEmptyRect("UnitStatusLayer", rt, Vector2.zero, new Vector2(76, 80));
        statusLayer.SetAsLastSibling();
        statusLayer.gameObject.AddComponent<CanvasRenderer>();
        AddBattleDragEvents(statusLayer.gameObject);

        Sprite unitSprite = LoadBattleUnitSprite(unit);
        if (unitSprite != null)
        {
            RectTransform spriteRt = CreateRect("UnitSprite", rt, new Vector2(0, 8), new Vector2(64, 64), new Color(1f, 1f, 1f, 0f));
            spriteRt.SetAsFirstSibling();
            Image spriteImage = spriteRt.GetComponent<Image>();
            spriteImage.sprite = unitSprite;
            spriteImage.color = Color.white;
            spriteImage.preserveAspect = true;
            spriteImage.raycastTarget = false;
            battleUnitSprites[unit.id] = spriteImage;
        }
        else
        {
            Text symbol = CreateText("Symbol", rt, RoleSymbol(unit.role), 21, Color.white, TextAnchor.MiddleCenter);
            symbol.raycastTarget = false;
            RectTransform symbolRt = symbol.GetComponent<RectTransform>();
            symbolRt.anchorMin = new Vector2(0.5f, 0.5f);
            symbolRt.anchorMax = new Vector2(0.5f, 0.5f);
            symbolRt.pivot = new Vector2(0.5f, 0.5f);
            symbolRt.anchoredPosition = new Vector2(0, 10);
            symbolRt.sizeDelta = new Vector2(42, 34);
        }

        Text label = CreateText("UnitLabel", statusLayer, ShortBattleUnitName(unit), 9, ink, TextAnchor.MiddleCenter);
        label.raycastTarget = false;
        RectTransform labelRt = label.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0, -25);
        labelRt.sizeDelta = new Vector2(64, 18);

        const float hpBarWidth = 42f;
        RectTransform hpBack = CreateRect("HpBack", statusLayer, new Vector2(0, -35), new Vector2(hpBarWidth, 4), new Color(0.15f, 0.10f, 0.08f, 0.95f));
        hpBack.GetComponent<Image>().raycastTarget = false;
        float hpWidth = hpBarWidth * Mathf.Clamp01((float)Mathf.Max(0, unit.hp) / Mathf.Max(1, unit.maxHp));
        RectTransform hp = CreateRect("Hp", statusLayer, new Vector2((hpWidth - hpBarWidth) * 0.5f, -35), new Vector2(hpWidth, 4), unit.faction == Faction.Player ? new Color(0.22f, 0.72f, 0.39f) : new Color(0.82f, 0.26f, 0.20f));
        hp.GetComponent<Image>().raycastTarget = false;
        hp.pivot = new Vector2(0.5f, 0.5f);

        battleUnitViews[unit.id] = rt;
        battleUnitBadges[unit.id] = badge;
    }

    private void AddBattleUnitHitArea(RectTransform parent, BattleUnit unit)
    {
        RectTransform hit = CreateRect("UnitHitArea", parent, new Vector2(0, 8), new Vector2(50, 52), new Color(1f, 1f, 1f, 0.001f));
        hit.SetAsLastSibling();
        Image hitImage = hit.GetComponent<Image>();
        hitImage.raycastTarget = true;
        Button button = hit.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitImage;
        button.onClick.AddListener(() => ActivateOnce("unit_" + unit.id, () => OnHexClicked(unit.q, unit.r)));
        AddBattleDragEvents(hit.gameObject);
    }

    private string ShortBattleUnitName(BattleUnit unit)
    {
        Army army = ArmyById(unit.armyId);
        string prefix = army != null ? army.name + "·" : "";
        string name = !string.IsNullOrEmpty(prefix) && unit.name.StartsWith(prefix) ? unit.name.Substring(prefix.Length) : unit.name;
        return name.Length > 6 ? name.Substring(name.Length - 6) : name;
    }

    private string BattleObjectivePanelText(string objectiveOwner)
    {
        if (battle != null && !battle.fromStrategy && battleLabDesign != null)
        {
            string type = BattleLabObjectiveType();
            if (type == "reach")
            {
                return TF("battle.objective_panel_reach", "战争目标：抵达据点\n目标格：第{0}行第{1}列\n当前控制：{2}\n触发器：{3}\n{4}",
                    BattleObjectiveR() + 1,
                    BattleObjectiveQ() + 1,
                    objectiveOwner,
                    battleLabDesign.triggers != null ? battleLabDesign.triggers.Count : 0,
                    BattleLabBattleRuleLine());
            }
            if (type == "rout")
            {
                int enemies = battle.units.Count(u => u.faction != Faction.Player && u.hp > 0);
                return TF("battle.objective_panel_rout", "战争目标：击溃敌军\n剩余敌军：{0}\n据点格：第{1}行第{2}列\n触发器：{3}\n{4}",
                    enemies,
                    BattleObjectiveR() + 1,
                    BattleObjectiveQ() + 1,
                    battleLabDesign.triggers != null ? battleLabDesign.triggers.Count : 0,
                    BattleLabBattleRuleLine());
            }
            return TF("battle.objective_panel_lab_capture", "战争目标：占据据点\n当前控制：{0}\n我方进度：{1}/{2}  敌方进度：{3}/{4}\n{5}",
                objectiveOwner,
                battle.playerObjectiveHold,
                PlayerObjectiveRequiredTurns(),
                battle.enemyObjectiveHold,
                EnemyObjectiveRequiredTurns(),
                BattleLabBattleRuleLine());
        }

        return TF("battle.objective_panel", "中央据点：{0}\n我方据点进度：{1}/{2}\n敌方据点进度：{3}/{4}",
            objectiveOwner,
            battle.playerObjectiveHold,
            PlayerObjectiveRequiredTurns(),
            battle.enemyObjectiveHold,
            EnemyObjectiveRequiredTurns());
    }

    private string BattleLabBattleRuleLine()
    {
        if (battleLabDesign == null) return "";
        string limit = battleLabDesign.turnLimit <= 0 ? T("common.unlimited", "不限") : battleLabDesign.turnLimit.ToString();
        return TF("battle_lab.battle_rule_line", "天气：{0}  回合限制：{1}", BattleLabWeatherLabel(), limit);
    }

    private void DrawBattlePanel(Transform parent)
    {
        RectTransform side = CreateUiPanel("BattleSide", parent, new Vector2(430, 0), new Vector2(360, 570));
        BattleUnit selected = UnitById(selectedUnitId);
        bool labBattle = battle != null && !battle.fromStrategy && battleLabDesign != null;
        UpdateObjectiveOwner();
        string objective = battle.objectiveOwner == Faction.Neutral ? T("battle.objective_contested", "争夺中") : FactionName(battle.objectiveOwner);
        string text = selected != null
            ? TF("battle.unit_text", "{0}\n兵种：{1}  编队：{2}\n兵力：{3}/{4}\n等级：{5}\n经验：{6}\n攻击：{7}\n士气：{8}\n移动：{9}  射程：{10}\n状态：{11}",
                selected.name, RoleName(selected.role), selected.formation, selected.hp, selected.maxHp, selected.level, selected.exp, selected.attack, MoraleLabel(selected.morale), selected.move, selected.range, BattleUnitStatusLabel(selected))
            : T("battle.empty_hint", "点击蓝方军团选择。\n绿色格：可移动。\n红色格：可攻击。\n拖动战场可移动视野。\n步兵均衡，骑兵克弓兵，弓兵射程远。");
        AddText(side, BattleObjectivePanelText(objective), labBattle ? new Vector2(0, 190) : new Vector2(0, 205), labBattle ? new Vector2(320, 122) : new Vector2(320, 92), labBattle ? 14 : 15, TextAnchor.UpperLeft, muted);
        if (selected != null) text += "\n" + TF("battle.unit_supply", "补给：{0}", SupplyStatus(ArmyById(selected.armyId)));
        if (selected != null) text += "\n\n" + BattleTargetPreview(selected);
        AddText(side, text, labBattle ? new Vector2(0, 20) : new Vector2(0, 40), labBattle ? new Vector2(320, 196) : new Vector2(320, 224), 16, TextAnchor.UpperLeft);
        if (selected != null && selected.faction == Faction.Player && battle.outcome == "playing")
        {
            AddButton(side, T("button.guard", "防御"), new Vector2(-82, -104), new Vector2(136, 36), () => GuardSelectedUnit(), new Color(0.28f, 0.37f, 0.26f));
            AddButton(side, T("button.wait", "待命"), new Vector2(82, -104), new Vector2(136, 36), () => WaitSelectedUnit());
        }
        AddText(side, TF("battle.log_panel", "军令：\n{0}\n\n战场日志：\n{1}", battleMessage, LatestLog(4)), new Vector2(0, -210), new Vector2(320, 138), 15, TextAnchor.UpperLeft, muted);
    }

    private string BattleUnitStatusLabel(BattleUnit unit)
    {
        if (unit == null) return "";
        if (unit.guarding) return T("battle.status_guarding", "防御中");
        if (unit.acted) return T("battle.status_acted", "已攻击");
        if (unit.moved) return T("battle.status_moved", "已移动，可攻击");
        return T("battle.status_ready", "可移动/攻击");
    }

    private string BattleTargetPreview(BattleUnit unit)
    {
        if (unit == null || unit.faction != Faction.Player) return "";
        if (unit.acted) return T("battle.target_preview_acted", "攻击预览：本回合已完成攻击。");
        List<BattleUnit> targets = battle.units
            .Where(enemy => enemy.faction != Faction.Player && enemy.hp > 0 && HexDistance(unit.q, unit.r, enemy.q, enemy.r) <= AttackRange(unit))
            .OrderByDescending(enemy => PreviewDamage(unit, enemy))
            .Take(3)
            .ToList();
        if (targets.Count == 0) return T("battle.target_preview_none", "攻击预览：当前射程内没有敌军。");
        return T("battle.target_preview_title", "攻击预览：") + "\n" + string.Join("\n", targets.Select(enemy =>
        {
            int damage = PreviewDamage(unit, enemy);
            int counter = HexDistance(unit.q, unit.r, enemy.q, enemy.r) <= AttackRange(enemy) ? PreviewCounterDamage(enemy, unit) : 0;
            return TF("battle.target_preview_row", "{0}  伤害{1}{2}", ShortBattleUnitName(enemy), damage, counter > 0 ? TF("battle.target_preview_counter", " / 反击{0}", counter) : "");
        }).ToArray());
    }

    private void GuardSelectedUnit()
    {
        BattleUnit unit = UnitById(selectedUnitId);
        if (unit == null || unit.faction != Faction.Player) return;
        unit.guarding = true;
        unit.moved = true;
        unit.acted = true;
        ConsumeBattleSupply(unit, "guard");
        SetBattleMessage(TF("battle.msg.guard", "{0}就地防御，本回合减伤提高。", unit.name));
        selectedUnitId = null;
        ShowBattle();
    }

    private void WaitSelectedUnit()
    {
        BattleUnit unit = UnitById(selectedUnitId);
        if (unit == null || unit.faction != Faction.Player) return;
        unit.moved = true;
        unit.acted = true;
        ConsumeBattleSupply(unit, "wait");
        SetBattleMessage(TF("battle.msg.wait", "{0}待命，保留阵型。", unit.name));
        selectedUnitId = null;
        ShowBattle();
    }

    private int PlayerObjectiveRequiredTurns()
    {
        return Mathf.Max(1, BattleCore().playerObjectiveRequiredTurns);
    }

    private int EnemyObjectiveRequiredTurns()
    {
        return Mathf.Max(1, BattleCore().enemyObjectiveRequiredTurns);
    }

    private void DrawBattleOutcomeOverlay()
    {
        CreateRect("BattleOutcomeShade", root, Vector2.zero, new Vector2(1400, 820), new Color(0.02f, 0.02f, 0.02f, 0.72f));
        string title = battle.outcome == "victory" ? T("battle.outcome_victory", "战役胜利") : T("battle.outcome_defeat", "战役失败");
        string body = battle.outcome == "victory" ? T("battle.outcome_victory_body", "敌军已被击溃，或中央据点已被我方巩固。") : T("battle.outcome_defeat_body", "我方军团被迫撤退，或中央据点落入敌方掌控。");
        AddText(root, title, new Vector2(0, 60), new Vector2(520, 70), 42, TextAnchor.MiddleCenter, highlightColor);
        AddText(root, body, new Vector2(0, -5), new Vector2(760, 70), 20, TextAnchor.MiddleCenter, new Color(1.00f, 0.96f, 0.84f));
        if (battle != null && !battle.fromStrategy)
        {
            AddButton(root, T("button.back_battle_lab", "返回工坊"), new Vector2(0, -90), new Vector2(210, 48), ReturnToBattleLabAfterTest, new Color(0.28f, 0.37f, 0.26f));
        }
        else
        {
            AddButton(root, T("button.back_strategy", "返回战略地图"), new Vector2(0, -90), new Vector2(210, 48), ReturnToStrategyAfterBattle, new Color(0.28f, 0.37f, 0.26f));
        }
    }

    private void SetBattleMessage(string message)
    {
        battleMessage = message;
        AddLog(message);
    }

    private void OnHexClicked(int q, int r)
    {
        if (battle == null || battle.outcome != "playing") return;
        if (Time.unscaledTime < battleIgnoreClickUntil || battleDragDistance > 8f)
        {
            battleDragDistance = 0f;
            return;
        }

        BattleUnit clicked = UnitAt(q, r);
        BattleUnit selected = UnitById(selectedUnitId);
        if (clicked != null && clicked.faction == Faction.Player)
        {
            if (battle.activeFaction != Faction.Player)
            {
                SetBattleMessage(T("battle.msg.enemy_turn", "现在是敌方回合，无法下令。"));
                ShowBattle();
                return;
            }
            if (clicked.id == selectedUnitId)
            {
                selectedUnitId = null;
                SetBattleMessage(T("battle.msg.cancel_unit", "取消选择部队。"));
            }
            else if (clicked.moved && clicked.acted)
            {
                selectedUnitId = clicked.id;
                SetBattleMessage(TF("battle.msg.unit_done", "{0}已经行动完毕。", clicked.name));
            }
            else
            {
                selectedUnitId = clicked.id;
                SetBattleMessage(TF("battle.msg.unit_selected", "选中{0}，绿色可移动，红色可攻击。", clicked.name));
            }
            ShowBattle();
            return;
        }

        if (selected == null)
        {
            if (clicked != null) SetBattleMessage(TF("battle.msg.enemy_info", "敌军：{0}，HP {1}。", clicked.name, clicked.hp));
            else SetBattleMessage(T("battle.msg.select_first", "请先选择蓝方军团。"));
            ShowBattle();
            return;
        }

        if (clicked != null && clicked.faction != Faction.Player)
        {
            int distance = HexDistance(selected.q, selected.r, clicked.q, clicked.r);
            if (selected.acted)
            {
                SetBattleMessage(TF("battle.msg.already_attacked", "{0}本回合已经攻击。", selected.name));
                ShowBattle();
                return;
            }
            if (distance > AttackRange(selected))
            {
                SetBattleMessage(T("battle.msg.out_of_range", "目标不在攻击范围内。"));
                ShowBattle();
                return;
            }
            ShowAttackConfirm(selected, clicked);
            return;
        }

        if (clicked == null)
        {
            if (selected.acted)
            {
                SetBattleMessage(TF("battle.msg.cannot_move_after_attack", "{0}已经攻击，不能再移动。", selected.name));
                ShowBattle();
                return;
            }
            if (selected.moved)
            {
                SetBattleMessage(TF("battle.msg.already_moved", "{0}本回合已经移动。", selected.name));
                ShowBattle();
                return;
            }
            if (!CanMoveTo(selected, q, r))
            {
                SetBattleMessage(T("battle.msg.invalid_move", "目标格超出移动范围，或被地形/单位阻挡。"));
                ShowBattle();
                return;
            }
            int fromQ = selected.q;
            int fromR = selected.r;
            selected.q = q;
            selected.r = r;
            selected.moved = true;
            selected.guarding = false;
            ConsumeBattleSupply(selected, "move");
            StartBattleAnimation(selected.id, BattleAnimationKind.Move, HexScreen(fromQ, fromR), HexScreen(q, r), 0.55f);
            UpdateObjectiveOwner();
            selectedUnitId = selected.id;
            SetBattleMessage(TF("battle.msg.moved_to", "{0}移动至第{1}行第{2}列，可继续攻击。", selected.name, r + 1, q + 1));
            if (TryFireBattleLabReachTrigger(selected)) return;
            CheckBattleOutcome();
            if (mode == ScreenMode.Battle) ShowBattle();
        }
    }

    private void ShowAttackConfirm(BattleUnit attacker, BattleUnit defender)
    {
        pendingAttackAttackerId = attacker.id;
        pendingAttackDefenderId = defender.id;
        mode = ScreenMode.BattleConfirm;
        CreateRect("AttackConfirmShade", root, Vector2.zero, new Vector2(1400, 820), new Color(0.02f, 0.02f, 0.02f, 0.58f));
        RectTransform box = CreateUiPanel("AttackConfirm", root, new Vector2(0, 0), new Vector2(620, 360));
        int preview = PreviewDamage(attacker, defender);
        int counter = HexDistance(attacker.q, attacker.r, defender.q, defender.r) <= AttackRange(defender) ? PreviewCounterDamage(defender, attacker) : 0;
        string body = TF("battle.attack_confirm_body", "{0}\n-> {1}\n\n预计伤害：{2}{3}\n地形：{4}  抗性：{5}%\n士气：{6} / {7}",
            attacker.name,
            defender.name,
            preview,
            counter > 0 ? TF("battle.counter_preview", "\n预计反击：{0}", counter) : T("battle.no_counter", "\n目标无法反击"),
            TerrainName(defender.q, defender.r),
            TerrainDefensePercent(defender.q, defender.r, attacker.role),
            MoraleLabel(attacker.morale),
            MoraleLabel(defender.morale)) + "\n\n" + BattleDamagePreviewBreakdown(attacker, defender);
        AddText(box, T("battle.attack_confirm_title", "确认攻击"), new Vector2(0, 142), new Vector2(540, 44), 26, TextAnchor.MiddleCenter, highlightColor);
        AddText(box, body, new Vector2(0, 28), new Vector2(540, 220), 16, TextAnchor.UpperLeft);
        AddButton(box, T("button.attack", "开战"), new Vector2(-90, -145), new Vector2(140, 42), ConfirmPendingAttack, new Color(0.45f, 0.18f, 0.18f));
        AddButton(box, T("button.cancel", "取消"), new Vector2(90, -145), new Vector2(140, 42), CancelPendingAttack);
    }

    private string BattleDamagePreviewBreakdown(BattleUnit attacker, BattleUnit defender)
    {
        BattleCoreConfig core = BattleCore();
        int role = RoleDamageModifier(attacker, defender);
        int aptitude = (attacker.faction == Faction.Player ? BattleAptitudeLevel(attacker.role) : attacker.level) * core.aptitudeDamagePerLevel;
        int morale = attacker.morale * FormationCoefficient(attacker) - defender.morale * FormationCoefficient(defender);
        int levelPenalty = defender.level * core.defenderLevelDamagePenalty;
        int terrain = TerrainDefensePercent(defender.q, defender.r, attacker.role);
        Army attackerArmy = ArmyById(attacker.armyId);
        Army defenderArmy = ArmyById(defender.armyId);
        string supply = IsSupplyShort(attackerArmy) || IsSupplyShort(defenderArmy)
            ? T("battle.breakdown_supply_bad", "补给：短缺会压低输出或防御。")
            : T("battle.breakdown_supply_good", "补给：正常。");
        return TF("battle.damage_breakdown", "影响：兵种克制 {0:+#;-#;0} / 熟练 +{1} / 士气 {2:+#;-#;0} / 敌等级 -{3} / 地形减伤 {4}%\n{5}",
            role,
            aptitude,
            morale,
            levelPenalty,
            terrain,
            supply);
    }

    private void ConfirmPendingAttack()
    {
        BattleUnit attacker = UnitById(pendingAttackAttackerId);
        BattleUnit defender = UnitById(pendingAttackDefenderId);
        pendingAttackAttackerId = "";
        pendingAttackDefenderId = "";
        mode = ScreenMode.Battle;
        if (attacker == null || defender == null)
        {
            ShowBattle();
            return;
        }
        bool storyOpened = ResolveAttack(attacker, defender);
        attacker.acted = true;
        selectedUnitId = null;
        CheckBattleOutcome();
        if (storyOpened) return;
        if (mode == ScreenMode.Battle) ShowBattle();
    }

    private void CancelPendingAttack()
    {
        pendingAttackAttackerId = "";
        pendingAttackDefenderId = "";
        mode = ScreenMode.Battle;
        ShowBattle();
    }

    private bool ResolveAttack(BattleUnit attacker, BattleUnit defender)
    {
        ConsumeBattleSupply(attacker, "attack");
        attacker.guarding = false;
        int damage = CalculateBattleDamage(attacker, defender, false);
        Vector2 attackerPos = HexScreen(attacker.q, attacker.r);
        Vector2 defenderPos = HexScreen(defender.q, defender.r);
        float direction = defenderPos.x >= attackerPos.x ? 1f : -1f;
        StartBattleAnimation(attacker.id, BattleAnimationKind.Attack, attackerPos, attackerPos, 0.45f, direction);
        StartBattleAnimation(defender.id, BattleAnimationKind.Hit, defenderPos, defenderPos, 0.55f, direction);
        defender.hp -= damage;
        BattleCoreConfig core = BattleCore();
        SetBattleMessage(TF("battle.msg.attack_damage", "{0}攻击{1}，造成{2}伤害。", attacker.name, defender.name, damage));
        GainBattleExp(attacker, core.battleExpHit);
        AdjustMoraleAfterDamage(defender);
        if (defender.hp <= 0)
        {
            defender.hp = 0;
            SetBattleMessage(TF("battle.msg.routed", "{0}溃散。", defender.name));
            attacker.morale = Mathf.Clamp(attacker.morale + 1, core.minMorale, core.maxMorale);
            GainBattleExp(attacker, core.battleExpKill);
            if (attacker.faction == Faction.Player && defender.faction != Faction.Player) player.enemiesDefeated += 1;
            UpdateObjectiveOwner();
            return TryFireBattleLabDefeatTrigger(attacker, defender);
        }

        if (HexDistance(attacker.q, attacker.r, defender.q, defender.r) <= AttackRange(defender) && !defender.acted)
        {
            ConsumeBattleSupply(defender, "attack");
            defender.guarding = false;
            int counter = CalculateBattleDamage(defender, attacker, true);
            StartBattleAnimation(defender.id, BattleAnimationKind.Attack, defenderPos, defenderPos, 0.42f, -direction);
            StartBattleAnimation(attacker.id, BattleAnimationKind.Hit, attackerPos, attackerPos, 0.52f, -direction);
            attacker.hp -= counter;
            AddLog(TF("battle.msg.counter_damage", "{0}反击，造成{1}伤害。", defender.name, counter));
            AdjustMoraleAfterDamage(attacker);
            if (attacker.hp <= 0)
            {
                attacker.hp = 0;
                AddLog(TF("battle.msg.routed", "{0}溃散。", attacker.name));
                defender.morale = Mathf.Clamp(defender.morale + 1, core.minMorale, core.maxMorale);
                if (defender.faction == Faction.Player && attacker.faction != Faction.Player) player.enemiesDefeated += 1;
                UpdateObjectiveOwner();
                return TryFireBattleLabDefeatTrigger(defender, attacker);
            }
        }
        UpdateObjectiveOwner();
        return false;
    }

    private int PreviewDamage(BattleUnit attacker, BattleUnit defender)
    {
        return CalculateBattleDamage(attacker, defender, false, true);
    }

    private int PreviewCounterDamage(BattleUnit attacker, BattleUnit defender)
    {
        return CalculateBattleDamage(attacker, defender, true, true);
    }

    private int CalculateBattleDamage(BattleUnit attacker, BattleUnit defender, bool counter, bool preview = false)
    {
        BattleCoreConfig core = BattleCore();
        int random = preview ? 0 : RandomRangeInt(core.battleRandomMin, core.battleRandomMaxExclusive);
        int aptitude = attacker.faction == Faction.Player ? BattleAptitudeLevel(attacker.role) : attacker.level;
        float health = HealthFactor(attacker);
        int moraleTerm = attacker.morale * FormationCoefficient(attacker) - defender.morale * FormationCoefficient(defender);
        int baseDamage = attacker.attack + RoleDamageModifier(attacker, defender) + moraleTerm + aptitude * core.aptitudeDamagePerLevel - defender.level * core.defenderLevelDamagePenalty + BattleLabWeatherAttackModifier(attacker) + random;
        if (counter) baseDamage = Mathf.RoundToInt(baseDamage * core.counterDamagePercent / 100f);
        float terrain = 1f - TerrainDefensePercent(defender.q, defender.r, attacker.role) / 100f;
        int attackPercent = attacker.faction == Faction.Player ? PassiveSkillSum(s => s.attackPercent) : 0;
        int defensePercent = defender.faction == Faction.Player ? PassiveSkillSum(s => s.defensePercent) : 0;
        if (defender.guarding) defensePercent += 25;
        Army attackerArmy = ArmyById(attacker.armyId);
        Army defenderArmy = ArmyById(defender.armyId);
        if (IsSupplyShort(attackerArmy)) attackPercent -= SupplyRule().shortageAttackPenalty;
        if (IsSupplyShort(defenderArmy)) defensePercent -= SupplyRule().shortageAttackPenalty;
        float skill = (100f + attackPercent) / 100f * (100f - Mathf.Clamp(defensePercent, -50, 80)) / 100f;
        int damage = Mathf.FloorToInt(Mathf.Max(1f, baseDamage * health * terrain * skill));
        return Mathf.Max(counter ? core.minCounterDamage : core.minDamage, damage);
    }

    private int BattleLabWeatherAttackModifier(BattleUnit attacker)
    {
        if (battle == null || battle.fromStrategy || battleLabDesign == null || attacker == null) return 0;
        string weather = SafeText(battleLabDesign.weather, "clear");
        if (weather == "rain" && IsRangedRole(attacker.role)) return -2;
        if (weather == "fog" && IsRangedRole(attacker.role)) return -1;
        if (weather == "night" && attacker.role == "skirmisher") return 2;
        if (weather == "night" && attacker.role == "artillery") return -2;
        return 0;
    }

    private int BattleAptitudeLevel(string role)
    {
        if (role == "infantry") return ExpLevel(player.infantryExp);
        if (role == "cavalry") return ExpLevel(player.cavalryExp);
        return ExpLevel(player.artilleryExp);
    }

    private int FormationCoefficient(BattleUnit unit)
    {
        BattleCoreConfig core = BattleCore();
        if (unit == null) return core.formationDefaultCoefficient;
        if (unit.formation >= 3) return core.formationThreeCoefficient;
        if (unit.formation == 2) return core.formationTwoCoefficient;
        return core.formationDefaultCoefficient;
    }

    private float HealthFactor(BattleUnit unit)
    {
        if (unit == null || unit.maxHp <= 0) return 1f;
        int hpPercent = Mathf.FloorToInt((float)Mathf.Max(0, unit.hp) * 100f / unit.maxHp);
        HealthFactorRule rule = HealthFactorRules()
            .Where(r => unit.formation >= r.minFormation && unit.formation <= r.maxFormation && hpPercent >= r.minHpPercent)
            .OrderByDescending(r => r.minHpPercent)
            .FirstOrDefault();
        if (rule == null || rule.denominator == 0) return 1f;
        return Mathf.Max(0.01f, (float)rule.numerator / rule.denominator);
    }

    private void AdjustMoraleAfterDamage(BattleUnit unit)
    {
        if (unit == null || unit.maxHp <= 0) return;
        float ratio = (float)Mathf.Max(0, unit.hp) / unit.maxHp;
        BattleCoreConfig core = BattleCore();
        if (ratio * 100f < core.lowMoraleHpPercent) unit.morale = Mathf.Clamp(unit.morale - 1, core.minMorale, core.maxMorale);
    }

    private string MoraleLabel(int morale)
    {
        if (morale >= 2) return T("morale.high", "高昂");
        if (morale == 1) return T("morale.up", "上升");
        if (morale == -1) return T("morale.low", "低落");
        if (morale <= -2) return T("morale.chaos", "混乱");
        return T("morale.normal", "正常");
    }

    private int RoleDamageModifier(BattleUnit attacker, BattleUnit defender)
    {
        BattleRoleDamageRule rule = BattleRoleDamageRules().FirstOrDefault(r => r.attackerRole == attacker.role && r.defenderRole == defender.role);
        return rule != null ? rule.modifier : 0;
    }

    private void GainBattleExp(BattleUnit unit, int amount)
    {
        unit.exp += amount;
        BattleCoreConfig core = BattleCore();
        while (unit.exp >= ExpForUnitLevel(unit.level + 1) && unit.level < core.unitLevelMax)
        {
            unit.level += 1;
            unit.attack += core.unitLevelAttackGain;
            unit.maxHp += core.unitLevelHpGain;
            unit.hp += core.unitLevelHpGain;
            AddLog(TF("log.unit_level_up", "{0}升至{1}级。", unit.name, unit.level));
        }
    }

    private int ExpForUnitLevel(int level)
    {
        return level * BattleCore().unitLevelExpStep;
    }

    private void EndBattleTurn()
    {
        if (battle == null) return;
        if (battle.outcome != "playing")
        {
            ReturnToStrategyAfterBattle();
            return;
        }
        if (battle.activeFaction == Faction.Player)
        {
            selectedUnitId = null;
            ScoreObjectiveControl();
            CheckBattleOutcome();
            if (battle.outcome != "playing")
            {
                ShowBattle();
                return;
            }
            battle.activeFaction = Faction.Imperial;
            RunEnemyBattleAi();
            ScoreObjectiveControl();
            CheckBattleOutcome();
            if (battle.outcome != "playing")
            {
                ShowBattle();
                return;
            }
            battle.activeFaction = Faction.Player;
            battle.turn += 1;
            if (CheckBattleLabTurnLimit())
            {
                ShowBattle();
                return;
            }
            foreach (BattleUnit unit in battle.units.Where(u => u.faction == Faction.Player && u.hp > 0))
            {
                unit.moved = false;
                unit.acted = false;
                unit.guarding = false;
            }
            SetBattleMessage(T("battle.msg.enemy_turn_end", "敌方行动结束，进入我方回合。"));
            if (mode == ScreenMode.Battle) ShowBattle();
        }
    }

    private void RunEnemyBattleAi()
    {
        SetBattleMessage(T("battle.msg.enemy_ai", "敌军正在调动部队。"));
        foreach (BattleUnit enemy in battle.units
            .Where(u => u.faction != Faction.Player && u.hp > 0)
            .OrderByDescending(AiInitiativeScore)
            .ToList())
        {
            if (enemy.hp <= 0) continue;
            Army enemyArmy = ArmyById(enemy.armyId);
            AiProfileConfig profile = AiProfileForArmy(enemyArmy);
            enemy.guarding = false;
            BattleUnit target = BestAiTarget(enemy, profile);
            if (target == null) break;

            if (AiShouldRetreat(enemy, target, profile) && TryMoveEnemy(enemy, target, profile, AiMoveIntent.Retreat))
            {
                ConsumeBattleSupply(enemy, "move");
                enemy.moved = true;
                AddLog(TF("battle.msg.enemy_retreat", "{0}收缩阵线。", enemy.name));
                CheckBattleOutcome();
                if (battle.outcome != "playing") break;
                continue;
            }

            BattleUnit attackTarget = BestAiAttackTarget(enemy, profile, enemy.q, enemy.r);
            if (attackTarget != null && AiShouldAttack(enemy, attackTarget, profile))
            {
                bool storyOpened = ResolveAttack(enemy, attackTarget);
                enemy.acted = true;
                if (storyOpened) break;
            }
            else
            {
                if (TryMoveEnemy(enemy, target, profile, AiMoveIntent.Advance))
                {
                    ConsumeBattleSupply(enemy, "move");
                    enemy.moved = true;
                    AddLog(TF("battle.msg.enemy_advance", "{0}向我方推进。", enemy.name));
                }

                attackTarget = BestAiAttackTarget(enemy, profile, enemy.q, enemy.r);
                if (attackTarget != null && AiShouldAttack(enemy, attackTarget, profile))
                {
                    bool storyOpened = ResolveAttack(enemy, attackTarget);
                    enemy.acted = true;
                    if (storyOpened) break;
                }
                else if (AiShouldGuard(enemy, target, profile))
                {
                    enemy.guarding = true;
                    enemy.moved = true;
                    enemy.acted = true;
                    ConsumeBattleSupply(enemy, "guard");
                    AddLog(TF("battle.msg.enemy_guard", "{0}固守阵地。", enemy.name));
                }
                else
                {
                    enemy.moved = true;
                    enemy.acted = true;
                    ConsumeBattleSupply(enemy, "wait");
                    AddLog(TF("battle.msg.enemy_wait", "{0}待命观察。", enemy.name));
                }
            }
            CheckBattleOutcome();
            if (battle.outcome != "playing") break;
        }
        foreach (BattleUnit enemy in battle.units.Where(u => u.faction != Faction.Player && u.hp > 0))
        {
            enemy.moved = false;
            enemy.acted = false;
        }
    }

    private int AiInitiativeScore(BattleUnit unit)
    {
        AiProfileConfig profile = AiProfileForArmy(ArmyById(unit.armyId));
        int hpPercent = Mathf.FloorToInt(unit.hp * 100f / Mathf.Max(1, unit.maxHp));
        return AiAggression(profile) + AiFlankBias(profile) / 4 + (100 - hpPercent) / 2 + AttackRange(unit) * 6;
    }

    private BattleUnit BestAiTarget(BattleUnit enemy, AiProfileConfig profile)
    {
        if (battle == null || enemy == null) return null;
        return battle.units
            .Where(u => u.faction == Faction.Player && u.hp > 0)
            .OrderByDescending(target => AiTargetScoreFromCell(enemy, profile, target, enemy.q, enemy.r))
            .FirstOrDefault();
    }

    private BattleUnit BestAiAttackTarget(BattleUnit enemy, AiProfileConfig profile, int fromQ, int fromR)
    {
        if (battle == null || enemy == null) return null;
        int range = AttackRange(enemy);
        return battle.units
            .Where(target => target.faction == Faction.Player && target.hp > 0 && HexDistance(fromQ, fromR, target.q, target.r) <= range)
            .OrderByDescending(target => AiTargetScoreFromCell(enemy, profile, target, fromQ, fromR))
            .FirstOrDefault();
    }

    private int AiTargetScoreFromCell(BattleUnit enemy, AiProfileConfig profile, BattleUnit target, int fromQ, int fromR)
    {
        int distance = HexDistance(fromQ, fromR, target.q, target.r);
        int hpPercent = Mathf.FloorToInt(target.hp * 100f / Mathf.Max(1, target.maxHp));
        int damage = PreviewDamage(enemy, target);
        int counter = distance <= AttackRange(target) ? PreviewCounterDamage(target, enemy) : 0;
        int score = 260 - distance * Mathf.Max(6, AiAggression(profile) / 12);
        score += damage * AiAggression(profile) / 28;
        score += (100 - hpPercent) * AiFocusFire(profile) / 95;
        score += RoleDamageModifier(enemy, target) * 4;
        score -= TerrainDefensePercent(target.q, target.r, enemy.role) * AiTerrainPreference(profile) / 120;
        score -= counter * AiAvoidCounter(profile) / 24;
        if (damage >= target.hp) score += AiFinishBias(profile);
        if (target.guarding) score -= AiCaution(profile) / 5;
        if (IsObjectiveCell(target.q, target.r)) score += AiObjectiveBias(profile) / 3;
        return score;
    }

    private bool AiShouldRetreat(BattleUnit enemy, BattleUnit target, AiProfileConfig profile)
    {
        if (enemy == null || target == null) return false;
        int hpPercent = Mathf.FloorToInt(enemy.hp * 100f / Mathf.Max(1, enemy.maxHp));
        int threshold = AiRetreatHpPercent(profile);
        if (IsSupplyShort(ArmyById(enemy.armyId))) threshold += 8;
        return hpPercent <= threshold && AiCaution(profile) + AiAvoidCounter(profile) > AiAggression(profile) + 30;
    }

    private bool AiShouldAttack(BattleUnit enemy, BattleUnit target, AiProfileConfig profile)
    {
        if (enemy == null || target == null) return false;
        int damage = PreviewDamage(enemy, target);
        if (damage >= target.hp) return true;
        int counter = HexDistance(enemy.q, enemy.r, target.q, target.r) <= AttackRange(target) ? PreviewCounterDamage(target, enemy) : 0;
        if (counter >= enemy.hp && AiCaution(profile) + AiAvoidCounter(profile) > AiAggression(profile) + AiFinishBias(profile) / 2) return false;
        if (IsObjectiveCell(enemy.q, enemy.r) && AiGuardBias(profile) > AiAggression(profile) && counter > damage) return false;
        if (target.guarding && damage < Mathf.Max(8, target.hp / 4) && AiCaution(profile) > AiAggression(profile)) return false;
        return true;
    }

    private bool AiShouldGuard(BattleUnit enemy, BattleUnit target, AiProfileConfig profile)
    {
        if (enemy == null) return false;
        int score = AiGuardBias(profile);
        score += TerrainDefensePercent(enemy.q, enemy.r, enemy.role) * AiTerrainPreference(profile) / 70;
        if (IsObjectiveCell(enemy.q, enemy.r)) score += AiObjectiveBias(profile);
        if (target != null && HexDistance(enemy.q, enemy.r, target.q, target.r) <= AttackRange(enemy)) score -= AiAggression(profile) / 2;
        int hpPercent = Mathf.FloorToInt(enemy.hp * 100f / Mathf.Max(1, enemy.maxHp));
        if (hpPercent <= AiRetreatHpPercent(profile) + 10) score += AiCaution(profile) / 2;
        return score >= 210;
    }

    private bool TryMoveEnemy(BattleUnit unit, BattleUnit target, AiProfileConfig profile, AiMoveIntent intent)
    {
        if (unit == null || target == null) return false;
        Vector2Int destination = BestAiMoveDestination(unit, target, profile, intent);
        if (destination.x == unit.q && destination.y == unit.r) return false;
        return MoveBattleUnit(unit, destination.x, destination.y);
    }

    private Vector2Int BestAiMoveDestination(BattleUnit unit, BattleUnit target, AiProfileConfig profile, AiMoveIntent intent)
    {
        Vector2Int best = new Vector2Int(unit.q, unit.r);
        int bestScore = AiMoveScore(unit, target, profile, intent, unit.q, unit.r);
        foreach (Vector2Int cell in AiReachableCells(unit, true))
        {
            int score = AiMoveScore(unit, target, profile, intent, cell.x, cell.y);
            if (score > bestScore)
            {
                bestScore = score;
                best = cell;
            }
        }
        return best;
    }

    private int AiMoveScore(BattleUnit unit, BattleUnit target, AiProfileConfig profile, AiMoveIntent intent, int q, int r)
    {
        int distance = target == null ? 0 : HexDistance(q, r, target.q, target.r);
        int terrain = TerrainDefensePercent(q, r, unit.role);
        int threat = AiThreatAtCell(unit, q, r);
        int objectiveDistance = HexDistance(q, r, BattleObjectiveQ(), BattleObjectiveR());
        int score = terrain * AiTerrainPreference(profile) / 6;
        score -= threat * (AiCaution(profile) + AiAvoidCounter(profile)) / 16;
        score += AiAllySupportAtCell(unit, q, r) * (AiFocusFire(profile) + AiGuardBias(profile)) / 24;

        if (IsObjectiveCell(q, r)) score += AiObjectiveBias(profile);
        else score -= objectiveDistance * AiObjectiveBias(profile) / 18;

        if (intent == AiMoveIntent.Retreat)
        {
            score += distance * AiCaution(profile);
            score += terrain * AiGuardBias(profile) / 12;
            return score;
        }

        BattleUnit attackTarget = BestAiAttackTarget(unit, profile, q, r);
        if (attackTarget != null)
        {
            score += AiTargetScoreFromCell(unit, profile, attackTarget, q, r) + AiAggression(profile);
        }
        else
        {
            int desired = AiDesiredDistance(unit, profile);
            score -= Mathf.Abs(distance - desired) * (18 + AiCaution(profile) / 12);
            score -= distance * Mathf.Max(4, AiAggression(profile) / 18);
        }

        if (target != null && distance <= AttackRange(unit) + 1)
        {
            score += AiFlankScore(unit, target, q, r) * AiFlankBias(profile) / 12;
        }

        return score;
    }

    private IEnumerable<Vector2Int> AiReachableCells(BattleUnit unit, bool includeCurrent)
    {
        if (unit == null) yield break;
        if (includeCurrent) yield return new Vector2Int(unit.q, unit.r);
        for (int r = 0; r < BattleHexRows(); r++)
        {
            for (int q = 0; q < BattleHexCols(); q++)
            {
                if (q == unit.q && r == unit.r) continue;
                if (CanMoveTo(unit, q, r)) yield return new Vector2Int(q, r);
            }
        }
    }

    private bool MoveBattleUnit(BattleUnit unit, int q, int r)
    {
        if (unit == null || !InsideHex(q, r) || UnitAt(q, r) != null) return false;
        if (unit.q == q && unit.r == r) return false;
        Vector2 from = HexScreen(unit.q, unit.r);
        unit.q = q;
        unit.r = r;
        unit.guarding = false;
        StartBattleAnimation(unit.id, BattleAnimationKind.Move, from, HexScreen(q, r), 0.55f);
        UpdateObjectiveOwner();
        return true;
    }

    private int AiThreatAtCell(BattleUnit unit, int q, int r)
    {
        if (battle == null || unit == null) return 0;
        int threat = 0;
        foreach (BattleUnit playerUnit in battle.units.Where(u => u.faction == Faction.Player && u.hp > 0))
        {
            int distance = HexDistance(q, r, playerUnit.q, playerUnit.r);
            if (distance > AttackRange(playerUnit)) continue;
            int terrain = TerrainDefensePercent(q, r, playerUnit.role);
            threat += Mathf.Max(1, playerUnit.attack / 5 + RoleDamageModifier(playerUnit, unit) - terrain);
        }
        return threat;
    }

    private int AiAllySupportAtCell(BattleUnit unit, int q, int r)
    {
        if (battle == null || unit == null) return 0;
        return battle.units.Count(u => u.faction != Faction.Player && u.hp > 0 && u.id != unit.id && HexDistance(q, r, u.q, u.r) <= 2);
    }

    private int AiFlankScore(BattleUnit unit, BattleUnit target, int q, int r)
    {
        if (battle == null || unit == null || target == null) return 0;
        int nearbyPlayers = battle.units.Count(u => u.faction == Faction.Player && u.hp > 0 && u.id != target.id && HexDistance(q, r, u.q, u.r) <= 2);
        int wounded = target.hp * 100 / Mathf.Max(1, target.maxHp) <= 55 ? 2 : 0;
        int rangedTarget = IsRangedRole(target.role) ? 2 : 0;
        return Mathf.Max(0, 5 - nearbyPlayers) + wounded + rangedTarget;
    }

    private bool IsObjectiveCell(int q, int r)
    {
        return q == BattleObjectiveQ() && r == BattleObjectiveR();
    }

    private int AiDesiredDistance(BattleUnit unit, AiProfileConfig profile)
    {
        int range = AttackRange(unit);
        if (range <= 1) return 1;
        return Mathf.Clamp(AiRangedSpacing(profile), 1, range);
    }

    private int AiAggression(AiProfileConfig profile) { return profile != null && profile.aggression > 0 ? profile.aggression : 100; }
    private int AiCaution(AiProfileConfig profile) { return profile != null && profile.caution > 0 ? profile.caution : 100; }
    private int AiFocusFire(AiProfileConfig profile) { return profile != null && profile.focusFire > 0 ? profile.focusFire : 100; }
    private int AiRetreatHpPercent(AiProfileConfig profile) { return profile != null && profile.retreatHpPercent > 0 ? profile.retreatHpPercent : 25; }
    private int AiTerrainPreference(AiProfileConfig profile) { return profile != null && profile.terrainPreference > 0 ? profile.terrainPreference : 100; }
    private int AiObjectiveBias(AiProfileConfig profile) { return profile != null && profile.objectiveBias > 0 ? profile.objectiveBias : 100; }
    private int AiGuardBias(AiProfileConfig profile) { return profile != null && profile.guardBias > 0 ? profile.guardBias : 80; }
    private int AiFlankBias(AiProfileConfig profile) { return profile != null && profile.flankBias > 0 ? profile.flankBias : 80; }
    private int AiRangedSpacing(AiProfileConfig profile) { return profile != null && profile.rangedSpacing > 0 ? profile.rangedSpacing : 1; }
    private int AiFinishBias(AiProfileConfig profile) { return profile != null && profile.finishBias > 0 ? profile.finishBias : 100; }
    private int AiAvoidCounter(AiProfileConfig profile) { return profile != null && profile.avoidCounter > 0 ? profile.avoidCounter : 80; }

    private bool StepAwayFrom(BattleUnit unit, BattleUnit target)
    {
        return TryMoveEnemy(unit, target, AiProfileForArmy(ArmyById(unit != null ? unit.armyId : "")), AiMoveIntent.Retreat);
    }

    private void StepToward(BattleUnit unit, BattleUnit target)
    {
        TryMoveEnemy(unit, target, AiProfileForArmy(ArmyById(unit != null ? unit.armyId : "")), AiMoveIntent.Advance);
    }

    private bool BattleLabBattleActive()
    {
        return battle != null && !battle.fromStrategy && battleLabDesign != null && battleLabDesign.triggers != null;
    }

    private bool TryFireBattleLabReachTrigger(BattleUnit unit)
    {
        if (!BattleLabBattleActive() || unit == null || unit.hp <= 0) return false;
        foreach (BattleLabTriggerConfig trigger in battleLabDesign.triggers)
        {
            if (trigger == null || trigger.kind != "reach") continue;
            if (!BattleLabTriggerCoversCell(trigger, unit.q, unit.r)) continue;
            if (!BattleLabTriggerMatchesUnit(trigger, unit)) continue;
            if (OpenBattleLabTriggerStory(trigger, unit, null)) return true;
        }
        return false;
    }

    private bool TryFireBattleLabDefeatTrigger(BattleUnit attacker, BattleUnit defeated)
    {
        if (!BattleLabBattleActive() || defeated == null || defeated.hp > 0) return false;
        foreach (BattleLabTriggerConfig trigger in battleLabDesign.triggers)
        {
            if (trigger == null || trigger.kind != "defeat") continue;
            bool sameOriginalCell = BattleLabTriggerCoversCell(trigger, defeated.startQ, defeated.startR);
            bool sameCurrentCell = BattleLabTriggerCoversCell(trigger, defeated.q, defeated.r);
            if (!sameOriginalCell && !sameCurrentCell) continue;
            if (!BattleLabTriggerMatchesUnit(trigger, defeated)) continue;
            if (OpenBattleLabTriggerStory(trigger, attacker, defeated)) return true;
        }
        return false;
    }

    private bool BattleLabTriggerMatchesUnit(BattleLabTriggerConfig trigger, BattleUnit unit)
    {
        if (trigger == null || unit == null) return false;
        string side = SafeText(trigger.side, "any").ToLowerInvariant();
        if (side == "attacker" && unit.faction != Faction.Player) return false;
        if (side == "defender" && unit.faction == Faction.Player) return false;
        string role = SafeText(trigger.role, "any");
        return role == "any" || string.Equals(role, unit.role, StringComparison.OrdinalIgnoreCase);
    }

    private bool OpenBattleLabTriggerStory(BattleLabTriggerConfig trigger, BattleUnit actor, BattleUnit target)
    {
        if (trigger == null || battle == null) return false;
        if (battle.firedTriggerIds == null) battle.firedTriggerIds = new List<string>();
        string triggerId = SafeText(trigger.id, NewBattleLabTriggerId());
        if (trigger.once && battle.firedTriggerIds.Contains(triggerId)) return false;
        if (trigger.once) battle.firedTriggerIds.Add(triggerId);

        string title = SafeText(trigger.title, BattleLabTriggerShortLabel(trigger));
        string body = FormatBattleLabTriggerBody(trigger, actor, target);
        SetBattleMessage(title);
        AddLog(TF("battle_lab.trigger_log", "战场剧情：{0}", title));
        ApplyBattleLabTriggerAction(trigger);

        activeStoryEventId = "";
        pendingStoryTitle = title;
        pendingStorySceneId = "battlefield";
        pendingStoryPortraitName = actor != null ? ShortBattleUnitName(actor) : player.name;
        pendingStoryBody = body;
        pendingStoryOptions = new List<Tuple<string, Action>>();
        pendingStoryReturnAction = () =>
        {
            if (battle != null) ShowBattle();
            else ShowStrategy();
        };
        ShowStoryEvent();
        return true;
    }

    private void ApplyBattleLabTriggerAction(BattleLabTriggerConfig trigger)
    {
        if (trigger == null || battle == null) return;
        string action = SafeText(trigger.action, "none");
        if (action == "none") return;
        if (action == "victory")
        {
            AddLog(T("battle_lab.action_log_victory", "触发器动作：判定蓝方胜利。"));
            ApplyBattleOutcome(true);
            return;
        }
        if (action == "defeat")
        {
            AddLog(T("battle_lab.action_log_defeat", "触发器动作：判定蓝方失败。"));
            ApplyBattleOutcome(false);
            return;
        }
        if (action == "morale_player" || action == "morale_enemy")
        {
            Faction faction = action == "morale_player" ? Faction.Player : Faction.Imperial;
            int delta = Mathf.Clamp(trigger.actionValue == 0 ? 1 : trigger.actionValue, -2, 2);
            foreach (BattleUnit unit in battle.units.Where(u => u.hp > 0 && (faction == Faction.Player ? u.faction == Faction.Player : u.faction != Faction.Player)))
            {
                unit.morale = Mathf.Clamp(unit.morale + delta, BattleCore().minMorale, BattleCore().maxMorale);
            }
            AddLog(TF("battle_lab.action_log_morale", "触发器动作：{0}士气 {1:+#;-#;0}。", faction == Faction.Player ? T("battle.side_player", "蓝方") : T("battle.side_enemy", "红方"), delta));
            return;
        }
        if (action == "spawn_player" || action == "spawn_enemy")
        {
            SpawnBattleLabTriggerUnit(trigger, action == "spawn_enemy" ? "defender" : "attacker");
        }
    }

    private void SpawnBattleLabTriggerUnit(BattleLabTriggerConfig trigger, string side)
    {
        Army army = string.Equals(side, "defender", StringComparison.OrdinalIgnoreCase)
            ? ArmyById(BattleLabDefenderId)
            : ArmyById(BattleLabAttackerId);
        if (army == null) return;
        Vector2Int cell = BattleLabNearestFreeCell(trigger.q, trigger.r);
        if (!InsideHex(cell.x, cell.y))
        {
            AddLog(T("battle_lab.action_log_spawn_failed", "触发器动作：没有空格可刷出援军。"));
            return;
        }
        string role = SafeText(trigger.actionRole, SafeText(trigger.role, battleLabRole));
        int attackBonus = string.Equals(side, "defender", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        BattleUnit unit = NewBattleUnit(army, BattleLabSpawnSuffix(role, side), role, cell.x, cell.y, army.attack + attackBonus, Mathf.Max(45, army.troops / 5));
        unit.id = unit.id + "_trigger_" + battle.units.Count;
        battle.units.Add(unit);
        AddLog(TF("battle_lab.action_log_spawn", "触发器动作：{0}援军出现在第{1}行第{2}列。", string.Equals(side, "defender", StringComparison.OrdinalIgnoreCase) ? T("battle.side_enemy", "红方") : T("battle.side_player", "蓝方"), cell.y + 1, cell.x + 1));
        UpdateObjectiveOwner();
    }

    private Vector2Int BattleLabNearestFreeCell(int q, int r)
    {
        Vector2Int best = new Vector2Int(-1, -1);
        int bestDistance = 999;
        for (int row = 0; row < BattleHexRows(); row++)
        {
            for (int col = 0; col < BattleHexCols(); col++)
            {
                if (UnitAt(col, row) != null) continue;
                int distance = HexDistance(q, r, col, row);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new Vector2Int(col, row);
                }
            }
        }
        return best;
    }

    private string FormatBattleLabTriggerBody(BattleLabTriggerConfig trigger, BattleUnit actor, BattleUnit target)
    {
        string actorName = actor != null ? ShortBattleUnitName(actor) : T("battle_lab.trigger_actor_default", "部队");
        string targetName = target != null ? ShortBattleUnitName(target) : T("battle_lab.trigger_target_default", "目标");
        string body = SafeText(trigger.body, BattleLabTriggerPresetBody(0));
        return body
            .Replace("{actor}", actorName)
            .Replace("{target}", targetName)
            .Replace("{q}", (trigger.q + 1).ToString())
            .Replace("{r}", (trigger.r + 1).ToString())
            .Replace("{radius}", Mathf.Max(0, trigger.radius).ToString());
    }

    private bool CheckBattleLabObjectiveOutcome()
    {
        if (battle == null || battle.fromStrategy || battleLabDesign == null || battle.outcome != "playing") return false;
        string type = BattleLabObjectiveType();
        if (type == "reach" && battle.units.Any(u => u.faction == Faction.Player && u.hp > 0 && u.q == BattleObjectiveQ() && u.r == BattleObjectiveR()))
        {
            SetBattleMessage(T("battle_lab.reach_objective_win", "蓝方已抵达战争目标，测试胜利。"));
            ApplyBattleOutcome(true);
            return true;
        }
        if (CheckBattleLabTurnLimit()) return true;
        return false;
    }

    private bool CheckBattleLabTurnLimit()
    {
        if (battle == null || battle.fromStrategy || battleLabDesign == null || battle.outcome != "playing") return false;
        int limit = battleLabDesign.turnLimit;
        if (limit <= 0 || battle.turn <= limit) return false;
        SetBattleMessage(TF("battle_lab.turn_limit_defeat", "已超过 {0} 回合限制，测试失败。", limit));
        ApplyBattleOutcome(false);
        return true;
    }

    private void CheckBattleOutcome()
    {
        if (battle == null || battle.outcomeApplied) return;
        bool playerAlive = battle.units.Any(u => u.faction == Faction.Player && u.hp > 0);
        bool enemyAlive = battle.units.Any(u => u.faction != Faction.Player && u.hp > 0);
        if (playerAlive && enemyAlive && CheckBattleLabObjectiveOutcome()) return;
        if (playerAlive && enemyAlive) return;
        ApplyBattleOutcome(playerAlive);
    }

    private void ApplyBattleOutcome(bool playerWon)
    {
        if (battle == null || battle.outcomeApplied) return;
        battle.outcome = playerWon ? "victory" : "defeat";
        battle.outcomeApplied = true;
        if (!battle.fromStrategy)
        {
            SetBattleMessage(playerWon ? T("battle_lab.test_victory", "工坊测试胜利：当前关卡可由玩家完成。") : T("battle_lab.test_defeat", "工坊测试失败：当前关卡压力偏高。"));
            return;
        }
        player.battlesFought += 1;
        if (playerWon) player.battleWins += 1;
        else player.battleLosses += 1;
        Army attacker = ArmyById(battle.attackerArmyId);
        Army defender = ArmyById(battle.defenderArmyId);
        Province province = ProvinceById(battle.provinceId);
        if (playerWon)
        {
            BattleCoreConfig core = BattleCore();
            SetBattleMessage(TF("battle.msg.victory_capture", "战斗胜利，{0}归入我方。", province.name));
            province.owner = Faction.Player;
            if (defender != null)
            {
                int captureChance = core.captureChancePercent;
                if (UnityEngine.Random.Range(0, 100) < captureChance)
                {
                    if (player.prisoners == null) player.prisoners = new List<string>();
                    player.prisoners.Add(TF("battle.prisoner_name", "{0}将领", defender.name));
                    AddLog(TF("log.prisoner_captured", "{0}将领被活捉。", defender.name));
                }
                armies.Remove(defender);
            }
            if (attacker != null)
            {
                Province from = ProvinceById(attacker.provinceId);
                if (from != null) from.armyId = "";
                attacker.provinceId = province.id;
                attacker.move = 0;
                attacker.troops = Mathf.Max(core.minTroopsAfterBattle, battle.units.Where(u => u.faction == Faction.Player).Sum(u => u.hp));
                attacker.exp += core.victoryArmyExp;
                ApplyArmyLevel(attacker);
                province.armyId = attacker.id;
                int meritReward = province.defense >= 70
                    ? core.victoryHighDefenseMerit
                    : province.defense >= 55 ? core.victoryMidDefenseMerit : core.victoryLowDefenseMerit;
                player.merit += meritReward;
                AddLog(TF("log.merit_reward", "军令奖励：战功 +{0}。", meritReward));
            }
        }
        else
        {
            BattleCoreConfig core = BattleCore();
            SetBattleMessage(T("battle.msg.defeat", "战斗失败，军团被迫撤退。"));
            if (attacker != null)
            {
                attacker.troops = Mathf.Max(core.minTroopsAfterBattle, attacker.troops / Mathf.Max(1, core.defeatTroopDivisor));
                attacker.move = 0;
            }
            int lockTurns = core.defeatCommandLockTurns;
            player.commandLockTurns = Mathf.Max(player.commandLockTurns, lockTurns);
            AddLog(T("log.command_removed", "上级暂时解除你的指挥权。"));
        }
        UpdatePlayerRank();
        RefreshProgressionSystems(true);
        AutoSave("AUTO_BAT_" + battle.provinceId + "_RESULT");
    }

    private void ReturnToStrategyAfterBattle()
    {
        battle = null;
        battleAnimations.Clear();
        battleUnitViews.Clear();
        battleUnitBadges.Clear();
        selectedUnitId = null;
        selectedArmyId = null;
        ShowStrategy();
    }

    private void ApplyArmyLevel(Army army)
    {
        BattleCoreConfig core = BattleCore();
        while (army.exp >= ExpForArmyLevel(army.level + 1) && army.level < core.armyLevelMax)
        {
            army.level += 1;
            army.attack += core.armyLevelAttackGain;
            army.maxTroops += core.armyLevelMaxTroopsGain;
            army.troops += core.armyLevelTroopsGain;
            AddLog(TF("log.army_level_up", "{0}升至{1}级。", army.name, army.level));
        }
    }

    private int ExpForArmyLevel(int level)
    {
        return level * BattleCore().armyLevelExpStep;
    }

    private void EndStrategyTurn()
    {
        RunStrategyEnemyAi();
        strategyTurn += 1;
        BattleCoreConfig core = BattleCore();
        if (strategyTurn % Mathf.Max(1, core.strategySeasonTurnModulo) == 1) season += 1;
        if (player.commandLockTurns > 0) player.commandLockTurns -= 1;
        int income = provinces.Where(p => p.owner == Faction.Player).Sum(p => p.income);
        player.treasury += income;
        foreach (Army army in armies)
        {
            army.move = army.maxMove;
            int supplyRestore = core.baseSupply + (army.faction == Faction.Player ? ExpLevel(player.logisticsExp) * core.supplyPerLogisticsLevel : 0);
            RestoreArmySupply(army, supplyRestore);
            if (army.faction == Faction.Player && player.treasury > 0)
            {
                int supply = Mathf.Min(core.baseSupply + ExpLevel(player.logisticsExp) * core.supplyPerLogisticsLevel, army.maxTroops - army.troops);
                army.troops += Mathf.Max(0, supply);
                player.treasury -= Mathf.Max(0, supply / Mathf.Max(1, core.supplyTreasuryDivisor));
            }
        }
        AddLog(TF("log.strategy_turn_end", "回合结束：收入 {0}，军团恢复行军力并获得补给。", income));
        RefreshProgressionSystems(true);
        AutoSave("AUTO_STRATEGY_TURN");
        ShowStrategy();
    }

    private void RunStrategyEnemyAi()
    {
        foreach (Army enemy in armies.Where(a => a.faction != Faction.Player).ToList())
        {
            Province from = ProvinceById(enemy.provinceId);
            if (from == null || enemy.move <= 0) continue;
            Province playerNeighbor = from.roads.Select(ProvinceById).Where(p => p != null && p.owner == Faction.Player).FirstOrDefault();
            if (playerNeighbor != null)
            {
                Army defender = ArmyById(playerNeighbor.armyId);
                if (defender != null)
                {
                    BattleCoreConfig core = BattleCore();
                    ConsumeArmySupply(enemy, "attack");
                    int enemyPower = enemy.troops + enemy.attack * core.enemyPowerAttackMultiplier + RandomRangeInt(core.enemyPowerRandomMin, core.enemyPowerRandomMaxExclusive);
                    int defenderPower = defender.troops + defender.attack * core.defenderPowerAttackMultiplier + playerNeighbor.defense + RandomRangeInt(core.enemyPowerRandomMin, core.enemyPowerRandomMaxExclusive);
                    if (enemyPower > defenderPower)
                    {
                        AddLog(TF("log.enemy_capture", "{0}攻陷{1}。", enemy.name, playerNeighbor.name));
                        armies.Remove(defender);
                        from.armyId = "";
                        enemy.provinceId = playerNeighbor.id;
                        enemy.move = 0;
                        playerNeighbor.owner = enemy.faction;
                        playerNeighbor.armyId = enemy.id;
                    }
                    else
                    {
                        enemy.troops = Mathf.Max(core.enemyDefeatMinTroops, enemy.troops - core.enemyDefeatTroopLoss);
                        defender.troops = Mathf.Max(core.enemyDefeatMinTroops, defender.troops - core.defenderVictoryTroopLoss);
                        AddLog(TF("log.enemy_attack_failed", "{0}进攻{1}失败。", enemy.name, playerNeighbor.name));
                    }
                    continue;
                }
            }
            Province target = from.roads.Select(ProvinceById).Where(p => p != null && p.owner != enemy.faction).OrderBy(p => p.owner == Faction.Neutral ? 0 : 1).FirstOrDefault();
            if (target != null && target.owner == Faction.Neutral)
            {
                from.armyId = "";
                target.owner = enemy.faction;
                target.armyId = enemy.id;
                enemy.provinceId = target.id;
                enemy.move = 0;
                ConsumeArmySupply(enemy, "move");
                AddLog(TF("log.enemy_occupy", "{0}占据了{1}。", enemy.name, target.name));
            }
        }
    }

    private Vector2 HexScreen(int q, int r)
    {
        const float stepX = 61f;
        const float stepY = 52.5f;
        const float oddOffset = 30.5f;
        float width = (BattleHexCols() - 1) * stepX + oddOffset;
        float height = (BattleHexRows() - 1) * stepY;
        return new Vector2(-width * 0.5f + q * stepX + (r % 2) * oddOffset, height * 0.5f - r * stepY);
    }

    private IEnumerable<Vector2Int> HexNeighbors(int q, int r)
    {
        int[,] even = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { -1, 1 }, { 0, -1 }, { -1, -1 } };
        int[,] odd = { { 1, 0 }, { -1, 0 }, { 1, 1 }, { 0, 1 }, { 1, -1 }, { 0, -1 } };
        int[,] dirs = r % 2 == 0 ? even : odd;
        for (int i = 0; i < 6; i++) yield return new Vector2Int(q + dirs[i, 0], r + dirs[i, 1]);
    }

    private int HexDistance(int q1, int r1, int q2, int r2)
    {
        Vector3Int a = OffsetToCube(q1, r1);
        Vector3Int b = OffsetToCube(q2, r2);
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
    }

    private Vector3Int OffsetToCube(int q, int r)
    {
        int x = q - (r - (r & 1)) / 2;
        int z = r;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    private bool InsideHex(int q, int r)
    {
        return q >= 0 && q < BattleHexCols() && r >= 0 && r < BattleHexRows();
    }

    private int AttackRange(BattleUnit unit)
    {
        int range = unit != null ? Mathf.Max(1, unit.range) : 1;
        if (battle != null && !battle.fromStrategy && battleLabDesign != null && SafeText(battleLabDesign.weather, "clear") == "fog" && unit != null && IsRangedRole(unit.role))
        {
            range = Mathf.Max(1, range - 1);
        }
        return range;
    }

    private int TerrainDefense(int q, int r)
    {
        string t = TerrainName(q, r);
        if (t == TerrainDisplayName("mountain")) return 6;
        if (t == TerrainDisplayName("forest")) return 3;
        if (q == BattleObjectiveQ() && r == BattleObjectiveR()) return BattleCore().objectiveDefenseBonusPercent;
        return 0;
    }

    private int TerrainDefensePercent(int q, int r, string attackerRole)
    {
        string t = TerrainName(q, r);
        int percent = TerrainDefenseByRole(TerrainRuleByName(t), attackerRole);
        if (q == BattleObjectiveQ() && r == BattleObjectiveR()) percent += BattleCore().objectiveDefenseBonusPercent;
        return percent;
    }

    private string TerrainName(int q, int r)
    {
        BattleTerrainTileConfig tile = BattleTerrainTiles().FirstOrDefault(t => t.q == q && t.r == r);
        if (tile != null) return TerrainDisplayName(tile.terrain);
        if (q == BattleObjectiveQ() && r == BattleObjectiveR()) return TerrainDisplayName("city");
        return TerrainDisplayName("plain");
    }

    private string TerrainDisplayName(string idOrName)
    {
        if (gameConfig.terrainRules != null)
        {
            TerrainRule rule = gameConfig.terrainRules.FirstOrDefault(r => r.id == idOrName || r.name == idOrName);
            if (rule != null && !string.IsNullOrEmpty(rule.name)) return rule.name;
        }
        if (idOrName == "mountain") return "山";
        if (idOrName == "forest") return "林";
        if (idOrName == "river") return "河";
        if (idOrName == "city") return "城";
        return "原";
    }

    private Color TerrainColor(int q, int r)
    {
        string t = TerrainName(q, r);
        TerrainRule rule = TerrainRuleByName(t);
        if (rule != null && TryParseHtmlColor(rule.color, out Color configuredColor)) return configuredColor;
        if (t == TerrainDisplayName("mountain")) return new Color(0.36f, 0.32f, 0.25f);
        if (t == TerrainDisplayName("forest")) return new Color(0.27f, 0.39f, 0.23f);
        if (t == TerrainDisplayName("river")) return new Color(0.18f, 0.34f, 0.48f);
        if (t == TerrainDisplayName("city")) return new Color(0.66f, 0.52f, 0.22f);
        return (q + r) % 2 == 0 ? new Color(0.40f, 0.43f, 0.31f) : new Color(0.34f, 0.39f, 0.28f);
    }

    private bool CanMoveTo(BattleUnit unit, int targetQ, int targetR)
    {
        if (unit == null || !InsideHex(targetQ, targetR) || UnitAt(targetQ, targetR) != null) return false;
        return MovementCostTo(unit, targetQ, targetR) <= unit.move;
    }

    private int MovementCostTo(BattleUnit unit, int targetQ, int targetR)
    {
        string startKey = unit.q + ":" + unit.r;
        Dictionary<string, int> costs = new Dictionary<string, int> { { startKey, 0 } };
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(new Vector2Int(unit.q, unit.r));
        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            int currentCost = costs[current.x + ":" + current.y];
            foreach (Vector2Int next in HexNeighbors(current.x, current.y))
            {
                if (!InsideHex(next.x, next.y) || UnitAt(next.x, next.y) != null) continue;
                int nextCost = currentCost + TerrainMoveCost(unit, next.x, next.y);
                string key = next.x + ":" + next.y;
                if (nextCost > unit.move) continue;
                if (costs.ContainsKey(key) && costs[key] <= nextCost) continue;
                costs[key] = nextCost;
                frontier.Enqueue(next);
            }
        }
        string targetKey = targetQ + ":" + targetR;
        return costs.ContainsKey(targetKey) ? costs[targetKey] : 999;
    }

    private int TerrainMoveCost(BattleUnit unit, int q, int r)
    {
        string t = TerrainName(q, r);
        TerrainRule rule = TerrainRuleByName(t);
        if (rule != null) return Mathf.Max(1, TerrainMoveByRole(rule, unit.role));
        if (t == TerrainDisplayName("mountain")) return IsCavalryRole(unit.role) ? 3 : 2;
        if (t == TerrainDisplayName("river")) return IsCavalryRole(unit.role) ? 3 : 2;
        if (t == TerrainDisplayName("forest")) return IsCavalryRole(unit.role) ? 2 : 1;
        return 1;
    }

    private TerrainRule TerrainRuleByName(string name)
    {
        if (gameConfig.terrainRules == null) return null;
        return gameConfig.terrainRules.FirstOrDefault(r => r.name == name || r.id == name);
    }

    private int TerrainDefenseByRole(TerrainRule rule, string role)
    {
        if (rule == null) return 0;
        if (IsCavalryRole(role)) return rule.defenseCavalry;
        if (IsRangedRole(role)) return rule.defenseArcher;
        return rule.defenseInfantry;
    }

    private int TerrainMoveByRole(TerrainRule rule, string role)
    {
        if (rule == null) return 1;
        if (IsCavalryRole(role)) return rule.moveCavalry;
        if (IsRangedRole(role)) return rule.moveArcher;
        return rule.moveInfantry;
    }

    private bool IsCavalryRole(string role)
    {
        return role == "cavalry" || role == "heavy_cavalry";
    }

    private bool IsRangedRole(string role)
    {
        return role == "archer" || role == "heavy_archer" || role == "musket" || role == "artillery";
    }

    private bool TryParseHtmlColor(string value, out Color color)
    {
        if (!string.IsNullOrEmpty(value) && ColorUtility.TryParseHtmlString(value, out color)) return true;
        color = Color.white;
        return false;
    }

    private void UpdateObjectiveOwner()
    {
        if (battle == null) return;
        BattleUnit controller = UnitAt(BattleObjectiveQ(), BattleObjectiveR());
        if (controller != null) battle.objectiveOwner = controller.faction;
    }

    private void ScoreObjectiveControl()
    {
        if (battle == null || battle.outcome != "playing") return;
        if (!battle.fromStrategy && BattleLabObjectiveType() != "capture") return;
        UpdateObjectiveOwner();
        if (battle.objectiveOwner == Faction.Player)
        {
            int required = PlayerObjectiveRequiredTurns();
            battle.playerObjectiveHold = Mathf.Min(required, battle.playerObjectiveHold + 1);
            battle.enemyObjectiveHold = 0;
            AddLog(TF("log.player_objective_hold", "我方正在巩固中央据点：{0}/{1}。", battle.playerObjectiveHold, required));
            if (battle.playerObjectiveHold >= required) ApplyBattleOutcome(true);
        }
        else if (battle.objectiveOwner != Faction.Neutral)
        {
            int required = EnemyObjectiveRequiredTurns();
            battle.enemyObjectiveHold = Mathf.Min(required, battle.enemyObjectiveHold + 1);
            battle.playerObjectiveHold = 0;
            AddLog(TF("log.enemy_objective_hold", "敌方正在巩固中央据点：{0}/{1}。", battle.enemyObjectiveHold, required));
            if (battle.enemyObjectiveHold >= required) ApplyBattleOutcome(false);
        }
        else
        {
            battle.playerObjectiveHold = 0;
            battle.enemyObjectiveHold = 0;
        }
    }

    private BattleUnit UnitAt(int q, int r)
    {
        if (battle == null) return null;
        return battle.units.FirstOrDefault(u => u.hp > 0 && u.q == q && u.r == r);
    }

    private BattleUnit UnitById(string id)
    {
        if (battle == null || string.IsNullOrEmpty(id)) return null;
        return battle.units.FirstOrDefault(u => u.id == id);
    }

    private bool HasBattleAnimation(string unitId)
    {
        return battleAnimations.Any(a => a.unitId == unitId);
    }

    private void StartBattleAnimation(string unitId, BattleAnimationKind kind, Vector2 from, Vector2 to, float duration, float direction = 0f)
    {
        battleAnimations.RemoveAll(a => a.unitId == unitId && a.kind == kind);
        battleAnimations.Add(new BattleAnimation
        {
            unitId = unitId,
            kind = kind,
            from = from,
            to = to,
            duration = duration,
            direction = direction
        });
    }

    private BattleAnimation BattleAnimationForUnit(string unitId)
    {
        for (int i = battleAnimations.Count - 1; i >= 0; i--)
        {
            if (battleAnimations[i].unitId == unitId) return battleAnimations[i];
        }
        return null;
    }

    private Vector2 UnitRenderPosition(BattleUnit unit)
    {
        BattleAnimation anim = BattleAnimationForUnit(unit.id);
        if (anim == null) return HexScreen(unit.q, unit.r);
        float p = Mathf.Clamp01(anim.elapsed / Mathf.Max(0.01f, anim.duration));
        if (anim.kind == BattleAnimationKind.Move)
        {
            float eased = p * p * (3f - 2f * p);
            return Vector2.Lerp(anim.from, anim.to, eased) + new Vector2(0, Mathf.Sin(p * Mathf.PI) * 12f);
        }
        if (anim.kind == BattleAnimationKind.Attack)
        {
            float pulse = Mathf.Sin(p * Mathf.PI);
            return anim.from + new Vector2(anim.direction * pulse * 9f, pulse * 4f);
        }
        return anim.from + new Vector2(Mathf.Sin(p * Mathf.PI * 3f) * 6f, 0);
    }

    private void RefreshBattleUnitViews()
    {
        if (battle == null) return;
        foreach (KeyValuePair<string, RectTransform> pair in battleUnitViews.ToList())
        {
            BattleUnit unit = UnitById(pair.Key);
            if (unit == null || (unit.hp <= 0 && !HasBattleAnimation(pair.Key)))
            {
                Destroy(pair.Value.gameObject);
                battleUnitViews.Remove(pair.Key);
                battleUnitBadges.Remove(pair.Key);
                battleUnitSprites.Remove(pair.Key);
                continue;
            }

            BattleAnimation anim = BattleAnimationForUnit(pair.Key);
            float p = anim == null ? 0f : Mathf.Clamp01(anim.elapsed / Mathf.Max(0.01f, anim.duration));
            float scale = 1f;
            float angle = 0f;
            float flash = 0f;
            if (anim != null && anim.kind == BattleAnimationKind.Attack)
            {
                float pulse = Mathf.Sin(p * Mathf.PI);
                scale = 1f + pulse * 0.08f;
                angle = anim.direction * pulse * -8f;
            }
            else if (anim != null && anim.kind == BattleAnimationKind.Hit)
            {
                flash = Mathf.Abs(Mathf.Sin(p * Mathf.PI * 3f));
                scale = 1f - p * 0.12f;
            }

            pair.Value.anchoredPosition = UnitRenderPosition(unit);
            pair.Value.localScale = Vector3.one * scale;
            pair.Value.localRotation = Quaternion.Euler(0, 0, angle);
            if (battleUnitBadges.TryGetValue(pair.Key, out BattleUnitBadgeGraphic badge))
            {
                badge.flash = flash;
                badge.SetVerticesDirty();
            }
            if (battleUnitSprites.TryGetValue(pair.Key, out Image spriteImage))
            {
                Sprite sprite = LoadBattleUnitSprite(unit);
                if (sprite != null) spriteImage.sprite = sprite;
            }
        }
    }

    private void UpdateBattleAnimations(float dt)
    {
        if (battle == null) return;
        if (battleAnimations.Count > 0)
        {
            for (int i = battleAnimations.Count - 1; i >= 0; i--)
            {
                battleAnimations[i].elapsed += dt;
                if (battleAnimations[i].elapsed >= battleAnimations[i].duration)
                {
                    battleAnimations.RemoveAt(i);
                }
            }
            PruneDefeatedBattleUnits();
        }
        RefreshBattleUnitViews();
    }

    private void PruneDefeatedBattleUnits()
    {
        if (battle == null) return;
        foreach (BattleUnit unit in battle.units.Where(u => u.hp <= 0 && !HasBattleAnimation(u.id)).ToList())
        {
            battle.units.Remove(unit);
            if (battleUnitViews.TryGetValue(unit.id, out RectTransform view))
            {
                Destroy(view.gameObject);
                battleUnitViews.Remove(unit.id);
                battleUnitBadges.Remove(unit.id);
            }
        }
    }

    private void AddBattleDragEvents(GameObject go)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
        AddEventTrigger(trigger, EventTriggerType.BeginDrag, _ => { battleDragDistance = 0f; });
        AddEventTrigger(trigger, EventTriggerType.Drag, data =>
        {
            PointerEventData pointer = data as PointerEventData;
            if (pointer == null) return;
            float scale = canvas != null ? Mathf.Max(0.1f, canvas.scaleFactor) : 1f;
            Vector2 delta = pointer.delta / scale;
            battleDragDistance += delta.magnitude;
            battlePan += delta;
            battlePan.x = Mathf.Clamp(battlePan.x, -190f, 190f);
            battlePan.y = Mathf.Clamp(battlePan.y, -105f, 135f);
            if (battleBoardContent != null) battleBoardContent.anchoredPosition = battlePan;
        });
        AddEventTrigger(trigger, EventTriggerType.EndDrag, _ =>
        {
            if (battleDragDistance > 8f) battleIgnoreClickUntil = Time.unscaledTime + 0.12f;
            battleDragDistance = 0f;
        });
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => callback(data));
        trigger.triggers.Add(entry);
    }

    private bool IsAdjacent(string a, string b)
    {
        Province pa = ProvinceById(a);
        return pa != null && pa.roads.Contains(b);
    }

    private Province ProvinceById(string id)
    {
        return provinces.FirstOrDefault(p => p.id == id);
    }

    private Army ArmyById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return armies.FirstOrDefault(a => a.id == id);
    }

    private Color ProvinceColor(Faction faction)
    {
        if (faction == Faction.Player) return playerColor;
        if (faction == Faction.Neutral) return neutralColor;
        if (faction == Faction.Native) return new Color(0.32f, 0.47f, 0.24f);
        if (faction == Faction.Imperial) return enemyColor;
        if (faction == Faction.Reformist) return new Color(0.50f, 0.36f, 0.66f);
        if (faction == Faction.Foreign) return new Color(0.66f, 0.48f, 0.22f);
        return enemyColor;
    }

    private string FactionName(Faction faction)
    {
        string id = faction.ToString();
        FactionConfig configured = FactionConfigs().FirstOrDefault(f => f.id == id);
        if (configured != null && !string.IsNullOrEmpty(configured.displayName)) return configured.displayName;
        switch (faction)
        {
            case Faction.Player: return "新京都督府";
            case Faction.Imperial: return "返乡团/龙旗朝廷";
            case Faction.Reformist: return "革故自治军";
            case Faction.Native: return "印第安乡党";
            case Faction.Foreign: return "外邦商馆";
            default: return "边地中立";
        }
    }

    private string FactionDisplayName(string value)
    {
        if (string.IsNullOrEmpty(value)) return T("common.unknown", "未知");
        FactionConfig configured = FactionConfigs().FirstOrDefault(f => f.id == value || f.displayName == value);
        if (configured != null && !string.IsNullOrEmpty(configured.displayName)) return configured.displayName;
        return FactionName(ParseFaction(value, Faction.Neutral));
    }

    private string ArmyShort(Army army)
    {
        return army.name + " Lv" + army.level + " " + army.troops;
    }

    private void AddLog(string line)
    {
        logLines.Insert(0, line);
        if (logLines.Count > 80) logLines.RemoveAt(logLines.Count - 1);
    }

    private string LatestLog(int count)
    {
        return string.Join("\n", logLines.Take(count).ToArray());
    }

    private void SaveGame()
    {
        SaveGameSlot("MANUAL_1", mode == ScreenMode.Strategy ? ScreenMode.Strategy : ScreenMode.Academy, false);
    }

    private SaveData CaptureSaveData()
    {
        SaveData data = new SaveData
        {
            player = player,
            relationships = relationships,
            stances = stances,
            provinces = provinces,
            armies = armies.Where(a => !IsBattleLabTempArmy(a)).ToList(),
            strategyTurn = strategyTurn,
            season = season,
            mode = mode == ScreenMode.BattleLab ? ScreenMode.Strategy : mode,
            log = string.Join("\n", logLines.ToArray()),
            currentMainEventId = currentMainEventId,
            completedStoryEvents = completedStoryEvents.ToList(),
            storyValues = storyValues
        };
        return data;
    }

    private string SaveKeyForSlot(string slot)
    {
        return SaveKey + "_" + SafeText(slot, "MANUAL_1");
    }

    private void SaveGameSlot(string slot, ScreenMode returnMode, bool showPanelAfter)
    {
        SaveData data = CaptureSaveData();
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.SetString(SaveKeyForSlot(slot), json);
        PlayerPrefs.SetString(SaveKeyForSlot(slot) + "_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        PlayerPrefs.Save();
        AddLog(TF("log.save_success_slot", "系统：已保存到 {0}。", slot));
        if (showPanelAfter) ShowSavePanel(returnMode);
        else if (mode == ScreenMode.Strategy) ShowStrategy();
        else if (mode == ScreenMode.Academy) ShowAcademy();
    }

    private void LoadGame()
    {
        LoadGameSlot("MANUAL_1", true);
    }

    private void LoadGameSlot(string slot, bool fallbackDefault = false)
    {
        string raw = PlayerPrefs.GetString(SaveKeyForSlot(slot), "");
        if (string.IsNullOrEmpty(raw) && fallbackDefault) raw = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(raw))
        {
            AddLog(T("log.save_missing", "没有找到存档。"));
            ShowTitle();
            return;
        }
        SaveData data = JsonUtility.FromJson<SaveData>(raw);
        player = data.player ?? new PlayerProfile();
        EnsurePlayerRuntimeLists(player);
        relationships = data.relationships ?? relationships;
        foreach (Relationship rel in relationships)
        {
            if (string.IsNullOrEmpty(rel.circle)) rel.circle = rel.stance;
            if (rel.knownLevel <= 0) rel.knownLevel = RelationshipKnownLevel(rel.affection);
            if (rel.lastInteractionWeek <= 0) rel.lastInteractionWeek = CurrentCalendarWeek();
        }
        stances = data.stances ?? stances;
        provinces = data.provinces ?? provinces;
        armies = data.armies ?? armies;
        bool upgradedStrategyMap = false;
        if (StrategyMapNeedsConfigRefresh())
        {
            BuildStrategyMap();
            upgradedStrategyMap = true;
        }
        foreach (Army army in armies)
        {
            if (army.maxSupply <= 0) army.maxSupply = DefaultArmyMaxSupply(army.faction);
            if (army.supply <= 0) army.supply = Mathf.Min(army.maxSupply, DefaultArmyMaxSupply(army.faction));
            if (string.IsNullOrEmpty(army.aiProfile)) army.aiProfile = DefaultAiProfileForFaction(army.faction);
        }
        strategyTurn = data.strategyTurn <= 0 ? 1 : data.strategyTurn;
        season = data.season <= 0 ? 1760 : data.season;
        logLines = string.IsNullOrEmpty(data.log) ? new List<string>() : data.log.Split('\n').ToList();
        currentMainEventId = string.IsNullOrEmpty(data.currentMainEventId) ? "EV001" : data.currentMainEventId;
        completedStoryEvents.Clear();
        if (data.completedStoryEvents != null) completedStoryEvents.AddRange(data.completedStoryEvents.Where(id => !string.IsNullOrEmpty(id)));
        storyValues = data.storyValues ?? new List<StoryValue>();
        RefreshProgressionSystems(false);
        AddLog(TF("log.load_success_slot", "系统：已读取 {0}。", slot));
        if (upgradedStrategyMap) AddLog(T("log.strategy_map_upgraded", "系统：战略地图已升级为北美战区配置。"));
        ShowStrategy();
    }

    private bool StrategyMapNeedsConfigRefresh()
    {
        if (provinces == null || provinces.Count < 30) return true;
        return provinces.Any(p => p == null || p.cities == null || p.cities.Count < 5);
    }

    private void AutoSave(string slot)
    {
        SaveData data = CaptureSaveData();
        PlayerPrefs.SetString(SaveKeyForSlot(slot), JsonUtility.ToJson(data));
        PlayerPrefs.SetString(SaveKeyForSlot(slot) + "_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        PlayerPrefs.Save();
    }

    private void StartNewGamePlus(ScreenMode returnMode)
    {
        int nextPlus = player.newGamePlus + 1;
        List<string> titles = player.unlockedTitles.ToList();
        List<string> achievements = player.unlockedAchievements.ToList();
        List<string> endings = player.unlockedEndings.ToList();
        int points = player.achievementPoints;
        ResetGame();
        player.newGamePlus = nextPlus;
        player.unlockedTitles = titles;
        player.unlockedAchievements = achievements;
        player.unlockedEndings = endings;
        player.achievementPoints = points;
        if (!player.unlockedTitles.Contains("echo_memory")) player.unlockedTitles.Add("echo_memory");
        player.equippedTitle = "echo_memory";
        RefreshProgressionSystems(true);
        AutoSave("AUTO_NG_PLUS");
        AddLog(TF("log.new_game_plus", "第 {0} 周目开始，记忆回响已保留。", player.newGamePlus + 1));
        ShowCharacterCreate();
    }

    private void Update()
    {
        if (mode == ScreenMode.Battle)
        {
            UpdateBattleAnimations(Time.unscaledDeltaTime);
        }
        else if (mode == ScreenMode.BattleLab)
        {
            RefreshBattleLabSpawnSprites();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mode == ScreenMode.Battle)
            {
                selectedUnitId = null;
                ShowBattle();
            }
            else if (mode == ScreenMode.BattleConfirm)
            {
                CancelPendingAttack();
            }
            else if (mode == ScreenMode.StoryEvent)
            {
                if (pendingStoryReturnAction != null)
                {
                    Action back = pendingStoryReturnAction;
                    pendingStoryReturnAction = null;
                    back();
                }
                else
                {
                    ReturnToStoryCaller();
                }
            }
            else if (mode != ScreenMode.Title) ShowTitle();
        }
        if (Input.GetKeyDown(KeyCode.End))
        {
            if (mode == ScreenMode.Battle) EndBattleTurn();
            else if (mode == ScreenMode.Strategy) EndStrategyTurn();
        }
    }
}
