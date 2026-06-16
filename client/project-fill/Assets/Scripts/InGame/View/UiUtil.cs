using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Tiny helpers for building the runtime board UI in code.
    public static class UiUtil
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = true)
        {
            var rt  = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color  = color;
            if (sprite != null && sliced && sprite.border != Vector4.zero)
                img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt  = Rect(parent, name);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = size;
            tmp.alignment     = align;
            tmp.color         = new Color(0.88f, 0.93f, 1f);
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        public static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static void Anchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
