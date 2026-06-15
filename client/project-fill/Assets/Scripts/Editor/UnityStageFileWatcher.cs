#if UNITY_EDITOR
using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class UnityStageFileWatcher
    {
        private static FileSystemWatcher _watcher;
        private static DateTime _lastWriteTime;

        static UnityStageFileWatcher()
        {
            // Set up watcher on shared/datas/stage/stage.csv
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string targetDir = Path.Combine(projectRoot, "shared", "datas", "stage");
            
            if (!Directory.Exists(targetDir))
            {
                return;
            }

            _watcher = new FileSystemWatcher
            {
                Path = targetDir,
                Filter = "stage.csv",
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnStageCsvChanged;
            _lastWriteTime = DateTime.MinValue;
            
            Debug.Log($"[UnityStageFileWatcher] Active on directory: {targetDir}");
        }

        private static void OnStageCsvChanged(object sender, FileSystemEventArgs e)
        {
            // Simple debounce using write timestamp to prevent multiple triggers in short intervals
            var currentWrite = File.GetLastWriteTime(e.FullPath);
            if ((currentWrite - _lastWriteTime).TotalMilliseconds < 1000)
            {
                return;
            }
            _lastWriteTime = currentWrite;

            Debug.Log("[UnityStageFileWatcher] stage.csv modification detected. Auto-triggering data generators...");
            
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string batPath = Path.Combine(projectRoot, "tools", "all_generator.bat");

            if (!File.Exists(batPath))
            {
                batPath = Path.Combine(projectRoot, "tools", "info_generator.bat");
            }

            if (File.Exists(batPath))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = batPath,
                        WorkingDirectory = Path.GetDirectoryName(batPath),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = new Process { StartInfo = startInfo })
                    {
                        process.OutputDataReceived += (s, ev) => { if (ev.Data != null) Debug.Log($"[Generator Output] {ev.Data}"); };
                        process.ErrorDataReceived += (s, ev) => { if (ev.Data != null) Debug.LogWarning($"[Generator Error] {ev.Data}"); };
                        
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnityStageFileWatcher] Failed to run generator: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[UnityStageFileWatcher] Generator batch file not found at: {batPath}");
            }
        }
    }
}
#endif
