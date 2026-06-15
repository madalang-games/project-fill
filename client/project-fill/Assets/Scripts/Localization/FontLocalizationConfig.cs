using System;
using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.Localization
{
    [CreateAssetMenu(fileName = "FontLocalizationConfig", menuName = "Flood/Font Localization Config")]
    public class FontLocalizationConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Language language;
            public TMP_FontAsset font;
        }

        [SerializeField] private Entry[] _entries;

        public TMP_FontAsset GetFont(Language lang)
        {
            if (_entries == null) return null;
            foreach (var e in _entries)
                if (e.language == lang) return e.font;
            return null;
        }
    }
}
