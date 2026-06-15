using UnityEngine;
using Game.Utils;
using ProjectFill.Data.Generated;

namespace Game.Services
{
    public class ItemDataService : MonoBehaviour
    {
        public static ItemDataService Instance { get; private set; }

        private Item[] _items = System.Array.Empty<Item>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _items = CsvLoader.Load<Item>(Item.ResourcePath) ?? System.Array.Empty<Item>();
        }

        public Item GetItem(int itemId) => System.Array.Find(_items, i => i.id == itemId);
    }
}
