using System.Collections.Generic;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.Rewards;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Converts server GrantedRewardDto lists into RewardPopupView RewardItem rows.
    /// Shared by attendance / achievement / daily-challenge claim flows.
    /// </summary>
    public static class RewardDisplay
    {
        public static List<RewardItem> Build(IReadOnlyList<GrantedRewardDto> rewards)
        {
            var list = new List<RewardItem>();
            if (rewards == null) return list;

            foreach (var r in rewards)
            {
                if (r == null) continue;
                switch (r.RewardType)
                {
                    case "SOFT_CURRENCY":
                        var gold = CurrencyDataService.Instance?.GetByRewardType("SOFT_CURRENCY");
                        list.Add(new RewardItem
                        {
                            Icon = GetSprite("ui_gold_icon"),
                            Quantity = r.Amount,
                            NameKey = gold?.name_key ?? "",
                            DescKey = gold?.desc_key ?? "",
                        });
                        break;
                    case "ITEM":
                        var item = ItemDataService.Instance?.GetItem(r.TargetId);
                        list.Add(new RewardItem
                        {
                            Icon = GetSprite(GetItemIconKey(r.TargetId)),
                            Quantity = r.Amount,
                            NameKey = item?.name_key ?? "",
                            DescKey = item?.desc_key ?? "",
                        });
                        break;
                    case "AVATAR":
                        list.Add(new RewardItem
                        {
                            Icon = GetSprite($"avatar_{r.TargetId}"),
                            Quantity = 1,
                            NameKey = "",
                            DescKey = "",
                        });
                        break;
                    case "NO_ADS":
                        list.Add(new RewardItem
                        {
                            Icon = GetSprite("ui_iap_no_ads"),
                            Quantity = 1,
                            NameKey = "shop.reward.no_ads",
                            DescKey = "",
                        });
                        break;
                }
            }
            return list;
        }

        private static UnityEngine.Sprite GetSprite(string key)
            => string.IsNullOrEmpty(key) ? null : DynamicResourceService.Instance?.GetSprite(key);

        private static string GetItemIconKey(int itemId)
        {
            switch (itemId)
            {
                case 1: return "item_add_turn";
                case 2: return "item_bomb";
                case 3: return "item_h_rocket";
                case 4: return "item_color_sweep";
                case 5: return "item_row_shift";
                case 6: return "item_cell_swap";
                default: return "";
            }
        }
    }
}
