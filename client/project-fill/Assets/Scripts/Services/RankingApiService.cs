using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Ranking;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class RankingApiService : MonoBehaviour
    {
        public static RankingApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchGlobalPage(string rankingType, int offset, int limit, Action<RankingPageResponse> onSuccess, Action<string> onError = null)
        {
            NetworkService.Instance.Get($"/api/rankings/global/{rankingType}?offset={offset}&limit={limit}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<RankingPageJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void FetchMyGlobalRank(string rankingType, Action<MyRankingResponse> onSuccess, Action<string> onError = null)
        {
            NetworkService.Instance.Get($"/api/rankings/global/{rankingType}/me", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<MyRankingJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void FetchWeeklyPage(int offset, int limit, Action<RankingPageResponse> onSuccess, Action<string> onError = null)
        {
            NetworkService.Instance.Get($"/api/rankings/weekly?offset={offset}&limit={limit}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<RankingPageJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void FetchMyWeeklyRank(Action<MyRankingResponse> onSuccess, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/rankings/weekly/me", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<MyRankingJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void FetchMyStageRank(int stageId, Action<StageRankResponse> onSuccess, Action<string> onError = null)
        {
            NetworkService.Instance.Get($"/api/rankings/stages/{stageId}/me", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<StageRankJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        [Serializable]
        private class RankingEntryJson
        {
            public long userId;
            public string displayName;
            public int avatarId;
            public int rank;
            public int score;

            public RankingEntryDto ToContract() => new RankingEntryDto
            {
                UserId      = userId,
                DisplayName = displayName ?? string.Empty,
                AvatarId    = avatarId,
                Rank        = rank,
                Score       = score,
            };
        }

        [Serializable]
        private class RankingPageJson
        {
            public string rankingType;
            public int offset;
            public int limit;
            public List<RankingEntryJson> entries;

            public RankingPageResponse ToContract()
            {
                var response = new RankingPageResponse
                {
                    RankingType = rankingType ?? string.Empty,
                    Offset      = offset,
                    Limit       = limit,
                };

                if (entries != null)
                    foreach (var entry in entries)
                        if (entry != null)
                            response.Entries.Add(entry.ToContract());

                return response;
            }
        }

        [Serializable]
        private class MyRankingJson
        {
            public string rankingType;
            public RankingEntryJson entry;

            public MyRankingResponse ToContract() => new MyRankingResponse
            {
                RankingType = rankingType ?? string.Empty,
                Entry       = entry?.ToContract(),
            };
        }

        [Serializable]
        private class StageRankJson
        {
            public int stageId;
            public int rank;
            public int bestMovesUsed;

            public StageRankResponse ToContract() => new StageRankResponse
            {
                StageId       = stageId,
                Rank          = rank > 0 ? rank : null,
                BestMovesUsed = bestMovesUsed > 0 ? bestMovesUsed : null,
            };
        }
    }
}
#pragma warning restore 0649
