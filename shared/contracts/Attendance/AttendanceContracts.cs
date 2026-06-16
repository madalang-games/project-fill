#nullable enable

using System.Collections.Generic;

namespace ProjectFill.Contracts.Attendance
{
    public sealed class AttendanceDayDto
    {
        public int Day { get; set; }
        public int RewardGroupId { get; set; }
        public bool IsClaimed { get; set; }
        public bool IsToday { get; set; }
    }

    public sealed class AttendanceStatusResponse
    {
        public int CurrentDay { get; set; }
        public int CurrentCycle { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int TotalAttendedDays { get; set; }
        public bool ClaimedToday { get; set; }
        public List<AttendanceDayDto> Days { get; set; } = new List<AttendanceDayDto>();
        public System.DateTimeOffset ServerTime { get; set; }
    }

    public sealed class AttendanceClaimResponse
    {
        public int Day { get; set; }
        public int Cycle { get; set; }
        public int Streak { get; set; }
        public int TotalAttendedDays { get; set; }
        public List<ProjectFill.Contracts.Rewards.GrantedRewardDto> GrantedRewards { get; set; } = new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>();
        public int MilestoneRewardGroupId { get; set; }
        public List<ProjectFill.Contracts.Rewards.GrantedRewardDto> MilestoneRewards { get; set; } = new List<ProjectFill.Contracts.Rewards.GrantedRewardDto>();
        public List<string> UnlockedCosmetics { get; set; } = new List<string>();
        public ProjectFill.Contracts.Currency.CurrencySnapshot Currency { get; set; } = new ProjectFill.Contracts.Currency.CurrencySnapshot();
        public System.DateTimeOffset ServerTime { get; set; }
    }
}
