using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _cancelLabel;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private Button   _cancelButton;
        [SerializeField] private Button   _confirmButton;
        [SerializeField] private Button   _backdropButton;
        [SerializeField] private Image    _confirmButtonImage;
        [SerializeField] private RectTransform _rewardCellContainer;
        [SerializeField] private GameObject    _rewardItemCellPrefab;
        [SerializeField] private GameObject    _rewardPanel;

        private Action _onConfirm;
        private Action _onCancel;

        private static readonly Color DangerColor = new Color(0.91f, 0.25f, 0.25f);

        public void Init(string title, string body, string confirmLabel,
                         Action onConfirm, Action onCancel = null,
                         string cancelLabel = "Cancel", bool danger = false,
                         System.Collections.Generic.List<(string iconKey, string qtyText, string nameKey, string descKey)> rewardItems = null)
        {
            _titleText.text   = title;
            _bodyText.text    = body ?? string.Empty;
            _bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));
            _cancelLabel.text  = cancelLabel;
            _confirmLabel.text = confirmLabel;

            if (danger && _confirmButtonImage != null)
                _confirmButtonImage.color = DangerColor;

            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(OnCancel);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnCancel);

            // Render Reward Cells if container is available
            if (_rewardCellContainer != null)
            {
                foreach (Transform child in _rewardCellContainer)
                {
                    Destroy(child.gameObject);
                }

                if (rewardItems != null && rewardItems.Count > 0 && _rewardItemCellPrefab != null)
                {
                    if (_rewardPanel != null) _rewardPanel.SetActive(true);
                    _rewardCellContainer.gameObject.SetActive(true);
                    foreach (var item in rewardItems)
                    {
                        var cellGo = Instantiate(_rewardItemCellPrefab, _rewardCellContainer);
                        cellGo.SetActive(true);

                        Sprite cellSprite = null;
                        var iconTrans = cellGo.transform.Find("Icon");
                        if (iconTrans != null && iconTrans.TryGetComponent<Image>(out var img))
                        {
                            if (Game.Services.DynamicResourceService.Instance != null)
                            {
                                cellSprite = Game.Services.DynamicResourceService.Instance.GetSprite(item.iconKey);
                                img.sprite = cellSprite;
                                img.preserveAspect = true;
                            }
                        }

                        var qtyTrans = cellGo.transform.Find("Quantity");
                        if (qtyTrans != null && qtyTrans.TryGetComponent<TMP_Text>(out var txt))
                            txt.text = item.qtyText;

                        var cellView = cellGo.GetComponent<RewardItemCellView>();
                        cellView?.Init(cellSprite, item.nameKey, item.descKey);
                    }
                }
                else
                {
                    if (_rewardPanel != null) _rewardPanel.SetActive(false);
                    _rewardCellContainer.gameObject.SetActive(false);
                }
            }
        }

        private void OnConfirm()
        {
            _onConfirm?.Invoke();
            Close();
        }

        private void OnCancel()
        {
            _onCancel?.Invoke();
            Close();
        }

        private void Close()
        {
            var appear = GetComponent<UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
