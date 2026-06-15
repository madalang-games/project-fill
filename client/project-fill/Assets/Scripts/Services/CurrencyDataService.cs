using Game.Utils;
using ProjectFill.Data.Generated;
using UnityEngine;

namespace Game.Services
{
    public class CurrencyDataService : MonoBehaviour
    {
        public static CurrencyDataService Instance { get; private set; }

        private Currency[] _currencies;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _currencies = CsvLoader.Load<Currency>(Currency.ResourcePath);
        }

        public Currency GetByRewardType(string rewardTypeKey)
        {
            foreach (var c in _currencies)
                if (c.reward_type_key == rewardTypeKey) return c;
            return null;
        }
    }
}
