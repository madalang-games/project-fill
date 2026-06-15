#if UNITY_EDITOR
using Game.Core.UI;
using UnityEditor;

namespace Game.Editor
{
    class StringCsvPostprocessor : AssetPostprocessor
    {
        private const string WatchPath = "Assets/Resources/Data/string/client_string.csv";

        static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets,    string[] movedFromPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path == WatchPath)
                {
                    LocalizedText.RefreshAllInEditor();
                    break;
                }
            }
        }

        [MenuItem("Tools/Localization/Refresh Editor Text Preview", false, 301)]
        static void RefreshManual() => LocalizedText.RefreshAllInEditor();
    }
}
#endif
