#if UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace ProjectLink.EditorTools
{
    public sealed class GoogleMobileAdsGradleManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string AndroidLibDirectoryName = "GoogleMobileAdsPlugin.androidlib";
        private const string LegacyPackageName = "com.google.unity.ads";
        private const string PatchedNamespaceName = "com.google.unity.ads.androidlib";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = FindGoogleMobileAdsManifest(path);
            if (string.IsNullOrEmpty(manifestPath))
            {
                return;
            }

            var document = XDocument.Load(manifestPath);
            var manifest = document.Root;
            if (manifest == null)
            {
                return;
            }

            var packageAttribute = manifest.Attribute("package");
            if (packageAttribute?.Value == LegacyPackageName)
            {
                packageAttribute.Remove();
                document.Save(manifestPath);
                Debug.Log($"Removed legacy Android package attribute from {manifestPath}");
            }

            var buildGradlePath = Path.Combine(Path.GetDirectoryName(manifestPath), "build.gradle");
            PatchGoogleMobileAdsNamespace(buildGradlePath);
        }

        private static string FindGoogleMobileAdsManifest(string path)
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            return Directory
                .GetFiles(path, "AndroidManifest.xml", SearchOption.AllDirectories)
                .FirstOrDefault(file => Path.GetFileName(Path.GetDirectoryName(file)) == AndroidLibDirectoryName);
        }

        private static void PatchGoogleMobileAdsNamespace(string buildGradlePath)
        {
            if (!File.Exists(buildGradlePath))
            {
                return;
            }

            var contents = File.ReadAllText(buildGradlePath);
            var originalNamespace = $"namespace \"{LegacyPackageName}\"";
            var patchedNamespace = $"namespace \"{PatchedNamespaceName}\"";
            if (!contents.Contains(originalNamespace))
            {
                return;
            }

            File.WriteAllText(buildGradlePath, contents.Replace(originalNamespace, patchedNamespace));
            Debug.Log($"Patched Google Mobile Ads androidlib namespace in {buildGradlePath}");
        }
    }
}
#endif
