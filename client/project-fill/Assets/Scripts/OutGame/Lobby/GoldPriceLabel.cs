using TMPro;
using UnityEngine;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Positions the gold-coin icon (the hidden `GoldIcon` child added by UIEditorSetup.AttachGoldIcon)
    /// flush to the left of a price StateText, or hides it. The StateText itself is never altered — it
    /// keeps its fixed width + center alignment + autosizing, so both numeric prices and long word
    /// states (e.g. avatar reward-only label) render centered without overflowing.
    ///
    /// We do NOT use a ContentSizeFitter: TMP autosizing makes preferredWidth unreliable, so a fitter
    /// never tightly hugs the content. Instead, for a gold price we measure the actually-rendered glyph
    /// width (after autosize) via ForceMeshUpdate + textBounds, then place the center-anchored icon just
    /// left of the centered number so it sits flush regardless of digit count.
    /// </summary>
    public static class GoldPriceLabel
    {
        // Gap between the coin's right edge and the number's left glyph.
        private const float Spacing = 16f;

        public static void Set(TMP_Text state, bool isGoldPrice)
        {
            if (state == null) return;
            var icon = state.transform.Find("GoldIcon") as RectTransform;
            if (icon == null) return;

            icon.gameObject.SetActive(isGoldPrice);
            if (!isGoldPrice) return;

            // Center-aligned number stays at the rect center; place the coin left of its glyphs + spacing.
            state.ForceMeshUpdate();
            float textW = state.textBounds.size.x;
            icon.anchoredPosition = new Vector2(-(textW * 0.5f + icon.sizeDelta.x * 0.5f + Spacing), 0f);
        }
    }
}
