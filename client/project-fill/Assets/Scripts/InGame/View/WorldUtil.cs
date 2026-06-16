using TMPro;
using UnityEngine;

namespace Game.InGame.View
{
    // Tiny helpers for building runtime World Space board elements in code.
    public static class WorldUtil
    {
        public static Transform CreateGameObject(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Color color, Vector2 size, bool sliced = true, int sortingOrder = 0)
        {
            var trans = CreateGameObject(parent, name);
            var sr = trans.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color  = color;
            sr.sortingOrder = sortingOrder;
            if (sprite == null) return sr;

            if (sliced && sprite.border != Vector4.zero)
            {
                // 9-slice sprite (procedural TextureFactory or imported art with a border):
                // keep corners crisp and size via SpriteRenderer.size.
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size     = size;
            }
            else
            {
                // Borderless / non-sliced sprite: Simple draw mode renders at the sprite's native
                // pixels-per-unit size, ignoring `size`. Scale the transform so slot-in art fits the
                // computed lane/chip size regardless of its import settings.
                var b = sprite.bounds.size;
                if (b.x > 0.0001f && b.y > 0.0001f)
                    trans.localScale = new Vector3(size.x / b.x, size.y / b.y, trans.localScale.z);
            }
            return sr;
        }

        public static TextMeshPro CreateLabel(Transform parent, string name, string text, float size, Vector2 containerSize,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var trans = CreateGameObject(parent, name);
            var tmp = trans.gameObject.AddComponent<TextMeshPro>();
            tmp.text          = text;
            tmp.alignment     = align;
            tmp.color         = new Color(0.88f, 0.93f, 1f);
            tmp.enableWordWrapping = false;
            
            // TextMeshPro 3D font sizing scale comparison:
            // Standard font size in UI is around 30, which is extremely large in 3D.
            // A scale of 0.1f brings 30f to 3.0f, which aligns beautifully in world coordinates.
            tmp.fontSize = size * 0.1f;
            tmp.rectTransform.sizeDelta = containerSize;

            return tmp;
        }
    }
}
