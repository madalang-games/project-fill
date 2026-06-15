using System.Collections.Generic;
using Game.Utils;
using ProjectFill.Data.Generated;
using UnityEngine;

namespace Game.Services
{
    public class DynamicResourceService : MonoBehaviour
    {
        public static DynamicResourceService Instance { get; private set; }

        private readonly Dictionary<string, DynamicResource> _table = new Dictionary<string, DynamicResource>();
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        private const string ResourcesPrefix = "Assets/Resources/";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var rows = CsvLoader.Load<DynamicResource>(DynamicResource.ResourcePath);
            foreach (var row in rows)
                _table[row.resource_key] = row;
        }

        public Sprite GetSprite(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey)) return null;
            if (_spriteCache.TryGetValue(resourceKey, out var cached)) return cached;

            if (!_table.TryGetValue(resourceKey, out var entry)) return null;

            var path = entry.sprite_path;
            if (!path.StartsWith(ResourcesPrefix)) return null;

            var loadPath = path.Substring(ResourcesPrefix.Length);
            if (loadPath.EndsWith(".png"))
                loadPath = loadPath.Substring(0, loadPath.Length - 4);

            var sprite = Resources.Load<Sprite>(loadPath);
            _spriteCache[resourceKey] = sprite;
            return sprite;
        }
    }
}
