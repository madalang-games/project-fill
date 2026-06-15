using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    const string DefaultOutputDir = "build/android-release";
    const string AabFileName      = "project-fill.aab";

    public static void BuildAndroidRelease()
    {
        var outputPath = GetArg("-buildOutput");
        if (string.IsNullOrEmpty(outputPath))
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            outputPath = Path.Combine(projectRoot, DefaultOutputDir, AabFileName);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var scenes = EditorBuildSettings.scenes.Length > 0
            ? Array.ConvertAll(EditorBuildSettings.scenes, s => s.path)
            : new[] { "Assets/Scenes/Bootstrap.unity" };

        var opts = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = outputPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        EditorUserBuildSettings.buildAppBundle = true;
        ApplyAndroidReleaseSizeSettings();

        var keystorePass = System.Environment.GetEnvironmentVariable("KEYSTORE_PASS") ?? GetArg("-keystorePass") ?? "";
        var keyAliasPass = System.Environment.GetEnvironmentVariable("KEY_ALIAS_PASS") ?? GetArg("-keyaliasPass") ?? keystorePass;

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystorePass      = keystorePass;
        PlayerSettings.Android.keyaliasPass      = keyAliasPass;

        Debug.Log($"[BuildScript] Building Android AAB → {outputPath}");
        var report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] SUCCESS: {report.summary.totalSize} bytes → {outputPath}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[BuildScript] FAILED: {report.summary.result} — {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
    }

    private static string? GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }

    private static void ApplyAndroidReleaseSizeSettings()
    {
        var android = NamedBuildTarget.Android;

        EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.Android.minifyRelease = true;
        PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCodeGeneration(android, Il2CppCodeGeneration.OptimizeSize);
    }
}
