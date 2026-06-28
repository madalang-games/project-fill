// Unity IAP legacy callback API (IStoreListener/UnityPurchasing) is marked obsolete in 5.x
// pending a V5 upgrade; it is intentionally used here for a stable callback flow that matches
// AdMobService. Suppress the obsolete warnings only for this file.
#pragma warning disable CS0618
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using Game.Utils;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Data.Generated;

namespace Game.Services
{
    /// <summary>
    /// Unity IAP adapter. Initializes the native store from the enabled IAP catalog, drives
    /// <c>InitiatePurchase</c>, then verifies the real store receipt on the server before
    /// confirming the transaction (Pending → ConfirmPendingPurchase). On unconfirmed redelivery
    /// (app relaunch with a pending transaction) the same server verification path runs.
    /// In the Editor there is no native store, so purchases route through mock verification.
    /// </summary>
    public class IAPService : MonoBehaviour, IDetailedStoreListener
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

        private const string PlatformMock = "mock";
        private const string VerifyEndpoint = "/api/iap/verify";
        private const int MaxVerifyRetries = 2;
        private const float VerifyRetryDelaySeconds = 2f;

        private readonly List<IapProduct> _products = new List<IapProduct>();
        private readonly Dictionary<int, int?> _remainingPurchases = new Dictionary<int, int?>();
        private readonly Dictionary<string, IapProduct> _byStoreId = new Dictionary<string, IapProduct>();
        private readonly Dictionary<string, Action<bool, string>> _pending = new Dictionary<string, Action<bool, string>>();

        private IStoreController _controller;

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
#if !UNITY_EDITOR
            InitializePurchasing();
#endif
        }

        private void LoadProducts()
        {
            try
            {
                var list = CsvLoader.Load<IapProduct>(IapProduct.ResourcePath);
                if (list != null)
                {
                    _products.AddRange(list);
                    foreach (var p in _products)
                    {
                        if (p.is_enabled && !string.IsNullOrEmpty(p.store_product_id))
                            _byStoreId[p.store_product_id] = p;
                    }
                    Debug.Log($"[IAPService] Loaded {_products.Count} products from CSV.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IAPService] Failed to load IAP products CSV: {ex.Message}");
            }
        }

        private void InitializePurchasing()
        {
            if (_controller != null || _byStoreId.Count == 0) return;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var p in _byStoreId.Values)
                builder.AddProduct(p.store_product_id, ToProductType(p.product_type));
            UnityPurchasing.Initialize(this, builder);
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

#if UNITY_EDITOR
            // Editor has no native store; route through mock verification so the shop flow stays testable.
            VerifyWithServer(product.info_id, storeProductId, product.price_usd, PlatformMock, "{}", onComplete);
#else
            if (_controller == null)
            {
                onComplete?.Invoke(false, "IAP_NOT_READY");
                InitializePurchasing();
                return;
            }

            var storeProduct = _controller.products.WithID(storeProductId);
            if (storeProduct == null || !storeProduct.availableToPurchase)
            {
                onComplete?.Invoke(false, "IAP_PRODUCT_UNAVAILABLE");
                return;
            }

            _pending[storeProductId] = onComplete;
            _controller.InitiatePurchase(storeProduct);
#endif
        }

        // --- IDetailedStoreListener ---

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
            => _controller = controller;

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
            => Debug.LogError($"[IAPService] init failed: {error} {message}");

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;
            var storeId = product.definition.id;
            _byStoreId.TryGetValue(storeId, out var meta);
            int infoId = meta?.info_id ?? 0;
            double price = meta?.price_usd ?? 0;

            // Verify on the server, then confirm so the store stops redelivering this transaction.
            VerifyWithServer(infoId, storeId, price, StorePlatform(), product.receipt ?? "", (success, err) =>
            {
                // Confirm on success, and also when the server reports the receipt already redeemed:
                // the grant happened on a prior attempt but ConfirmPendingPurchase was lost, so confirm
                // now to stop the store from redelivering this transaction on every relaunch.
                if (success || err == ServerErrorCodes.DuplicateOrder)
                    _controller.ConfirmPendingPurchase(product);

                if (_pending.TryGetValue(storeId, out var cb))
                {
                    _pending.Remove(storeId);
                    cb?.Invoke(success, err);
                }
            });

            // Keep the transaction open until the server grants and we ConfirmPendingPurchase.
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
            => FailPending(product?.definition?.id, failureReason);

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
            => FailPending(product?.definition?.id, failureDescription?.reason ?? PurchaseFailureReason.Unknown);

        private void FailPending(string storeId, PurchaseFailureReason reason)
        {
            if (string.IsNullOrEmpty(storeId)) return;
            var code = reason == PurchaseFailureReason.UserCancelled ? "IAP_CANCELLED" : "IAP_PURCHASE_FAILED";
            if (_pending.TryGetValue(storeId, out var cb))
            {
                _pending.Remove(storeId);
                cb?.Invoke(false, code);
            }
        }

        private void VerifyWithServer(int infoId, string storeProductId, double price,
            string platform, string rawReceipt, Action<bool, string> onComplete)
        {
            var request = new VerifyIapRequestJson
            {
                infoId = infoId,
                storeProductId = storeProductId ?? string.Empty,
                price = price,
                currency = "USD",
                platform = platform,
                rawReceipt = string.IsNullOrEmpty(rawReceipt) ? "{}" : rawReceipt,
            };

            // Mock receipts carry no real store ids; generate them so the server idempotency guard works.
            if (platform == PlatformMock)
            {
                request.orderId = "MOCK_GPA_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
                request.purchaseToken = "MOCK_TOK_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
            }

            StartCoroutine(VerifyRoutine(request, onComplete));
        }

        // Verify on the server. IAP_VERIFY_PENDING means the store/Google API was transiently
        // unreachable; retry a few times. The final outcome is always success or a terminal error.
        private IEnumerator VerifyRoutine(VerifyIapRequestJson request, Action<bool, string> onComplete)
        {
            var jsonPayload = JsonUtility.ToJson(request);
            Debug.Log($"[IAPService] Requesting verify: infoId={request.infoId} storeId={request.storeProductId} platform={request.platform}");

            for (int attempt = 0; attempt <= MaxVerifyRetries; attempt++)
            {
                if (attempt > 0)
                    yield return new WaitForSecondsRealtime(VerifyRetryDelaySeconds);

                bool done = false;
                bool ok = false;
                string body = null;
                NetworkService.Instance.Post(VerifyEndpoint, jsonPayload, (responseOk, text) =>
                {
                    ok = responseOk;
                    body = text;
                    done = true;
                });
                yield return new WaitUntil(() => done);

                if (ok)
                {
                    string applyError = ApplyVerifyResponse(request.storeProductId, body);
                    onComplete?.Invoke(string.IsNullOrEmpty(applyError), applyError ?? string.Empty);
                    yield break;
                }

                string errCode = ServerErrorCodes.Parse(body);
                if (errCode == ServerErrorCodes.IapVerifyPending && attempt < MaxVerifyRetries)
                {
                    Debug.LogWarning($"[IAPService] verify pending, retrying ({attempt + 1}/{MaxVerifyRetries})");
                    continue;
                }

                Debug.LogWarning($"[IAPService] Purchase verification failed: {errCode}");
                onComplete?.Invoke(false, errCode);
                yield break;
            }
        }

        // Applies a successful verify response to local progress. Returns null on success, else an error code.
        private string ApplyVerifyResponse(string storeProductId, string body)
        {
            try
            {
                var response = JsonUtility.FromJson<VerifyIapResponseJson>(body);
                if (response == null || !response.success)
                    return response != null && !string.IsNullOrEmpty(response.errorCode) ? response.errorCode : "VERIFY_FAILED";

                if (PlayerProgressService.Instance != null)
                {
                    if (response.isNoAds)
                        PlayerProgressService.Instance.SetNoAds(true);

                    // Route through the central gold setter so a malformed/empty verify (currentGold defaulting
                    // to 0) cannot wipe the balance. A real gold purchase yields currentGold > 0 and applies.
                    CurrencyApiService.Instance?.UpdateGold(
                        new ProjectFill.Contracts.Currency.CurrencySnapshot { SoftAmount = response.currentGold });

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

                    PlayerPrefs.Save();
                }

                var product = GetProductByStoreId(storeProductId);
                if (product != null)
                    _remainingPurchases[product.info_id] = response.remainingPurchases < 0 ? null : (int?)response.remainingPurchases;

                Debug.Log($"[IAPService] Purchase success: {storeProductId}. Gold={response.currentGold}, NoAds={response.isNoAds}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IAPService] Exception during parse purchase response: {ex.Message}");
                return "PARSE_ERROR";
            }
        }

        private static ProductType ToProductType(IapProductType productType)
            => productType == IapProductType.NonConsumable ? ProductType.NonConsumable : ProductType.Consumable;

        private static string StorePlatform()
        {
#if UNITY_IOS
            return "apple";
#else
            return "google";
#endif
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
