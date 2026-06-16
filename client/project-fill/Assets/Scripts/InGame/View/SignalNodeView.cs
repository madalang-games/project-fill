using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.InGame.View
{
    // One Signal Panel node: a glowing pad that lights when its set is registered.
    // Thin prefab (GO + this component); body is procedural so art swaps via BoardSkin/SpriteSet.
    public class SignalNodeView : MonoBehaviour
    {
        private SpriteRenderer _glow, _body, _ring, _connector;
        private TextMeshPro _glyph;
        private bool _built;

        public SignalType Type { get; private set; }
        public bool Lit { get; private set; }

        public SignalNodeView Initialize(SpriteSet sprites, SignalType type, bool showConnector, Vector2 size)
        {
            Type = type;
            if (!_built) Construct(sprites, showConnector, size);
            return this;
        }

        private void Construct(SpriteSet s, bool showConnector, Vector2 size)
        {
            _built = true;

            float connW = size.x * 0.15f;
            float connH = size.y * 0.08f;
            _connector = WorldUtil.CreateSprite(transform, "Connector", null, new Color(0.2f, 0.22f, 0.3f, 1f), new Vector2(connW, connH), sliced: false, sortingOrder: 2);
            _connector.transform.localPosition = new Vector3(-size.x * 0.5f - connW * 0.5f, 0f, 0f);
            _connector.enabled = showConnector;

            _glow = WorldUtil.CreateSprite(transform, "Glow", s.Glow, new Color(1, 1, 1, 0f), size + new Vector2(0.12f, 0.12f), sortingOrder: 3);
            
            // Circular disc body + concentric ring
            _body = WorldUtil.CreateSprite(transform, "Body", s.Disc, new Color(0.16f, 0.17f, 0.26f), size, sliced: false, sortingOrder: 4);
            _ring = WorldUtil.CreateSprite(transform, "Ring", s.Ring, new Color(0.3f, 0.34f, 0.44f), size, sliced: false, sortingOrder: 5);

            _glyph = WorldUtil.CreateLabel(transform, "Glyph", Type.ToLabel(), 24f, size);
            _glyph.color = new Color(0.45f, 0.5f, 0.62f);
            _glyph.fontStyle = FontStyles.Bold;
            _glyph.sortingOrder = 6;
        }

        public void SetState(bool lit, bool pending)
        {
            Lit = lit;
            if (lit)
            {
                var c = Type.ToColor();
                _body.color  = Color.Lerp(c, Color.white, 0.15f); // glowing bulb core
                _ring.color  = Color.Lerp(c, Color.white, 0.6f);  // white-hot rim
                _glyph.color = new Color(0, 0, 0, 0.75f);
                if (_connector.enabled) _connector.color = Color.Lerp(c, Color.white, 0.2f);
            }
            else
            {
                var inactiveCol = pending ? new Color(0.30f, 0.24f, 0.08f) : new Color(0.16f, 0.17f, 0.26f);
                _body.color  = inactiveCol;
                _ring.color  = pending ? new Color(0.8f, 0.6f, 0.2f) : new Color(0.3f, 0.34f, 0.44f);
                _glyph.color = pending ? new Color(0.85f, 0.7f, 0.4f) : new Color(0.45f, 0.5f, 0.62f);
                if (_connector.enabled) _connector.color = new Color(0.2f, 0.22f, 0.3f);
            }
        }

        public Vector3 WorldPos => _body != null ? _body.transform.position : transform.position;

        public void PlayRegister() => StartCoroutine(PopRoutine());

        private IEnumerator PopRoutine()
        {
            var c = Type.ToColor();
            float dur = 0.45f, t = 0f;
            var brt = _body.transform;
            var rrt = _ring.transform;
            while (t < dur)
            {
                float p = t / dur;
                float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.35f;
                brt.localScale = Vector3.one * s;
                rrt.localScale = Vector3.one * s;
                _glow.color = new Color(c.r, c.g, c.b, Mathf.Sin(p * Mathf.PI) * 0.9f);
                t += Time.deltaTime;
                yield return null;
            }
            brt.localScale = Vector3.one;
            rrt.localScale = Vector3.one;
            _glow.color = new Color(c.r, c.g, c.b, 0.35f); // lit resting glow
        }

        private void Update()
        {
            if (_glow == null || !Lit) return;
            float pulse = Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f;
            var c = Type.ToColor();
            _glow.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.25f, 0.5f, pulse));
        }
    }
}
