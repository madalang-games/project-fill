using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Services;

namespace Game.Core.UI
{
    public class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private Image         _icon;
        [SerializeField] private TMP_Text      _titleText;
        [SerializeField] private TMP_Text      _descText;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Button        _backdropButton;

        private void Awake()
        {
            if (_backdropButton != null)
                _backdropButton.onClick.AddListener(Close);
        }

        public void Init(Sprite icon, string nameKey, string descKey, Vector3 cellScreenPos)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.preserveAspect = true;
                _icon.gameObject.SetActive(icon != null);
            }

            SetLocalizedText(_titleText, nameKey);
            SetLocalizedText(_descText, descKey);

            PositionPanel(cellScreenPos);
        }

        private void SetLocalizedText(TMP_Text tmp, string key)
        {
            if (tmp == null || string.IsNullOrEmpty(key)) return;
            tmp.text = LocalizationService.Instance?.Get(key) ?? key;
        }

        private void PositionPanel(Vector3 cellScreenPos)
        {
            if (_panel == null) return;

            var parentRT = _panel.parent as RectTransform;
            if (parentRT == null) return;

            var canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera : null;

            Vector2 screenPos = new Vector2(cellScreenPos.x, cellScreenPos.y);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, screenPos, cam, out Vector2 localPos);

            // Default: show above the cell
            float halfH = _panel.sizeDelta.y * 0.5f;
            float halfW = _panel.sizeDelta.x * 0.5f;
            localPos.y += halfH + 20f;

            // Clamp so the panel stays within the visible canvas area
            Rect bounds = parentRT.rect;
            localPos.x = Mathf.Clamp(localPos.x, bounds.xMin + halfW + 10f, bounds.xMax - halfW - 10f);
            localPos.y = Mathf.Clamp(localPos.y, bounds.yMin + halfH + 10f, bounds.yMax - halfH - 10f);

            _panel.anchoredPosition = localPos;
        }

        private void Close()
        {
            var appear = GetComponent<UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => UIManager.Instance?.CloseTopPopup());
            else
                UIManager.Instance?.CloseTopPopup();
        }
    }
}
