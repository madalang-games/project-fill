using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using TMPro;
using ProjectFill.Contracts.GameTypes;
using Game.Utils;
using GeneratedIapProduct = ProjectFill.Data.Generated.IapProduct;
using GeneratedRewardItem = ProjectFill.Data.Generated.RewardItem;
using GeneratedIapCategory = ProjectFill.Data.Generated.IapCategory;

namespace Game.OutGame.Lobby
{
    public class ShopTabView : MonoBehaviour
    {
        [SerializeField] private RectTransform _contentContainer;

        private Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
        private Dictionary<string, GameObject> _cards = new Dictionary<string, GameObject>();
        private Dictionary<int, GameObject> _categoryHeaders = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> _categorySpacers = new Dictionary<int, GameObject>();
        private List<GeneratedRewardItem> _rewardItems = new List<GeneratedRewardItem>();
        private List<GeneratedIapCategory> _categories = new List<GeneratedIapCategory>();

        private void OnEnable()
        {
            if (PlayerProgressService.Instance != null)
                PlayerProgressService.Instance.OnNoAdsChanged += OnNoAdsChanged;
            if (LocalizationService.Instance != null)
                LocalizationService.Instance.OnLanguageChanged += OnLanguageChanged;
            RefreshUI();
            VerifyNoAdsOwnershipIfNeeded();
        }

        private void OnDisable()
        {
            if (PlayerProgressService.Instance != null)
                PlayerProgressService.Instance.OnNoAdsChanged -= OnNoAdsChanged;
            if (LocalizationService.Instance != null)
                LocalizationService.Instance.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            InitializeCards();
            RefreshUI();
        }

        private void OnNoAdsChanged(bool isNoAds) => RefreshUI();

        private void VerifyNoAdsOwnershipIfNeeded()
        {
            if (IAPService.Instance == null || IAPService.Instance.IsNoAdsPurchased) return;
            if (PlayerApiService.Instance == null) return;

            PlayerApiService.Instance.FetchProgress((ok, progress) =>
            {
                if (ok && progress != null)
                    PlayerProgressService.Instance.LoadFromServer(progress);
                RefreshUI();
            });
        }

        private void Start()
        {
            LoadRewardItems();
            LoadCategories();
            InitializeCards();
            RefreshUI();
            IAPService.Instance?.FetchProductStatuses((_) => RefreshUI());
        }

        private void LoadRewardItems()
        {
            try
            {
                var list = CsvLoader.Load<GeneratedRewardItem>(GeneratedRewardItem.ResourcePath);
                if (list != null) _rewardItems.AddRange(list);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopTabView] Failed to load reward items: {ex.Message}");
            }
        }

        private void LoadCategories()
        {
            try
            {
                var list = CsvLoader.Load<GeneratedIapCategory>(GeneratedIapCategory.ResourcePath);
                if (list != null) _categories.AddRange(list);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopTabView] Failed to load IAP categories: {ex.Message}");
            }
        }

        private void InitializeCards()
        {
            if (_contentContainer == null) return;

            var toDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in _contentContainer)
            {
                if (child.name.EndsWith("Card") || child.name.EndsWith("_Header") || child.name == "Spacer")
                    toDestroy.Add(child.gameObject);
            }
            foreach (var obj in toDestroy) Destroy(obj);

            var cardPrefab = Resources.Load<GameObject>("Prefabs/UI/IAPProductCard");
            if (cardPrefab == null)
            {
                Debug.LogError("[ShopTabView] Failed to load IAPProductCard prefab from Resources!");
                return;
            }

            _buttons.Clear();
            _cards.Clear();
            _categoryHeaders.Clear();
            _categorySpacers.Clear();

            var products = IAPService.Instance?.Products;
            if (products == null) return;

            var sortedCategories = _categories.OrderBy(c => c.sort_order).ToList();
            bool firstCategory = true;

            foreach (var cat in sortedCategories)
            {
                var catProducts = products
                    .Where(p => p.is_enabled && p.category_id == cat.id)
                    .Where(p => !(p.product_type == IapProductType.NonConsumable && IAPService.Instance.IsNoAdsPurchased))
                    .Where(p => !(p.purchase_limit > 0 && p.reset_period == PurchaseResetPeriod.None &&
                                 (IAPService.Instance?.GetRemainingPurchases(p.id) ?? 1) <= 0))
                    .OrderBy(p => p.sort_order)
                    .ToList();

                if (catProducts.Count == 0) continue;

                if (!firstCategory) CreateSpacer(cat.id);
                firstCategory = false;

                CreateCategoryHeader(cat);

                foreach (var prod in catProducts)
                    CreateAndConfigureCard(cardPrefab, prod, prod.store_product_id + "Card");
            }
        }

        private void CreateAndConfigureCard(GameObject prefab, GeneratedIapProduct prod, string cardName)
        {
            var go = Instantiate(prefab, _contentContainer);
            go.name = cardName;
            _cards[prod.store_product_id] = go;

            var iconTrans = go.transform.Find("Visual/Icon");
            if (iconTrans != null && iconTrans.TryGetComponent<Image>(out var iconImg))
            {
                if (DynamicResourceService.Instance != null)
                {
                    iconImg.sprite = DynamicResourceService.Instance.GetSprite(prod.icon_res);
                    iconImg.preserveAspect = true;
                }
            }

            var titleTrans = go.transform.Find("Visual/TitleText");
            if (titleTrans != null && titleTrans.TryGetComponent<TextMeshProUGUI>(out var titleTxt))
            {
                titleTxt.text = LocalizationService.Instance != null ? LocalizationService.Instance.Get(prod.name_key) : "";
                if (titleTrans.TryGetComponent<LocalizedText>(out var lt)) lt.SetStringId(prod.name_key);
                if (titleTrans.TryGetComponent<UITextStyle>(out var style)) style.ApplyStyle();
            }

            var descTrans = go.transform.Find("Visual/DescText");
            if (descTrans != null && descTrans.TryGetComponent<TextMeshProUGUI>(out var descTxt))
            {
                descTxt.text = LocalizationService.Instance != null ? LocalizationService.Instance.Get(prod.desc_key) : "";
                if (descTrans.TryGetComponent<LocalizedText>(out var lt)) lt.SetStringId(prod.desc_key);
                if (descTrans.TryGetComponent<UITextStyle>(out var style)) style.ApplyStyle();
            }

            var priceBtnTrans = go.transform.Find("Visual/PriceButton");
            if (priceBtnTrans != null && priceBtnTrans.TryGetComponent<Button>(out var priceBtn))
            {
                priceBtn.onClick.RemoveAllListeners();
                priceBtn.onClick.AddListener(() => Purchase(prod.store_product_id));
                _buttons[prod.store_product_id] = priceBtn;
            }
        }

        private void CreateCategoryHeader(GeneratedIapCategory cat)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/ShopCategoryHeader");
            if (prefab == null)
            {
                Debug.LogWarning("[ShopTabView] Prefabs/UI/ShopCategoryHeader not found — skipping header");
                return;
            }
            var go = Instantiate(prefab, _contentContainer);
            go.name = cat.id + "_Header";
            _categoryHeaders[cat.id] = go;

            var labelTrans = go.transform.Find("Label");
            if (labelTrans != null)
            {
                if (labelTrans.TryGetComponent<TextMeshProUGUI>(out var tmp))
                    tmp.text = LocalizationService.Instance?.Get(cat.name_key) ?? cat.name_key;
                if (labelTrans.TryGetComponent<LocalizedText>(out var lt))
                    lt.SetStringId(cat.name_key);
            }
        }

        private void CreateSpacer(int forCategoryId, float height = 16f)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(_contentContainer, false);
            go.AddComponent<RectTransform>();
            var elem = go.AddComponent<LayoutElement>();
            elem.preferredHeight = height;
            elem.flexibleWidth = 1f;
            _categorySpacers[forCategoryId] = go;
        }

        private void Purchase(string storeProductId)
        {
            if (IAPService.Instance == null) return;

            var prod = IAPService.Instance.GetProductByStoreId(storeProductId);
            if (prod == null) return;

            bool isNonConsumable = prod.product_type == IapProductType.NonConsumable;
            if (isNonConsumable && IAPService.Instance.IsNoAdsPurchased)
            {
                ShowToast("toast.iap_already_owned", ToastType.Warning);
                return;
            }

            var remaining = IAPService.Instance.GetRemainingPurchases(prod.id);
            if (remaining.HasValue && remaining.Value <= 0)
            {
                ShowToast("toast.iap_limit_reached", ToastType.Warning);
                return;
            }

            var loc = LocalizationService.Instance;
            string prodName = loc.Get(prod.name_key);
            var rewardList = BuildRewardList(prod);

            UIManager.Instance?.ShowPopup<ConfirmDialogView>(v => v.Init(
                title: prodName,
                body: $"{prodName} (${prod.price_usd:F2})",
                confirmLabel: loc.Get("common.btn_confirm"),
                onConfirm: () =>
                {
                    UIManager.Instance?.ShowLoading();
                    IAPService.Instance.PurchaseProduct(storeProductId, (success, err) =>
                    {
                        UIManager.Instance?.HideLoading();
                        if (success)
                        {
                            ShowToast("toast.iap_purchase_success", ToastType.Success);
                            InitializeCards();
                            RefreshUI();
                            GetComponentInParent<LobbyView>()?.RefreshGoldDisplay();
                        }
                        else
                        {
                            var msg = LocalizationService.Instance?.GetError(err);
                            if (string.IsNullOrEmpty(msg) || msg == err)
                                msg = LocalizationService.Instance?.Get("toast.ad_cancelled");
                            UIManager.Instance?.ShowToast(msg, ToastType.Warning);
                            if (err == "ALREADY_OWNED") RefreshUI();
                        }
                    });
                },
                onCancel: null,
                cancelLabel: loc.Get("common.btn_cancel"),
                danger: false,
                rewardItems: rewardList
            ));
        }

        private List<(string iconKey, string qtyText, string nameKey, string descKey)> BuildRewardList(GeneratedIapProduct prod)
        {
            var list = new List<(string, string, string, string)>();
            var items = _rewardItems.Where(r => r.reward_group_id == prod.reward_group_id).OrderBy(r => r.sort_order).ToList();

            foreach (var item in items)
            {
                switch (item.reward_type)
                {
                    case "NO_ADS":
                        list.Add(("ui_iap_no_ads", "", "shop.reward.no_ads", ""));
                        break;
                    case "SOFT_CURRENCY":
                        var goldCurrency = Services.CurrencyDataService.Instance?.GetByRewardType("SOFT_CURRENCY");
                        list.Add(("ui_gold_icon", $"+{item.amount}", goldCurrency?.name_key ?? "", goldCurrency?.desc_key ?? ""));
                        break;
                    case "ITEM":
                        var itemData = Services.ItemDataService.Instance?.GetItem(item.target_id);
                        list.Add((GetItemIconKey(item.target_id), $"x{item.amount}", itemData?.name_key ?? "", itemData?.desc_key ?? ""));
                        break;
                }
            }

            return list;
        }

        private static string GetItemIconKey(int itemId)
        {
            switch (itemId)
            {
                case 1: return "item_add_turn";
                case 2: return "item_bomb";
                case 3: return "item_h_rocket";
                case 4: return "item_color_sweep";
                case 5: return "item_row_shift";
                case 6: return "item_cell_swap";
                default: return "";
            }
        }

        public void RefreshUI()
        {
            bool isNoAds = IAPService.Instance != null && IAPService.Instance.IsNoAdsPurchased;
            var loc = LocalizationService.Instance;

            foreach (var kvp in _cards)
            {
                var cardGo = kvp.Value;
                if (cardGo == null) continue;
                var prod = IAPService.Instance?.GetProductByStoreId(kvp.Key);
                if (prod == null) continue;

                bool isNonConsumable = prod.product_type == IapProductType.NonConsumable;
                bool isLimitNoneAndReached = false;

                if (prod.purchase_limit > 0 && prod.reset_period == PurchaseResetPeriod.None)
                {
                    var remaining = IAPService.Instance?.GetRemainingPurchases(prod.id) ?? prod.purchase_limit;
                    if (remaining <= 0) isLimitNoneAndReached = true;
                }

                bool showCard = !(isNonConsumable && isNoAds) && !isLimitNoneAndReached;
                cardGo.SetActive(showCard);

                var limitTrans = cardGo.transform.Find("Visual/LimitText");
                if (limitTrans != null && limitTrans.TryGetComponent<TextMeshProUGUI>(out var limitTxt))
                {
                    if (prod.purchase_limit > 0)
                    {
                        var remaining = IAPService.Instance?.GetRemainingPurchases(prod.id) ?? prod.purchase_limit;
                        string limitFormatKey = prod.reset_period switch
                        {
                            PurchaseResetPeriod.Daily => "shop.iap.limit.daily",
                            PurchaseResetPeriod.Weekly => "shop.iap.limit.weekly",
                            PurchaseResetPeriod.Monthly => "shop.iap.limit.monthly",
                            _ => "shop.iap.limit.none"
                        };
                        string formatStr = loc != null ? loc.Get(limitFormatKey) : "";
                        limitTxt.text = string.Format(formatStr, remaining, prod.purchase_limit);
                        limitTxt.gameObject.SetActive(true);
                    }
                    else
                    {
                        limitTxt.gameObject.SetActive(false);
                    }
                }
            }

            foreach (var kvp in _buttons)
            {
                var button = kvp.Value;
                if (button == null) continue;

                var prod = IAPService.Instance?.GetProductByStoreId(kvp.Key);
                if (prod == null) continue;

                var txt = button.GetComponentInChildren<TMP_Text>();
                bool isNonConsumable = prod.product_type == IapProductType.NonConsumable;
                var remaining = IAPService.Instance?.GetRemainingPurchases(prod.id);
                bool limitReached = remaining.HasValue && remaining.Value <= 0;
                bool canBuy = !limitReached && !(isNonConsumable && isNoAds);

                if (txt != null)
                    txt.text = canBuy ? $"${prod.price_usd:F2}" : loc?.Get("shop.iap.purchased") ?? "Owned";
                button.interactable = canBuy;
            }

            UpdateCategoryVisibility();
        }

        private void UpdateCategoryVisibility()
        {
            var visibleCatIds = new HashSet<int>();
            foreach (var kvp in _cards)
            {
                if (kvp.Value != null && kvp.Value.activeSelf)
                {
                    var prod = IAPService.Instance?.GetProductByStoreId(kvp.Key);
                    if (prod != null) visibleCatIds.Add(prod.category_id);
                }
            }

            foreach (var kvp in _categoryHeaders)
                if (kvp.Value != null) kvp.Value.SetActive(visibleCatIds.Contains(kvp.Key));

            var sortedVisible = visibleCatIds
                .OrderBy(id => _categories.FirstOrDefault(c => c.id == id)?.sort_order ?? 0)
                .ToList();

            foreach (var kvp in _categorySpacers)
            {
                if (kvp.Value == null) continue;
                int idx = sortedVisible.IndexOf(kvp.Key);
                kvp.Value.SetActive(idx > 0);
            }
        }

        private void ShowToast(string key, ToastType type)
        {
            if (LocalizationService.Instance != null)
                UIManager.Instance?.ShowToast(LocalizationService.Instance.Get(key), type);
        }
    }
}
