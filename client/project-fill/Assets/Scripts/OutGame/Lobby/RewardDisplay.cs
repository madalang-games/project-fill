using System.Collections.Generic;
using System.Linq;
using Game.Core.UI;
using Game.Services;
using Game.Utils;
using ProjectFill.Contracts.Rewards;
using GeneratedRewardItem = ProjectFill.Data.Generated.RewardItem;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Converts reward definitions into RewardPopupView RewardItem rows.
    /// Build() handles server GrantedRewardDto lists (claim results);
    /// BuildFromGroup() handles static reward_group definitions (preview, e.g. attendance).
    /// Shared by attendance / achievement / daily-challenge flows.
    /// </summary>
    public static class RewardDisplay
    {
        private static List<GeneratedRewardItem> _rewardItems;

        public static List<RewardItem> Build(IReadOnlyList<GrantedRewardDto> rewards)
        {
            var list = new List<RewardItem>();
            if (rewards == null) return list;

            foreach (var r in rewards)
            {
                if (r == null) continue;
                list.Add(MapReward(r.RewardType, r.TargetId, r.Amount));
            }
            return list;
        }

        /// <summary>Builds display rows for a static reward_group_id, ordered by sort_order.</summary>
        public static List<RewardItem> BuildFromGroup(int rewardGroupId)
        {
            var list = new List<RewardItem>();
            if (rewardGroupId == 0) return list;
            EnsureRewardItems();

            foreach (var row in _rewardItems.Where(r => r.reward_group_id == rewardGroupId).OrderBy(r => r.sort_order))
                list.Add(MapReward(row.reward_type, row.target_id, row.amount));

            return list;
        }

        private static RewardItem MapReward(string rewardType, int targetId, int amount)
        {
            var (iconKey, nameKey, descKey) = ResolveVisual(rewardType, targetId);
            bool singleQty = rewardType == "AVATAR" || rewardType == "NO_ADS";
            return new RewardItem
            {
                Icon = GetSprite(iconKey),
                Quantity = singleQty ? 1 : amount,
                NameKey = nameKey,
                DescKey = descKey,
            };
        }

        /// <summary>
        /// Single source for reward icon/text resolution: maps a reward type/target to its
        /// dynamic-resource icon key + localization keys. Shared by reward popups, the shop
        /// preview rows, and chest claim — keep all reward icon lookups here.
        /// </summary>
        public static (string iconKey, string nameKey, string descKey) ResolveVisual(string rewardType, int targetId)
        {
            switch (rewardType)
            {
                case "SOFT_CURRENCY":
                    var gold = CurrencyDataService.Instance?.GetByRewardType("SOFT_CURRENCY");
                    return (gold?.icon_name ?? "", gold?.name_key ?? "", gold?.desc_key ?? "");
                case "ITEM":
                    var item = ItemDataService.Instance?.GetItem(targetId);
                    return (item?.icon_name ?? "", item?.name_key ?? "", item?.desc_key ?? "");
                case "AVATAR":
                    return ($"avatar_{targetId}", "", "");
                case "NO_ADS":
                    return (ResourceKeys.IapNoAds, "shop.reward.no_ads", "");
                default:
                    return ("", "", "");
            }
        }

        private static void EnsureRewardItems()
        {
            if (_rewardItems != null) return;
            try
            {
                var loaded = CsvLoader.Load<GeneratedRewardItem>(GeneratedRewardItem.ResourcePath);
                _rewardItems = loaded != null ? new List<GeneratedRewardItem>(loaded) : new List<GeneratedRewardItem>();
            }
            catch { _rewardItems = new List<GeneratedRewardItem>(); }
        }

        private static UnityEngine.Sprite GetSprite(string key)
            => string.IsNullOrEmpty(key) ? null : DynamicResourceService.Instance?.GetSprite(key);
    }
}
