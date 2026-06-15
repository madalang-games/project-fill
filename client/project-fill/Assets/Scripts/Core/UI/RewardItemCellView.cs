using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Core.UI
{
    public class RewardItemCellView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Sprite   _icon;
        private string   _nameKey;
        private string   _descKey;
        private Coroutine _longPressCoroutine;

        private const float LongPressDuration = 0.4f;

        public void Init(Sprite icon, string nameKey, string descKey)
        {
            _icon    = icon;
            _nameKey = nameKey;
            _descKey = descKey;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_nameKey) && string.IsNullOrEmpty(_descKey)) return;
            _longPressCoroutine = StartCoroutine(LongPressTimer());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelLongPress();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelLongPress();
        }

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
                v.Init(_icon, _nameKey, _descKey, transform.position));
        }
    }
}
