using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Inventory;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class InventoryApiService : MonoBehaviour
    {
        public static InventoryApiService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchInventory(Action<InventorySnapshot> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/inventory", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<InventorySnapshotJson>(result);
                var snapshot = json.ToContract();
                PlayerProgressService.Instance?.SetInventory(snapshot);
                onSuccess?.Invoke(snapshot);
            });
        }

        public void SpendItem(int itemId, int amount, string reason, Action<InventorySnapshot> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"itemId\":{itemId},\"amount\":{amount},\"reason\":\"{Escape(reason)}\"}}";
            NetworkService.Instance.Post("/api/inventory/spend", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<InventorySnapshotJson>(result);
                var snapshot = json.ToContract();
                PlayerProgressService.Instance?.SetInventory(snapshot);
                onSuccess?.Invoke(snapshot);
            });
        }

        public void BuyItem(int itemId, Action<BuyItemResponse> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"itemId\":{itemId}}}";
            NetworkService.Instance.Post("/api/inventory/buy", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<BuyItemResponseJson>(result);
                var response = json.ToContract();
                if (response != null)
                {
                    PlayerProgressService.Instance?.SetInventory(response.Inventory);
                    PlayerProgressService.Instance?.SetGold((int)response.Currency.SoftAmount);
                }
                onSuccess?.Invoke(response);
            });
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class InventoryItemJson
        {
            public int itemId;
            public int count;

            public InventoryItemDto ToContract() => new InventoryItemDto
            {
                ItemId = itemId,
                Count = count
            };
        }

        [Serializable]
        private class InventorySnapshotJson
        {
            public List<InventoryItemJson> items;

            public InventorySnapshot ToContract()
            {
                var snapshot = new InventorySnapshot();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (item != null)
                            snapshot.Items.Add(item.ToContract());
                    }
                }
                return snapshot;
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
        private class BuyItemResponseJson
        {
            public InventorySnapshotJson inventory;
            public CurrencySnapshotJson currency;
            public string serverTime;

            public BuyItemResponse ToContract()
            {
                return new BuyItemResponse
                {
                    Inventory = inventory?.ToContract() ?? new InventorySnapshot(),
                    Currency = currency?.ToContract() ?? new ProjectFill.Contracts.Currency.CurrencySnapshot(),
                    ServerTime = DateTimeOffset.TryParse(serverTime, out var dt) ? dt : DateTimeOffset.UtcNow
                };
            }
        }
    }
}
#pragma warning restore 0649
