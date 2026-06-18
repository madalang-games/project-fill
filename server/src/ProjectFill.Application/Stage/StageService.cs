using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Event;
using ProjectFill.Application.Logging;
using ProjectFill.Application.Ranking;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Contracts.Stage;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFill.Application.Stage;

public sealed class StageService
{
    private const int CurrentRulesetVersion = 1;
    private const int RewardVersion = 1;
    private static readonly TimeSpan StageSessionTtl = TimeSpan.FromHours(1);

    private readonly AppDbContext _db;
    private readonly IStaticDataService _staticData;
    private readonly RewardService _reward;
    private readonly AchievementService _achievement;
    private readonly WeeklyMissionService _weeklyMission;
    private readonly RankingService _ranking;
    private readonly CurrencyService _currency;
    private readonly IDatabase _redis;

    public StageService(
        AppDbContext db,
        IStaticDataService staticData,
        RewardService reward,
        AchievementService achievement,
        WeeklyMissionService weeklyMission,
        RankingService ranking,
        CurrencyService currency,
        IConnectionMultiplexer redis)
    {
        _db = db;
        _staticData = staticData;
        _reward = reward;
        _achievement = achievement;
        _weeklyMission = weeklyMission;
        _ranking = ranking;
        _currency = currency;
        _redis = redis.GetDatabase();
    }

    // Per-start attempt token. Issued on start, validated+consumed on clear, so a clear cannot be
    // posted without a fresh start (closes the start-skip bypass, e.g. result-screen "Next").
    private static string SessionKey(long userId, int stageId) => $"stage_session:{userId}:{stageId}";

    public async Task<StageStartResponse> StartStageAsync(long userId, int stageId, CancellationToken ct)
    {
        _ = _staticData.GetStage(stageId)
            ?? throw new GameApiException(ErrorCodes.StageNotFound, $"Stage {stageId} not found.");

        var maxClearedStageId = await ValidateUnlockedAsync(userId, stageId, ct);

        // Issue a fresh single-use attempt token; the matching clear must echo it back.
        var sessionId = Guid.NewGuid().ToString("N");
        await _redis.StringSetAsync(SessionKey(userId, stageId), sessionId, StageSessionTtl);

        return new StageStartResponse
        {
            StageId = stageId,
            MaxClearedStageId = maxClearedStageId,
            RulesetVersion = CurrentRulesetVersion,
            SessionId = sessionId,
            ServerTime = DateTimeOffset.UtcNow,
        };
    }

    // Campaign gating: stage 1 is always open; stage N requires the player to have first-cleared N-1
    // (max_cleared_stage_id >= N-1). Server-authoritative — both stage entry and clear go through this.
    private async Task<int> ValidateUnlockedAsync(long userId, int stageId, CancellationToken ct)
    {
        var totals = await _db.UserRankingTotals.FindAsync(userId, ct);
        var maxClearedStageId = totals?.MaxClearedStageId ?? 0;
        if (stageId > 1 && maxClearedStageId < stageId - 1)
            throw new GameApiException(ErrorCodes.StageLocked, $"Stage {stageId} is locked.");
        return maxClearedStageId;
    }

    public async Task<StageClearResponse> ClearStageAsync(long userId, int stageId, StageClearRequest request, string correlationId, CancellationToken ct)
    {
        var stage = _staticData.GetStage(stageId)
            ?? throw new GameApiException(ErrorCodes.StageNotFound, $"Stage {stageId} not found.");

        if (request.RulesetVersion != CurrentRulesetVersion)
            throw new GameApiException(ErrorCodes.StageRulesetMismatch, "Stage ruleset version mismatch.");

        if (request.MovesUsed < 1)
            throw new GameApiException(ErrorCodes.InvalidStageClear, "moves_used must be at least 1.");

        // Reject clears for stages the player cannot legally reach (closes the start-validation bypass).
        await ValidateUnlockedAsync(userId, stageId, ct);

        var expectedTypes = Enumerable.Range(0, stage.Types).ToHashSet();
        if (!expectedTypes.SetEquals(request.CompletedSignalTypes))
            throw new GameApiException(ErrorCodes.InvalidStageClear, "completed_signal_types do not match the stage definition.");

        // Validate+consume the start-issued attempt token: a clear must follow a matching start. A
        // missing key means the token never existed or its TTL expired — both reject as invalid.
        var sessionKey = SessionKey(userId, stageId);
        var storedSession = await _redis.StringGetAsync(sessionKey);
        if (storedSession.IsNullOrEmpty || storedSession != request.SessionId)
            throw new GameApiException(ErrorCodes.InvalidStageAttempt, "Stage session missing, mismatched, or expired.");
        await _redis.KeyDeleteAsync(sessionKey);

        var now = DateTimeOffset.UtcNow;
        var weekStart = CurrentWeekStart();

        var progress = await _db.UserStageProgress.FindAsync(userId, stageId, ct);
        var isFirstClear = progress is null || !progress.StageClear;
        var hadBest = progress is not null && progress.StageClear && progress.BestMovesUsed > 0;
        var isNewBest = isFirstClear || request.MovesUsed < progress!.BestMovesUsed;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (progress is null)
        {
            progress = new UserStageProgressRow { UserId = userId, StageId = stageId };
            _db.UserStageProgress.Insert(progress);
        }
        progress.StageClear = true;
        progress.LatestMovesUsed = request.MovesUsed;
        progress.BestMovesUsed = isNewBest ? request.MovesUsed : progress.BestMovesUsed;
        progress.FirstClearedAt ??= now;
        progress.UpdatedAt = now;

        var totals = await _db.UserRankingTotals.FindAsync(userId, ct);
        if (totals is null)
        {
            totals = new UserRankingTotalsRow { UserId = userId };
            _db.UserRankingTotals.Insert(totals);
        }
        if (isFirstClear)
        {
            totals.TotalClearedStages += 1;
            totals.TotalClearedAt = now;
            if (stageId > totals.MaxClearedStageId)
            {
                totals.MaxClearedStageId = stageId;
                totals.MaxStageAchievedAt = now;
            }
        }
        totals.WinStreak += 1;
        totals.MaxWinStreak = Math.Max(totals.MaxWinStreak, totals.WinStreak);
        totals.UpdatedAt = now;

        var isNewPerfect = false;
        if (stage.ParMoves > 0 && progress.BestMovesUsed <= stage.ParMoves && !progress.IsPerfect)
        {
            progress.IsPerfect = true;
            isNewPerfect = true;
            totals.PerfectClears += 1;
            totals.PerfectClearedAt = now;
        }

        var weekly = await _db.UserWeeklyRanking.FindAsync(userId, ct);
        if (weekly is null)
        {
            weekly = new UserWeeklyRankingRow { UserId = userId, WeekStartDate = weekStart };
            _db.UserWeeklyRanking.Insert(weekly);
        }
        if (weekly.WeekStartDate != weekStart)
        {
            weekly.WeekStartDate = weekStart;
            weekly.WeeklyClearedCount = 0;
        }
        weekly.WeeklyClearedCount += 1;
        weekly.WeeklyClearedAt = now;
        weekly.UpdatedAt = now;

        var response = new StageClearResponse
        {
            StageId = stageId,
            MovesUsed = request.MovesUsed,
            BestMovesUsed = progress.BestMovesUsed,
            IsNewBest = isNewBest,
            IsFirstClear = isFirstClear,
            WeeklyClearedCount = weekly.WeeklyClearedCount,
            TotalClearedStages = totals.TotalClearedStages,
            MaxClearedStageId = totals.MaxClearedStageId,
        };

        CurrencySnapshot? currency = null;

        if (isFirstClear)
        {
            var (granted, gainCurrency) = await _reward.GrantRewardGroupAsync(userId, stage.RewardGroupId, RewardVersion, correlationId, ct);
            response.GrantedRewards.AddRange(granted);
            if (gainCurrency is not null) currency = gainCurrency;
            _db.EventLogs.Insert(EventLogFactory.StageClearRewardGranted(userId, correlationId, stageId, stage.RewardGroupId));

            var (chapterCompleted, chestGroupId, chestGranted, chestCurrency) =
                await TryGrantChapterMilestoneAsync(userId, stage.ChapterId, stageId, now, correlationId, ct);
            if (chapterCompleted)
            {
                response.ChapterCompleted = true;
                response.ChapterChestRewardGroupId = chestGroupId;
                response.GrantedRewards.AddRange(chestGranted);
                if (chestCurrency is not null) currency = chestCurrency;
            }
        }

        await _db.SaveAsync(ct);

        if (isFirstClear)
            await _achievement.ReportCountAsync(userId, AchievementConditionType.StageClearCount, 1, ct);
        if (isNewBest && hadBest)
            await _achievement.ReportCountAsync(userId, AchievementConditionType.BestMovesRenewCount, 1, ct);
        if (response.ChapterCompleted)
            await _achievement.ReportCountAsync(userId, AchievementConditionType.ChapterComplete, 1, ct);
        if (!request.BoostersUsed)
            await _achievement.ReportCountAsync(userId, AchievementConditionType.BoosterlessClearCount, 1, ct);

        // Weekly Mission Event progress (same seam as the achievement reports; campaign-play aggregate).
        await _weeklyMission.ReportProgressAsync(userId, WeeklyMissionConditionType.StageClearCount, 1, ct);
        if (isFirstClear)
            await _weeklyMission.ReportProgressAsync(userId, WeeklyMissionConditionType.ChapterProgress, 1, ct);
        if (isNewBest && hadBest)
            await _weeklyMission.ReportProgressAsync(userId, WeeklyMissionConditionType.BestMovesRenew, 1, ct);
        if (isNewPerfect)
            await _weeklyMission.ReportProgressAsync(userId, WeeklyMissionConditionType.PerfectClearCount, 1, ct);
        if (!request.BoostersUsed)
            await _weeklyMission.ReportProgressAsync(userId, WeeklyMissionConditionType.BoosterlessClear, 1, ct);

        await tx.CommitAsync(ct);

        await _ranking.RecordClearAsync(userId, stageId, ct);

        var stageRank = await _ranking.GetStageRankAsync(userId, stageId, ct);
        response.StageRank = stageRank.Rank ?? 0;
        response.Currency = currency ?? await CurrentCurrencyAsync(userId, ct);
        response.ServerTime = now;
        return response;
    }

    private async Task<(bool Completed, int ChestGroupId, List<ProjectFill.Contracts.Rewards.GrantedRewardDto> Granted, CurrencySnapshot? Currency)>
        TryGrantChapterMilestoneAsync(long userId, int chapterId, int clearedStageId, DateTimeOffset now, string correlationId, CancellationToken ct)
    {
        var chapter = _staticData.GetChapter(chapterId);
        if (chapter is null || chapter.ChestRewardGroupId <= 0)
            return (false, 0, new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>(), null);

        var claimKey = $"chapter_chest:{chapterId}";
        var alreadyGranted = await _db.UserRewardClaimState.FindAsync(userId, claimKey, "once", ct);
        if (alreadyGranted is not null)
            return (false, 0, new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>(), null);

        var chapterStageIds = _staticData.GetAllStages()
            .Where(s => s.ChapterId == chapterId)
            .Select(s => s.StageId)
            .ToHashSet();
        if (chapterStageIds.Count == 0)
            return (false, 0, new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>(), null);

        var clearedIds = await _db.UserStageProgress.Query()
            .Where(x => x.UserId == userId && x.StageClear && chapterStageIds.Contains(x.StageId))
            .Select(x => x.StageId)
            .ToListAsync(ct);
        var clearedSet = clearedIds.ToHashSet();
        clearedSet.Add(clearedStageId);

        if (!chapterStageIds.All(clearedSet.Contains))
            return (false, 0, new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>(), null);

        var (granted, currency) = await _reward.GrantRewardGroupAsync(userId, chapter.ChestRewardGroupId, RewardVersion, correlationId, ct);
        _db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
        {
            UserId = userId,
            SourceId = claimKey,
            PeriodKey = "once",
            ClaimCount = 1,
            LastClaimedAt = now,
            UpdatedAt = now,
        });
        _db.EventLogs.Insert(EventLogFactory.RewardClaimed(userId, correlationId, claimKey, chapter.ChestRewardGroupId));

        return (true, chapter.ChestRewardGroupId, granted, currency);
    }

    private async Task<CurrencySnapshot> CurrentCurrencyAsync(long userId, CancellationToken ct)
    {
        var c = await _db.UserCurrency.FindAsync(userId, ct);
        return new CurrencySnapshot { SoftAmount = c?.SoftAmount ?? 0 };
    }

    private static string CurrentWeekStart()
    {
        var today = DateTimeOffset.UtcNow.UtcDateTime.Date;
        var diff = ((int)today.DayOfWeek + 6) % 7; // Monday = 0
        return today.AddDays(-diff).ToString("yyyy-MM-dd");
    }
}
