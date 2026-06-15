using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineStrip : MaskableGraphic
    {
        public float lineWidth = 12f;
        public float textureTiling = 1f;
        public float scrollSpeed = 0f;
        
        [Header("Outline Settings")]
        public bool useOutline = true;
        public float outlineWidth = 4f;
        public Color outlineColor = new Color(0f, 0f, 0f, 0.6f);

        [Header("Procedural Settings")]
        public bool useProceduralDashes = false;
        public float dashLength = 30f;
        public float gapLength = 15f;

        private readonly List<Vector2> _pts = new List<Vector2>();
        private float _uvOffset = 0f;
        private Texture _customTexture;

        public override Texture mainTexture
        {
            get
            {
                if (_customTexture != null)
                    return _customTexture;
                return base.mainTexture;
            }
        }

        public void SetTexture(Texture tex)
        {
            _customTexture = tex;
            SetMaterialDirty();
            SetVerticesDirty();
        }

        public void SetPoints(IList<Vector2> pts)
        {
            _pts.Clear();
            foreach (var p in pts) _pts.Add(p);
            SetVerticesDirty();
        }

        protected virtual void Update()
        {
            if (scrollSpeed != 0f && Application.isPlaying)
            {
                _uvOffset += scrollSpeed * Time.deltaTime;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_pts.Count < 2) return;

            // 1. Calculate cumulative lengths for correct UV tiling
            float[] lengths = new float[_pts.Count];
            float totalLength = 0f;
            lengths[0] = 0f;
            for (int i = 1; i < _pts.Count; i++)
            {
                totalLength += Vector2.Distance(_pts[i - 1], _pts[i]);
                lengths[i] = totalLength;
            }

            if (totalLength <= 0f) return;

            // 2. Draw outline if requested
            if (useOutline)
            {
                BuildLineGeometry(vh, lengths, totalLength, lineWidth + outlineWidth * 2f, outlineColor, true);
            }

            // 3. Draw main line
            BuildLineGeometry(vh, lengths, totalLength, lineWidth, color, false);
        }

        private void BuildLineGeometry(VertexHelper vh, float[] lengths, float totalLength, float width, Color drawColor, bool isOutline)
        {
            float hw = width * 0.5f;
            
            // Build geometry by segments
            for (int i = 0; i < _pts.Count - 1; i++)
            {
                Vector2 a = _pts[i];
                Vector2 b = _pts[i + 1];
                Vector2 d = (b - a).normalized;
                Vector2 n = new Vector2(-d.y, d.x) * hw;

                float uStart = (lengths[i] / totalLength) * textureTiling - _uvOffset;
                float uEnd   = (lengths[i + 1] / totalLength) * textureTiling - _uvOffset;

                int k = vh.currentVertCount;

                Color cA = drawColor;
                Color cB = drawColor;

                // Procedural dash effect (applied to main line only when no custom texture)
                if (!isOutline && useProceduralDashes && (_customTexture == null || _customTexture == Texture2D.whiteTexture))
                {
                    float distA = lengths[i];
                    float distB = lengths[i + 1];
                    float cycle = dashLength + gapLength;
                    if (cycle > 0f)
                    {
                        // Match visual position considering UV offset speed
                        float offsetPos = _uvOffset * totalLength / textureTiling;
                        float modA = (distA - offsetPos) % cycle;
                        if (modA < 0) modA += cycle;
                        float modB = (distB - offsetPos) % cycle;
                        if (modB < 0) modB += cycle;

                        if (modA > dashLength) cA.a *= 0.15f;
                        if (modB > dashLength) cB.a *= 0.15f;
                    }
                }

                // Vertical textures (chain, water strip): V tiles along path, U spans width.
                // Horizontal-oriented fallback: U tiles along path, V spans width.
                bool verticalTex = _customTexture != null;
                if (verticalTex)
                {
                    vh.AddVert(a - n, cA, new Vector2(0f, uStart));
                    vh.AddVert(a + n, cA, new Vector2(1f, uStart));
                    vh.AddVert(b + n, cB, new Vector2(1f, uEnd));
                    vh.AddVert(b - n, cB, new Vector2(0f, uEnd));
                }
                else
                {
                    vh.AddVert(a - n, cA, new Vector2(uStart, 0f));
                    vh.AddVert(a + n, cA, new Vector2(uStart, 1f));
                    vh.AddVert(b + n, cB, new Vector2(uEnd, 1f));
                    vh.AddVert(b - n, cB, new Vector2(uEnd, 0f));
                }

                vh.AddTriangle(k,     k + 1, k + 2);
                vh.AddTriangle(k,     k + 2, k + 3);
            }
        }
    }
}
