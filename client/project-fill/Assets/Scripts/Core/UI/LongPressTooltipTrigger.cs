using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Core.UI
{
    // Generic long-press (0.4s) → ItemTooltipView trigger. Decoupled from rewards/items:
    // attach to any icon GO and call SetTooltip(nameKey, descKey). The tooltip sprite is read
    // from the assigned _icon Image. RewardItemCellView keeps its own copy of this pattern.
    public class LongPressTooltipTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image _icon;

        private string    _nameKey;
        private string    _descKey;
        private Coroutine _longPressCoroutine;

        private const float LongPressDuration = 0.4f;

        public void SetTooltip(string nameKey, string descKey)
        {
            _nameKey = nameKey;
            _descKey = descKey;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_nameKey) && string.IsNullOrEmpty(_descKey)) return;
            _longPressCoroutine = StartCoroutine(LongPressTimer());
        }

        public void OnPointerUp(PointerEventData eventData) => CancelLongPress();
        public void OnPointerExit(PointerEventData eventData) => CancelLongPress();

        private void CancelLongPress()
        {
            if (_longPressCoroutine == null) return;
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
        }

        private IEnumerator LongPressTimer()
        {
            yield return new WaitForSeconds(LongPressDuration);
            _longPressCoroutine = null;
            UIManager.Instance?.ShowPopup<ItemTooltipView>(v =>
                v.Init(_icon != null ? _icon.sprite : null, _nameKey, _descKey, transform.position));
        }
    }
}
