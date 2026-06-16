using System;
using System.Collections;
using System.Collections.Generic;
using Game.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Services
{
    public sealed class BootstrapService : MonoBehaviour
    {
        public static BootstrapService Instance { get; private set; }

        public enum BootstrapResult { OK, ForceUpdate, PatchFailed }

        [Serializable]
        private class ConfigJson
        {
            public bool   forceUpdate;
            public string dataSchemaVersion;
            public string metaHash;
        }

        [Serializable]
        private class BundleFileJson
        {
            public string path;
            public string content;
        }

        [Serializable]
        private class BundleJson
        {
            public string schemaVersion;
            public string metaHash;
            public BundleFileJson[] files;
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(Action<BootstrapResult> onComplete)
            => StartCoroutine(Run(onComplete));

        private IEnumerator Run(Action<BootstrapResult> onComplete)
        {
            var baseUrl = NetworkService.Instance.BaseUrl.TrimEnd('/');

            using var configReq = UnityWebRequest.Get(baseUrl + "/api/bootstrap/config");
            configReq.SetRequestHeader("X-Client-Version",   Application.version);
            configReq.SetRequestHeader("X-Protocol-Version", NetworkService.Instance.ProtocolVersion);
            configReq.timeout = 10;
            yield return configReq.SendWebRequest();

            var configOk = configReq.result == UnityWebRequest.Result.Success;
            LogHttp("GET", baseUrl + "/api/bootstrap/config", (int)configReq.responseCode, configReq.downloadHandler.text, !configOk);

            if (!configOk) { onComplete(BootstrapResult.PatchFailed); yield break; }

            var config = JsonUtility.FromJson<ConfigJson>(configReq.downloadHandler.text);
            if (config == null) { onComplete(BootstrapResult.PatchFailed); yield break; }

            if (config.forceUpdate) { onComplete(BootstrapResult.ForceUpdate); yield break; }

            // Data schema version — structural CSV change requires app update
            var embeddedSchema = LoadText("data/data_schema_version");
            if (!string.IsNullOrEmpty(config.dataSchemaVersion) && !string.IsNullOrEmpty(embeddedSchema)
                && config.dataSchemaVersion != embeddedSchema)
            {
                onComplete(BootstrapResult.ForceUpdate);
                yield break;
            }

            // Meta hash — data content changed, apply OTA patch
            var embeddedHash = LoadText("data/meta_hash_cs");
            var patchedHash  = CsvLoader.GetPatchedMetaHash();

            if (!string.IsNullOrEmpty(config.metaHash) && config.metaHash != patchedHash)
            {
                if (config.metaHash == embeddedHash && !string.IsNullOrEmpty(patchedHash))
                {
                    CsvLoader.ClearPatch();
                }
                else if (config.metaHash != embeddedHash)
                {
                    using var bundleReq = UnityWebRequest.Get(baseUrl + "/api/data/bundle");
                    bundleReq.timeout = 60;
                    yield return bundleReq.SendWebRequest();

                    var bundleOk = bundleReq.result == UnityWebRequest.Result.Success;
                    LogHttp("GET", baseUrl + "/api/data/bundle", (int)bundleReq.responseCode, bundleReq.downloadHandler.text, !bundleOk);

                    if (!bundleOk) { onComplete(BootstrapResult.PatchFailed); yield break; }

                    var bundle = JsonUtility.FromJson<BundleJson>(bundleReq.downloadHandler.text);
                    if (bundle?.files == null) { onComplete(BootstrapResult.PatchFailed); yield break; }

                    var fileDict = new Dictionary<string, string>(bundle.files.Length);
                    foreach (var f in bundle.files)
                        if (f != null) fileDict[f.path] = f.content;

                    try { CsvLoader.ApplyPatchAtomic(fileDict, bundle.metaHash); }
                    catch (Exception e)
                    {
                        Debug.LogError($"[BootstrapService] Patch apply failed: {e.Message}");
                        onComplete(BootstrapResult.PatchFailed);
                        yield break;
                    }
                }
            }

            onComplete(BootstrapResult.OK);
        }

        private static void LogHttp(string method, string url, int code, string response, bool isError)
        {
            var msg = $"[HTTP {(isError ? "✗" : "✓")}] {method} {url} → {code}\n  response: {response}";
            if (isError) Debug.LogError(msg); else Debug.Log(msg);
        }

        private static string LoadText(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            return asset != null ? asset.text.Trim() : string.Empty;
        }

    }
}
