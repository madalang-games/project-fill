using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public class ScrollRectForwarder : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ScrollRect _sr;

        private void Awake() => _sr = GetComponentInParent<ScrollRect>();

        public void OnInitializePotentialDrag(PointerEventData e) => _sr?.OnInitializePotentialDrag(e);
        public void OnBeginDrag(PointerEventData e)                => _sr?.OnBeginDrag(e);
        public void OnDrag(PointerEventData e)                     => _sr?.OnDrag(e);
        public void OnEndDrag(PointerEventData e)                  => _sr?.OnEndDrag(e);
    }
}
