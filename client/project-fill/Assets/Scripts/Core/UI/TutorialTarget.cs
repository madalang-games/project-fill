using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.UI
{
    /// <summary>
    /// Attach to any scene/UI GameObject and set _targetId to the tutorial_step.csv target_ui_id value.
    /// Self-registers into a static registry on Enable; TutorialOverlay queries it by id — no hardcoding needed.
    /// </summary>
    public class TutorialTarget : MonoBehaviour
    {
        [SerializeField] private string[] _targetIds = System.Array.Empty<string>();

        static readonly Dictionary<string, TutorialTarget> _registry = new();

        void OnEnable()
        {
            foreach (var id in _targetIds)
                if (!string.IsNullOrEmpty(id))
                    _registry[id] = this;
        }

        void OnDisable()
        {
            foreach (var id in _targetIds)
                if (!string.IsNullOrEmpty(id))
                    if (_registry.TryGetValue(id, out var t) && t == this)
                        _registry.Remove(id);
        }

        // Runtime id assignment (e.g. BoardView tags each lane slot_lane_{n} as it spawns).
        // Re-registers immediately so a component added after Awake still resolves.
        public void SetIds(params string[] ids)
        {
            foreach (var id in _targetIds)
                if (!string.IsNullOrEmpty(id) && _registry.TryGetValue(id, out var t) && t == this)
                    _registry.Remove(id);

            _targetIds = ids ?? System.Array.Empty<string>();

            if (isActiveAndEnabled)
                foreach (var id in _targetIds)
                    if (!string.IsNullOrEmpty(id))
                        _registry[id] = this;
        }

        public static TutorialTarget Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _registry.TryGetValue(id, out var t);
            return t;
        }
    }
}
