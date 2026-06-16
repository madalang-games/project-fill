using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Top Signal Panel: one node per Signal Type that lights up when its set is registered.
    // For Relay stages nodes follow the required order; pending types blink "awaiting".
    public class SignalPanelView : MonoBehaviour
    {
        private class Node
        {
            public SignalType Type;
            public Image Glow, Body, Connector;
            public TextMeshProUGUI Glyph;
            public bool Lit;
        }

        private readonly List<Node> _nodes = new();
        private RectTransform _rt;

        public static SignalPanelView Build(Transform parent, SpriteSet sprites, int types, IReadOnlyList<SignalType> relayOrder)
        {
            var rt = UiUtil.Rect(parent, "SignalPanel");
            UiUtil.Stretch(rt);
            var v = rt.gameObject.AddComponent<SignalPanelView>();
            v.Construct(sprites, types, relayOrder);
            return v;
        }

        private void Construct(SpriteSet s, int types, IReadOnlyList<SignalType> relayOrder)
        {
            _rt = (RectTransform)transform;
            bool relay = relayOrder != null && relayOrder.Count > 0;

            float gap = 0.02f;
            float w   = (1f - 0.06f - gap * (types - 1)) / types;
            for (int i = 0; i < types; i++)
            {
                var type = relay ? relayOrder[i] : (SignalType)i;
                float x0 = 0.03f + i * (w + gap);

                var holder = UiUtil.Rect(transform, $"Node_{i}");
                UiUtil.Anchors(holder, x0, 0.12f, x0 + w, 0.88f);

                var connector = UiUtil.Image(holder, "Connector", null, new Color(0.2f, 0.22f, 0.3f, 1f));
                UiUtil.Anchors(connector.rectTransform, -0.12f, 0.46f, 0.0f, 0.54f);
                if (i == 0) connector.enabled = false;

                var glow = UiUtil.Image(holder, "Glow", s.Glow, new Color(1, 1, 1, 0f));
                UiUtil.Stretch(glow.rectTransform);
                glow.rectTransform.offsetMin = new Vector2(-10, -10);
                glow.rectTransform.offsetMax = new Vector2(10, 10);

                var body  = UiUtil.Image(holder, "Body", s.PanelNode, new Color(0.16f, 0.17f, 0.26f));
                UiUtil.Stretch(body.rectTransform, 2f);
                var glyph = UiUtil.Label(holder, "Glyph", type.ToLabel(), 24f);
                UiUtil.Stretch(glyph.rectTransform);
                glyph.color = new Color(0.45f, 0.5f, 0.62f);
                glyph.fontStyle = FontStyles.Bold;

                _nodes.Add(new Node { Type = type, Glow = glow, Body = body, Connector = connector, Glyph = glyph });
            }
        }

        public void UpdateState(Board board)
        {
            var registered = board.RegisteredTypes;
            var pending = new HashSet<SignalType>();
            if (board.HasRelay)
                foreach (var lane in board.Lanes)
                    if (lane.Pending && lane.Count > 0) pending.Add(lane.Chips[0].Type);

            foreach (var n in _nodes)
            {
                bool lit = registered.Contains(n.Type);
                n.Lit = lit;
                if (lit)
                {
                    var c = n.Type.ToColor();
                    n.Body.color  = c;
                    n.Glyph.color = new Color(0, 0, 0, 0.6f);
                    if (n.Connector.enabled) n.Connector.color = Color.Lerp(c, Color.white, 0.2f);
                }
                else
                {
                    n.Body.color  = pending.Contains(n.Type) ? new Color(0.30f, 0.24f, 0.08f) : new Color(0.16f, 0.17f, 0.26f);
                    n.Glyph.color = new Color(0.45f, 0.5f, 0.62f);
                    if (n.Connector.enabled) n.Connector.color = new Color(0.2f, 0.22f, 0.3f);
                }
            }
        }

        public Vector3 NodeWorldPos(SignalType type)
        {
            foreach (var n in _nodes)
                if (n.Type == type) return n.Body.rectTransform.position;
            return _rt.position;
        }

        public void PlayRegister(SignalType type)
        {
            foreach (var n in _nodes)
                if (n.Type == type) StartCoroutine(PopRoutine(n));
        }

        private System.Collections.IEnumerator PopRoutine(Node n)
        {
            var c = n.Type.ToColor();
            float dur = 0.45f, t = 0f;
            var brt = n.Body.rectTransform;
            while (t < dur)
            {
                float p = t / dur;
                float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.35f;
                brt.localScale = Vector3.one * s;
                n.Glow.color = new Color(c.r, c.g, c.b, Mathf.Sin(p * Mathf.PI) * 0.9f);
                t += Time.deltaTime;
                yield return null;
            }
            brt.localScale = Vector3.one;
            n.Glow.color = new Color(c.r, c.g, c.b, 0.35f); // lit resting glow
        }

        private void Update()
        {
            // gentle resting glow on lit nodes + blink pending
            float pulse = Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f;
            foreach (var n in _nodes)
            {
                if (n.Lit)
                {
                    var c = n.Type.ToColor();
                    n.Glow.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.25f, 0.5f, pulse));
                }
            }
        }
    }
}
