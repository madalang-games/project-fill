using Game.InGame.View;
using ProjectFill.Contracts.Cosmetic;
using ProjectFill.Contracts.GameTypes;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Builds a procedural UI representation of a cosmetic skin inside a CosmeticItemCell / preview popup,
    /// composed from the SAME <see cref="TextureFactory"/> sprites + <see cref="BoardTheme"/> tokens the
    /// skin uses in-game — i.e. the world-space SpriteRenderer look recreated with UI Images. No
    /// RenderTexture, no prefab edit: layers are added at runtime under the existing "Preview" Image and
    /// anchored by FRACTION of the preview rect, so previews are resolution-independent and the chip
    /// preview stays scaled-down (central region only) regardless of cell size.
    /// </summary>
    public static class CosmeticPreview
    {
        private const string RootName = "SkinPreview";

        // Representative body colours for the chip/lane preview (gameplay uses the live signal colour; here
        // we only need a couple of contrasting samples to show how the skin restyles outline/finish/body).
        private static readonly Color SampleA = new(0.30f, 0.70f, 0.95f); // cyan
        private static readonly Color SampleB = new(1.00f, 0.62f, 0.24f); // amber
        private static readonly Color SampleC = new(0.55f, 0.85f, 0.45f); // green

        public static void Build(Image preview, CosmeticItemDto item)
        {
            if (preview == null || item == null) return;
            var parent = preview.rectTransform;

            var existing = parent.Find(RootName);
            if (existing != null) Object.Destroy(existing.gameObject);

            // Neutral dark backdrop on the Preview image itself; the composition sits on top.
            preview.sprite = null;
            preview.color  = new Color(0.04f, 0.05f, 0.10f, 1f);
            preview.enabled = true;

            var root = NewRect(RootName, parent, Vector2.zero, Vector2.one);
            root.transform.SetAsFirstSibling(); // behind SelectedHighlight + LockOverlay (added before them)

            switch ((CosmeticCategory)item.Category)
            {
                case CosmeticCategory.Board: BuildBoard(root.transform, item.CosmeticId); break;
                case CosmeticCategory.Lane:  BuildLane(root.transform, item.CosmeticId); break;
                case CosmeticCategory.Chip:  BuildChip(root.transform, item.CosmeticId); break;
            }
        }

        // ── Per-category composition ─────────────────────────────────────────

        private static void BuildBoard(Transform rt, string id)
        {
            var t = BoardTheme.Resolve(id, "", "");
            var a = new Vector2(0.06f, 0.10f); var b = new Vector2(0.94f, 0.90f);
            AddImg(rt, "Surface", TextureFactory.RoundedRect(), t.Surface, a, b, sliced: true);
            AddImg(rt, "Edge", TextureFactory.RoundedOutline(), t.SurfaceBorder, a, b, sliced: true);

            // Ambient accent glints (more on premium boards) — static stand-ins for the in-game motes/sparks.
            var ac = t.Accent;
            if (t.BoardMoteCount > 0 || t.SurfaceEdgeSparkCount > 0)
            {
                AddImg(rt, "Glint0", TextureFactory.Glow(), new Color(ac.r, ac.g, ac.b, 0.5f), new(0.14f, 0.60f), new(0.27f, 0.78f), false);
                AddImg(rt, "Glint1", TextureFactory.Glow(), new Color(ac.r, ac.g, ac.b, 0.4f), new(0.70f, 0.18f), new(0.83f, 0.34f), false);
            }

            // A short row of mini chips on the surface (shows the board's chip-outline tone).
            float[] x0 = { 0.22f, 0.42f, 0.62f };
            Color[] cols = { SampleA, SampleB, SampleC };
            for (int i = 0; i < 3; i++)
            {
                var lo = new Vector2(x0[i], 0.34f); var hi = new Vector2(x0[i] + 0.16f, 0.60f);
                AddImg(rt, $"Chip{i}", TextureFactory.RoundedRect(), cols[i], lo, hi, sliced: true);
                AddImg(rt, $"ChipRim{i}", TextureFactory.RoundedRimSolid(), t.ChipOutline, lo, hi, sliced: true);
            }
        }

        private static void BuildLane(Transform rt, string id)
        {
            var t = BoardTheme.Resolve("", "", id);
            var bl = t.LaneBacklight;
            AddImg(rt, "Backlight", TextureFactory.Glow(), new Color(bl.r, bl.g, bl.b, Mathf.Max(bl.a, 0.25f)), new(0.26f, 0.05f), new(0.74f, 0.95f), false);

            var a = new Vector2(0.34f, 0.08f); var b = new Vector2(0.66f, 0.92f);
            AddImg(rt, "Body", TextureFactory.RoundedRect(), t.LaneBody, a, b, sliced: true);
            AddImg(rt, "Border", TextureFactory.RoundedOutline(), t.LaneBorder, a, b, sliced: true);

            // Two stacked mini chips inside the column.
            AddImg(rt, "ChipA", TextureFactory.RoundedRect(), SampleA, new(0.38f, 0.50f), new(0.62f, 0.70f), sliced: true);
            AddImg(rt, "ChipB", TextureFactory.RoundedRect(), SampleB, new(0.38f, 0.26f), new(0.62f, 0.46f), sliced: true);
        }

        private static void BuildChip(Transform rt, string id)
        {
            var t = BoardTheme.Resolve("", id, "");
            var ac = t.Accent;
            // Chip occupies only the central region so the preview reads as a small token (cell stays compact).
            var lo = new Vector2(0.30f, 0.30f); var hi = new Vector2(0.70f, 0.70f);

            AddImg(rt, "Glow", TextureFactory.Glow(), new Color(ac.r, ac.g, ac.b, Mathf.Clamp01(t.ChipGlowAlpha * 2.2f)), new(0.16f, 0.16f), new(0.84f, 0.84f), false);

            var body = SampleA; body.a = t.ChipFillAlpha;
            AddImg(rt, "Fill", TextureFactory.RoundedRect(), body, lo, hi, sliced: true);

            var finish = TextureFactory.ChipFinishOverlay(t.ChipFinish);
            if (finish != null) AddImg(rt, "Finish", finish, Color.white, lo, hi, sliced: false);

            AddImg(rt, "Outline", TextureFactory.RoundedRimSolid(), t.ChipOutline, lo, hi, sliced: true);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return go;
        }

        private static Image AddImg(Transform parent, string name, Sprite spr, Color col, Vector2 aMin, Vector2 aMax, bool sliced)
        {
            var go = NewRect(name, parent, aMin, aMax);
            var img = go.AddComponent<Image>();
            img.sprite = spr;
            img.color = col;
            img.raycastTarget = false;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            return img;
        }
    }
}
