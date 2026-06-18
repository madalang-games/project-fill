using UnityEngine;
using Game.Utils;
using ProjectFill.Data.Generated;

namespace Game.Services
{
    public class ItemDataService : MonoBehaviour
    {
        private static ItemDataService _instance;

        // Lazy-instantiated if not placed in scene, so item data (reward icons) is always available
        // rather than silently resolving to empty sprites.
        public static ItemDataService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ItemDataService>();
                    if (_instance == null)
                        _instance = new GameObject(nameof(ItemDataService)).AddComponent<ItemDataService>();
                }
                return _instance;
            }
        }

        private Item[] _items;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureLoaded();
        }

        private void EnsureLoaded()
        {
            if (_items == null) _items = CsvLoader.Load<Item>(Item.ResourcePath) ?? System.Array.Empty<Item>();
        }

        public Item GetItem(int itemId)
        {
            EnsureLoaded();
            return System.Array.Find(_items, i => i.id == itemId);
        }
    }
}
