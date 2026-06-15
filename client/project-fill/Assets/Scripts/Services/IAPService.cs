using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Utils;
using ProjectFill.Data.Generated;
using ProjectFill.Contracts.Iap;
using ProjectFill.Contracts.Rewards;

namespace Game.Services
{
    public class IAPService : MonoBehaviour
    {
        private static IAPService _instance;

        public static IAPService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("IAPService");
                    _instance = go.AddComponent<IAPService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private List<IapProduct> _products = new List<IapProduct>();
        private Dictionary<int, int?> _remainingPurchases = new Dictionary<int, int?>();

        public List<IapProduct> Products => _products;

        public bool IsNoAdsPurchased
        {
            get
            {
                if (PlayerProgressService.Instance != null)
                    return PlayerProgressService.Instance.IsNoAds;
                return PlayerPrefs.GetInt("is_no_ads", 0) == 1;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                var list = CsvLoader.Load<IapProduct>(IapProduct.ResourcePath);
                if (list != null)
                {
                    _products.AddRange(list);
                    Debug.Log($"[IAPService] Loaded {_products.Count} products from CSV.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IAPService] Failed to load IAP products CSV: {ex.Message}");
            }
        }

        public IapProduct GetProduct(int infoId)
        {
            return _products.Find(p => p.info_id == infoId);
        }

        public IapProduct GetProductByStoreId(string storeProductId)
        {
            return _products.Find(p => p.store_product_id == storeProductId);
        }

        public int? GetRemainingPurchases(int infoId)
        {
            return _remainingPurchases.TryGetValue(infoId, out var v) ? v : null;
        }

        public void FetchProductStatuses(Action<bool> onComplete = null)
        {
            NetworkService.Instance.Get("/api/iap/products", (ok, text) =>
            {
                if (!ok) { onComplete?.Invoke(false); return; }
                try
                {
                    var resp = JsonUtility.FromJson<GetIapProductsResponseJson>(text);
                    if (resp?.products != null)
                    {
                        _remainingPurchases.Clear();
                        foreach (var p in resp.products)
                            _remainingPurchases[p.infoId] = p.remainingPurchases < 0 ? null : (int?)p.remainingPurchases;
                    }
                    onComplete?.Invoke(true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[IAPService] FetchProductStatuses parse error: {ex.Message}");
                    onComplete?.Invoke(false);
                }
            });
        }

        public void PurchaseProduct(string storeProductId, Action<bool, string> onComplete)
        {
            var product = GetProductByStoreId(storeProductId);
            if (product == null)
            {
                onComplete?.Invoke(false, "PRODUCT_NOT_FOUND");
                return;
            }

            var remaining = GetRemainingPurchases(product.info_id);
            if (remaining.HasValue && remaining.Value <= 0)
            {
                onComplete?.Invoke(false, "PURCHASE_LIMIT_REACHED");
                return;
            }

            var mockOrderId = "MOCK_GPA_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            var mockToken = "MOCK_TOK_" + Guid.NewGuid().ToString("N").ToUpper();

            var request = new VerifyIapRequestJson
            {
                infoId = product.info_id,
                storeProductId = storeProductId,
                orderId = mockOrderId,
                purchaseToken = mockToken,
                price = product.price_usd,
                currency = "USD",
                platform = "mock",
                rawReceipt = "{}"
            };

            var jsonPayload = JsonUtility.ToJson(request);
            Debug.Log($"[IAPService] Requesting verify: infoId={product.info_id} storeId={storeProductId}");

            NetworkService.Instance.Post("/api/iap/verify", jsonPayload, (ok, text) =>
            {
                if (!ok)
                {
                    string errCode;
                    try { errCode = JsonUtility.FromJson<ErrorResponseJson>(text)?.code ?? "NETWORK_ERROR"; }
                    catch { errCode = "NETWORK_ERROR"; }

                    Debug.LogWarning($"[IAPService] Purchase verification failed: {errCode}");

                    if (errCode == "ALREADY_OWNED")
                    {
                        PlayerApiService.Instance.FetchProgress((fetchOk, progress) =>
                        {
                            if (fetchOk && progress != null)
                                PlayerProgressService.Instance.LoadFromServer(progress);
                            onComplete?.Invoke(false, errCode);
                        });
                        return;
                    }

                    onComplete?.Invoke(false, errCode);
                    return;
                }

                try
                {
                    var response = JsonUtility.FromJson<VerifyIapResponseJson>(text);
                    if (response != null && response.success)
                    {
                        if (PlayerProgressService.Instance != null)
                        {
                            if (response.isNoAds)
                                PlayerProgressService.Instance.SetNoAds(true);

                            PlayerProgressService.Instance.SetGold((int)response.currentGold);

                            if (response.grantedRewards != null)
                            {
                                foreach (var reward in response.grantedRewards)
                                {
                                    if (reward.rewardType == "ITEM" && reward.targetId > 0 && reward.amount > 0)
                                    {
                                        PlayerProgressService.Instance.SetItemCount(
                                            reward.targetId,
                                            PlayerProgressService.Instance.GetItemCount(reward.targetId) + reward.amount);
                                    }
                                }
                            }

                            _remainingPurchases[product.info_id] = response.remainingPurchases < 0
                                ? null
                                : (int?)response.remainingPurchases;

                            PlayerPrefs.Save();
                        }

                        Debug.Log($"[IAPService] Purchase success: {storeProductId}. Gold={response.currentGold}, NoAds={response.isNoAds}");
                        onComplete?.Invoke(true, "");
                    }
                    else
                    {
                        string err = response != null ? response.errorCode : "VERIFY_FAILED";
                        onComplete?.Invoke(false, err);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[IAPService] Exception during parse purchase response: {ex.Message}");
                    onComplete?.Invoke(false, "PARSE_ERROR");
                }
            });
        }

        // --- JSON Helper Classes ---
        [Serializable]
        private class VerifyIapRequestJson
        {
            public int infoId;
            public string storeProductId;
            public string orderId;
            public string purchaseToken;
            public double price;
            public string currency;
            public string platform;
            public string rawReceipt;
        }

        [Serializable]
        private class GrantedRewardJson
        {
            public string rewardType;
            public int targetId;
            public int amount;
            public int durationSeconds;
        }

        [Serializable]
        private class VerifyIapResponseJson
        {
            public bool success;
            public string errorCode;
            public bool isNoAds;
            public long currentGold;
            public int remainingPurchases;
            public GrantedRewardJson[] grantedRewards;
        }

        [Serializable]
        private class IapProductStatusJson
        {
            public int infoId;
            public string storeProductId;
            public int remainingPurchases; // -1 = null (unlimited)
        }

        [Serializable]
        private class GetIapProductsResponseJson
        {
            public IapProductStatusJson[] products;
        }
    }
}
