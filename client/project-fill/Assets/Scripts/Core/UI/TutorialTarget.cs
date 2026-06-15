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

        public static TutorialTarget Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _registry.TryGetValue(id, out var t);
            return t;
        }
    }
}
