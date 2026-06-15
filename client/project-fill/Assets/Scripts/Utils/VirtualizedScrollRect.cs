using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Utils
{
    [RequireComponent(typeof(ScrollRect))]
    public class VirtualizedScrollRect : MonoBehaviour
    {
        [SerializeField] private RectTransform _itemPrefab;
        [SerializeField] private float _itemHeight = 80f;
        [SerializeField] private float _spacing = 5f;
        [SerializeField] private int _bufferCount = 2;

        private ScrollRect _scrollRect;
        private RectTransform _content;
        private int _totalItemsCount;
        private Action<int, GameObject> _onBindItem;

        private readonly List<RectTransform> _activeItems = new List<RectTransform>();
        private readonly Queue<RectTransform> _pool = new Queue<RectTransform>();

        private int _prevStartIdx = -1;
        private int _prevEndIdx = -1;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _content = _scrollRect.content;
            _scrollRect.onValueChanged.AddListener(OnScroll);
            if (_itemPrefab != null)
            {
                _itemPrefab.gameObject.SetActive(false);
            }
        }

        public void SetItemHeight(float height, float spacing)
        {
            _itemHeight = height;
            _spacing = spacing;
        }

        public void Init(int totalCount, Action<int, GameObject> onBindItem)
        {
            _totalItemsCount = totalCount;
            _onBindItem = onBindItem;

            ClearActiveItems();

            _prevStartIdx = -1;
            _prevEndIdx = -1;

            float totalHeight = totalCount * _itemHeight + Mathf.Max(0, totalCount - 1) * _spacing;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0);

            UpdateVisibleItems(true);
        }

        private void OnScroll(Vector2 normalizedPos)
        {
            UpdateVisibleItems(false);
        }

        private void UpdateVisibleItems(bool forceRefresh)
        {
            if (_totalItemsCount == 0 || _itemPrefab == null) return;

            float viewHeight = _scrollRect.viewport != null ? _scrollRect.viewport.rect.height : ((RectTransform)transform).rect.height;
            float scrollPos = _content.anchoredPosition.y;

            float itemStep = _itemHeight + _spacing;
            int startIdx = Mathf.FloorToInt(scrollPos / itemStep) - _bufferCount;
            int endIdx = Mathf.CeilToInt((scrollPos + viewHeight) / itemStep) + _bufferCount;

            startIdx = Mathf.Clamp(startIdx, 0, _totalItemsCount - 1);
            endIdx = Mathf.Clamp(endIdx, 0, _totalItemsCount - 1);

            if (!forceRefresh && startIdx == _prevStartIdx && endIdx == _prevEndIdx)
            {
                return;
            }

            _prevStartIdx = startIdx;
            _prevEndIdx = endIdx;

            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                var item = _activeItems[i];
                int idx = GetItemIndex(item);
                if (idx < startIdx || idx > endIdx || idx >= _totalItemsCount)
                {
                    RecycleItem(item);
                    _activeItems.RemoveAt(i);
                }
            }

            var activeIndices = new HashSet<int>();
            foreach (var item in _activeItems)
            {
                activeIndices.Add(GetItemIndex(item));
            }

            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                if (activeIndices.Contains(idx)) continue;

                var item = GetOrCreateItem();
                SetItemIndex(item, idx);
                PositionItem(item, idx);
                _activeItems.Add(item);
                _onBindItem?.Invoke(idx, item.gameObject);
            }
        }

        private RectTransform GetOrCreateItem()
        {
            RectTransform item;
            if (_pool.Count > 0)
            {
                item = _pool.Dequeue();
                item.gameObject.SetActive(true);
            }
            else
            {
                item = Instantiate(_itemPrefab, _content);
                item.gameObject.SetActive(true);
            }
            return item;
        }

        private void RecycleItem(RectTransform item)
        {
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }

        private void ClearActiveItems()
        {
            foreach (var item in _activeItems)
            {
                RecycleItem(item);
            }
            _activeItems.Clear();
        }

        private void PositionItem(RectTransform item, int index)
        {
            float yPos = -index * (_itemHeight + _spacing);
            item.anchoredPosition = new Vector2(0f, yPos);
        }

        private int GetItemIndex(RectTransform item)
        {
            var meta = item.GetComponent<VirtualizedItemMetadata>();
            return meta != null ? meta.Index : -1;
        }

        private void SetItemIndex(RectTransform item, int index)
        {
            var meta = item.GetComponent<VirtualizedItemMetadata>();
            if (meta == null)
            {
                meta = item.gameObject.AddComponent<VirtualizedItemMetadata>();
            }
            meta.Index = index;
        }
    }

    public class VirtualizedItemMetadata : MonoBehaviour
    {
        public int Index { get; set; } = -1;
    }
}
