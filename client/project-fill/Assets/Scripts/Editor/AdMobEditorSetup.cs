using UnityEditor;
using UnityEditor.Build;
using Game.Core;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class AdMobEditorSetup
    {
        static AdMobEditorSetup()
        {
            EnsureDefineSymbol(NamedBuildTarget.Android, "GOOGLE_MOBILE_ADS");
            EnsureDefineSymbol(NamedBuildTarget.iOS, "GOOGLE_MOBILE_ADS");
            EnsureDefineSymbol(NamedBuildTarget.Standalone, "GOOGLE_MOBILE_ADS");

            SyncAdMobSettings();
        }

        private static void SyncAdMobSettings()
        {
            var assemblyName = "Assembly-CSharp-Editor-firstpass";
            var typeName = "GoogleMobileAds.Editor.GoogleMobileAdsSettings";
            var type = System.Type.GetType($"{typeName}, {assemblyName}");
            if (type == null)
            {
                type = System.Type.GetType($"{typeName}, Assembly-CSharp-Editor");
            }
            if (type == null)
            {
                UnityEngine.Debug.LogWarning("[AdMobEditorSetup] Could not find GoogleMobileAdsSettings type via reflection.");
                return;
            }

            var loadInstanceMethod = type.GetMethod("LoadInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (loadInstanceMethod == null)
            {
                UnityEngine.Debug.LogWarning("[AdMobEditorSetup] LoadInstance method not found.");
                return;
            }

            var settings = loadInstanceMethod.Invoke(null, null) as UnityEngine.ScriptableObject;
            if (settings == null)
            {
                UnityEngine.Debug.LogWarning("[AdMobEditorSetup] Failed to load GoogleMobileAdsSettings instance.");
                return;
            }

            var androidProp = type.GetProperty("GoogleMobileAdsAndroidAppId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var iosProp = type.GetProperty("GoogleMobileAdsIOSAppId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (androidProp == null || iosProp == null)
            {
                UnityEngine.Debug.LogWarning("[AdMobEditorSetup] App ID properties not found on GoogleMobileAdsSettings.");
                return;
            }

            string currentAndroid = androidProp.GetValue(settings) as string;
            string currentIos = iosProp.GetValue(settings) as string;

            bool changed = false;

            if (currentAndroid != AppConfig.AdMobAndroidAppId)
            {
                androidProp.SetValue(settings, AppConfig.AdMobAndroidAppId);
                changed = true;
            }

            if (currentIos != AppConfig.AdMobIOSAppId)
            {
                iosProp.SetValue(settings, AppConfig.AdMobIOSAppId);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("[AdMobEditorSetup] Automatically synchronized AdMob App IDs from AppConfig via Reflection.");
            }
        }

        private static void EnsureDefineSymbol(NamedBuildTarget target, string symbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(target);
            if (string.IsNullOrEmpty(defines))
            {
                PlayerSettings.SetScriptingDefineSymbols(target, symbol);
            }
            else if (!defines.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbols(target, $"{defines};{symbol}");
            }
        }
    }
}
