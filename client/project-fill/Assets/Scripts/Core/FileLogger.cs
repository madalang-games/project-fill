using System;
using System.IO;
using UnityEngine;

namespace Game.Core
{
    public static class FileLogger
    {
        private static StreamWriter _writer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
#if UNITY_EDITOR
            return;
#endif
            var dir = Path.Combine(Application.persistentDataPath, "logs");
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, $"game_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _writer = new StreamWriter(path, append: false) { AutoFlush = true };

            Application.logMessageReceived += OnLog;
            Application.quitting += OnQuit;

            _writer.WriteLine($"[FileLogger] start={DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine($"[FileLogger] path={path}");
            _writer.WriteLine($"[FileLogger] device={SystemInfo.deviceModel} os={SystemInfo.operatingSystem}");
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (_writer == null) return;

            var level = type switch
            {
                LogType.Error     => "ERROR",
                LogType.Warning   => "WARN",
                LogType.Exception => "EXCEPTION",
                LogType.Assert    => "ASSERT",
                _                 => "INFO",
            };

            _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{level}] {condition}");

            if (type is LogType.Error or LogType.Exception)
                _writer.WriteLine(stackTrace);
        }

        private static void OnQuit()
        {
            _writer?.WriteLine($"[FileLogger] quit={DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer?.Close();
            _writer = null;
        }
    }
}
