#if UNITY_EDITOR
using Game.OutGame.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Editor
{
    public static class StageNodeEditorSetup
    {
        private const string PrefabRoot = "Assets/Resources/Prefabs/UI";

        static void CreateStageNodePrefab()
        {
            var root = new GameObject("StageNodeView");
            root.AddComponent<RectTransform>();
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120, 120);

            // Base circle
            var baseImg = root.AddComponent<Image>();
            baseImg.color = UIEditorColors.UI_BG_MID;

            root.AddComponent<Button>().targetGraphic = baseImg;
            root.AddComponent<Game.Core.UI.UIButtonAnimator>();
            root.AddComponent<StageNodeView>();

            // Stage label
            var label = Child(root, "StageLabel");
            Fixed(label, new Vector2(0, 30), new Vector2(100, 40));
            var labelTxt = label.AddComponent<TextMeshProUGUI>();
            labelTxt.text = "1"; labelTxt.fontSize = 22; labelTxt.color = UIEditorColors.UI_TEXT;
            labelTxt.alignment = TextAlignmentOptions.Center;
            
            var style = label.AddComponent<Game.Core.UI.UITextStyle>();
            style.ApplyStyle();

            // Cleared badge (single marker at bottom; star rating removed)
            var clearedBadge = Child(root, "ClearedBadge");
            Fixed(clearedBadge, new Vector2(0, -40), new Vector2(24, 24));
            clearedBadge.AddComponent<Image>().color = UIEditorColors.UI_SUCCESS;
            clearedBadge.SetActive(false);

            // Lock overlay
            var lockOverlay = Child(root, "LockOverlay");
            Fixed(lockOverlay, Vector2.zero, new Vector2(120, 120));
            lockOverlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            lockOverlay.SetActive(false);

            // Pulse ring (UIGlowFlow shader)
            var ring = Child(root, "PulseRing");
            Fixed(ring, Vector2.zero, new Vector2(138, 138));
            var ringImg = ring.AddComponent<Image>();
            ringImg.material = LoadOrCreateGlowFlowMaterial();
            ring.transform.SetAsFirstSibling();
            ring.SetActive(false);

            // Wire StageNodeView
            var so = new SerializedObject(root.GetComponent<StageNodeView>());
            so.FindProperty("_stageLabel").objectReferenceValue = labelTxt;
            so.FindProperty("_button").objectReferenceValue     = root.GetComponent<Button>();
            so.FindProperty("_lockOverlay").objectReferenceValue = lockOverlay;
            so.FindProperty("_pulseRing").objectReferenceValue  = ring;
            so.FindProperty("_clearedBadge").objectReferenceValue = clearedBadge;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabRoot + "/StageNodeView.prefab");
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log("[UIEditorSetup] StageNodeView prefab created.");
        }

        static Material LoadOrCreateGlowFlowMaterial()
        {
            const string matPath = "Assets/Resources/Prefabs/UI/UIGlowFlowMaterial.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) return mat;

            var shader = Shader.Find("UI/GlowFlow");
            if (shader == null)
            {
                Debug.LogError("[StageNodeEditorSetup] Shader 'UI/GlowFlow' not found. Import UIGlowFlow.shader first.");
                return null;
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static GameObject Child(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name); go.AddComponent<RectTransform>();
            go.transform.SetParent(parent.transform, false); return go;
        }

        static void Fixed(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

    }

    internal static class UIEditorColors
    {
        public static Color UI_BG_MID => Hex("1A2F45");
        public static Color UI_CTA    => Hex("E8A020");
        public static Color UI_TEXT   => Hex("F0EAD6");
        public static Color UI_SUCCESS => Hex("3DBE6E");
        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }
    }
}
#endif
