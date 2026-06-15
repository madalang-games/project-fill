using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class IconAutomator : EditorWindow
{
    private Texture2D sourceIcon;
    private const string TargetPath = "Assets/GeneratedIcons";

    [MenuItem("Tools/Icon Automator")]
    public static void ShowWindow()
    {
        GetWindow<IconAutomator>("Icon Automator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Unity Icon Automation (Unity 6+)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceIcon = (Texture2D)EditorGUILayout.ObjectField("Source Icon (1024x1024)", sourceIcon, typeof(Texture2D), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate and Apply to All Platforms", GUILayout.Height(40)))
        {
            if (sourceIcon == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a source icon.", "OK");
                return;
            }

            GenerateAndApply();
        }

        EditorGUILayout.HelpBox("Resizes source icon and applies to Android (Legacy & Adaptive) and iOS. Saved to Assets/GeneratedIcons.", MessageType.Info);
    }

    private void GenerateAndApply()
    {
        try
        {
            EnsureTextureReadable(sourceIcon);

            if (!AssetDatabase.IsValidFolder(TargetPath))
                AssetDatabase.CreateFolder("Assets", "GeneratedIcons");
            if (!AssetDatabase.IsValidFolder($"{TargetPath}/Android"))
                AssetDatabase.CreateFolder(TargetPath, "Android");
            if (!AssetDatabase.IsValidFolder($"{TargetPath}/iOS"))
                AssetDatabase.CreateFolder(TargetPath, "iOS");

            ProcessStandardIcons(BuildTargetGroup.Android, "Android");
            ProcessStandardIcons(BuildTargetGroup.iOS, "iOS");
            ProcessAndroidAdaptiveIcons();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", "Icons generated and applied successfully.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[IconAutomator] Error: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", "An error occurred: " + e.Message, "OK");
        }
    }

    private void EnsureTextureReadable(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    private void ProcessStandardIcons(BuildTargetGroup group, string platformName)
    {
        int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(group);
        Texture2D[] textures = new Texture2D[sizes.Length];

        for (int i = 0; i < sizes.Length; i++)
            textures[i] = CreateResizedTextureAsset(platformName, $"Icon_{sizes[i]}", sizes[i]);

        PlayerSettings.SetIconsForTargetGroup(group, textures);
    }

    private void ProcessAndroidAdaptiveIcons()
    {
        NamedBuildTarget target = NamedBuildTarget.Android;
        PlatformIconKind adaptiveKind = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(target, adaptiveKind);

        if (icons == null || icons.Length == 0)
        {
            Debug.LogWarning("[IconAutomator] No Adaptive Icon slots found for Android.");
            return;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            int size = icons[i].width;
            Texture2D fg = CreateResizedTextureAsset("Android", $"Adaptive_FG_{size}", size);
            Texture2D bg = CreateResizedTextureAsset("Android", $"Adaptive_BG_{size}", size);
            icons[i].SetTextures(bg, fg);
        }

        PlayerSettings.SetPlatformIcons(target, adaptiveKind, icons);
    }

    private Texture2D CreateResizedTextureAsset(string platform, string name, int size)
    {
        string fileName = $"{name}.png";
        string assetPath = $"{TargetPath}/{platform}/{fileName}";
        string fullPath = Path.Combine(Application.dataPath, "..", assetPath);

        Texture2D resized = ResizeTexture(sourceIcon, size, size);
        byte[] bytes = resized.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        DestroyImmediate(resized);

        AssetDatabase.ImportAsset(assetPath);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
