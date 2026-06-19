using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Game.Core.UI;
using Game.Services;
using Game.Utils;
using ProjectFill.Contracts.Cosmetic;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Contracts.Rewards;
using GeneratedRewardItem = ProjectFill.Data.Generated.RewardItem;
using GeneratedCosmeticItem = ProjectFill.Data.Generated.CosmeticItem;
using GeneratedAchievement = ProjectFill.Data.Generated.Achievement;

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
        private static List<GeneratedCosmeticItem> _cosmeticItems;
        private static List<GeneratedAchievement> _achievements;

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

        /// <summary>
        /// Cosmetics are not reward_group items — a cosmetic reverse-references the achievement that
        /// gates it (cosmetic_item.unlock_condition_id == achievementId, unlock_type == Achievement).
        /// Returns the cosmetic rows unlocked by a given achievement, rendered procedurally via
        /// <see cref="CosmeticPreview.Build"/> (no flat sprite) — the same look the Shop cosmetic grid uses.
        /// </summary>
        public static List<RewardItem> BuildCosmeticUnlocks(string achievementId)
        {
            var list = new List<RewardItem>();
            if (string.IsNullOrEmpty(achievementId)) return list;
            EnsureCosmeticItems();

            foreach (var row in _cosmeticItems
                         .Where(c => c.unlock_type == CosmeticUnlockType.Achievement &&
                                     c.unlock_condition_id == achievementId)
                         .OrderBy(c => c.sort_order))
            {
                var dto = new CosmeticItemDto
                {
                    CosmeticId = row.cosmetic_id,
                    Category   = (int)row.category,
                    NameKey    = row.name_key,
                    DescKey    = row.desc_key,
                };
                list.Add(new RewardItem
                {
                    Quantity     = 1,
                    NameKey      = row.name_key,
                    DescKey      = row.desc_key,
                    CustomRender = img => CosmeticPreview.Build(img, dto),
                });
            }
            return list;
        }

        /// <summary>
        /// Representative reward renderer for an achievement cell, applied to the cell's RewardIcon Image.
        /// Priority: any cosmetic the achievement unlocks comes first (headline) and renders procedurally
        /// via <see cref="CosmeticPreview.Build"/> (cosmetics have no flat sprite); otherwise the reward
        /// group's highest sort_order item as a flat icon. Both pick highest sort_order (descending).
        /// Returns null when neither resolves.
        /// </summary>
        public static Action<Image> RepresentativeRewardRender(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return null;

            EnsureCosmeticItems();
            var cos = _cosmeticItems
                .Where(c => c.unlock_type == CosmeticUnlockType.Achievement &&
                            c.unlock_condition_id == achievementId)
                .OrderByDescending(c => c.sort_order)
                .FirstOrDefault();
            if (cos != null)
            {
                var dto = new CosmeticItemDto
                {
                    CosmeticId = cos.cosmetic_id,
                    Category   = (int)cos.category,
                    NameKey    = cos.name_key,
                    DescKey    = cos.desc_key,
                };
                return img => CosmeticPreview.Build(img, dto);
            }

            EnsureAchievements();
            var ach = _achievements.FirstOrDefault(a => a.achievement_id == achievementId);
            if (ach == null) return null;

            EnsureRewardItems();
            var row = _rewardItems
                .Where(r => r.reward_group_id == ach.reward_group_id)
                .OrderByDescending(r => r.sort_order)
                .FirstOrDefault();
            if (row == null) return null;

            var (iconKey, _, _) = ResolveVisual(row.reward_type, row.target_id);
            var spr = GetSprite(iconKey);
            if (spr == null) return null;
            return img =>
            {
                img.sprite = spr;
                img.color  = UnityEngine.Color.white;
            };
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

        private static void EnsureCosmeticItems()
        {
            if (_cosmeticItems != null) return;
            try
            {
                var loaded = CsvLoader.Load<GeneratedCosmeticItem>(GeneratedCosmeticItem.ResourcePath);
                _cosmeticItems = loaded != null ? new List<GeneratedCosmeticItem>(loaded) : new List<GeneratedCosmeticItem>();
            }
            catch { _cosmeticItems = new List<GeneratedCosmeticItem>(); }
        }

        private static void EnsureAchievements()
        {
            if (_achievements != null) return;
            try
            {
                var loaded = CsvLoader.Load<GeneratedAchievement>(GeneratedAchievement.ResourcePath);
                _achievements = loaded != null ? new List<GeneratedAchievement>(loaded) : new List<GeneratedAchievement>();
            }
            catch { _achievements = new List<GeneratedAchievement>(); }
        }

        private static UnityEngine.Sprite GetSprite(string key)
            => string.IsNullOrEmpty(key) ? null : DynamicResourceService.Instance?.GetSprite(key);
    }
}
