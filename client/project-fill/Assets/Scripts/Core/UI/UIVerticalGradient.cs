using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIVerticalGradient : MaskableGraphic
    {
        [SerializeField] private Color _topColor    = Color.white;
        [SerializeField] private Color _bottomColor = Color.black;

        private (float t, Color color)[] _stops; // t: 0=bottom, 1=top

        public void SetColors(Color top, Color bottom)
        {
            _stops        = null;
            _topColor    = top;
            _bottomColor = bottom;
            SetVerticesDirty();
        }

        // stops must be sorted by t ascending (0=bottom → 1=top)
        public void SetStops(params (float t, Color color)[] stops)
        {
            _stops = stops;
            System.Array.Sort(_stops, (a, b) => a.t.CompareTo(b.t));
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();

            if (_stops != null && _stops.Length >= 2)
            {
                for (int i = 0; i < _stops.Length - 1; i++)
                {
                    float y0 = Mathf.Lerp(r.yMin, r.yMax, _stops[i].t);
                    float y1 = Mathf.Lerp(r.yMin, r.yMax, _stops[i + 1].t);
                    Color c0 = _stops[i].color;
                    Color c1 = _stops[i + 1].color;
                    int   k  = vh.currentVertCount;
                    vh.AddVert(new Vector3(r.xMin, y1), c1, Vector2.up);
                    vh.AddVert(new Vector3(r.xMax, y1), c1, Vector2.one);
                    vh.AddVert(new Vector3(r.xMax, y0), c0, Vector2.right);
                    vh.AddVert(new Vector3(r.xMin, y0), c0, Vector2.zero);
                    vh.AddTriangle(k, k + 1, k + 2);
                    vh.AddTriangle(k, k + 2, k + 3);
                }
            }
            else
            {
                vh.AddVert(new Vector3(r.xMin, r.yMax), _topColor,    new Vector2(0, 1));
                vh.AddVert(new Vector3(r.xMax, r.yMax), _topColor,    new Vector2(1, 1));
                vh.AddVert(new Vector3(r.xMax, r.yMin), _bottomColor, new Vector2(1, 0));
                vh.AddVert(new Vector3(r.xMin, r.yMin), _bottomColor, new Vector2(0, 0));
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(0, 2, 3);
            }
        }
    }
}
