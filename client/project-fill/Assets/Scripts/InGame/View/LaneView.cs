using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.InGame.View
{
    // One slot lane: card-rack body, four sockets, stacked chips, and gimmick decorations
    // (lock overlay, blind marker, relay pending badge). Owns its BoxCollider2D for tapping.
    public class LaneView : MonoBehaviour
    {
        private static readonly Color BorderNormal  = new(0.35f, 0.40f, 0.55f, 0.60f);
        private static readonly Color BorderSelect  = new(0.45f, 0.75f, 1.00f, 1f);
        private static readonly Color BorderValid   = new(0.35f, 0.95f, 0.55f, 1f);
        private static readonly Color BorderInvalid = new(1.00f, 0.32f, 0.32f, 1f);
        private static readonly Color BorderBlind   = new(0.30f, 0.85f, 0.95f, 0.95f);
        private static readonly Color BorderPending = new(1.00f, 0.72f, 0.20f, 1f);
        private static readonly Color BodyColor     = new(0.08f, 0.10f, 0.15f, 0.45f); // Glass substrate feel
        private static readonly Color SocketColor   = new(0.06f, 0.07f, 0.10f, 0.60f);

        private SpriteRenderer _border;
        private SpriteRenderer _backlight;    // inner ambient glow (skin accent channel)
        private ChipView[] _chips;
        private Transform[] _slots;

        private const int MaxPips = 5; // relay queue pips cap (orders longer than this clamp)

        private GameObject _lockOverlay;
        private GameObject _blindMarker;
        private GameObject _pendingBadge;
        private SpriteRenderer _scanLine;     // blind-lane scanning sweep
        private SpriteRenderer _edgeFrame;    // premium lane skin: tight inner accent frame (breathes, in-bounds)
        private SpriteRenderer[] _pips;        // relay queue-position dots

        private SlotLane _lane;
        private bool _selected, _validTarget, _flashing;

        // Skin tokens (from SpriteSet/BoardTheme); defaults reproduce the original look.
        private Color       _borderNormal   = BorderNormal;
        private Color       _accent         = new(0.21f, 0.84f, 0.95f);
        private Color       _railColor      = new(0.22f, 0.25f, 0.34f, 1f);
        private Color       _backlightColor = new(0.21f, 0.84f, 0.95f, 0.18f);
        private float       _edgePulse      = 0f; // premium lane skin breathe amplitude (0 = static)
        private BoardFxTier _fx             = BoardFxTier.Static;

        private bool _built;
        private Vector2 _chipSize;
        private Vector2 _laneSize;

        public int Index { get; private set; }
        public event Action<int> OnTapped;

        private GameObject _chipPrefab;

        // Builds the lane visuals once. Call after instantiating the LaneView prefab.
        // (Thin prefab + procedural body; art slot-in is via BoardSkin/SpriteSet.)
        public LaneView Initialize(int index, SpriteSet sprites, SlotLane lane, GameObject chipPrefab, Vector2 size)
        {
            Index = index;
            _chipPrefab = chipPrefab;
            _laneSize = size;
            if (!_built) Construct(sprites, lane, size);
            else _lane = lane;
            return this;
        }

        private void Construct(SpriteSet s, SlotLane lane, Vector2 size)
        {
            _built = true;
            _lane = lane;

            _borderNormal   = s.LaneBorder;
            _accent         = s.Accent;
            _railColor      = s.LaneRail;
            _backlightColor = s.LaneBacklight;
            _edgePulse      = s.LaneEdgePulse;
            _fx             = s.Fx;

            // BoxCollider2D on the whole lane for raycasting
            var col = gameObject.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
            }
            col.size = size;
            col.offset = Vector2.zero;

            // Inner backlight: soft ambient behind the column, tinted to the skin accent. Always-visible
            // skin-identity channel — stays lit while the state-driven border changes color, so the lane
            // never loses its look during selection. - sortingOrder: 2
            _backlight = WorldUtil.CreateSprite(transform, "Backlight", s.Glow, _backlightColor, new Vector2(size.x * 0.82f, size.y * 0.92f), sliced: false, sortingOrder: 2);

            // Register-column body: the lane is a single vertical register, not a box of parts. - sortingOrder: 3
            var body = WorldUtil.CreateSprite(transform, "Body", s.LaneSlot, s.LaneBody, size, sortingOrder: 3);

            // Recessed track spanning all cells: one continuous channel so the lane reads as ONE column
            // of stacked data cells rather than four separate sockets. - sortingOrder: 3
            var channelCol = Color.Lerp(s.LaneBody, Color.black, 0.45f); channelCol.a = 0.6f;
            WorldUtil.CreateSprite(transform, "Channel", s.LaneSlot, channelCol, new Vector2(size.x * 0.80f, size.y * 0.94f), sortingOrder: 3);

            const float vpad = 0.035f, gap = 0.014f;
            float usable = 1f - 2f * vpad;
            float slotH  = usable / SlotLane.Capacity;

            _slots = new Transform[SlotLane.Capacity];
            _chips = new ChipView[SlotLane.Capacity];

            float sw = 0.74f * size.x;
            float sh = (slotH - gap) * size.y;
            _chipSize = new Vector2(sw, sh);

            // Cell guides inside the column (faint — the continuous channel already carries the column
            // read; these only hint where each cell seats). - sortingOrder: 4
            for (int sIdx = 0; sIdx < SlotLane.Capacity; sIdx++)
            {
                float yMin = vpad + sIdx * slotH + gap * 0.5f;
                float yMax = vpad + (sIdx + 1) * slotH - gap * 0.5f;
                float yCenter_norm = (yMin + yMax) * 0.5f;
                float yCenter_local = -size.y * 0.5f + yCenter_norm * size.y;

                var sockCol = s.LaneSocket; sockCol.a *= 0.5f;
                var sock = WorldUtil.CreateSprite(transform, $"Socket_{sIdx}", s.LaneOutline, sockCol, _chipSize, sortingOrder: 4);
                sock.transform.localPosition = new Vector3(0f, yCenter_local, 0f);

                // Cell separator between stacked cells — a thin divider so the stack reads as discrete
                // registers, not one fused bar. - sortingOrder: 5
                if (sIdx > 0)
                {
                    float bNorm  = vpad + sIdx * slotH;
                    float bLocal = -size.y * 0.5f + bNorm * size.y;
                    var div = WorldUtil.CreateSprite(transform, $"Divider_{sIdx}", s.LaneSlot, _railColor, new Vector2(_chipSize.x * 0.92f, size.y * 0.012f), sortingOrder: 5);
                    div.transform.localPosition = new Vector3(0f, bLocal, 0f);
                }

                var slot = WorldUtil.CreateGameObject(transform, $"Slot_{sIdx}");
                slot.localPosition = new Vector3(0f, yCenter_local, 0f);
                _slots[sIdx] = slot;
                _chips[sIdx] = SpawnChip(slot, s, _chipSize);
                _chips[sIdx].Hide();
            }

            // Premium lane skin frame: a tight accent outline INSET inside the lane (x*0.94/y*0.97) so a
            // breathing premium lane never bleeds onto its neighbours. Separate from `_border` (the
            // interaction-state channel, overwritten every frame) — this carries skin identity only.
            // Sits below the chips (≥7); alpha breathes in Update. - sortingOrder: 5
            if (_edgePulse > 0f)
            {
                _edgeFrame = WorldUtil.CreateSprite(transform, "EdgeFrame", s.LaneOutline, new Color(_accent.r, _accent.g, _accent.b, 0f), new Vector2(size.x * 0.94f, size.y * 0.97f), sortingOrder: 5);
            }

            // Outer border - sortingOrder: 5
            _border = WorldUtil.CreateSprite(transform, "Border", s.LaneOutline, _borderNormal, size, sortingOrder: 5);

            BuildLockOverlay(s, lane, size);
            BuildBlindMarker(s, size);
            BuildPendingBadge(s, size);
        }

        // Spawns a chip from the prefab (thin GO + ChipView) into a slot; falls back to a bare
        // GameObject when no prefab is wired (keeps editor/test paths working).
        private ChipView SpawnChip(Transform parent, SpriteSet s, Vector2 size)
        {
            GameObject go = _chipPrefab != null
                ? Instantiate(_chipPrefab, parent)
                : new GameObject("Chip");
            if (_chipPrefab == null) go.transform.SetParent(parent, false);
            var cv = go.GetComponent<ChipView>() ?? go.AddComponent<ChipView>();
            return cv.Initialize(s, size);
        }

        private void BuildLockOverlay(SpriteSet s, SlotLane lane, Vector2 size)
        {
            _lockOverlay = WorldUtil.CreateGameObject(transform, "Lock").gameObject;

            // Translucent dark shroud - sortingOrder: 15
            var seal = WorldUtil.CreateSprite(_lockOverlay.transform, "Seal", s.LaneSlot, new Color(0.05f, 0.06f, 0.09f, 0.92f), size, sortingOrder: 15);

            // Padlock icon tinted to the UnlockType color — the color tells the player which signal
            // opens this lane (same palette as the Signal Panel nodes). No text, no emoji.
            float lockSize = size.x * 0.5f;
            var lockCol = Color.Lerp(lane.UnlockType.ToColor(), Color.white, 0.2f);
            var lockIcon = WorldUtil.CreateSprite(_lockOverlay.transform, "LockIcon", s.LockSeal, lockCol, new Vector2(lockSize, lockSize), sliced: false, sortingOrder: 17);
            lockIcon.transform.localPosition = new Vector3(0f, size.y * 0.04f, 0f);

            _lockOverlay.SetActive(lane.Kind == LaneKind.Locked);
        }

        private void BuildBlindMarker(SpriteSet s, Vector2 size)
        {
            float badge = size.x * 0.60f;
            float y = size.y * 0.5f + badge * 0.55f; // sit above the frame so it never covers the top chip

            _blindMarker = WorldUtil.CreateGameObject(transform, "BlindMark").gameObject;
            _blindMarker.transform.localPosition = new Vector3(0f, y, 0f);

            // "?" disc badge (stand-in for the lane_blind_marker sprite; "?" is an ASCII glyph icon).
            WorldUtil.CreateSprite(_blindMarker.transform, "Bg", s.Disc, new Color(0.07f, 0.20f, 0.25f, 0.95f), new Vector2(badge, badge), sliced: false, sortingOrder: 18);
            var q = WorldUtil.CreateLabel(_blindMarker.transform, "Q", "?", 40f, new Vector2(badge, badge));
            q.color     = BorderBlind;
            q.fontStyle = FontStyles.Bold;
            q.sortingOrder = 19;
            _blindMarker.SetActive(_lane.Kind == LaneKind.Blind);

            // Scanning sweep: a thin bright bar that travels over the masked stack (animated in Update).
            _scanLine = WorldUtil.CreateSprite(transform, "ScanLine", s.Chip, new Color(BorderBlind.r, BorderBlind.g, BorderBlind.b, 0f), new Vector2(size.x * 0.68f, size.y * 0.03f), sliced: false, sortingOrder: 14);
            _scanLine.enabled = false;
        }

        private void BuildPendingBadge(SpriteSet s, Vector2 size)
        {
            float w = size.x * 0.78f;
            float h = size.x * 0.20f;
            float y = size.y * 0.5f + h * 0.7f; // sit above the frame so it never covers the top chip

            _pendingBadge = WorldUtil.CreateGameObject(transform, "Pending").gameObject;
            _pendingBadge.transform.localPosition = new Vector3(0f, y, 0f);

            WorldUtil.CreateSprite(_pendingBadge.transform, "Bg", s.LaneSlot, new Color(0.25f, 0.14f, 0.02f, 0.9f), new Vector2(w, h), sortingOrder: 18);

            // Queue-position pips: filled count = steps until this lane's signal is next (1 = up next,
            // shown green). Replaces the "WAIT #N" text. Orders longer than MaxPips clamp.
            _pips = new SpriteRenderer[MaxPips];
            float pipD = h * 0.55f;
            float spacing = pipD * 1.4f;
            float x0 = -spacing * (MaxPips - 1) * 0.5f;
            for (int i = 0; i < MaxPips; i++)
            {
                var pip = WorldUtil.CreateSprite(_pendingBadge.transform, $"Pip_{i}", s.Disc, BorderPending, new Vector2(pipD, pipD), sliced: false, sortingOrder: 19);
                pip.transform.localPosition = new Vector3(x0 + i * spacing, 0f, 0f);
                _pips[i] = pip;
            }
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

            // Lock → unlock plays the dissolve FX; otherwise set state directly.
            if (_lockOverlay.activeSelf && !lane.Locked) PlayUnlockFlash();
            else if (!_flashing) _lockOverlay.SetActive(lane.Locked);
            _blindMarker.SetActive(lane.Kind == LaneKind.Blind);
            _pendingBadge.SetActive(lane.Pending);
            if (lane.Pending) SetPips(pendingNumber);
        }

        // Lights the first N pips (N = relay queue position); next-up (<=1) is green, others amber.
        private void SetPips(int n)
        {
            n = Mathf.Clamp(n, 1, MaxPips);
            var lit = n <= 1 ? BorderValid : BorderPending;
            for (int i = 0; i < MaxPips; i++)
                _pips[i].color = i < n ? lit : new Color(lit.r, lit.g, lit.b, 0.18f);
        }

        public Vector3 SlotWorldPos(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, SlotLane.Capacity - 1);
            return _slots[slotIndex].position;
        }

        public Vector3 ChipWorldPos(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, SlotLane.Capacity - 1);
            if (_chips[slotIndex] != null && _chips[slotIndex].gameObject.activeSelf)
            {
                return _chips[slotIndex].transform.position;
            }
            return _slots[slotIndex].position;
        }

        public Vector2 ChipPixelSize() => _chipSize;

        // ── Highlight state ──────────────────────────────────────────────────

        public void SetSelected(bool on)
        {
            _selected = on;
            int top = _lane.Count - 1;
            // Highlight only (glow/scale + lane border) — no vertical lift, so a batch move doesn't
            // look like a single chip hopping up while the rest of the run stays put.
            if (top >= 0) _chips[top].SetSelected(on, 0f);
        }

        public void SetValidTarget(bool on) => _validTarget = on;

        public void ClearHighlight()
        {
            _selected = _validTarget = false;
            int top = _lane.Count - 1;
            if (top >= 0) _chips[top].SetSelected(false, 0f);
        }

        public void PlayInvalid(Action onDone = null) => StartCoroutine(ShakeRoutine(onDone));

        public void AnimateTopReveal()
        {
            int top = _lane.Count - 1;
            if (top >= 0) _chips[top].AnimateReveal();
        }

        public void PlayUnlockFlash() => StartCoroutine(UnlockRoutine());

        public void Tap()
        {
            OnTapped?.Invoke(Index);
        }

        // ── Animation ────────────────────────────────────────────────────────

        private void Update()
        {
            if (_lane == null || _border == null) return;
            if (_flashing) return;
            float pulse = Mathf.Sin(Time.unscaledTime * 5f) * 0.5f + 0.5f;

            Color target;
            if (_selected)              target = BorderSelect;
            else if (_validTarget)      target = new Color(BorderValid.r, BorderValid.g, BorderValid.b, Mathf.Lerp(0.5f, 1f, pulse));
            else if (_lane.Pending)     target = new Color(BorderPending.r, BorderPending.g, BorderPending.b, Mathf.Lerp(0.45f, 0.95f, pulse));
            else if (_lane.Kind == LaneKind.Blind) target = BorderBlind;
            else if (_fx == BoardFxTier.Dynamic)
                // Premium boards: idle border breathes toward the neon accent.
                target = Color.Lerp(_borderNormal, _accent, Mathf.Lerp(0.15f, 0.55f, pulse));
            else                        target = _borderNormal;
            // Smooth neon color transition between states (no hard snap).
            _border.color = Color.Lerp(_border.color, target, 1f - Mathf.Exp(-Time.deltaTime * 12f));

            // Lane skin identity breathe. Premium lane skins (LaneEdgePulse>0) pulse the inner backlight
            // AND a tight inner accent frame — BOTH inside the lane bounds, so a breathing premium lane
            // never bleeds onto its neighbours (replaces the old vertical LaneFlow sweep that collided
            // with the blind scan). Otherwise Dynamic boards give a gentle backlight breathe; static
            // lanes hold the resting skin colour.
            if (_edgePulse > 0f)
            {
                if (_backlight != null)
                {
                    var c = _backlightColor; c.a = _backlightColor.a * Mathf.Lerp(0.7f, 1f + 0.7f * _edgePulse, pulse);
                    _backlight.color = c;
                }
                if (_edgeFrame != null)
                {
                    var c = _accent; c.a = Mathf.Lerp(0.06f, 0.12f + 0.30f * _edgePulse, pulse);
                    _edgeFrame.color = c;
                }
            }
            else if (_fx == BoardFxTier.Dynamic && _backlight != null)
            {
                var c = _backlightColor; c.a = _backlightColor.a * Mathf.Lerp(0.6f, 1.25f, pulse);
                _backlight.color = c;
            }

            // Blind scanning sweep: a bright bar travels up the masked stack, fading at the ends.
            if (_scanLine != null)
            {
                bool blind = _lane.Kind == LaneKind.Blind;
                _scanLine.enabled = blind;
                if (blind)
                {
                    float ph = (Time.unscaledTime * 0.5f) % 1f;
                    _scanLine.transform.localPosition = new Vector3(0f, Mathf.Lerp(-_laneSize.y * 0.42f, _laneSize.y * 0.42f, ph), 0f);
                    var c = _scanLine.color; c.a = 0.12f + 0.5f * Mathf.Sin(ph * Mathf.PI); _scanLine.color = c;
                }
            }
        }

        private IEnumerator ShakeRoutine(Action onDone)
        {
            _flashing = true;
            _border.color = BorderInvalid;
            // Capture the lane's REAL placed position live — Construct runs before BoardView assigns
            // the lane localPosition, so a cached home would snap the lane back to the board origin.
            Vector3 home = transform.localPosition;
            float amp = _laneSize.x * 0.18f;
            float dur = 0.22f, t = 0f;
            while (t < dur)
            {
                float p = t / dur;
                transform.localPosition = home + new Vector3(Mathf.Sin(p * Mathf.PI * 9f) * amp * (1f - p), 0f, 0f);
                t += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = home;
            _flashing = false;
            onDone?.Invoke();
        }

        private IEnumerator UnlockRoutine()
        {
            _flashing = true;
            float dur = 0.35f, t = 0f;
            var lockTrans = _lockOverlay.transform;
            while (t < dur)
            {
                float p = t / dur;
                _border.color = Color.Lerp(BorderValid, BorderNormal, p);
                lockTrans.localScale = Vector3.one * (1f - p);
                t += Time.deltaTime;
                yield return null;
            }
            _lockOverlay.SetActive(false);
            lockTrans.localScale = Vector3.one;
            _flashing = false;
        }
    }
}
