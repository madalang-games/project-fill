using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public struct RewardItem
    {
        public Sprite Icon;
        public int    Quantity;
        public string Label;
        public string NameKey;
        public string DescKey;
        // Optional: when set, the row's Icon Image is rendered by this delegate instead of a flat
        // sprite (e.g. cosmetics have no sprite — RewardDisplay injects CosmeticPreview.Build here).
        // Keeps Core.UI free of any OutGame dependency.
        public Action<Image> CustomRender;
    }

    public class RewardPopupView : MonoBehaviour
    {
        [SerializeField] private Transform  _itemContainer;
        [SerializeField] private GameObject _itemRowPrefab;
        [SerializeField] private Button     _okButton;
        [SerializeField] private Button     _closeButton;

        private const float ItemDelay    = 0.15f;
        private const float ItemDuration = 0.25f;
        private const float OvershootScale = 1.2f;

        private void Awake()
        {
            _okButton.onClick.AddListener(Close);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        }

        public void Init(IReadOnlyList<RewardItem> rewards)
        {
            StartCoroutine(SpawnRewards(rewards));
        }

        private IEnumerator SpawnRewards(IReadOnlyList<RewardItem> rewards)
        {
            int count = Mathf.Min(rewards.Count, 4);
            for (int i = 0; i < count; i++)
            {
                var row = Instantiate(_itemRowPrefab, _itemContainer);
                row.transform.localScale = Vector3.zero;

                var icon = row.transform.Find("Icon")?.GetComponent<Image>();
                var qty  = row.transform.Find("Quantity")?.GetComponent<TMP_Text>();
                if (icon != null)
                {
                    if (rewards[i].CustomRender != null) rewards[i].CustomRender(icon);
                    else icon.sprite = rewards[i].Icon;
                }
                if (qty  != null) qty.text    = $"× {rewards[i].Quantity}";

                var cellView = row.GetComponent<RewardItemCellView>();
                cellView?.Init(rewards[i].Icon, rewards[i].NameKey, rewards[i].DescKey);

                yield return new WaitForSeconds(ItemDelay);
                yield return PopItem(row.GetComponent<RectTransform>());
            }
        }

        private IEnumerator PopItem(RectTransform rt)
        {
            float elapsed = 0f;
            while (elapsed < ItemDuration)
            {
                elapsed += Time.deltaTime;
                float t = UIEasing.EaseOutBack(Mathf.Clamp01(elapsed / ItemDuration));
                rt.localScale = Vector3.one * Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            rt.localScale = Vector3.one;
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
