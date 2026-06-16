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
                        AvatarId = res.avatarId
                    };
                    AuthService.Instance.UpdateCachedProfile(response.DisplayName, response.AvatarId);
                    if (PlayerProgressService.Instance != null)
                    {
                        // BoardThemeId dropped from contract — equip from the raw server JSON field.
                        PlayerProgressService.Instance.SetEquippedBoardTheme(res.boardThemeId);
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
            public List<int> unlockedAvatarIds;
            public bool isNoAds;

            public PlayerProgressResponse ToContract()
                => new PlayerProgressResponse
                {
                    UnlockedAvatarIds = unlockedAvatarIds ?? new List<int>(),
                    IsNoAds = isNoAds,
                };
        }
    }
}
#pragma warning restore 0649
