using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
