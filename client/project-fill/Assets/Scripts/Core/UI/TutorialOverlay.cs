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
        private Coroutine _autoAdvanceCoroutine;
        private Coroutine _pulseCoroutine;
        private Coroutine _fingerTapCoroutine;
        private Transform _currentTargetTransform;
        private RectTransform _currentTargetRT;
        private TargetSpaceType _currentTargetSpace;
        private CellView _currentTargetCellView;
        private Vector2 _designedResolution = new Vector2(1080, 1920);

        public void Init(TutorialStepSequencer sequencer)
        {
            _sequencer = sequencer;
            _sequencer.OnStepChanged += ShowStep;
            _sequencer.OnComplete += Close;

            if (_fullscreenDismissButton != null)
            {
                _fullscreenDismissButton.onClick.AddListener(OnFullscreenTapped);
            }

            if (_spotlightGlow != null)
            {
                _pulseCoroutine = StartCoroutine(AnimateGlowPulse());
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

            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
            if (_fingerTapCoroutine != null) StopCoroutine(_fingerTapCoroutine);
        }

        private void LateUpdate()
        {
            // Responsive update in case of layout shifts or board animations
            UpdateSpotlightPosition();
        }

        private void ShowStep(TutorialStep step)
        {
            if (step == null) return;

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            if (_fingerTapCoroutine != null)
            {
                StopCoroutine(_fingerTapCoroutine);
                _fingerTapCoroutine = null;
            }

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

            // Fullscreen dismiss button handles tap-to-advance for non-blocking steps only.
            // DimLayer (_dimLayer) stays active for all steps.
            if (_fullscreenDismissButton != null)
            {
                _fullscreenDismissButton.gameObject.SetActive(!step.is_blocking);
            }

            // Start finger tap animation for FingerOverlay steps
            if (_fingerOverlay != null && _fingerOverlay.gameObject.activeSelf)
            {
                _fingerOverlay.localScale = Vector3.one;
                _fingerTapCoroutine = StartCoroutine(AnimateFingerTap());
            }

            // Trigger bubble anim (Scale up)
            if (_tooltipBubble != null)
            {
                _tooltipBubble.localScale = Vector3.zero;
                StartCoroutine(AnimateBubbleAppear());
            }

            // Set up auto-advance timer if specified
            if (step.auto_advance_sec > 0.01f)
            {
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceTimer(step.auto_advance_sec));
            }

            // Instantly evaluate position for this frame
            UpdateSpotlightPosition();
        }

        private void ResolveTarget(TutorialStep step)
        {
            _currentTargetTransform = null;
            _currentTargetRT = null;
            _currentTargetSpace = step.target_space;

            if (string.IsNullOrEmpty(step.target_ui_id))
            {
                _spotlightCutout.gameObject.SetActive(false);
                _fingerOverlay.gameObject.SetActive(false);
                return;
            }

            _spotlightCutout.gameObject.SetActive(true);
            _fingerOverlay.gameObject.SetActive(step.content_type == TutorialContentType.FingerOverlay);

            // Priority 1: TutorialTarget component registry — id-based, no name coupling
            var tutTarget = TutorialTarget.Find(step.target_ui_id);
            if (tutTarget != null)
            {
                var rt = tutTarget.GetComponent<RectTransform>();
                if (rt != null && step.target_space == TargetSpaceType.UI)
                    _currentTargetRT = rt;
                else
                    _currentTargetTransform = tutTarget.transform;
                return;
            }

            // Priority 2: board_cell_[r][c] — requires live BoardView
            if (step.target_space == TargetSpaceType.World && step.target_ui_id.StartsWith("board_cell_"))
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

            // Priority 3: dynamic board cell searches (protector/core/obstacle)
            if (step.target_space == TargetSpaceType.World)
            {
                var boardView = FindObjectOfType<BoardView>();
                if (boardView != null)
                {
                    _currentTargetTransform = step.target_ui_id switch
                    {
                        "board_protector_cell" => FindCellWithProtector(boardView),
                        "board_core_cell"      => FindCellWithCore(boardView),
                        "board_obstacle_cell"  => FindCellWithObstacle(boardView),
                        _                      => null
                    };
                }
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

            var step = _sequencer.CurrentStep;
            Canvas overlayCanvas = GetComponentInParent<Canvas>();
            if (overlayCanvas == null) return;

            RectTransform overlayRt = overlayCanvas.GetComponent<RectTransform>();
            Vector2 screenPos = Vector2.zero;
            Vector2 targetSize = new Vector2(150, 150); // fallback size

            if (_currentTargetSpace == TargetSpaceType.World && _currentTargetTransform != null)
            {
                var cellView = _currentTargetTransform.GetComponent<CellView>();
                Vector3 worldCenter = _currentTargetTransform.position;
                if (cellView != null) worldCenter = cellView.GetWorldCenter();

                if (cellView != null)
                {
                    // Use viewport fractions → canvas rect units (matches project-link approach).
                    // WorldToScreenPoint gives screen pixels; sizeDelta needs canvas local units.
                    // Viewport space is resolution-independent and avoids scaleFactor mismatch.
                    Bounds wb = cellView.GetScreenBounds();
                    Vector3 vpCenter = Camera.main.WorldToViewportPoint(worldCenter);
                    Vector3 vpRight  = Camera.main.WorldToViewportPoint(worldCenter + new Vector3(wb.extents.x, 0, 0));
                    Vector3 vpTop    = Camera.main.WorldToViewportPoint(worldCenter + new Vector3(0, wb.extents.y, 0));

                    float canvasW = overlayRt.rect.width;
                    float canvasH = overlayRt.rect.height;
                    float w = Mathf.Abs(vpRight.x - vpCenter.x) * 2f * canvasW;
                    float h = Mathf.Abs(vpTop.y  - vpCenter.y) * 2f * canvasH;
                    targetSize = new Vector2(w, h);

                    screenPos = new Vector2(vpCenter.x * Screen.width, vpCenter.y * Screen.height);
                }
                else
                {
                    // board_area or other whole-board targets: hide spotlight, show tooltip only
                    _spotlightCutout.gameObject.SetActive(false);
                    _fingerOverlay.gameObject.SetActive(false);
                    return;
                }
            }
            else if (_currentTargetSpace == TargetSpaceType.UI && _currentTargetRT != null)
            {
                screenPos = RectTransformUtility.WorldToScreenPoint(null, _currentTargetRT.position);
                targetSize = _currentTargetRT.rect.size * _currentTargetRT.lossyScale / overlayCanvas.scaleFactor;
            }
            else
            {
                // No target - hide or spotlight middle
                _spotlightCutout.gameObject.SetActive(false);
                _fingerOverlay.gameObject.SetActive(false);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRt,
                screenPos,
                overlayCanvas.worldCamera,
                out Vector2 localPoint
            );

            _spotlightCutout.anchoredPosition = localPoint;
            _spotlightCutout.sizeDelta = targetSize + new Vector2(4f, 4f);

            // Position finger indicator relative to target center
            if (_fingerOverlay.gameObject.activeSelf)
            {
                _fingerOverlay.anchoredPosition = localPoint + new Vector2(40f, -40f);
            }

            // Position tooltip bubble nearby target
            if (_tooltipBubble != null)
            {
                // Place bubble above target if target is low, below target if target is high
                float halfHeight = overlayRt.rect.height * 0.5f;
                float verticalOffset = localPoint.y > 0 ? -280f : 280f;
                _tooltipBubble.anchoredPosition = new Vector2(0f, Mathf.Clamp(localPoint.y + verticalOffset, -halfHeight + 200f, halfHeight - 200f));
            }
        }

        private void OnFullscreenTapped()
        {
            if (_sequencer != null && _sequencer.IsActive)
            {
                var step = _sequencer.CurrentStep;
                if (step != null && !step.is_blocking)
                {
                    _sequencer.Next();
                }
            }
        }

        private IEnumerator AutoAdvanceTimer(float seconds)
        {
            yield return new WaitForSeconds(seconds);
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

        private IEnumerator AnimateGlowPulse()
        {
            while (true)
            {
                float t = Time.time * 1.2f;
                float alpha = 0.08f + 0.08f * Mathf.Sin(t); // 0.0–0.16: barely visible border
                if (_spotlightGlow != null)
                {
                    Color color = _spotlightGlow.color;
                    color.a = alpha;
                    _spotlightGlow.color = color;
                }
                yield return null;
            }
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
