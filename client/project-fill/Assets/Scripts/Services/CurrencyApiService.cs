using System;
using ProjectFill.Contracts.Currency;
using UnityEngine;

#pragma warning disable 0649
namespace Game.Services
{
    public class CurrencyApiService : MonoBehaviour
    {
        private static CurrencyApiService _instance;

        public static CurrencyApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[CurrencyApiService] Instance is missing! Ensure it is placed in the Boot scene.");
                }
                return _instance;
            }
        }

        public event Action<long, long> OnGoldChanged; // (amount, delta)

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Canonical gold setter. Single chokepoint for every server-confirmed gold change.
        // Ignores an empty/no-op snapshot (server omitted currency → contract default 0/0,
        // or a missing user_currency row read back as 0) so a balance-less response never
        // wipes gold to 0. authoritative=true (direct balance reads like FetchGold/SpendGold,
        // post-transaction buy/IAP balances) always applies — even a true 0.
        public void UpdateGold(CurrencySnapshot snapshot, bool authoritative = false)
        {
            if (snapshot == null) return;
            if (!authoritative && snapshot.SoftAmount == 0 && snapshot.SoftDelta == 0) return;
            PlayerProgressService.Instance?.SetGold((int)snapshot.SoftAmount);
            OnGoldChanged?.Invoke(snapshot.SoftAmount, snapshot.SoftDelta);
        }

        public void FetchGold(Action<CurrencySnapshot> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/currency", NetworkRetryOptions.LobbyAndSave, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<CurrencySnapshotJson>(result);
                var snapshot = json.ToContract();
                UpdateGold(snapshot, authoritative: true); // authoritative balance read — a true 0 must apply
                onSuccess?.Invoke(snapshot);
            });
        }

        public void SpendGold(int amount, string reason, Action<CurrencySnapshot> onSuccess = null, Action<string> onError = null)
        {
            var body = $"{{\"amount\":{amount},\"reason\":\"{Escape(reason)}\"}}";
            NetworkService.Instance.Post("/api/currency/spend", body, (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<CurrencySnapshotJson>(result);
                var snapshot = json.ToContract();
                UpdateGold(snapshot, authoritative: true); // post-spend balance — a true 0 (spent all) must apply
                onSuccess?.Invoke(snapshot);
            });
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        [Serializable]
        private class CurrencySnapshotJson
        {
            public long softAmount;
            public long softDelta;

            public CurrencySnapshot ToContract() => new CurrencySnapshot { SoftAmount = softAmount, SoftDelta = softDelta };
        }
    }
}
#pragma warning restore 0649
