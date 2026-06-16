using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Logging;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Attendance;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Attendance;

public sealed class AttendanceService
{
    private const int CycleLength = 7;
    private readonly AppDbContext _db;
    private readonly RewardService _reward;
    private readonly CosmeticService _cosmetic;
    private readonly IStaticDataService _staticData;

    public AttendanceService(AppDbContext db, RewardService reward, CosmeticService cosmetic, IStaticDataService staticData)
    {
        _db = db;
        _reward = reward;
        _cosmetic = cosmetic;
        _staticData = staticData;
    }

    public async Task<AttendanceStatusResponse> GetStatusAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.UtcDateTime.Date;
        var row = await _db.UserLoginAttendance.FindAsync(userId, ct);

        var claimedToday = row?.LastAttendedDate?.UtcDateTime.Date == today && row?.LastAttendedDate is not null;

        int pendDay, pendCycle, claimedCount;
        if (row is null)
        {
            pendDay = 1; pendCycle = 1; claimedCount = 0;
        }
        else if (claimedToday)
        {
            pendDay = row.CurrentDay; pendCycle = row.CurrentCycle; claimedCount = row.CurrentDay;
        }
        else if (row.CurrentDay >= CycleLength)
        {
            pendDay = 1; pendCycle = row.CurrentCycle + 1; claimedCount = 0;
        }
        else
        {
            pendDay = row.CurrentDay + 1; pendCycle = row.CurrentCycle; claimedCount = row.CurrentDay;
        }

        var response = new AttendanceStatusResponse
        {
            CurrentDay = pendDay,
            CurrentCycle = pendCycle,
            CurrentStreak = row?.CurrentStreak ?? 0,
            BestStreak = row?.BestStreak ?? 0,
            TotalAttendedDays = row?.TotalAttendedDays ?? 0,
            ClaimedToday = claimedToday,
            ServerTime = now,
        };

        for (int d = 1; d <= CycleLength; d++)
        {
            response.Days.Add(new AttendanceDayDto
            {
                Day = d,
                RewardGroupId = RewardGroupFor(pendCycle, d),
                IsClaimed = d <= claimedCount,
                IsToday = !claimedToday && d == pendDay,
            });
        }
        return response;
    }

    public async Task<AttendanceClaimResponse> ClaimAsync(long userId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.UtcDateTime.Date;
        var row = await _db.UserLoginAttendance.FindAsync(userId, ct);

        if (row?.LastAttendedDate?.UtcDateTime.Date == today && row?.LastAttendedDate is not null)
            throw new GameApiException(ErrorCodes.AttendanceAlreadyClaimed, "Attendance already claimed today.");

        int newDay, newCycle, newStreak;
        if (row is null)
        {
            newDay = 1; newCycle = 1; newStreak = 1;
        }
        else
        {
            var yesterday = today.AddDays(-1);
            newStreak = row.LastAttendedDate?.UtcDateTime.Date == yesterday ? row.CurrentStreak + 1 : 1;
            if (row.CurrentDay >= CycleLength) { newDay = 1; newCycle = row.CurrentCycle + 1; }
            else { newDay = row.CurrentDay + 1; newCycle = row.CurrentCycle; }
        }
        var newTotal = (row?.TotalAttendedDays ?? 0) + 1;
        var bestStreak = Math.Max(row?.BestStreak ?? 0, newStreak);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (row is null)
        {
            row = new UserLoginAttendanceRow { UserId = userId };
            _db.UserLoginAttendance.Insert(row);
        }
        row.CurrentDay = newDay;
        row.CurrentCycle = newCycle;
        row.CurrentStreak = newStreak;
        row.BestStreak = bestStreak;
        row.TotalAttendedDays = newTotal;
        row.LastAttendedDate = new DateTimeOffset(today, TimeSpan.Zero);
        row.RewardClaimedToday = true;
        row.UpdatedAt = now;

        var groupId = RewardGroupFor(newCycle, newDay);
        var (granted, currency) = await _reward.GrantRewardGroupAsync(userId, groupId, 1, correlationId, ct);

        var response = new AttendanceClaimResponse
        {
            Day = newDay,
            Cycle = newCycle,
            Streak = newStreak,
            TotalAttendedDays = newTotal,
            GrantedRewards = granted,
        };

        foreach (var milestone in _staticData.GetAllDailyLoginMilestones().Where(m => m.ThresholdDays == newTotal))
        {
            var (mGranted, mCurrency) = await _reward.GrantRewardGroupAsync(userId, milestone.RewardGroupId, 1, correlationId, ct);
            response.MilestoneRewardGroupId = milestone.RewardGroupId;
            response.MilestoneRewards.AddRange(mGranted);
            if (mCurrency is not null) currency = mCurrency;

            if (!string.IsNullOrEmpty(milestone.CosmeticConditionId))
                response.UnlockedCosmetics.AddRange(await _cosmetic.UnlockByConditionAsync(userId, milestone.CosmeticConditionId, correlationId, ct));
        }

        _db.EventLogs.Insert(EventLogFactory.AttendanceClaimed(userId, correlationId, newCycle, newDay, newStreak, groupId));
        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        response.Currency = currency ?? await CurrentCurrencyAsync(userId, ct);
        response.ServerTime = now;
        return response;
    }

    private int RewardGroupFor(int cycle, int day)
    {
        var cycleType = cycle <= 1 ? AttendanceCycleType.First : AttendanceCycleType.Repeat;
        var entry = _staticData.GetAllDailyLoginRewards().FirstOrDefault(x => x.CycleType == cycleType && x.Day == day);
        return entry?.RewardGroupId ?? 0;
    }

    private async Task<CurrencySnapshot> CurrentCurrencyAsync(long userId, CancellationToken ct)
    {
        var c = await _db.UserCurrency.FindAsync(userId, ct);
        return new CurrencySnapshot { SoftAmount = c?.SoftAmount ?? 0 };
    }
}
