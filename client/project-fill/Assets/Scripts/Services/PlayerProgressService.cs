using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Player;
using UnityEngine;

namespace Game.Services
{
    public class PlayerProgressService : MonoBehaviour
    {
        public static PlayerProgressService Instance { get; private set; }

        private const string KeyGold         = "gold";
        private const string KeyStarPrefix   = "stars_";
        private const string KeyUnlockPrefix = "unlocked_";

        private int _gold;
        private readonly Dictionary<int, int> _bestStars     = new Dictionary<int, int>();
        private readonly Dictionary<int, bool> _unlocked     = new Dictionary<int, bool>();
        private readonly Dictionary<int, int> _inventory    = new Dictionary<int, int>();
        private readonly HashSet<int>          _unlockedAvatarIds = new HashSet<int>();
        private bool _isNoAds;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // --- No Ads ---
        public bool IsNoAds => _isNoAds;
        public event Action<bool> OnNoAdsChanged;

        public void SetNoAds(bool isNoAds)
        {
            _isNoAds = isNoAds;
            PlayerPrefs.SetInt("is_no_ads", isNoAds ? 1 : 0);
            PlayerPrefs.Save();
            OnNoAdsChanged?.Invoke(isNoAds);
        }

        // --- Gold ---
        public int  Gold         => _gold;
        public bool CanAfford(int cost) => _gold >= cost;

        public bool SpendGold(int cost)
        {
            if (_gold < cost) return false;
            _gold -= cost;
            PlayerPrefs.SetInt(KeyGold, _gold);
            return true;
        }

        public void AddGold(int amount)
        {
            _gold += amount;
            PlayerPrefs.SetInt(KeyGold, _gold);
        }

        public void SetGold(int amount)
        {
            _gold = amount;
            PlayerPrefs.SetInt(KeyGold, _gold);
        }

        // --- Avatars ---
        public bool IsAvatarUnlocked(int avatarId)
        {
            var list = Utils.CsvLoader.Load<ProjectFill.Data.Generated.Avatar>(ProjectFill.Data.Generated.Avatar.ResourcePath);
            if (list != null)
            {
                foreach (var av in list)
                {
                    if (av.avatar_id == avatarId)
                    {
                        if (av.unlock_type == "free" || av.unlock_type == "common") return true;
                        break;
                    }
                }
            }
            return _unlockedAvatarIds.Contains(avatarId);
        }

        public void UnlockAvatarLocally(int avatarId)
        {
            _unlockedAvatarIds.Add(avatarId);
        }

        // --- Stage stars / unlock (local cache, game-mechanics TBD) ---
        public int GetBestStars(int stageId)
        {
            if (_bestStars.TryGetValue(stageId, out int s)) return s;
            s = PlayerPrefs.GetInt(KeyStarPrefix + stageId, 0);
            if (s > 0) _bestStars[stageId] = s;
            return s;
        }

        public bool IsStageUnlocked(int stageId)
        {
            if (_unlocked.TryGetValue(stageId, out bool u)) return u;
            u = stageId == 1 || PlayerPrefs.GetInt(KeyUnlockPrefix + stageId, 0) == 1;
            _unlocked[stageId] = u;
            return u;
        }

        public void RecordClear(int stageId, int stars)
        {
            if (stars > GetBestStars(stageId))
            {
                _bestStars[stageId] = stars;
                PlayerPrefs.SetInt(KeyStarPrefix + stageId, stars);
            }
            UnlockStage(stageId + 1);
        }

        public void UnlockStage(int stageId)
        {
            if (_unlocked.TryGetValue(stageId, out bool v) && v) return;
            _unlocked[stageId] = true;
            PlayerPrefs.SetInt(KeyUnlockPrefix + stageId, 1);
        }

        // --- Inventory ---
        public int GetItemCount(int itemId)
        {
            _inventory.TryGetValue(itemId, out int count);
            return count;
        }

        public void SetItemCount(int itemId, int count)
        {
            _inventory[itemId] = count;
        }

        public void SetInventory(ProjectFill.Contracts.Inventory.InventorySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Items == null) return;
            foreach (var item in snapshot.Items)
            {
                _inventory[item.ItemId] = item.Count;
            }
        }

        public void LoadFromServer(PlayerProgressResponse response)
        {
            if (response == null) return;

            SetNoAds(response.IsNoAds || _isNoAds);
            _unlockedAvatarIds.Clear();
            if (response.UnlockedAvatarIds != null)
            {
                foreach (var id in response.UnlockedAvatarIds)
                    _unlockedAvatarIds.Add(id);
            }

            PlayerPrefs.Save();
            Debug.Log($"[PlayerProgressService] Loaded from server: unlockedAvatars={response.UnlockedAvatarIds?.Count ?? 0}");
        }

        public void ResetData()
        {
            _gold = 500;
            _bestStars.Clear();
            _unlocked.Clear();
            _unlocked[1] = true;
            _inventory.Clear();
            _unlockedAvatarIds.Clear();
            SetNoAds(false);
            Debug.Log("[PlayerProgressService] Memory cache cleared.");
        }

        private void Load()
        {
            _gold = PlayerPrefs.GetInt(KeyGold, 500);
            _unlocked[1] = true;
            _isNoAds = PlayerPrefs.GetInt("is_no_ads", 0) == 1;
        }
    }
}
