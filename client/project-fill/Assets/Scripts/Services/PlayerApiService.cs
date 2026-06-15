using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Player;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class PlayerApiService : MonoBehaviour
    {
        public static PlayerApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchProgress(Action<bool, PlayerProgressResponse> onComplete)
        {
            NetworkService.Instance.Get("/api/player/progress", NetworkRetryOptions.LobbyAndSave, (ok, text) =>
            {
                if (!ok) { onComplete?.Invoke(false, null); return; }
                try
                {
                    var json = JsonUtility.FromJson<PlayerProgressJson>(text);
                    onComplete?.Invoke(true, json?.ToContract());
                }
                catch
                {
                    onComplete?.Invoke(false, null);
                }
            });
        }

        public void UpdateProfile(string displayName, int? avatarId, int? boardThemeId, Action<bool, UserProfileUpdateResponse, string> onComplete)
        {
            var parts = new List<string>();
            if (displayName != null)
            {
                parts.Add($"\"displayName\":\"{displayName}\"");
            }
            if (avatarId.HasValue)
            {
                parts.Add($"\"avatarId\":{avatarId.Value}");
            }
            if (boardThemeId.HasValue)
            {
                parts.Add($"\"boardThemeId\":{boardThemeId.Value}");
            }
            string json = "{" + string.Join(",", parts) + "}";

            NetworkService.Instance.Post("/api/player/profile", json, NetworkRetryOptions.LobbyAndSave, (ok, text) =>
            {
                if (!ok) { onComplete?.Invoke(false, null, text); return; }
                try
                {
                    var res = JsonUtility.FromJson<UserProfileUpdateResponseJson>(text);
                    var response = new UserProfileUpdateResponse
                    {
                        DisplayName = res.displayName,
                        AvatarId = res.avatarId,
                        BoardThemeId = res.boardThemeId
                    };
                    AuthService.Instance.UpdateCachedProfile(response.DisplayName, response.AvatarId);
                    if (PlayerProgressService.Instance != null)
                    {
                        PlayerProgressService.Instance.SetEquippedBoardTheme(response.BoardThemeId);
                    }
                    CurrencyApiService.Instance?.UpdateGold(new ProjectFill.Contracts.Currency.CurrencySnapshot
                    {
                        SoftAmount = res.currency.softAmount,
                        SoftDelta = res.currency.softDelta
                    });
                    onComplete?.Invoke(true, response, null);
                }
                catch (Exception ex)
                {
                    onComplete?.Invoke(false, null, ex.Message);
                }
            });
        }

        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;
            public long softDelta;
        }

        [Serializable]
        private class UserProfileUpdateResponseJson
        {
            public string displayName;
            public int avatarId;
            public int boardThemeId;
            public CurrencySnapshotJson currency;
        }

        [Serializable]
        private class PlayerProgressJson
        {
            public int maxClearedStageId;
            public StageProgressEntryJson[] stages;
            public List<int> unlockedAvatarIds;
            public int equippedBoardThemeId;
            public List<int> unlockedBoardThemeIds;

            public PlayerProgressResponse ToContract()
            {
                var response = new PlayerProgressResponse
                {
                    MaxClearedStageId = maxClearedStageId,
                    Stages = new List<StageProgressEntry>(),
                    UnlockedAvatarIds = unlockedAvatarIds ?? new List<int>(),
                    EquippedBoardThemeId = equippedBoardThemeId == 0 ? 1 : equippedBoardThemeId,
                    UnlockedBoardThemeIds = unlockedBoardThemeIds ?? new List<int>()
                };
                if (stages != null)
                    foreach (var s in stages)
                        response.Stages.Add(s.ToContract());
                return response;
            }
        }

        [Serializable]
        private class StageProgressEntryJson
        {
            public int stageId;
            public int bestStar;

            public StageProgressEntry ToContract()
                => new StageProgressEntry { StageId = stageId, BestStar = bestStar };
        }
    }
}
#pragma warning restore 0649
