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
        private ChipView[] _chips;
        private Transform[] _slots;

        private GameObject _lockOverlay;
        private GameObject _blindMarker;
        private GameObject _pendingBadge;
        private TextMeshPro _pendingLabel;

        private SlotLane _lane;
        private bool _selected, _validTarget, _flashing;

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

            // BoxCollider2D on the whole lane for raycasting
            var col = gameObject.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
            }
            col.size = size;
            col.offset = Vector2.zero;

            // Lane frame body - sortingOrder: 3
            var body = WorldUtil.CreateSprite(transform, "Body", s.LaneSlot, BodyColor, size, sortingOrder: 3);

            // Motherboard PCIe/RAM slot contact pins at the bottom - proportional to width size.x
            float contactW = size.x * 0.75f;
            float contactH = size.x * 0.10f; // Scale contact height with width to prevent vertical stretching
            var contacts = WorldUtil.CreateSprite(transform, "Contacts", s.Chip, new Color(0.92f, 0.69f, 0.20f, 1f), new Vector2(contactW, contactH), sortingOrder: 2);
            contacts.transform.localPosition = new Vector3(0f, -size.y * 0.5f - contactH * 0.4f, 0f);
            
            // Adding a small socket block that "inserts" the contacts - sortingOrder: 1
            var insertSocket = WorldUtil.CreateSprite(transform, "InsertSocket", s.ChipOutline, new Color(0.08f, 0.09f, 0.13f, 1f), new Vector2(size.x * 0.9f, contactH * 1.5f), sortingOrder: 1);
            insertSocket.transform.localPosition = new Vector3(0f, -size.y * 0.5f - contactH * 0.8f, 0f);

            // Mechanical ejector latches at the top - proportional to width size.x
            float ejectW = size.x * 0.15f;
            float ejectH = size.x * 0.22f; // Scale ejector height with width to prevent stretching
            var ejectL = WorldUtil.CreateSprite(transform, "Ejector_L", s.Chip, new Color(0.25f, 0.28f, 0.35f, 1f), new Vector2(ejectW, ejectH), sortingOrder: 6);
            ejectL.transform.localPosition = new Vector3(-size.x * 0.5f - ejectW * 0.2f, size.y * 0.5f, 0f);
            ejectL.transform.localRotation = Quaternion.Euler(0, 0, 15f);
            
            var ejectR = WorldUtil.CreateSprite(transform, "Ejector_R", s.Chip, new Color(0.25f, 0.28f, 0.35f, 1f), new Vector2(ejectW, ejectH), sortingOrder: 6);
            ejectR.transform.localPosition = new Vector3(size.x * 0.5f + ejectW * 0.2f, size.y * 0.5f, 0f);
            ejectR.transform.localRotation = Quaternion.Euler(0, 0, -15f);

            const float vpad = 0.035f, gap = 0.014f;
            float usable = 1f - 2f * vpad;
            float slotH  = usable / SlotLane.Capacity;

            _slots = new Transform[SlotLane.Capacity];
            _chips = new ChipView[SlotLane.Capacity];

            float sw = 0.74f * size.x;
            float sh = (slotH - gap) * size.y;
            _chipSize = new Vector2(sw, sh);

            // Sockets and slots behind chips - sortingOrder: 4
            for (int sIdx = 0; sIdx < SlotLane.Capacity; sIdx++)
            {
                float yMin = vpad + sIdx * slotH + gap * 0.5f;
                float yMax = vpad + (sIdx + 1) * slotH - gap * 0.5f;
                float yCenter_norm = (yMin + yMax) * 0.5f;
                float yCenter_local = -size.y * 0.5f + yCenter_norm * size.y;

                var sock = WorldUtil.CreateSprite(transform, $"Socket_{sIdx}", s.ChipOutline, SocketColor, _chipSize, sortingOrder: 4);
                sock.transform.localPosition = new Vector3(0f, yCenter_local, 0f);

                var slot = WorldUtil.CreateGameObject(transform, $"Slot_{sIdx}");
                slot.localPosition = new Vector3(0f, yCenter_local, 0f);
                _slots[sIdx] = slot;
                _chips[sIdx] = SpawnChip(slot, s, _chipSize);
                _chips[sIdx].Hide();
            }

            // Outer border - sortingOrder: 5
            _border = WorldUtil.CreateSprite(transform, "Border", s.ChipOutline, BorderNormal, size, sortingOrder: 5);

            BuildLockOverlay(s, lane, size);
            BuildBlindMarker(size);
            BuildPendingBadge(size);
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

            // Access lock chip visual instead of padlock - proportional to size.x
            float chipW = size.x * 0.45f;
            float chipH = size.x * 0.45f;
            float chipY = size.y * 0.12f;
            var securityChip = WorldUtil.CreateSprite(_lockOverlay.transform, "SecurityChip", s.Chip, new Color(0.15f, 0.16f, 0.22f, 1f), new Vector2(chipW, chipH), sortingOrder: 16);
            securityChip.transform.localPosition = new Vector3(0f, chipY, 0f);
            
            // Red warning outline indicating locked state - sortingOrder: 17
            var outline = WorldUtil.CreateSprite(securityChip.transform, "ChipOutline", s.ChipOutline, new Color(1f, 0.32f, 0.32f, 0.8f), new Vector2(chipW, chipH), sortingOrder: 17);
            
            var label = WorldUtil.CreateLabel(securityChip.transform, "LockGlyph", "🔒", 26f, new Vector2(chipW, chipH));
            label.color = new Color(1f, 0.32f, 0.32f);
            label.alignment = TextAlignmentOptions.Center;
            label.sortingOrder = 18;

            // unlock-condition badge (type color) - proportional to size.x
            float badgeW = size.x * 0.85f;
            float badgeH = size.x * 0.22f;
            float badgeY = -size.y * 0.18f;
            
            var badgeBg = WorldUtil.CreateSprite(_lockOverlay.transform, "BadgeBg", s.ChipOutline, new Color(0.20f, 0.22f, 0.30f, 1f), new Vector2(badgeW, badgeH * 1.2f), sortingOrder: 16);
            badgeBg.transform.localPosition = new Vector3(0f, badgeY, 0f);
            
            var badge = WorldUtil.CreateSprite(badgeBg.transform, "Badge", s.Chip, lane.UnlockType.ToColor(), new Vector2(badgeW * 0.9f, badgeH), sortingOrder: 17);
            
            var bl = WorldUtil.CreateLabel(badge.transform, "BadgeGlyph", $"{lane.UnlockType.ToLabel()} ACTIVE", 14f, new Vector2(badgeW * 0.9f, badgeH));
            bl.color = new Color(0, 0, 0, 0.75f);
            bl.fontStyle = FontStyles.Bold;
            bl.sortingOrder = 18;

            _lockOverlay.SetActive(lane.Kind == LaneKind.Locked);
        }

        private void BuildBlindMarker(Vector2 size)
        {
            float w = size.x * 0.88f;
            float h = size.x * 0.22f; // Scale height with width
            float y = size.y * 0.5f + h * 0.65f; // sit above the frame so it never covers the top chip

            _blindMarker = WorldUtil.CreateGameObject(transform, "BlindMark").gameObject;
            _blindMarker.transform.localPosition = new Vector3(0f, y, 0f);

            var markBg = WorldUtil.CreateSprite(_blindMarker.transform, "Bg", null, new Color(0.07f, 0.20f, 0.25f, 0.9f), new Vector2(w, h), sortingOrder: 18);
            var q = WorldUtil.CreateLabel(_blindMarker.transform, "Q", "SCANNING", 14f, new Vector2(w, h));
            q.color     = BorderBlind;
            q.fontStyle = FontStyles.Bold;
            q.sortingOrder = 19;
            _blindMarker.SetActive(_lane.Kind == LaneKind.Blind);
        }

        private void BuildPendingBadge(Vector2 size)
        {
            float w = size.x * 0.88f;
            float h = size.x * 0.22f; // Scale height with width
            float y = size.y * 0.5f + h * 0.65f; // sit above the frame so it never covers the top chip

            _pendingBadge = WorldUtil.CreateGameObject(transform, "Pending").gameObject;
            _pendingBadge.transform.localPosition = new Vector3(0f, y, 0f);

            // Flashing warning diagonal stripe design
            var markBg = WorldUtil.CreateSprite(_pendingBadge.transform, "Bg", null, new Color(0.25f, 0.12f, 0.02f, 0.9f), new Vector2(w, h), sortingOrder: 18);
            _pendingLabel = WorldUtil.CreateLabel(_pendingBadge.transform, "Lbl", "SIGNAL PENDING", 14f, new Vector2(w, h));
            _pendingLabel.color = BorderPending;
            _pendingLabel.fontStyle = FontStyles.Bold;
            _pendingLabel.sortingOrder = 19;
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
