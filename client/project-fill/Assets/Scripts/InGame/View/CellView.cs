using UnityEngine;

namespace Game.InGame.View
{
    public class CellView : MonoBehaviour
    {
        public void SetTargetHighlight(bool active) { }
        public Vector3 GetWorldCenter() => transform.position;
        public Bounds GetScreenBounds() => new Bounds(transform.position, Vector3.zero);
    }
}
