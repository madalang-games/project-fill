#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Game.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.UIColorPalette;

namespace Game.Editor
{
    // ── Declarative UI spec — PROTOTYPE ────────────────────────────────────────────
    // Proves the data-driven path for UIEditorSetup: a popup is described by JSON and
    // built by an interpreter that reuses the existing imperative helpers (Panel/Btn/TMP/
    // CloseBtnAt/Save). One menu builds every spec, so adding a popup = adding a JSON file
    // (no new MenuItem, no CreateAllPrefabs edit, no priority-index bookkeeping).
    //
    // Scope: flat "panel > children" layouts (JsonUtility-friendly — no recursion). Nested
    // containers / per-popup custom logic (e.g. ConfirmDialog's reward panel) stay in hand
    // builders for now; the next step is a recursive node tree + fileID-preserving reuse
    // (Transform.Find diff) so Final-variant overrides survive a Base rebuild.

    [Serializable] public class UISpecBackdrop { public string name = "Backdrop"; public string color = "DIM"; }

    [Serializable] public class UISpecElement
    {
        public string type;                 // ribbonTitle | text | button | closeButton
        public string name;
        public string text;
        public string stringId;             // localization key (e.g. "common.btn_confirm"); empty = none
        public string color = "UI_TEXT";    // UIColorPalette member name, or raw hex (one-off)
        public int    fontSize = 20;
        public string category = "Normal";  // Header | Button | Normal
        public float[] pos;                 // [x,y]
        public float[] size;                // [w,h]
        public float[] rect;                // [x,y,w,h] (text)
    }

    [Serializable] public class UISpecBinding
    {
        public string field;                // serialized field on viewComponent
        public string path;                 // transform path under root
        public string component;            // TMP_Text | Button | Image | RectTransform | (empty = GameObject)
    }

    [Serializable] public class UISpec
    {
        public string prefabName;
        public string viewComponent;        // optional MonoBehaviour type name
        public UISpecBackdrop backdrop;     // optional
        public string panelName = "Panel";
        public string panelColor = "UI_BG_MID";
        public float[] panelSize;           // [w,h]
        public UISpecElement[] elements;
        public UISpecBinding[] bindings;
    }

    public static partial class UIEditorSetup
    {
        private const string SpecDir = "Assets/Scripts/Editor/UISpecs";

        [MenuItem("Tools/UI Setup/Specs/Build All From Specs", false, 150)]
        static void BuildAllFromSpecs()
        {
            EnsureDirs();
            var specs = LoadSpecs();
            foreach (var s in specs) BuildFromSpec(s);
            AssetDatabase.Refresh();
            Debug.Log($"[UISpec] Built {specs.Count} prefab(s) from {SpecDir}");
        }

        static List<UISpec> LoadSpecs()
        {
            var list = new List<UISpec>();
            if (!Directory.Exists(SpecDir)) { Debug.LogWarning($"[UISpec] No spec dir: {SpecDir}"); return list; }
            foreach (var f in Directory.GetFiles(SpecDir, "*.json"))
            {
                if (Path.GetFileName(f).StartsWith("_")) continue;
                try { list.Add(JsonUtility.FromJson<UISpec>(File.ReadAllText(f))); }
                catch (Exception e) { Debug.LogError($"[UISpec] Failed to parse {f}: {e.Message}"); }
            }
            return list;
        }

        static void BuildFromSpec(UISpec s)
        {
            if (string.IsNullOrEmpty(s.prefabName)) { Debug.LogError("[UISpec] spec missing prefabName"); return; }

            var root = FullScreen(s.prefabName);
            var viewType = string.IsNullOrEmpty(s.viewComponent) ? null : FindType(s.viewComponent);
            if (viewType != null) root.AddComponent(viewType);
            Comp<UIPanelAppear>(root);   // popup mandatory (UIManager.ShowPopup contract)
            Comp<CanvasGroup>(root);

            if (s.backdrop != null)
            {
                var bd = Btn(root, s.backdrop.name, Vector2.zero, new Vector2(1080, 1920), ResolveColor(s.backdrop.color), "");
                Stretch(bd);
            }

            var panel = Panel(root, s.panelName, ToV2(s.panelSize, new Vector2(900, 600)), ResolveColor(s.panelColor));

            foreach (var e in s.elements ?? Array.Empty<UISpecElement>())
            {
                switch (e.type)
                {
                    case "ribbonTitle":
                        RibbonTitle(panel, e.name, e.text, NullIfEmpty(e.stringId));
                        break;
                    case "text":
                        TMP(panel, e.name, ToRect(e.rect), e.fontSize, ResolveColor(e.color), e.text, NullIfEmpty(e.stringId), ParseCategory(e.category));
                        break;
                    case "button":
                        Btn(panel, e.name, ToV2(e.pos, Vector2.zero), ToV2(e.size, new Vector2(320, 90)), ResolveColor(e.color), e.text, NullIfEmpty(e.stringId));
                        break;
                    case "closeButton":
                        CloseBtnAt(panel, ToV2(e.pos, new Vector2(395, 295)));
                        break;
                    default:
                        Debug.LogWarning($"[UISpec] {s.prefabName}: unknown element type '{e.type}'");
                        break;
                }
            }

            if (viewType != null && s.bindings != null && s.bindings.Length > 0)
                ApplyBindings(root, viewType, s.bindings);

            Save(root, s.prefabName);
            Debug.Log($"[UISpec] Built {s.prefabName}");
        }

        static void ApplyBindings(GameObject root, Type viewType, UISpecBinding[] bindings)
        {
            var so = new SerializedObject(root.GetComponent(viewType));
            foreach (var b in bindings)
            {
                var prop = so.FindProperty(b.field);
                if (prop == null) { Debug.LogWarning($"[UISpec] field '{b.field}' not found on {viewType.Name}"); continue; }
                var tr = root.transform.Find(b.path);
                if (tr == null) { Debug.LogWarning($"[UISpec] path '{b.path}' not found"); continue; }
                prop.objectReferenceValue = ResolveBindingTarget(tr.gameObject, b.component);
            }
            so.ApplyModifiedProperties();
        }

        static UnityEngine.Object ResolveBindingTarget(GameObject go, string component)
        {
            switch (component)
            {
                case "TMP_Text":      return go.GetComponent<TMP_Text>();
                case "Button":        return go.GetComponent<Button>();
                case "Image":         return go.GetComponent<Image>();
                case "RectTransform": return go.GetComponent<RectTransform>();
                default:              return go;
            }
        }

        // ── resolve helpers ────────────────────────────────────────────────────────
        static Color ResolveColor(string s)
        {
            if (string.IsNullOrEmpty(s)) return UI_TEXT;
            var prop = typeof(UIColorPalette).GetProperty(s, BindingFlags.Public | BindingFlags.Static);
            if (prop != null) return (Color)prop.GetValue(null);
            return Hex(s); // one-off hex literal (e.g. "24172E")
        }

        static TextCategory ParseCategory(string s)
            => Enum.TryParse<TextCategory>(s, out var c) ? c : TextCategory.Normal;

        static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types) if (t.Name == name) return t;
            }
            Debug.LogWarning($"[UISpec] component type '{name}' not found");
            return null;
        }

        static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
        static Vector2 ToV2(float[] a, Vector2 fallback) => (a != null && a.Length >= 2) ? new Vector2(a[0], a[1]) : fallback;
        static Rect ToRect(float[] a) => (a != null && a.Length >= 4) ? Center(a[0], a[1], a[2], a[3]) : Center(0, 0, 800, 80);
    }
}
#endif
