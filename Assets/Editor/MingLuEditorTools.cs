using System.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MingLuEditorTools
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [InitializeOnLoadMethod]
    public static void EnsureProjectScene()
    {
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        if (!File.Exists(ScenePath))
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MingLuRuntimeBootstrapScene");
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    public static void Verify()
    {
        EnsureProjectScene();
        string[] scripts = AssetDatabase.FindAssets("MingLuGame t:MonoScript");
        if (scripts == null || scripts.Length == 0)
        {
            Debug.LogError("MingLuGame.cs was not imported.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("MingLu verification passed: scene and scripts are importable.");
        EditorApplication.Exit(0);
    }

    public static void AuditUiTextFit()
    {
        EnsureProjectScene();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject go = new GameObject("MingLuUiAuditGame");
        MingLuGame game = go.AddComponent<MingLuGame>();
        Type gameType = typeof(MingLuGame);
        Invoke(game, gameType, "Awake");

        List<string> issues = new List<string>();
        AuditCurrentScreen("Title", issues);

        for (int step = 0; step <= 5; step++)
        {
            SetPrivateField(game, gameType, "creationStep", step);
            Invoke(game, gameType, "ShowCharacterCreate");
            AuditCurrentScreen("CharacterCreate step " + step, issues);
        }

        Invoke(game, gameType, "ShowAcademy");
        AuditCurrentScreen("Academy", issues);

        Invoke(game, gameType, "ShowStrategy");
        AuditCurrentScreen("Strategy", issues);

        Invoke(game, gameType, "ShowBattleLabEditor");
        AuditCurrentScreen("BattleLab", issues);

        Type screenModeType = gameType.GetNestedType("ScreenMode", BindingFlags.NonPublic);
        MethodInfo startStory = gameType.GetMethod("StartStory", BindingFlags.Instance | BindingFlags.NonPublic);
        if (screenModeType != null && startStory != null)
        {
            object academyMode = Enum.Parse(screenModeType, "Academy");
            startStory.Invoke(game, new object[] { "EV001", academyMode });
            AuditCurrentScreen("Story EV001", issues);
        }

        if (issues.Count > 0)
        {
            Debug.LogError("MingLu UI text fit audit found " + issues.Count + " issue(s).");
            foreach (string issue in issues) Debug.LogWarning(issue);
            EditorApplication.Exit(2);
            return;
        }

        Debug.Log("MingLu UI text fit audit passed: no visible text overflow detected in sampled screens.");
        EditorApplication.Exit(0);
    }

    private static void Invoke(object target, Type type, string methodName)
    {
        MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new MissingMethodException(type.FullName, methodName);
        method.Invoke(target, null);
        Canvas.ForceUpdateCanvases();
    }

    private static void SetPrivateField(object target, Type type, string fieldName, object value)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(type.FullName, fieldName);
        field.SetValue(target, value);
    }

    private static void AuditCurrentScreen(string screenName, List<string> issues)
    {
        Canvas.ForceUpdateCanvases();
        foreach (Text text in UnityEngine.Object.FindObjectsOfType<Text>(true))
        {
            if (text == null || !text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
            RectTransform rt = text.rectTransform;
            if (rt == null) continue;
            Vector2 size = rt.rect.size;
            if (size.x <= 2f || size.y <= 2f) continue;

            TextGenerationSettings heightSettings = text.GetGenerationSettings(new Vector2(size.x, 0f));
            TextGenerationSettings widthSettings = text.GetGenerationSettings(Vector2.zero);
            if (text.resizeTextForBestFit)
            {
                int minSize = Mathf.Max(1, text.resizeTextMinSize);
                heightSettings.fontSize = minSize;
                widthSettings.fontSize = minSize;
            }

            float preferredHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, heightSettings) / text.pixelsPerUnit;
            float preferredWidth = text.cachedTextGeneratorForLayout.GetPreferredWidth(text.text, widthSettings) / text.pixelsPerUnit;
            float tolerance = text.resizeTextForBestFit ? 4f : 2f;
            bool verticalTooTall = preferredHeight > size.y + tolerance;
            bool horizontalOverflow = text.horizontalOverflow == HorizontalWrapMode.Overflow && preferredWidth > size.x + tolerance;
            if (!verticalTooTall && !horizontalOverflow) continue;

            string mode = verticalTooTall
                ? (text.verticalOverflow == VerticalWrapMode.Overflow ? "overflows" : "clips")
                : "overflows horizontally";
            issues.Add(string.Format(
                "[{0}] {1} {2}: rect={3:0.#}x{4:0.#}, preferred={5:0.#}x{6:0.#}, text=\"{7}\"",
                screenName,
                GameObjectPath(text.gameObject),
                mode,
                size.x,
                size.y,
                preferredWidth,
                preferredHeight,
                PreviewText(text.text)));
        }
    }

    private static string GameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }

    private static string PreviewText(string text)
    {
        string value = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        return value.Length > 48 ? value.Substring(0, 48) + "..." : value;
    }

    public static void BuildWindows()
    {
        EnsureProjectScene();
        string output = "Builds/Windows/MingLu.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReportGuard(BuildPipeline.BuildPlayer(options));
    }

    private static void BuildReportGuard(UnityEditor.Build.Reporting.BuildReport report)
    {
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("Build failed: " + report.summary.result);
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("Build succeeded: " + report.summary.outputPath);
        EditorApplication.Exit(0);
    }
}
