using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Common;
using ProjectFill.Application.Logging;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.Event;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Event;

public sealed class WeeklyMissionService
{
    private const int MissionsPerWeek = 5;
    private const int RewardVersion = 1;

    private readonly AppDbContext _db;
    private readonly IStaticDataService _staticData;
    private readonly RewardService _reward;
    private readonly AchievementService _achievement;

    public WeeklyMissionService(AppDbContext db, IStaticDataService staticData, RewardService reward, AchievementService achievement)
    {
        _db = db;
        _staticData = staticData;
        _reward = reward;
        _achievement = achievement;
    }

    // Monday 00:00 UTC boundary — identical to UserWeeklyRanking rollover (StageService.CurrentWeekStart).
    public static string CurrentWeekStart()
    {
        var today = DateTimeOffset.UtcNow.UtcDateTime.Date;
        var diff = ((int)today.DayOfWeek + 6) % 7; // Monday = 0
        return today.AddDays(-diff).ToString("yyyy-MM-dd");
    }

    public async Task<WeeklyMissionResponse> GetStatusAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var weekStart = CurrentWeekStart();
        var set = await EnsureSetAsync(weekStart, ct);
        var missionIds = SplitIds(set.MissionIds);

        var stored = await _db.UserWeeklyMissions.Query()
            .Where(x => x.UserId == userId && x.WeekStartDate == weekStart)
            .ToListAsync(ct);
        var progressMap = stored.ToDictionary(x => x.MissionId);

        var state = await _db.UserWeeklyMissionState.FindAsync(userId, weekStart, ct);
        var totalEp = state?.TotalEp ?? 0;
        var claimed = ParseThresholds(state?.ClaimedThresholds);

        var response = new WeeklyMissionResponse
        {
            WeekStartDate = weekStart,
            DaysRemaining = Math.Max(0, (int)Math.Ceiling((WeekEndUtc(weekStart) - now).TotalDays)),
            TotalEp = totalEp,
            ServerTime = now,
        };

        foreach (var missionId in missionIds)
        {
            var pool = _staticData.GetWeeklyMissionPool(missionId);
            if (pool is null) continue;
            progressMap.TryGetValue(missionId, out var um);
            response.Missions.Add(new WeeklyMissionDto
            {
                MissionId = missionId,
                ConditionType = (int)pool.ConditionType,
                NameKey = pool.NameKey,
                DescKey = pool.DescKey,
                TargetValue = pool.ConditionValue,
                Progress = Math.Min(um?.Progress ?? 0, pool.ConditionValue),
                IsCompleted = um?.IsCompleted ?? false,
                EpReward = pool.EpReward,
            });
        }

        foreach (var track in _staticData.GetAllWeeklyMissionTracks().OrderBy(x => x.EpThreshold))
        {
            response.Track.Add(new WeeklyMissionMilestoneDto
            {
                EpThreshold = track.EpThreshold,
                RewardGroupId = track.RewardGroupId,
                IsReached = totalEp >= track.EpThreshold,
                IsClaimed = claimed.Contains(track.EpThreshold),
            });
        }

        return response;
    }

    public async Task<ClaimWeeklyMissionResponse> ClaimAsync(long userId, int threshold, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var weekStart = CurrentWeekStart();
        await EnsureSetAsync(weekStart, ct);

        var track = _staticData.GetWeeklyMissionTrack(threshold)
            ?? throw new GameApiException(ErrorCodes.WeeklyMissionInvalidThreshold, $"Threshold {threshold} is not a milestone.");

        var state = await _db.UserWeeklyMissionState.FindAsync(userId, weekStart, ct);
        var totalEp = state?.TotalEp ?? 0;
        if (totalEp < threshold)
            throw new GameApiException(ErrorCodes.WeeklyMissionThresholdNotReached, "Cumulative EP below the milestone threshold.");

        var claimed = ParseThresholds(state?.ClaimedThresholds);
        if (claimed.Contains(threshold))
            throw new GameApiException(ErrorCodes.WeeklyMissionAlreadyClaimed, "Milestone reward already claimed this week.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (state is null)
        {
            state = new UserWeeklyMissionStateRow { UserId = userId, WeekStartDate = weekStart, TotalEp = totalEp };
            _db.UserWeeklyMissionState.Insert(state);
        }
        claimed.Add(threshold);
        state.ClaimedThresholds = string.Join(",", claimed.OrderBy(x => x));
        state.UpdatedAt = now;

        var (granted, currency) = await _reward.GrantRewardGroupAsync(userId, track.RewardGroupId, RewardVersion, correlationId, ct);
        _db.EventLogs.Insert(EventLogFactory.WeeklyMissionClaimed(userId, correlationId, weekStart, threshold, track.RewardGroupId));
        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return new ClaimWeeklyMissionResponse
        {
            EpThreshold = threshold,
            GrantedRewards = granted,
            Currency = currency ?? await CurrentCurrencyAsync(userId, ct),
            ServerTime = now,
        };
    }

    /// <summary>Stage-clear-flow seam: increments matching mission progress for the current week and accrues EP
    /// on completion. Mirrors the achievement report seams; no dedicated submit endpoint exists.</summary>
    public async Task ReportProgressAsync(long userId, WeeklyMissionConditionType conditionType, int increment, CancellationToken ct)
    {
        if (increment <= 0) return;

        var now = DateTimeOffset.UtcNow;
        var weekStart = CurrentWeekStart();
        var set = await EnsureSetAsync(weekStart, ct);

        var matched = SplitIds(set.MissionIds)
            .Select(id => _staticData.GetWeeklyMissionPool(id))
            .Where(p => p is not null && p.ConditionType == conditionType)
            .ToList();
        if (matched.Count == 0) return;

        var state = await _db.UserWeeklyMissionState.FindAsync(userId, weekStart, ct);
        if (state is null)
        {
            state = new UserWeeklyMissionStateRow { UserId = userId, WeekStartDate = weekStart, TotalEp = 0, UpdatedAt = now };
            _db.UserWeeklyMissionState.Insert(state);
        }

        var maxThreshold = MaxThreshold();
        var trackWasComplete = maxThreshold > 0 && state.TotalEp >= maxThreshold;
        var epGained = 0;

        foreach (var pool in matched)
        {
            var um = await _db.UserWeeklyMissions.FindAsync(userId, weekStart, pool!.MissionId, ct);
            if (um is null)
            {
                um = new UserWeeklyMissionsRow { UserId = userId, WeekStartDate = weekStart, MissionId = pool.MissionId, UpdatedAt = now };
                _db.UserWeeklyMissions.Insert(um);
            }
            if (um.IsCompleted) continue;

            um.Progress += increment;
            um.UpdatedAt = now;
            if (um.Progress >= pool.ConditionValue)
            {
                um.Progress = pool.ConditionValue;
                um.IsCompleted = true;
                epGained += pool.EpReward;
            }
        }

        if (epGained > 0)
        {
            state.TotalEp += epGained;
            state.UpdatedAt = now;
        }
        await _db.SaveAsync(ct);

        // Track full completion → Dedication achievements (ded_03 once, ded_04 cumulative 4 weeks).
        if (!trackWasComplete && maxThreshold > 0 && state.TotalEp >= maxThreshold)
            await _achievement.ReportCountAsync(userId, AchievementConditionType.WeeklyMissionComplete, 1, ct);
    }

    // Deterministic per-week mission selection from the pool (global — identical worldwide).
    private async Task<WeeklyMissionSetsRow> EnsureSetAsync(string weekStart, CancellationToken ct)
    {
        var row = await _db.WeeklyMissionSets.FindAsync(weekStart, ct);
        if (row is not null) return row;

        var ordered = _staticData.GetAllWeeklyMissionPools()
            .OrderBy(x => StableHash(weekStart + ":" + x.MissionId))
            .ThenBy(x => x.MissionId)
            .Select(x => x.MissionId)
            .Take(MissionsPerWeek)
            .ToList();

        row = new WeeklyMissionSetsRow
        {
            WeekStartDate = weekStart,
            MissionIds = string.Join(",", ordered),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.WeeklyMissionSets.Insert(row);
        await _db.SaveAsync(ct);
        return row;
    }

    private int MaxThreshold()
    {
        var tracks = _staticData.GetAllWeeklyMissionTracks();
        return tracks.Count == 0 ? 0 : tracks.Max(x => x.EpThreshold);
    }

    private async Task<CurrencySnapshot> CurrentCurrencyAsync(long userId, CancellationToken ct)
    {
        var c = await _db.UserCurrency.FindAsync(userId, ct);
        return new CurrencySnapshot { SoftAmount = c?.SoftAmount ?? 0 };
    }

    private static string[] SplitIds(string ids) => ids.Split(',', StringSplitOptions.RemoveEmptyEntries);

    private static HashSet<int> ParseThresholds(string? csv)
    {
        var set = new HashSet<int>();
        if (string.IsNullOrEmpty(csv)) return set;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(part, out var v)) set.Add(v);
        return set;
    }

    private static DateTimeOffset WeekEndUtc(string weekStart)
    {
        var monday = DateTime.ParseExact(weekStart, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new DateTimeOffset(monday.AddDays(7), TimeSpan.Zero);
    }

    private static uint StableHash(string s)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var c in s) { hash ^= c; hash *= 16777619; }
            return hash;
        }
    }
}
