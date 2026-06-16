using System.Collections.Generic;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.StaticData;

namespace ProjectFill.API.Tests
{
    /// <summary>
    /// Shared test fake for IStaticDataService. Returns safe empty/null defaults for
    /// every accessor; tests override only the accessors they exercise. Centralizes
    /// interface-growth maintenance so adding a static-data CSV does not break every fake.
    /// </summary>
    public class FakeStaticData : IStaticDataService
    {
        public virtual AdPlacementData? GetAdPlacement(int id) => null;
        public virtual IReadOnlyList<AdPlacementData> GetAllAdPlacements() => new List<AdPlacementData>();
        public virtual AvatarData? GetAvatar(int id) => null;
        public virtual IReadOnlyList<AvatarData> GetAllAvatars() => new List<AvatarData>();
        public virtual ColorPaletteData? GetColorPalette(byte id) => null;
        public virtual IReadOnlyList<ColorPaletteData> GetAllColorPalettes() => new List<ColorPaletteData>();
        public virtual CurrencyData? GetCurrency(int id) => null;
        public virtual IReadOnlyList<CurrencyData> GetAllCurrencys() => new List<CurrencyData>();
        public virtual ItemData? GetItem(int id) => null;
        public virtual IReadOnlyList<ItemData> GetAllItems() => new List<ItemData>();
        public virtual RewardGroupData? GetRewardGroup(int reward_group_id) => null;
        public virtual IReadOnlyList<RewardGroupData> GetAllRewardGroups() => new List<RewardGroupData>();
        public virtual RewardItemData? GetRewardItem(int id) => null;
        public virtual IReadOnlyList<RewardItemData> GetAllRewardItems() => new List<RewardItemData>();
        public virtual RewardSourceData? GetRewardSource(int id) => null;
        public virtual IReadOnlyList<RewardSourceData> GetAllRewardSources() => new List<RewardSourceData>();
        public virtual IapProductData? GetIapProduct(int info_id) => null;
        public virtual IReadOnlyList<IapProductData> GetAllIapProducts() => new List<IapProductData>();
        public virtual ChapterData? GetChapter(int chapter_id) => null;
        public virtual IReadOnlyList<ChapterData> GetAllChapters() => new List<ChapterData>();
        public virtual StageData? GetStage(int stage_id) => null;
        public virtual IReadOnlyList<StageData> GetAllStages() => new List<StageData>();
        public virtual CosmeticItemData? GetCosmeticItem(string cosmetic_id) => null;
        public virtual IReadOnlyList<CosmeticItemData> GetAllCosmeticItems() => new List<CosmeticItemData>();
        public virtual DailyLoginRewardData? GetDailyLoginReward(int id) => null;
        public virtual IReadOnlyList<DailyLoginRewardData> GetAllDailyLoginRewards() => new List<DailyLoginRewardData>();
        public virtual DailyLoginMilestoneData? GetDailyLoginMilestone(int id) => null;
        public virtual IReadOnlyList<DailyLoginMilestoneData> GetAllDailyLoginMilestones() => new List<DailyLoginMilestoneData>();
        public virtual AchievementData? GetAchievement(string achievement_id) => null;
        public virtual IReadOnlyList<AchievementData> GetAllAchievements() => new List<AchievementData>();
    }
}
