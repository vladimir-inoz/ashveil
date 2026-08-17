using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class GameBuild
{
    const string ScenePath = "Assets/Scenes/Boot.unity";

    [MenuItem("Ashveil/Подготовить проект")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Builds");
        PlayerSettings.companyName = "Ashveil";
        PlayerSettings.productName = "Ashveil";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.fullScreenMode = FullScreenMode.MaximizedWindow;
        PatchInputHandler();
        IncludeGameShaders();

        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.Refresh();
        Debug.Log("Ashveil project ready.");
    }

    [MenuItem("Ashveil/Собрать и запустить")]
    public static void BuildAndRun()
    {
        Setup();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
        string dest = Path.GetFullPath("Builds/Ashveil.app");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = dest,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Ashveil build failed: " + report.summary.result);
        Debug.Log("Ashveil build OK: " + dest);
    }

    public static void PerformBuild()
    {
        try
        {
            BuildAndRun();
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            EditorApplication.Exit(1);
        }
    }

    static void IncludeGameShaders()
    {
        var names = new[] { "Ashveil/Unlit", "Ashveil/UnlitAlpha", "Ashveil/Sky" };
        var so = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) return;
        foreach (var name in names)
        {
            var sh = Shader.Find(name);
            if (sh == null) continue;
            bool found = false;
            for (int i = 0; i < arr.arraySize; i++)
            {
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh)
                    found = true;
            }
            if (found) continue;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
        }
        so.ApplyModifiedProperties();
    }

    static void PatchInputHandler()
    {
        const string path = "ProjectSettings/ProjectSettings.asset";
        if (!File.Exists(path)) return;
        string text = File.ReadAllText(path);
        string patched = Regex.Replace(text, @"activeInputHandler: \d+", "activeInputHandler: 2");
        if (patched != text)
            File.WriteAllText(path, patched);
    }
}

[InitializeOnLoad]
static class GameAutoSetup
{
    static GameAutoSetup()
    {
        EditorApplication.delayCall += GameBuild.Setup;
    }
}
