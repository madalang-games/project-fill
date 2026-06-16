using System.Collections.Generic;
using System.Linq;
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
                Vector2 nodeSize = new Vector2(xMax - xMin, yMax - yMin);

                var node = SpawnNode(nodePrefab);
                node.transform.localPosition = localPos;
                node.Initialize(sprites, type, showConnector: i != 0, nodeSize);
                _nodes.Add(node);
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
    }
}
