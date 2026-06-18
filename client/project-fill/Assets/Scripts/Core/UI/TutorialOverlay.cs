using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Services.Tutorial;
using Game.OutGame.Settings;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Data.Generated;
using Game.InGame.View;
using Game.InGame.Controller;

namespace Game.Core.UI
{
    public class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] private Image _dimLayer;
        [SerializeField] private RectTransform _spotlightCutout;
        [SerializeField] private Image _spotlightGlow; // optional glow border
        [SerializeField] private RectTransform _tooltipBubble;
        [SerializeField] private TMP_Text _tooltipText;
        [SerializeField] private RectTransform _fingerOverlay;
        [SerializeField] private Image _characterAvatar; // Guide character visual
        [SerializeField] private Button _fullscreenDismissButton; // for non-blocking taps

        private TutorialStepSequencer _sequencer;
        private Coroutine _fingerTapCoroutine;
        private Transform _currentTargetTransform;
        private RectTransform _currentTargetRT;
        private Transform _currentTargetToTransform;   // drag destination (target_ui_id_to)
        private RectTransform _currentTargetToRT;
        private TargetSpaceType _currentTargetSpace;
        private CellView _currentTargetCellView;
        private Vector2 _designedResolution = new Vector2(1080, 1920);

        // Pointer visual reuse: _fingerOverlay shows either the finger (tap/select) or the drag pointer
        // (move). Sprites cached/loaded once; swapped per step.
        private Image _fingerImage;
        private Sprite _fingerSprite;
        private Sprite _dragSprite;
        private bool _isDragStep;
        private Coroutine _dragCoroutine;
        private Vector2 _dragFromLocal, _dragToLocal;
        private bool _dragEndpointsValid;

        // Highlight = a hole punched in the dim (the target shows through), NOT a colored glow.
        // 4 runtime border quads surround the hole; _holeScale drives a quick 3x reveal blink on appear.
        private Image[] _dimBorders;
        private bool _hasSpotlight;
        private float _holeScale = 1f;
        private Coroutine _holeBlinkCoroutine;

        private void Awake()
        {
            if (_fingerOverlay != null) _fingerImage = _fingerOverlay.GetComponent<Image>();
            if (_fingerImage != null) _fingerSprite = _fingerImage.sprite;
            _dragSprite = Services.DynamicResourceService.Instance != null
                ? Services.DynamicResourceService.Instance.GetSprite("ui_drag_pointer")
                : null;

            // Highlight is the dim-hole, not the old yellow frame/glow — keep those off.
            if (_spotlightCutout != null) _spotlightCutout.gameObject.SetActive(false);
            if (_spotlightGlow != null) _spotlightGlow.gameObject.SetActive(false);
            CreateDimBorders();
        }

        // Four dim quads that frame the spotlight hole (reuse the dim color). Visual only — tap blocking
        // for Tap steps is the fullscreen dismiss button; action steps leave the board reachable.
        private void CreateDimBorders()
        {
            if (_dimLayer == null) return;
            var parent = _dimLayer.transform.parent != null ? _dimLayer.transform.parent : transform;
            int after = _dimLayer.transform.GetSiblingIndex() + 1;
            _dimBorders = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"DimBorder{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.SetSiblingIndex(after + i);
                var img = go.GetComponent<Image>();
                img.color = _dimLayer.color;
                img.raycastTarget = false;
                go.SetActive(false);
                _dimBorders[i] = img;
            }
        }

        public void Init(TutorialStepSequencer sequencer)
        {
            _sequencer = sequencer;
            _sequencer.OnStepChanged += ShowStep;
            _sequencer.OnComplete += Close;

            // Full-screen raycast block: the dim layer eats every UI tap so nothing behind the
            // overlay is interactable; the dismiss button is the tap-to-advance catcher above it.
            if (_dimLayer != null) _dimLayer.raycastTarget = true;

            if (_fullscreenDismissButton != null)
            {
                _fullscreenDismissButton.onClick.AddListener(OnFullscreenTapped);
            }

            ShowStep(_sequencer.CurrentStep);
        }

        private void OnDestroy()
        {
            if (_sequencer != null)
            {
                _sequencer.OnStepChanged -= ShowStep;
                _sequencer.OnComplete -= Close;
            }

            if (_currentTargetCellView != null)
            {
                _currentTargetCellView.SetTargetHighlight(false);
                _currentTargetCellView = null;
            }

            if (_holeBlinkCoroutine != null) StopCoroutine(_holeBlinkCoroutine);
            if (_fingerTapCoroutine != null) StopCoroutine(_fingerTapCoroutine);
            if (_dragCoroutine != null) StopCoroutine(_dragCoroutine);
        }

        private void LateUpdate()
        {
            // Responsive update in case of layout shifts or board animations
            UpdateSpotlightPosition();
        }

        private void ShowStep(TutorialStep step)
        {
            if (step == null) return;

            if (_fingerTapCoroutine != null)
            {
                StopCoroutine(_fingerTapCoroutine);
                _fingerTapCoroutine = null;
            }

            if (_dragCoroutine != null)
            {
                StopCoroutine(_dragCoroutine);
                _dragCoroutine = null;
            }
            _dragEndpointsValid = false;

            // Clear previous cell highlight before resolving new target
            if (_currentTargetCellView != null)
            {
                _currentTargetCellView.SetTargetHighlight(false);
                _currentTargetCellView = null;
            }

            // Update localized text
            if (_tooltipText != null)
            {
                _tooltipText.text = Services.LocalizationService.Instance != null
                    ? Services.LocalizationService.Instance.Get(step.text_key)
                    : step.text_key;
            }

            // Set user selected avatar if available
            Sprite userAvatar = null;
            var popupPrefab = Resources.Load<GameObject>("Prefabs/UI/AccountPopupView");
            if (popupPrefab != null)
            {
                var popupView = popupPrefab.GetComponent<AccountPopupView>();
                if (popupView != null && Services.AuthService.Instance != null)
                {
                    userAvatar = popupView.GetAvatarSprite(Services.AuthService.Instance.AvatarId);
                }
            }

            if (_characterAvatar != null && userAvatar != null)
            {
                _characterAvatar.sprite = userAvatar;
                _characterAvatar.preserveAspect = true;
            }

            // Locate target
            ResolveTarget(step);

            bool isTap = step.advance_mode == TutorialAdvanceMode.Tap;
            _isDragStep = step.content_type == TutorialContentType.DragPointer;

            // Tap (informational) step: overlay eats the tap to advance + blocks the scene. Action
            // (Select/Move) step: let the real tap reach the board; the overlay never advances on tap.
            if (_dimLayer != null) _dimLayer.raycastTarget = isTap;
            if (_fullscreenDismissButton != null) _fullscreenDismissButton.gameObject.SetActive(isTap);

            // Pointer sprite: drag pointer for Move steps, finger otherwise.
            if (_fingerImage != null)
                _fingerImage.sprite = (_isDragStep && _dragSprite != null) ? _dragSprite : _fingerSprite;
            if (_fingerOverlay != null) _fingerOverlay.localRotation = Quaternion.identity;

            if (_fingerOverlay != null && _fingerOverlay.gameObject.activeSelf)
            {
                _fingerOverlay.localScale = Vector3.one;
                if (_isDragStep) _dragCoroutine    = StartCoroutine(AnimateDragPointer());
                else             _fingerTapCoroutine = StartCoroutine(AnimateFingerTap());
            }

            // Highlight = dim opens at the target; blink the reveal 3x quickly for emphasis, then hold open.
            if (_holeBlinkCoroutine != null) StopCoroutine(_holeBlinkCoroutine);
            _holeScale = 1f;
            if (_hasSpotlight) _holeBlinkCoroutine = StartCoroutine(BlinkHole());

            // Trigger bubble anim (Scale up)
            if (_tooltipBubble != null)
            {
                _tooltipBubble.localScale = Vector3.zero;
                StartCoroutine(AnimateBubbleAppear());
            }

            // Instantly evaluate position for this frame
            UpdateSpotlightPosition();
        }

        private void ResolveTarget(TutorialStep step)
        {
            _currentTargetTransform = null;
            _currentTargetRT = null;
            _currentTargetToTransform = null;
            _currentTargetToRT = null;
            _currentTargetSpace = step.target_space;

            bool wantPointer = step.content_type == TutorialContentType.FingerOverlay
                            || step.content_type == TutorialContentType.DragPointer;

            if (string.IsNullOrEmpty(step.target_ui_id))
            {
                _hasSpotlight = false;
                _fingerOverlay.gameObject.SetActive(false);
                return;
            }

            _hasSpotlight = true;
            _fingerOverlay.gameObject.SetActive(wantPointer);

            // Source: TutorialTarget registry (id-based, no name coupling); legacy board scan as fallback.
            if (!TryResolveById(step.target_ui_id, step.target_space, out _currentTargetTransform, out _currentTargetRT))
                ResolveLegacyWorldSource(step);

            // Drag destination (Move step) — registry only (lanes register at runtime).
            if (!string.IsNullOrEmpty(step.target_ui_id_to))
                TryResolveById(step.target_ui_id_to, step.target_space, out _currentTargetToTransform, out _currentTargetToRT);
        }

        // Resolves a target_ui_id to a UI RectTransform (UI space) or a world Transform via TutorialTarget.
        private static bool TryResolveById(string id, TargetSpaceType space, out Transform tr, out RectTransform rt)
        {
            tr = null; rt = null;
            var t = TutorialTarget.Find(id);
            if (t == null) return false;
            var r = t.GetComponent<RectTransform>();
            if (r != null && space == TargetSpaceType.UI) rt = r;
            else tr = t.transform;
            return true;
        }

        // Legacy board-cell targeting (board_cell_/protector/core/obstacle). Dead in Signal Sort
        // (GetCellView returns null) but kept so old CSV ids degrade gracefully.
        private void ResolveLegacyWorldSource(TutorialStep step)
        {
            if (step.target_space != TargetSpaceType.World) return;

            if (step.target_ui_id.StartsWith("board_cell_"))
            {
                var boardView = FindObjectOfType<BoardView>();
                if (boardView != null && ParseCellTarget(step.target_ui_id, out int r, out int c))
                {
                    var cellView = boardView.GetCellView(r, c);
                    if (cellView != null)
                    {
                        _currentTargetTransform = cellView.transform;
                        _currentTargetCellView = cellView;
                        cellView.SetTargetHighlight(true);
                    }
                }
                return;
            }

            var bv = FindObjectOfType<BoardView>();
            if (bv != null)
            {
                _currentTargetTransform = step.target_ui_id switch
                {
                    "board_protector_cell" => FindCellWithProtector(bv),
                    "board_core_cell"      => FindCellWithCore(bv),
                    "board_obstacle_cell"  => FindCellWithObstacle(bv),
                    _                      => null
                };
            }
        }

        private Transform FindCellWithProtector(BoardView boardView)
        {
            for (int r = 0; r < 20; r++)
            for (int c = 0; c < 20; c++)
            {
                var cellView = boardView.GetCellView(r, c);
                if (cellView != null && cellView.gameObject.activeSelf)
                {
                    var renderers = cellView.GetComponentsInChildren<SpriteRenderer>(false);
                    foreach (var rdr in renderers)
                    {
                        if (rdr.gameObject.name.Contains("Protector") && rdr.gameObject.activeSelf)
                            return cellView.transform;
                    }
                }
            }
            return null;
        }

        private Transform FindCellWithCore(BoardView boardView)
        {
            for (int r = 0; r < 20; r++)
            for (int c = 0; c < 20; c++)
            {
                var cellView = boardView.GetCellView(r, c);
                if (cellView != null && cellView.gameObject.activeSelf)
                {
                    foreach (Transform child in cellView.transform)
                    {
                        if (child.gameObject.name.Contains("Core") && child.gameObject.activeSelf)
                            return cellView.transform;
                    }
                }
            }
            return null;
        }

        private Transform FindCellWithObstacle(BoardView boardView)
        {
            for (int r = 0; r < 20; r++)
            for (int c = 0; c < 20; c++)
            {
                var cellView = boardView.GetCellView(r, c);
                if (cellView != null && cellView.gameObject.activeSelf)
                {
                    var srs = cellView.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var s in srs)
                    {
                        if (s.sprite != null && s.sprite.name.Contains("obstacle"))
                            return cellView.transform;
                    }
                }
            }
            return null;
        }

        private void UpdateSpotlightPosition()
        {
            if (_sequencer == null || _sequencer.CurrentStep == null) return;

            Canvas overlayCanvas = GetComponentInParent<Canvas>();
            if (overlayCanvas == null) return;
            RectTransform overlayRt = overlayCanvas.GetComponent<RectTransform>();

            Vector2 screenPos = Vector2.zero;
            Vector2 targetSize = new Vector2(150, 150); // fallback size
            bool haveTarget;

            if (_currentTargetSpace == TargetSpaceType.World && _currentTargetTransform != null)
            {
                haveTarget = TryWorldTargetScreen(_currentTargetTransform, overlayRt, out screenPos, out targetSize);
            }
            else if (_currentTargetSpace == TargetSpaceType.UI && _currentTargetRT != null)
            {
                screenPos = RectTransformUtility.WorldToScreenPoint(null, _currentTargetRT.position);
                targetSize = _currentTargetRT.rect.size * _currentTargetRT.lossyScale / overlayCanvas.scaleFactor;
                haveTarget = true;
            }
            else
            {
                haveTarget = false;
            }

            if (!haveTarget)
            {
                // Whole-board / unresolved target: full dim, centered tooltip, no pointer.
                SetFullDim();
                if (!_isDragStep && _fingerOverlay != null) _fingerOverlay.gameObject.SetActive(false);
                _dragEndpointsValid = false;
                if (_tooltipBubble != null) _tooltipBubble.anchoredPosition = Vector2.zero;
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRt, screenPos, overlayCanvas.worldCamera, out Vector2 localPoint);

            LayoutDimHole(overlayRt, localPoint, targetSize);

            if (_isDragStep)
            {
                // Drag pointer slides from source → destination; the coroutine reads these endpoints.
                _dragFromLocal = localPoint;
                _dragEndpointsValid = TryGetDestLocal(overlayRt, overlayCanvas, out _dragToLocal);
                // No destination (e.g. last node) → park the pointer on the source instead of drifting.
                if (!_dragEndpointsValid && _fingerOverlay != null) _fingerOverlay.anchoredPosition = localPoint;
            }
            else if (_fingerOverlay != null && _fingerOverlay.gameObject.activeSelf)
            {
                _fingerOverlay.anchoredPosition = localPoint + new Vector2(40f, -40f);
            }

            PlaceTooltip(overlayRt, localPoint, targetSize);
        }

        // Places the tooltip on whichever vertical side of the spotlight has room, never overlapping the
        // spotlight rect, fully clamped inside the canvas (so it never runs off-screen / past the board).
        private void PlaceTooltip(RectTransform overlayRt, Vector2 targetLocal, Vector2 targetSize)
        {
            if (_tooltipBubble == null) return;

            Vector2 bubble = _tooltipBubble.rect.size;
            const float margin = 40f;
            float halfH = overlayRt.rect.height * 0.5f;
            float spotHalf = targetSize.y * 0.5f + 8f;

            float below = targetLocal.y - spotHalf - bubble.y * 0.5f - margin; // bubble under the spotlight
            float above = targetLocal.y + spotHalf + bubble.y * 0.5f + margin; // bubble over the spotlight
            bool belowFits = (below - bubble.y * 0.5f) >= (-halfH + margin);
            bool aboveFits = (above + bubble.y * 0.5f) <= (halfH - margin);

            float by = belowFits ? below : (aboveFits ? above : below);
            by = Mathf.Clamp(by, -halfH + bubble.y * 0.5f + margin, halfH - bubble.y * 0.5f - margin);
            _tooltipBubble.anchoredPosition = new Vector2(0f, by);
        }

        // Frames the spotlight hole with 4 dim quads so the target shows through the dim (no colored glow).
        // _holeScale (0..1) shrinks the hole for the reveal blink; at 0 the borders meet → full dim.
        private void LayoutDimHole(RectTransform overlayRt, Vector2 center, Vector2 size)
        {
            if (_dimBorders == null) { return; }
            if (_dimLayer != null && _dimLayer.gameObject.activeSelf) _dimLayer.gameObject.SetActive(false);

            float hw = overlayRt.rect.width * 0.5f;
            float hh = overlayRt.rect.height * 0.5f;
            Vector2 half = size * 0.5f * _holeScale + new Vector2(6f, 6f);

            float left   = Mathf.Clamp(center.x - half.x, -hw, hw);
            float right  = Mathf.Clamp(center.x + half.x, -hw, hw);
            float bottom = Mathf.Clamp(center.y - half.y, -hh, hh);
            float top    = Mathf.Clamp(center.y + half.y, -hh, hh);

            SetBorder(_dimBorders[0], new Vector2(0f, (top + hh) * 0.5f),       new Vector2(hw * 2f, hh - top));      // above hole
            SetBorder(_dimBorders[1], new Vector2(0f, (bottom - hh) * 0.5f),    new Vector2(hw * 2f, bottom + hh));   // below hole
            SetBorder(_dimBorders[2], new Vector2((left - hw) * 0.5f,  (top + bottom) * 0.5f), new Vector2(left + hw, top - bottom)); // left
            SetBorder(_dimBorders[3], new Vector2((right + hw) * 0.5f, (top + bottom) * 0.5f), new Vector2(hw - right, top - bottom)); // right
        }

        private static void SetBorder(Image img, Vector2 pos, Vector2 size)
        {
            if (img == null) return;
            if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);
            var rt = img.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
        }

        private void SetFullDim()
        {
            if (_dimLayer != null && !_dimLayer.gameObject.activeSelf) _dimLayer.gameObject.SetActive(true);
            if (_dimBorders != null)
                foreach (var b in _dimBorders)
                    if (b != null && b.gameObject.activeSelf) b.gameObject.SetActive(false);
        }

        // 3 quick reveal blinks: dim closes over the target then opens, ×3, settling open.
        private IEnumerator BlinkHole()
        {
            for (int i = 0; i < 3; i++)
            {
                _holeScale = 0f;                                  // dim closed over target
                yield return new WaitForSeconds(0.06f);
                const float d = 0.09f;
                for (float t = 0f; t < d; t += Time.deltaTime) { _holeScale = Mathf.Clamp01(t / d); yield return null; }
                _holeScale = 1f;                                  // fully open
                yield return new WaitForSeconds(0.08f);
            }
            _holeScale = 1f;
        }

        // World transform → screen pos + spotlight size (canvas units). Uses CellView bounds (legacy),
        // else Collider2D/Renderer bounds (lanes), else a default size around the transform origin.
        private bool TryWorldTargetScreen(Transform t, RectTransform overlayRt, out Vector2 screenPos, out Vector2 sizeCanvas)
        {
            screenPos = default; sizeCanvas = default;
            var cam = Camera.main;
            if (cam == null) return false;

            Vector3 center; Vector3 extents;
            var cellView = t.GetComponent<CellView>();
            if (cellView != null) { center = cellView.GetWorldCenter(); extents = cellView.GetScreenBounds().extents; }
            else
            {
                var col = t.GetComponentInChildren<Collider2D>();
                if (col != null) { center = col.bounds.center; extents = col.bounds.extents; }
                else
                {
                    // Encapsulate ALL child renderers so a multi-part target (e.g. the whole Signal Panel
                    // = many node renderers) is fully covered, not just its first child.
                    var rends = t.GetComponentsInChildren<Renderer>();
                    if (rends.Length > 0)
                    {
                        Bounds b = rends[0].bounds;
                        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                        center = b.center; extents = b.extents;
                    }
                    else { center = t.position; extents = Vector3.zero; }
                }
            }

            Vector3 vpC = cam.WorldToViewportPoint(center);
            Vector3 vpR = cam.WorldToViewportPoint(center + new Vector3(extents.x, 0, 0));
            Vector3 vpT = cam.WorldToViewportPoint(center + new Vector3(0, extents.y, 0));

            float w = Mathf.Abs(vpR.x - vpC.x) * 2f * overlayRt.rect.width;
            float h = Mathf.Abs(vpT.y - vpC.y) * 2f * overlayRt.rect.height;
            sizeCanvas = new Vector2(Mathf.Max(w, 90f), Mathf.Max(h, 90f)); // floor so a lane spotlight stays visible
            screenPos = new Vector2(vpC.x * Screen.width, vpC.y * Screen.height);
            return true;
        }

        private bool TryGetDestLocal(RectTransform overlayRt, Canvas canvas, out Vector2 local)
        {
            local = default;
            Vector2 screen;
            if (_currentTargetSpace == TargetSpaceType.World && _currentTargetToTransform != null)
            {
                if (!TryWorldTargetScreen(_currentTargetToTransform, overlayRt, out screen, out _)) return false;
            }
            else if (_currentTargetToRT != null)
            {
                screen = RectTransformUtility.WorldToScreenPoint(null, _currentTargetToRT.position);
            }
            else return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRt, screen, canvas.worldCamera, out local);
            return true;
        }

        private void OnFullscreenTapped()
        {
            if (_sequencer != null && _sequencer.IsActive)
            {
                _sequencer.Next();
            }
        }

        private IEnumerator AnimateBubbleAppear()
        {
            float duration = 0.2f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                float scale = EaseOutBack(p);
                _tooltipBubble.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            _tooltipBubble.localScale = Vector3.one;
        }

        private IEnumerator AnimateFingerTap()
        {
            while (true)
            {
                // Idle
                yield return new WaitForSeconds(1.1f);

                // Press down
                const float pressDur = 0.1f;
                for (float t = 0f; t < pressDur; t += Time.deltaTime)
                {
                    float s = Mathf.Lerp(1f, 0.72f, t / pressDur);
                    _fingerOverlay.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }

                yield return new WaitForSeconds(0.06f);

                // Release with EaseOutBack overshoot
                const float releaseDur = 0.22f;
                for (float t = 0f; t < releaseDur; t += Time.deltaTime)
                {
                    float s = Mathf.Lerp(0.72f, 1f, EaseOutBack(t / releaseDur));
                    _fingerOverlay.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                _fingerOverlay.localScale = Vector3.one;
            }
        }

        // Drag pointer slides from the source lane to the destination lane, loops. Endpoints are
        // refreshed every frame by UpdateSpotlightPosition (responsive to board layout).
        private IEnumerator AnimateDragPointer()
        {
            while (true)
            {
                if (!_dragEndpointsValid) { yield return null; continue; }
                Vector2 from = _dragFromLocal, to = _dragToLocal;
                const float dur = 0.95f;
                for (float t = 0f; t < dur; t += Time.deltaTime)
                {
                    float p = Mathf.Clamp01(t / dur);
                    _fingerOverlay.anchoredPosition = Vector2.Lerp(from, to, p);
                    float s = 0.9f + 0.1f * Mathf.Sin(p * Mathf.PI); // gentle swell mid-path
                    _fingerOverlay.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                _fingerOverlay.anchoredPosition = to;
                yield return new WaitForSeconds(0.3f);
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static bool ParseCellTarget(string targetId, out int row, out int col)
        {
            row = -1;
            col = -1;
            try
            {
                int rStart = targetId.IndexOf('[');
                int rEnd = targetId.IndexOf(']');
                int cStart = targetId.IndexOf('[', rEnd + 1);
                int cEnd = targetId.IndexOf(']', cStart + 1);
                
                if (rStart >= 0 && rEnd > rStart && cStart > rEnd && cEnd > cStart)
                {
                    string rStr = targetId.Substring(rStart + 1, rEnd - rStart - 1);
                    string cStr = targetId.Substring(cStart + 1, cEnd - cStart - 1);
                    if (int.TryParse(rStr, out row) && int.TryParse(cStr, out col))
                    {
                        // CSV uses 1-based indexing; convert to 0-based for board array
                        row -= 1;
                        col -= 1;
                        return true;
                    }
                }
            }
            catch {}
            return false;
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
