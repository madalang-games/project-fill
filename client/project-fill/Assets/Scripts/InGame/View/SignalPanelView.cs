using System.Collections.Generic;
using System.Linq;
using Game.Core.UI;
using UnityEngine;

namespace Game.InGame.View
{
    // Top Signal Panel binder. Lives on the authored panel container (from UIEditorSetup) and
    // spawns one SignalNodeView per Signal Type at runtime. Nodes light when their set registers;
    // for Relay stages nodes follow the required order and pending types show "awaiting".
    public class SignalPanelView : MonoBehaviour
    {
        private readonly List<SignalNodeView> _nodes = new();

        // (Re)builds the panel nodes for a stage. nodePrefab is a thin SignalNodeView prefab.
        public void Initialize(SpriteSet sprites, GameObject nodePrefab, int types, IReadOnlyList<SignalType> relayOrder, float panelWidth, float panelHeight)
        {
            foreach (var n in _nodes) if (n) Destroy(n.gameObject);
            _nodes.Clear();

            bool relay = relayOrder != null && relayOrder.Count > 0;
            float gap = 0.02f;
            float w   = (1f - 0.06f - gap * (types - 1)) / types;

            for (int i = 0; i < types; i++)
            {
                var type = relay ? relayOrder[i] : (SignalType)i;
                float x0 = 0.03f + i * (w + gap);

                float xMin = -panelWidth * 0.5f + x0 * panelWidth;
                float xMax = -panelWidth * 0.5f + (x0 + w) * panelWidth;
                float yMin = -panelHeight * 0.5f + 0.12f * panelHeight;
                float yMax = -panelHeight * 0.5f + 0.88f * panelHeight;

                Vector3 localPos = new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, 0f);
                // Fixed LED-style node: a stable fraction of panel height, NOT stretched to fill the
                // (often wide) cell — keeps the lit disc/ring/glow compact instead of ballooning.
                float diameter = Mathf.Min(panelHeight * 0.42f, (xMax - xMin) * 0.8f, yMax - yMin);
                Vector2 nodeSize = new Vector2(diameter, diameter);

                var node = SpawnNode(nodePrefab);
                node.transform.localPosition = localPos;
                node.Initialize(sprites, type, showConnector: i != 0, nodeSize);
                _nodes.Add(node);

                // Tag node by order (signal_node_1 = first in the relay order) so the Relay tutorial can
                // highlight individual nodes and run the drag pointer between them.
                var tt = node.gameObject.GetComponent<TutorialTarget>() ?? node.gameObject.AddComponent<TutorialTarget>();
                tt.SetIds($"signal_node_{i + 1}");
            }
        }

        private SignalNodeView SpawnNode(GameObject nodePrefab)
        {
            GameObject go = nodePrefab != null
                ? Instantiate(nodePrefab, transform)
                : new GameObject("Node");
            if (nodePrefab == null) go.transform.SetParent(transform, false);
            return go.GetComponent<SignalNodeView>() ?? go.AddComponent<SignalNodeView>();
        }

        public void UpdateState(Board board)
        {
            var registered = board.RegisteredTypes;
            var pending = new HashSet<SignalType>();
            if (board.HasRelay)
                foreach (var lane in board.Lanes)
                    if (lane.Pending && lane.Count > 0) pending.Add(lane.Chips[0].Type);

            foreach (var n in _nodes)
                n.SetState(registered.Contains(n.Type), pending.Contains(n.Type));
        }

        public Vector3 NodeWorldPos(SignalType type)
        {
            foreach (var n in _nodes)
                if (n.Type == type) return n.WorldPos;
            return transform.position;
        }

        public void PlayRegister(SignalType type)
        {
            foreach (var n in _nodes)
                if (n.Type == type) n.PlayRegister();
        }

        // World-space segment from a node to the next one in the chain — drives the register light-pulse
        // trace. False for the last node (no forward connector to propagate along).
        public bool TryGetTraceTarget(SignalType type, out Vector3 from, out Vector3 to)
        {
            from = to = Vector3.zero;
            int idx = _nodes.FindIndex(n => n.Type == type);
            if (idx < 0 || idx + 1 >= _nodes.Count) return false;
            from = _nodes[idx].WorldPos;
            to   = _nodes[idx + 1].WorldPos;
            return true;
        }
    }
}
