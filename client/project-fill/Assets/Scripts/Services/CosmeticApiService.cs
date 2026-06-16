using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Cosmetic;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class CosmeticApiService : MonoBehaviour
    {
        public static CosmeticApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchCosmetics(Action<CosmeticListResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/cosmetics", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<CosmeticListResponseJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void UnlockCosmetic(string cosmeticId, Action<UnlockCosmeticResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Post($"/api/cosmetics/{cosmeticId}/unlock", "{}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<UnlockCosmeticResponseJson>(result);
                var response = json.ToContract();
                if (response?.Currency != null)
                    CurrencyApiService.Instance?.UpdateGold(response.Currency);
                onSuccess?.Invoke(response);
            });
        }

        public void SetActive(SetActiveCosmeticRequest request, Action<ActiveCosmeticsDto> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"chipSkin\":\"{Escape(request.ChipSkin)}\",\"laneSkin\":\"{Escape(request.LaneSkin)}\",\"boardSkin\":\"{Escape(request.BoardSkin)}\",\"useCustomBoardSkin\":{(request.UseCustomBoardSkin ? "true" : "false")}}}";
            NetworkService.Instance.Post("/api/cosmetics/active", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<SetActiveCosmeticResponseJson>(result);
                onSuccess?.Invoke(json?.active?.ToContract() ?? new ActiveCosmeticsDto());
            });
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class CosmeticItemJson
        {
            public string cosmeticId;
            public int category;
            public string nameKey;
            public string descKey;
            public int unlockType;
            public int unlockCost;
            public string unlockConditionId;
            public string previewRes;
            public int sortOrder;
            public bool unlocked;

            public CosmeticItemDto ToContract() => new CosmeticItemDto
            {
                CosmeticId = cosmeticId,
                Category = category,
                NameKey = nameKey,
                DescKey = descKey,
                UnlockType = unlockType,
                UnlockCost = unlockCost,
                UnlockConditionId = unlockConditionId,
                PreviewRes = previewRes,
                SortOrder = sortOrder,
                Unlocked = unlocked,
            };
        }

        [Serializable]
        private class ActiveCosmeticsJson
        {
            public string chipSkin;
            public string laneSkin;
            public string boardSkin;
            public bool useCustomBoardSkin;

            public ActiveCosmeticsDto ToContract() => new ActiveCosmeticsDto
            {
                ChipSkin = chipSkin,
                LaneSkin = laneSkin,
                BoardSkin = boardSkin,
                UseCustomBoardSkin = useCustomBoardSkin,
            };
        }

        [Serializable]
        private class CosmeticListResponseJson
        {
            public List<CosmeticItemJson> items;
            public ActiveCosmeticsJson active;

            public CosmeticListResponse ToContract()
            {
                var response = new CosmeticListResponse();
                if (items != null)
                {
                    foreach (var item in items)
                        if (item != null) response.Items.Add(item.ToContract());
                }
                response.Active = active?.ToContract() ?? new ActiveCosmeticsDto();
                return response;
            }
        }

        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;

            public ProjectFill.Contracts.Currency.CurrencySnapshot ToContract() => new ProjectFill.Contracts.Currency.CurrencySnapshot
            {
                SoftAmount = softAmount
            };
        }

        [Serializable]
        private class UnlockCosmeticResponseJson
        {
            public string cosmeticId;
            public CurrencySnapshotJson currency;

            public UnlockCosmeticResponse ToContract() => new UnlockCosmeticResponse
            {
                CosmeticId = cosmeticId,
                Currency = currency?.ToContract() ?? new ProjectFill.Contracts.Currency.CurrencySnapshot(),
            };
        }

        [Serializable]
        private class SetActiveCosmeticResponseJson
        {
            public ActiveCosmeticsJson active;
        }
    }
}
#pragma warning restore 0649
