using System.Text.Json;
using ProjectFill.Domain.Logging;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Logging;

public static class EventLogFactory
{
    public static EventLogsRow Create(long userId, int trId, string correlationId, object parameters)
        => new()
        {
            UserId = userId,
            TrId = trId,
            CorrelationId = correlationId,
            Params = JsonSerializer.Serialize(parameters),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public static EventLogsRow StageAttemptStarted(long userId, string correlationId, string attemptId, int stageId, DateTimeOffset expiresAt)
        => Create(userId, EventLogIds.StageAttemptStarted, correlationId, new { attempt_id = attemptId, stage_id = stageId, expires_at_utc = expiresAt });

    public static EventLogsRow StageAttemptCleared(long userId, string correlationId, string attemptId, int stageId)
        => Create(userId, EventLogIds.StageAttemptCleared, correlationId, new { attempt_id = attemptId, stage_id = stageId });

    public static EventLogsRow StageAttemptFailed(long userId, string correlationId, string attemptId, int stageId, string reason)
        => Create(userId, EventLogIds.StageAttemptFailed, correlationId, new { attempt_id = attemptId, stage_id = stageId, reason });

    public static EventLogsRow StageAttemptReplaced(long userId, string correlationId, string attemptId, int stageId)
        => Create(userId, EventLogIds.StageAttemptReplaced, correlationId, new { attempt_id = attemptId, stage_id = stageId, reason = "replaced_by_new_attempt" });

    public static EventLogsRow StageAttemptRevivedByAd(long userId, string correlationId, string attemptId, int stageId, int reviveCount, string adTxId)
        => Create(userId, EventLogIds.StageAttemptRevivedByAd, correlationId, new { attempt_id = attemptId, stage_id = stageId, revive_count = reviveCount, ad_tx_id = adTxId });

    public static EventLogsRow RewardClaimed(long userId, string correlationId, string sourceId, int rewardGroupId)
        => Create(userId, EventLogIds.RewardClaimed, correlationId, new { source_id = sourceId, reward_group_id = rewardGroupId });

    public static EventLogsRow AdRewardClaimed(long userId, string correlationId, string adTxId, string placementId, string rewardType, int rewardValue, bool duplicate)
        => Create(userId, EventLogIds.AdRewardClaimed, correlationId, new { ad_tx_id = adTxId, placement_id = placementId, reward_type = rewardType, reward_value = rewardValue, duplicate });

    public static EventLogsRow StageClearRewardGranted(long userId, string correlationId, int stageId, int rewardGroupId)
        => Create(userId, EventLogIds.StageClearRewardGranted, correlationId, new { stage_id = stageId, reward_group_id = rewardGroupId });

    public static EventLogsRow AdInterstitialShown(long userId, string correlationId, int stageId)
        => Create(userId, EventLogIds.AdInterstitialShown, correlationId, new { stage_id = stageId });

    public static EventLogsRow AdDoubleRewardGranted(long userId, string correlationId, string adTxId, int stageId, string attemptId)
        => Create(userId, EventLogIds.AdDoubleRewardGranted, correlationId, new { ad_tx_id = adTxId, stage_id = stageId, attempt_id = attemptId });

    public static EventLogsRow InventoryChanged(long userId, string correlationId, int itemId, int delta, string reason, int currentAfter)
        => Create(userId, EventLogIds.InventoryChanged, correlationId, new { item_id = itemId, delta, reason, current_after = currentAfter });

    public static EventLogsRow IapPurchaseCompleted(long userId, string correlationId, int infoId, string storeProductId, string orderId, double price, string currency)
        => Create(userId, EventLogIds.IapPurchaseCompleted, correlationId, new { info_id = infoId, store_product_id = storeProductId, order_id = orderId, price, currency });

    public static EventLogsRow CosmeticUnlocked(long userId, string correlationId, string cosmeticId, string unlockType, string conditionId)
        => Create(userId, EventLogIds.CosmeticUnlocked, correlationId, new { cosmetic_id = cosmeticId, unlock_type = unlockType, condition_id = conditionId });

    public static EventLogsRow CosmeticEquipped(long userId, string correlationId, string chipSkin, string laneSkin, string boardSkin, bool useCustomBoardSkin)
        => Create(userId, EventLogIds.CosmeticEquipped, correlationId, new { chip_skin = chipSkin, lane_skin = laneSkin, board_skin = boardSkin, use_custom_board_skin = useCustomBoardSkin });

    public static EventLogsRow AttendanceClaimed(long userId, string correlationId, int cycle, int day, int streak, int rewardGroupId)
        => Create(userId, EventLogIds.AttendanceClaimed, correlationId, new { cycle, day, streak, reward_group_id = rewardGroupId });

    public static EventLogsRow AchievementClaimed(long userId, string correlationId, string achievementId, int rewardGroupId)
        => Create(userId, EventLogIds.AchievementClaimed, correlationId, new { achievement_id = achievementId, reward_group_id = rewardGroupId });

    public static EventLogsRow ChallengeCleared(long userId, string correlationId, string challengeDate, int movesUsed, int clearTimeSeconds, int streak)
        => Create(userId, EventLogIds.ChallengeCleared, correlationId, new { challenge_date = challengeDate, moves_used = movesUsed, clear_time_seconds = clearTimeSeconds, streak });
}
