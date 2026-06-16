#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Tools/UI Setup/Responsive — headless responsive QA for Final popup variants.
    /// Loads each prefab under Assets/Resources/Prefabs/UI, replays the CanvasScaler
    /// (ScaleWithScreenSize, ref 1080×1920, match 0.5) math per device resolution, rebuilds
    /// layout, then measures every visible Graphic's world rect against the screen box and a
    /// safe-area inset. Writes a markdown report next to the Unity project — NO prefab/yaml is
    /// written. Pure layout math: works in batchmode (-executeMethod), no GameView, no GPU.
    /// </summary>
    public static class UIResponsiveHarness
    {
        const string PopupDir    = "Assets/Resources/Prefabs/UI";
        const string ReportPath  = "../ui-responsive-report.md"; // relative to Application.dataPath → client/project-fill/
        const float  RefW        = 1080f;
        const float  RefH        = 1920f;
        const float  Match       = 0.5f;
        const float  Eps         = 0.5f; // canvas-unit tolerance

        // Device matrix. Safe insets are screen-space px approximations (real Screen.safeArea
        // is device-runtime-only); they bracket common notch / home-indicator regions.
        struct Device { public string Name; public float W, H, Top, Bottom, Left, Right; }
        static readonly Device[] Devices =
        {
            new Device { Name = "Reference 9:16",        W = 1080, H = 1920, Top = 0,   Bottom = 0,   Left = 0, Right = 0 },
            new Device { Name = "Android Tall 19.5:9",   W = 1080, H = 2340, Top = 90,  Bottom = 48,  Left = 0, Right = 0 },
            new Device { Name = "Narrow 9:21",           W = 1080, H = 2520, Top = 100, Bottom = 60,  Left = 0, Right = 0 },
            new Device { Name = "iPhone ProMax 9:19.5",  W = 1290, H = 2796, Top = 150, Bottom = 100, Left = 0, Right = 0 },
            new Device { Name = "iPad 3:4",              W = 1620, H = 2160, Top = 40,  Bottom = 40,  Left = 0, Right = 0 },
        };

        enum Severity { Offscreen, Overflow, SafeArea }

        struct Violation
        {
            public string Prefab;
            public string Device;
            public string Element;
            public Severity Sev;
            public string Detail;
        }

        [MenuItem("Tools/UI Setup/Responsive/Test All Final Popups", false, 160)]
        static void TestAllMenu() => Run();

        /// <summary>Batchmode entry: Unity.exe -batchmode -quit -executeMethod Game.Editor.UIResponsiveHarness.RunFromCLI</summary>
        public static void RunFromCLI() => Run();

        static void Run()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PopupDir });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[UIResponsive] No prefabs in {PopupDir}. Run 'Tools/UI Setup/1 - Create All Prefabs' first.");
                return;
            }

            var violations = new List<Violation>();
            int testedPrefabs = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;
                testedPrefabs++;
                foreach (var dev in Devices)
                    MeasureOne(asset, dev, violations);
            }

            WriteReport(violations, testedPrefabs, guids.Length);
        }

        static void MeasureOne(GameObject asset, Device dev, List<Violation> outList)
        {
            // CanvasScaler ScaleWithScreenSize replay → canvas-unit size for this screen.
            float logW = Mathf.Log(dev.W / RefW, 2f);
            float logH = Mathf.Log(dev.H / RefH, 2f);
            float scaleFactor = Mathf.Pow(2f, Mathf.Lerp(logW, logH, Match));
            float canvasW = dev.W / scaleFactor;
            float canvasH = dev.H / scaleFactor;

            // World-space canvas so RectTransform size is driven by us, not Screen.
            var canvasGo = new GameObject("__ResTestCanvas", typeof(Canvas), typeof(RectTransform));
            canvasGo.hideFlags = HideFlags.HideAndDontSave;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rootRt = canvasGo.GetComponent<RectTransform>();
            rootRt.position = Vector3.zero;
            rootRt.rotation = Quaternion.identity;
            rootRt.localScale = Vector3.one;
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(canvasW, canvasH);

            GameObject inst = null;
            try
            {
                inst = (GameObject)UnityEngine.Object.Instantiate(asset, canvasGo.transform);
                var instRt = inst.GetComponent<RectTransform>();
                if (instRt != null)
                {
                    instRt.anchorMin = Vector2.zero;
                    instRt.anchorMax = Vector2.one;
                    instRt.offsetMin = Vector2.zero;
                    instRt.offsetMax = Vector2.zero;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);

                // Screen box from root world corners. [0]=BL [1]=TL [2]=TR [3]=BR
                var rc = new Vector3[4];
                rootRt.GetWorldCorners(rc);
                float sMinX = rc[0].x, sMaxX = rc[2].x, sMinY = rc[0].y, sMaxY = rc[2].y;

                // Safe insets converted to canvas units.
                float inTop    = dev.Top    / scaleFactor;
                float inBottom = dev.Bottom / scaleFactor;
                float inLeft   = dev.Left   / scaleFactor;
                float inRight  = dev.Right  / scaleFactor;

                var ec = new Vector3[4];
                foreach (var g in inst.GetComponentsInChildren<Graphic>(false))
                {
                    string n = g.gameObject.name;
                    if (n == "Shadow" || n == "Border") continue; // intentional ±8px offset layers

                    var rt = g.rectTransform;
                    rt.GetWorldCorners(ec);
                    float eMinX = ec[0].x, eMaxX = ec[2].x, eMinY = ec[0].y, eMaxY = ec[2].y;

                    bool fullyOut = eMaxX <= sMinX + Eps || eMinX >= sMaxX - Eps
                                 || eMaxY <= sMinY + Eps || eMinY >= sMaxY - Eps;
                    bool partOut  = eMinX < sMinX - Eps || eMaxX > sMaxX + Eps
                                 || eMinY < sMinY - Eps || eMaxY > sMaxY + Eps;

                    if (fullyOut)
                        outList.Add(Mk(asset, dev, inst, g, Severity.Offscreen, "fully outside screen box"));
                    else if (partOut)
                        outList.Add(Mk(asset, dev, inst, g, Severity.Overflow, EdgeDetail(eMinX, eMaxX, eMinY, eMaxY, sMinX, sMaxX, sMinY, sMaxY)));
                    else if (eMinX < sMinX + inLeft - Eps || eMaxX > sMaxX - inRight + Eps
                          || eMinY < sMinY + inBottom - Eps || eMaxY > sMaxY - inTop + Eps)
                        outList.Add(Mk(asset, dev, inst, g, Severity.SafeArea, "inside screen but crosses safe-area inset"));
                }
            }
            finally
            {
                if (inst != null) UnityEngine.Object.DestroyImmediate(inst);
                UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }

        static string EdgeDetail(float eMinX, float eMaxX, float eMinY, float eMaxY,
                                 float sMinX, float sMaxX, float sMinY, float sMaxY)
        {
            var sb = new StringBuilder("overflow ");
            if (eMinY < sMinY - Eps) sb.Append($"bottom {(sMinY - eMinY):0}u ");
            if (eMaxY > sMaxY + Eps) sb.Append($"top {(eMaxY - sMaxY):0}u ");
            if (eMinX < sMinX - Eps) sb.Append($"left {(sMinX - eMinX):0}u ");
            if (eMaxX > sMaxX + Eps) sb.Append($"right {(eMaxX - sMaxX):0}u ");
            return sb.ToString().TrimEnd();
        }

        static Violation Mk(GameObject asset, Device dev, GameObject root, Graphic g, Severity sev, string detail)
            => new Violation { Prefab = asset.name, Device = dev.Name, Element = PathOf(root.transform, g.transform), Sev = sev, Detail = detail };

        static string PathOf(Transform root, Transform t)
        {
            var stack = new List<string>();
            while (t != null && t != root) { stack.Add(t.name); t = t.parent; }
            stack.Reverse();
            return string.Join("/", stack);
        }

        static void WriteReport(List<Violation> v, int tested, int found)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# UI Responsive Report");
            sb.AppendLine();
            sb.AppendLine($"- Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"- Prefabs tested: {tested}/{found} (`{PopupDir}`)");
            sb.AppendLine($"- Devices: {Devices.Length} | Reference {RefW:0}×{RefH:0}, match {Match}");
            sb.AppendLine($"- Units: `u` = canvas units (= reference px at the reference resolution)");
            sb.AppendLine();

            int off = v.FindAll(x => x.Sev == Severity.Offscreen).Count;
            int ovf = v.FindAll(x => x.Sev == Severity.Overflow).Count;
            int saf = v.FindAll(x => x.Sev == Severity.SafeArea).Count;
            sb.AppendLine($"**Totals — Offscreen: {off} · Overflow: {ovf} · SafeArea: {saf}**");
            sb.AppendLine();
            sb.AppendLine("> Offscreen/Overflow = error (element leaves the screen box). SafeArea = warn (inside screen, crosses notch/home-indicator inset; verify against real device).");
            sb.AppendLine();

            if (v.Count == 0)
            {
                sb.AppendLine("✅ No violations.");
            }
            else
            {
                sb.AppendLine("| prefab | device | element | severity | detail |");
                sb.AppendLine("|--------|--------|---------|----------|--------|");
                v.Sort((a, b) =>
                {
                    int c = string.CompareOrdinal(a.Prefab, b.Prefab); if (c != 0) return c;
                    c = a.Sev.CompareTo(b.Sev); if (c != 0) return c;
                    return string.CompareOrdinal(a.Device, b.Device);
                });
                foreach (var x in v)
                    sb.AppendLine($"| {x.Prefab} | {x.Device} | `{x.Element}` | {Tag(x.Sev)} | {x.Detail} |");
            }

            var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, ReportPath));
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[UIResponsive] {v.Count} violation(s) across {tested} prefab(s) → {outPath}");
        }

        static string Tag(Severity s) => s switch
        {
            Severity.Offscreen => "🔴 OFFSCREEN",
            Severity.Overflow  => "🟠 OVERFLOW",
            _                  => "🟡 SAFEAREA",
        };
    }
}
#endif
