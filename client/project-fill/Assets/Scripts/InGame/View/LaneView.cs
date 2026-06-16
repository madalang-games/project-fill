using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // One slot lane: card-rack body, four sockets, stacked chips, and gimmick decorations
    // (lock overlay, blind marker, relay pending badge). Owns its tap button.
    public class LaneView : MonoBehaviour
    {
        private static readonly Color BorderNormal  = new(0.30f, 0.34f, 0.46f, 0.9f);
        private static readonly Color BorderSelect  = new(0.45f, 0.75f, 1.00f, 1f);
        private static readonly Color BorderValid   = new(0.35f, 0.95f, 0.55f, 1f);
        private static readonly Color BorderInvalid = new(1.00f, 0.32f, 0.32f, 1f);
        private static readonly Color BorderBlind   = new(0.30f, 0.85f, 0.95f, 0.95f);
        private static readonly Color BorderPending = new(1.00f, 0.72f, 0.20f, 1f);
        private static readonly Color BodyColor     = new(0.13f, 0.14f, 0.21f, 1f);
        private static readonly Color SocketColor   = new(0.18f, 0.19f, 0.28f, 1f);

        private RectTransform _rt;
        private Image  _border;
        private ChipView[] _chips;
        private RectTransform[] _slots;

        private GameObject _lockOverlay;
        private GameObject _blindMarker;
        private GameObject _pendingBadge;
        private TextMeshProUGUI _pendingLabel;

        private SlotLane _lane;
        private bool _selected, _validTarget, _flashing;
        private Vector2 _homePos;

        public int Index { get; private set; }
        public event Action<int> OnTapped;

        public static LaneView Build(Transform parent, int index, SpriteSet sprites, SlotLane lane)
        {
            var rt = UiUtil.Rect(parent, $"Lane_{index}");
            var lv = rt.gameObject.AddComponent<LaneView>();
            lv.Index = index;
            lv.Construct(sprites, lane);
            return lv;
        }

        private void Construct(SpriteSet s, SlotLane lane)
        {
            _rt   = (RectTransform)transform;
            _lane = lane;

            // transparent raycast target + button on the whole lane
            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var btn = gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnTapped?.Invoke(Index));

            var body = UiUtil.Image(transform, "Body", s.LaneSlot, BodyColor);
            UiUtil.Stretch(body.rectTransform, 2f);

            const float vpad = 0.035f, gap = 0.014f;
            float usable = 1f - 2f * vpad;
            float slotH  = usable / SlotLane.Capacity;

            // sockets (behind chips)
            for (int sIdx = 0; sIdx < SlotLane.Capacity; sIdx++)
            {
                var sock = UiUtil.Image(transform, $"Socket_{sIdx}", s.ChipOutline, SocketColor);
                PlaceSlot(sock.rectTransform, sIdx, vpad, slotH, gap);
            }

            // chip slots + persistent chip views
            _slots = new RectTransform[SlotLane.Capacity];
            _chips = new ChipView[SlotLane.Capacity];
            for (int sIdx = 0; sIdx < SlotLane.Capacity; sIdx++)
            {
                var slot = UiUtil.Rect(transform, $"Slot_{sIdx}");
                PlaceSlot(slot, sIdx, vpad, slotH, gap);
                _slots[sIdx] = slot;
                _chips[sIdx] = ChipView.Build(slot, s);
                _chips[sIdx].Hide();
            }

            _border = UiUtil.Image(transform, "Border", s.ChipOutline, BorderNormal);
            UiUtil.Stretch(_border.rectTransform);

            BuildLockOverlay(s, lane);
            BuildBlindMarker();
            BuildPendingBadge();

            _homePos = _rt.anchoredPosition;
        }

        private static void PlaceSlot(RectTransform rt, int slotIndex, float vpad, float slotH, float gap)
        {
            float yMin = vpad + slotIndex * slotH + gap * 0.5f;
            float yMax = vpad + (slotIndex + 1) * slotH - gap * 0.5f;
            UiUtil.Anchors(rt, 0.13f, yMin, 0.87f, yMax);
        }

        private void BuildLockOverlay(SpriteSet s, SlotLane lane)
        {
            _lockOverlay = UiUtil.Rect(transform, "Lock").gameObject;
            UiUtil.Stretch((RectTransform)_lockOverlay.transform);

            var seal = UiUtil.Image(_lockOverlay.transform, "Seal", s.LaneSlot, new Color(0.06f, 0.07f, 0.11f, 0.86f));
            UiUtil.Stretch(seal.rectTransform, 2f);

            // padlock: ring shackle + rounded body, centred upper area
            var shackle = UiUtil.Image(_lockOverlay.transform, "Shackle", s.Ring, new Color(0.85f, 0.88f, 1f));
            UiUtil.Anchors(shackle.rectTransform, 0.40f, 0.60f, 0.60f, 0.74f);
            var lockBody = UiUtil.Image(_lockOverlay.transform, "Body", s.Chip, new Color(0.85f, 0.88f, 1f));
            UiUtil.Anchors(lockBody.rectTransform, 0.36f, 0.46f, 0.64f, 0.62f);

            // unlock-condition badge (the type color that frees this lane)
            var badge = UiUtil.Image(_lockOverlay.transform, "Badge", s.Chip, lane.UnlockType.ToColor());
            UiUtil.Anchors(badge.rectTransform, 0.34f, 0.28f, 0.66f, 0.42f);
            var bl = UiUtil.Label(badge.transform, "BadgeGlyph", lane.UnlockType.ToLabel(), 22f);
            UiUtil.Stretch(bl.rectTransform);
            bl.color = new Color(0, 0, 0, 0.6f);

            _lockOverlay.SetActive(lane.Kind == LaneKind.Locked);
        }

        private void BuildBlindMarker()
        {
            _blindMarker = UiUtil.Rect(transform, "BlindMark").gameObject;
            UiUtil.Anchors((RectTransform)_blindMarker.transform, 0.30f, 0.86f, 0.70f, 0.99f);
            var q = UiUtil.Label(_blindMarker.transform, "Q", "?", 30f);
            UiUtil.Stretch(q.rectTransform);
            q.color    = BorderBlind;
            q.fontStyle = FontStyles.Bold;
            _blindMarker.SetActive(_lane.Kind == LaneKind.Blind);
        }

        private void BuildPendingBadge()
        {
            _pendingBadge = UiUtil.Rect(transform, "Pending").gameObject;
            UiUtil.Anchors((RectTransform)_pendingBadge.transform, 0.06f, 0.88f, 0.94f, 1.0f);
            var bg = UiUtil.Image(_pendingBadge.transform, "Bg", null, new Color(0.20f, 0.14f, 0.02f, 0.9f));
            UiUtil.Stretch(bg.rectTransform);
            _pendingLabel = UiUtil.Label(_pendingBadge.transform, "Lbl", "WAIT", 20f);
            UiUtil.Stretch(_pendingLabel.rectTransform);
            _pendingLabel.color = BorderPending;
            _pendingBadge.SetActive(false);
        }

        // ── Refresh from model ───────────────────────────────────────────────

        public void Refresh(SlotLane lane, int pendingNumber = 0)
        {
            _lane = lane;
            for (int s = 0; s < SlotLane.Capacity; s++)
            {
                if (s < lane.Count)
                {
                    bool revealed = lane.Kind != LaneKind.Blind || s == lane.Count - 1;
                    _chips[s].SetChip(lane.Chips[s], revealed);
                }
                else _chips[s].Hide();
            }

            _lockOverlay.SetActive(lane.Locked);
            _blindMarker.SetActive(lane.Kind == LaneKind.Blind);
            _pendingBadge.SetActive(lane.Pending);
            if (lane.Pending && pendingNumber > 0) _pendingLabel.text = $"WAIT #{pendingNumber}";
        }

        public Vector3 SlotWorldPos(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, SlotLane.Capacity - 1);
            return _slots[slotIndex].position;
        }

        public Vector2 ChipPixelSize() => _slots[0].rect.size;

        // ── Highlight state ──────────────────────────────────────────────────

        public void SetSelected(bool on)
        {
            _selected = on;
            int top = _lane.Count - 1;
            if (top >= 0) _chips[top].SetSelected(on);
        }

        public void SetValidTarget(bool on) => _validTarget = on;

        public void ClearHighlight()
        {
            _selected = _validTarget = false;
            int top = _lane.Count - 1;
            if (top >= 0) _chips[top].SetSelected(false);
        }

        public void PlayInvalid(Action onDone = null) => StartCoroutine(ShakeRoutine(onDone));

        public void AnimateTopReveal()
        {
            int top = _lane.Count - 1;
            if (top >= 0) _chips[top].AnimateReveal();
        }

        public void PlayUnlockFlash() => StartCoroutine(UnlockRoutine());

        // ── Animation ────────────────────────────────────────────────────────

        private void Update()
        {
            if (_flashing) return;
            float pulse = Mathf.Sin(Time.unscaledTime * 5f) * 0.5f + 0.5f;

            if (_selected)              _border.color = BorderSelect;
            else if (_validTarget)      _border.color = new Color(BorderValid.r, BorderValid.g, BorderValid.b, Mathf.Lerp(0.5f, 1f, pulse));
            else if (_lane.Pending)     _border.color = new Color(BorderPending.r, BorderPending.g, BorderPending.b, Mathf.Lerp(0.45f, 0.95f, pulse));
            else if (_lane.Kind == LaneKind.Blind) _border.color = BorderBlind;
            else                        _border.color = BorderNormal;
        }

        private IEnumerator ShakeRoutine(Action onDone)
        {
            _flashing = true;
            _border.color = BorderInvalid;
            float dur = 0.22f, t = 0f;
            while (t < dur)
            {
                float p = t / dur;
                _rt.anchoredPosition = _homePos + new Vector2(Mathf.Sin(p * Mathf.PI * 9f) * 16f * (1f - p), 0f);
                t += Time.deltaTime;
                yield return null;
            }
            _rt.anchoredPosition = _homePos;
            _flashing = false;
            onDone?.Invoke();
        }

        private IEnumerator UnlockRoutine()
        {
            _flashing = true;
            float dur = 0.35f, t = 0f;
            var lockRt = (RectTransform)_lockOverlay.transform;
            while (t < dur)
            {
                float p = t / dur;
                _border.color = Color.Lerp(BorderValid, BorderNormal, p);
                lockRt.localScale = Vector3.one * (1f - p);
                t += Time.deltaTime;
                yield return null;
            }
            _lockOverlay.SetActive(false);
            lockRt.localScale = Vector3.one;
            _flashing = false;
        }
    }
}
