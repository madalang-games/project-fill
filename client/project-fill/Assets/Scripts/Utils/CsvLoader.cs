using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Game.Utils
{
    public static class CsvLoader
    {
        static string PatchDir => Path.Combine(Application.persistentDataPath, "data_patch");
        static string PatchTmpDir => Path.Combine(Application.persistentDataPath, "data_patch_tmp");
        static string PatchHashPath => Path.Combine(PatchDir, "meta_hash.txt");

        public static T[] Load<T>(string resourcePath) where T : new()
        {
            var patchFile = Path.Combine(PatchDir, resourcePath + ".csv");
            if (File.Exists(patchFile))
                return Parse<T>(File.ReadAllText(patchFile, Encoding.UTF8));

            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[CsvLoader] Missing resource: {resourcePath}");
                return Array.Empty<T>();
            }
            return Parse<T>(asset.text);
        }

        public static void WritePatchFile(string resourcePath, string csvContent)
        {
            var filePath = Path.Combine(PatchDir, resourcePath + ".csv");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, csvContent, Encoding.UTF8);
        }

        public static string GetPatchedMetaHash()
        {
            return File.Exists(PatchHashPath) ? File.ReadAllText(PatchHashPath).Trim() : "";
        }

        public static void SavePatchedMetaHash(string metaHash)
        {
            Directory.CreateDirectory(PatchDir);
            File.WriteAllText(PatchHashPath, metaHash, Encoding.UTF8);
        }

        public static void ClearPatch()
        {
            if (Directory.Exists(PatchDir))
                Directory.Delete(PatchDir, true);
        }

        public static void ApplyPatchAtomic(System.Collections.Generic.Dictionary<string, string> files, string metaHash)
        {
            if (Directory.Exists(PatchTmpDir)) Directory.Delete(PatchTmpDir, true);

            foreach (var kv in files)
            {
                var filePath = Path.Combine(PatchTmpDir, kv.Key + ".csv");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, kv.Value, Encoding.UTF8);
            }

            if (Directory.Exists(PatchDir)) Directory.Delete(PatchDir, true);
            Directory.Move(PatchTmpDir, PatchDir);
            File.WriteAllText(PatchHashPath, metaHash, Encoding.UTF8);
        }

        static T[] Parse<T>(string text) where T : new()
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return Array.Empty<T>();

            var headers = ParseCsvLine(lines[0]);
            var type    = typeof(T);
            var fields  = new FieldInfo[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                fields[i] = type.GetField(headers[i]);

            var results = new List<T>(lines.Length - 1);
            for (int r = 1; r < lines.Length; r++)
            {
                var values = ParseCsvLine(lines[r]);
                var obj    = new T();
                for (int c = 0; c < fields.Length; c++)
                {
                    if (fields[c] == null || c >= values.Length) continue;
                    try
                    {
                        fields[c].SetValue(obj, ConvertValue(values[c], fields[c].FieldType));
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException($"CSV parse failed: {type.Name}.{fields[c].Name} at row {r + 1}, value '{values[c]}'", ex);
                    }
                }
                results.Add(obj);
            }
            return results.ToArray();
        }

        static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = string.Empty;
            bool inQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else inQuote = !inQuote;
                }
                else if (c == ',' && !inQuote)
                {
                    fields.Add(current.Trim());
                    current = string.Empty;
                }
                else current += c;
            }

            fields.Add(current.Trim());
            return fields.ToArray();
        }

        static object ConvertValue(string raw, Type type)
        {
            if (type == typeof(string)) return raw ?? "";
            if (string.IsNullOrEmpty(raw)) return Activator.CreateInstance(type);
            if (type == typeof(sbyte))  return sbyte.Parse(raw);
            if (type == typeof(byte))   return byte.Parse(raw);
            if (type == typeof(short))  return short.Parse(raw);
            if (type == typeof(ushort)) return ushort.Parse(raw);
            if (type == typeof(int))    return int.Parse(raw);
            if (type == typeof(uint))   return uint.Parse(raw);
            if (type == typeof(long))   return long.Parse(raw);
            if (type == typeof(ulong))  return ulong.Parse(raw);
            if (type == typeof(float))  return float.Parse(raw, CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(raw, CultureInfo.InvariantCulture);
            if (type == typeof(bool))   return bool.Parse(raw);
            if (type.IsEnum)            return Enum.Parse(type, raw, ignoreCase: true);
            return raw;
        }
    }
}
