using System.Collections;
using System.Collections.Generic;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Data.Generated;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class HomeTabView : MonoBehaviour
    {
        [SerializeField] private ScrollRect    _scrollRect;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GameObject    _stageNodePrefab;
        [SerializeField] private GameObject    _chestPrefab;
        [SerializeField] private float         _nodeSpacingY      = 120f;
        [SerializeField] private Sprite        _guideOrbSprite;
        [SerializeField] private LobbyBadgeContainer _badgeContainer;

        private const float OverdrawBuffer = 400f;

                private readonly List<StageNodeView>       _pool          = new List<StageNodeView>();
        private readonly List<UILineStrip>         _pathStrips    = new List<UILineStrip>();
        private readonly Dictionary<int, Vector2>  _stagePositions = new Dictionary<int, Vector2>();
        private Coroutine                    _guideOrbCoroutine;
        private Image                        _guideOrb;
        private Stage[]   _stages;
        private int       _currentStageId;
        private bool      _layoutBuilt    = false;
        private Vector2[] _builtPositions;

        private readonly List<ChapterChestView>     _chestNodes = new List<ChapterChestView>();
        private readonly List<ChapterBackgroundView> _bgViews    = new List<ChapterBackgroundView>();
        private GameObject                           _bgGradientGo;
        private readonly Dictionary<string, bool> _chestClaimed = new Dictionary<string, bool>
        {
            { "chapter1_chest", false },
            { "chapter2_chest", false },
            { "chapter3_chest", false },
        };

        private const string InGameScene = "InGame";

        private void Awake()
        {
            if (_contentRoot == null && _scrollRect != null)
                _contentRoot = _scrollRect.content;

            if (_chestPrefab == null)
                _chestPrefab = Resources.Load<GameObject>("Prefabs/UI/ChapterChest");

            var viewport = _scrollRect != null ? _scrollRect.viewport : null;
            if (viewport != null && viewport.GetComponent<Image>() == null)
            {
                var img = viewport.gameObject.AddComponent<Image>();
                img.color          = Color.clear;
                img.raycastTarget  = true;
            }
        }

        private void OnEnable()
        {
            _stages         = StageDataService.Instance?.GetAll();
            _currentStageId = FindCurrentStage();

            if (!_layoutBuilt)
            {
                BuildPool();
                _layoutBuilt = true;
            }
            else if (_builtPositions != null && _stages != null)
            {
                StartGuideOrb(_builtPositions, _stages.Length);
            }

            _scrollRect.onValueChanged.AddListener(OnScrolled);

            // Immediate render if layout already built (tab re-enter); otherwise ApplyScrollNextFrame handles it
            if (_scrollRect.viewport.rect.height > 0f)
                OnScrolled(new Vector2(0f, _scrollRect.verticalNormalizedPosition));

            RestoreScrollPosition();

            if (RewardsApiService.Instance != null)
            {
                RewardsApiService.Instance.FetchRewardSources(response =>
                {
                    _chestClaimed["chapter1_chest"] = true;
                    _chestClaimed["chapter2_chest"] = true;
                    _chestClaimed["chapter3_chest"] = true;

                    foreach (var src in response.Sources)
                    {
                        if (_chestClaimed.ContainsKey(src.SourceId))
                        {
                            _chestClaimed[src.SourceId] = !src.Claimable;
                        }
                    }
                    RefreshChestNodes();
                }, err => Debug.LogWarning($"[HomeTabView] failed to fetch reward sources: {err}"));
            }

            if (_badgeContainer != null)
            {
                _badgeContainer.Refresh(GetComponentInParent<LobbyView>());
            }
        }

        private void OnDisable()
        {
            ScrollStateCache.HomeScrollPosition = _scrollRect != null
                ? _scrollRect.verticalNormalizedPosition : 0f;

            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrolled);

            if (_guideOrbCoroutine != null)
            {
                StopCoroutine(_guideOrbCoroutine);
                _guideOrbCoroutine = null;
            }
        }

        private int FindCurrentStage()
        {
            if (_stages == null) return 1;
            var progress = PlayerProgressService.Instance;
            foreach (var s in _stages)
            {
                if (progress == null || !progress.IsStageUnlocked(s.stage_id)) break;
                if (progress.GetBestMoves(s.stage_id) == 0) return s.stage_id; // first not-yet-cleared
            }
            return _stages.Length > 0 ? _stages[^1].stage_id : 1;
        }

        private void BuildPool()
        {
            if (_stages == null) return;

            foreach (var ps in _pathStrips)
            {
                if (ps != null) Destroy(ps.gameObject);
            }
            _pathStrips.Clear();

            // Clean up editor dummy placeholder nodes so they don't overlap at runtime
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = _contentRoot.GetChild(i);
                if (child.gameObject.name.StartsWith("StageNode_"))
                {
                    Destroy(child.gameObject);
                }
            }

            if (_bgGradientGo != null) { Destroy(_bgGradientGo); _bgGradientGo = null; }
            foreach (var bg in _bgViews)
                if (bg != null) Destroy(bg.gameObject);
            _bgViews.Clear();

            // Deactivate all pool nodes — prevents stale 0-position nodes on re-enable
            foreach (var n in _pool) n.gameObject.SetActive(false);
            _stagePositions.Clear();

            int needed = Mathf.Min(_stages.Length, Game.Core.GameConfig.StageNodePoolSize);
            while (_pool.Count < needed)
            {
                var go   = Instantiate(_stageNodePrefab, _contentRoot);
                var node = go.GetComponent<StageNodeView>();
                node.OnTapped += OnStageTapped;
                go.AddComponent<ScrollRectForwarder>(); // forward drags to parent ScrollRect
                go.SetActive(false);
                _pool.Add(node);
            }

            // 1. Calculate dynamic viewport width (with robust fallbacks)
            float viewportWidth = _scrollRect != null && _scrollRect.viewport != null 
                ? _scrollRect.viewport.rect.width 
                : 0f;
            
            if (viewportWidth <= 0f)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var canvasRT = canvas.GetComponent<RectTransform>();
                    if (canvasRT != null)
                        viewportWidth = canvasRT.rect.width;
                }
                if (viewportWidth <= 0f)
                {
                    viewportWidth = 1080f; // Reference width fallback
                }
            }

            int   stageCount = _stages.Length;
            float bottomPadding = 180f;



            // 2. Compute raw coordinates using a serpentine winding layout
            var positions = new Vector2[stageCount];
            float maxAllowedX = (viewportWidth * 0.5f) - 180f;

            var oldRandomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(1004); // Fixed seed for consistent layout rendering

            // Start first node at the bottom-left/center area
            bool goingRight = true;
            positions[0] = new Vector2(UnityEngine.Random.Range(-maxAllowedX * 0.6f, -maxAllowedX * 0.2f), bottomPadding);

            for (int i = 1; i < stageCount; i++)
            {
                Vector2 prevPos = positions[i - 1];
                float stepDistance = UnityEngine.Random.Range(310f, 380f); // Spacing strictly between 300 and 400

                // Determine if we need to turn (transition to next tier)
                bool needTurn = false;
                if (goingRight && prevPos.x + 150f > maxAllowedX)
                {
                    needTurn = true;
                }
                else if (!goingRight && prevPos.x - 150f < -maxAllowedX)
                {
                    needTurn = true;
                }

                if (needTurn)
                {
                    // Vertical step up to the next tier
                    float angle = UnityEngine.Random.Range(75f, 105f) * Mathf.Deg2Rad; // Mostly vertical
                    float testX = prevPos.x + Mathf.Cos(angle) * stepDistance;
                    float testY = prevPos.y + Mathf.Sin(angle) * stepDistance;

                    positions[i] = new Vector2(Mathf.Clamp(testX, -maxAllowedX, maxAllowedX), testY);
                    goingRight = !goingRight; // Reverse direction
                }
                else
                {
                    // Horizontal step with Y undulations
                    float angleDeg;
                    if (goingRight)
                    {
                        // Angle range: -25 to 40 degrees (going right, slightly up/down/flat)
                        angleDeg = UnityEngine.Random.Range(-25f, 40f);
                    }
                    else
                    {
                        // Angle range: 140 to 205 degrees (going left, slightly up/down/flat)
                        angleDeg = UnityEngine.Random.Range(140f, 205f);
                    }

                    float angle = angleDeg * Mathf.Deg2Rad;
                    float testX = prevPos.x + Mathf.Cos(angle) * stepDistance;
                    float testY = prevPos.y + Mathf.Sin(angle) * stepDistance;
                    testY = Mathf.Max(testY, bottomPadding);

                    // Check if this step would overshoot the screen bounds. If so, force a turn instead!
                    if (testX < -maxAllowedX || testX > maxAllowedX)
                    {
                        // Force a vertical turn step instead
                        float turnAngle = UnityEngine.Random.Range(75f, 105f) * Mathf.Deg2Rad;
                        testX = prevPos.x + Mathf.Cos(turnAngle) * stepDistance;
                        testY = prevPos.y + Mathf.Sin(turnAngle) * stepDistance;
                        testY = Mathf.Max(testY, bottomPadding);
                        positions[i] = new Vector2(Mathf.Clamp(testX, -maxAllowedX, maxAllowedX), testY);
                        goingRight = !goingRight;
                    }
                    else
                    {
                        positions[i] = new Vector2(testX, testY);
                    }
                }
            }

            UnityEngine.Random.state = oldRandomState;

            // 3. Deterministic relaxation pass to guarantee min distance (300f) between ALL nodes
            float minDistance = 300f; // Minimum distance between all StageNodes is 300f
            for (int iter = 0; iter < 18; iter++) // 18 iterations to ensure correct convergence
            {
                for (int i = 0; i < stageCount; i++)
                {
                    for (int j = i + 1; j < stageCount; j++)
                    {
                        float dist = Vector2.Distance(positions[i], positions[j]);
                        if (dist < minDistance)
                        {
                            Vector2 dir = positions[i] - positions[j];
                            if (dir.sqrMagnitude == 0f) dir = Vector2.up;
                            dir.Normalize();
                            float overlap = minDistance - dist;
                            positions[i] += dir * (overlap * 0.5f);
                            positions[j] -= dir * (overlap * 0.5f);
                            
                            // Keep X within responsive limits
                            positions[i].x = Mathf.Clamp(positions[i].x, -maxAllowedX, maxAllowedX);
                            positions[j].x = Mathf.Clamp(positions[j].x, -maxAllowedX, maxAllowedX);
                        }
                    }
                }
            }

            // Find total height based on relaxed positions
            float maxRelaxedY = 0f;
            for (int i = 0; i < stageCount; i++)
            {
                if (positions[i].y > maxRelaxedY) maxRelaxedY = positions[i].y;
            }
            float totalHeight = maxRelaxedY + bottomPadding + 50f;
            _contentRoot.sizeDelta = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

            // Convert relaxed positions to anchor-top space (negative Y) and store
            for (int i = 0; i < stageCount; i++)
            {
                float x = positions[i].x;
                float y = -(totalHeight - positions[i].y);
                positions[i] = new Vector2(x, y);
                _stagePositions[_stages[i].stage_id] = positions[i];
            }

            _builtPositions = positions;

            foreach (var node in _chestNodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
            _chestNodes.Clear();

            var chapterFirstIdx = new Dictionary<int, int>();
            var chapterLastIdx  = new Dictionary<int, int>();
            for (int i = 0; i < stageCount; i++)
            {
                int cid = _stages[i].chapter_id;
                if (!chapterFirstIdx.ContainsKey(cid))
                    chapterFirstIdx[cid] = i;
                chapterLastIdx[cid] = i;
            }

            var sortedChapters = new List<int>(chapterLastIdx.Keys);
            sortedChapters.Sort();
            foreach (int cid in sortedChapters)
            {
                int firstIdx = chapterFirstIdx[cid];
                int lastIdx  = chapterLastIdx[cid];
                
                // Chapter boundaries in anchor-top space (firstY is bottom-most/more negative, lastY is top-most/less negative)
                float firstY = positions[firstIdx].y;
                float lastY  = positions[lastIdx].y;
                
                float yBotLimit = firstY - 100f - 160f; // Bottom boundary (Y value is more negative)
                float yTopLimit = lastY + 100f + 160f;  // Top boundary (Y value is less negative)
                
                CreateChestNode(cid, positions[lastIdx], totalHeight, yBotLimit, yTopLimit);
            }

            BuildPath(positions, stageCount, totalHeight);
            StartGuideOrb(positions, stageCount);
            BuildChapterBackgrounds(positions, stageCount, totalHeight);
            RefreshChestNodes();
        }

        private void CreateChestNode(int chapterNum, Vector2 stagePos, float totalHeight, float yBotLimit, float yTopLimit)
        {
            if (_chestPrefab == null) return;

            var go = Instantiate(_chestPrefab, _contentRoot);
            go.name = $"ChestNode_{chapterNum}";
            go.AddComponent<ScrollRectForwarder>();
            
            var chestView = go.GetComponent<ChapterChestView>();
            var nodeRt = chestView.GetComponent<RectTransform>();
            nodeRt.anchorMin = nodeRt.anchorMax = new Vector2(0.5f, 1f);
            nodeRt.pivot = new Vector2(0.5f, 0.5f);

            // Dynamically scale chest position offset based on viewport width
            float viewportWidth = _scrollRect != null && _scrollRect.viewport != null 
                ? _scrollRect.viewport.rect.width 
                : 0f;
            if (viewportWidth <= 0f)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var canvasRT = canvas.GetComponent<RectTransform>();
                    if (canvasRT != null)
                        viewportWidth = canvasRT.rect.width;
                }
                if (viewportWidth <= 0f) viewportWidth = 1080f;
            }

            float maxAllowedX = (viewportWidth * 0.5f) - 180f;
            float chestOffset = Mathf.Min(260f, maxAllowedX * 0.6f);
            
            // Build stage node positions list for collision checking
            var stagePositionsList = new List<Vector2>();
            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stagePositions.TryGetValue(_stages[i].stage_id, out Vector2 p))
                {
                    stagePositionsList.Add(p);
                }
            }

            float targetX = 0f;
            float targetY = 0f;
            bool foundSpot = false;
            float safeDistance = 300f; // Spacing between ChapterChest and all StageNodes must be at least 300f

            // Candidate chest placement offsets relative to stagePos:
            // We prioritize opposite side lane, then same side lane, with varying Y-offsets (offset from node center)
            var candidates = new List<Vector2>();
            float oppX  = stagePos.x >= 0 ? -chestOffset : chestOffset;
            float sameX = stagePos.x >= 0 ? chestOffset : -chestOffset;
            
            // 1. Opposite side candidates (highest priority)
            candidates.Add(new Vector2(oppX, stagePos.y + 110f));
            candidates.Add(new Vector2(oppX, stagePos.y - 110f));
            candidates.Add(new Vector2(oppX, stagePos.y + 180f));
            candidates.Add(new Vector2(oppX, stagePos.y - 180f));
            candidates.Add(new Vector2(oppX, stagePos.y));
            
            // 2. Same side candidates (fallback if opposite side is crowded)
            candidates.Add(new Vector2(sameX, stagePos.y + 140f));
            candidates.Add(new Vector2(sameX, stagePos.y - 140f));
            candidates.Add(new Vector2(sameX, stagePos.y + 200f));
            candidates.Add(new Vector2(sameX, stagePos.y - 200f));

            foreach (var cand in candidates)
            {
                // Clamp candidate Y to ensure the chest stays strictly within the chapter visual boundary
                float clampedY = Mathf.Clamp(cand.y, yBotLimit + 60f, yTopLimit - 60f);
                Vector2 testPos = new Vector2(cand.x, clampedY);
                
                bool overlaps = false;
                foreach (var pos in stagePositionsList)
                {
                    if (Vector2.Distance(testPos, pos) < safeDistance)
                    {
                        overlaps = true;
                        break;
                    }
                }
                
                if (!overlaps)
                {
                    targetX = testPos.x;
                    targetY = testPos.y;
                    foundSpot = true;
                    break;
                }
            }
            
            // 3. Ultimate fallback (push vertically within boundaries if all candidates overlap)
            if (!foundSpot)
            {
                targetX = oppX;
                targetY = Mathf.Clamp(stagePos.y + 110f, yBotLimit + 60f, yTopLimit - 60f);
                int attempts = 0;
                bool overlaps = true;
                while (overlaps && attempts < 25)
                {
                    overlaps = false;
                    foreach (var pos in stagePositionsList)
                    {
                        if (Vector2.Distance(new Vector2(targetX, targetY), pos) < safeDistance)
                        {
                            overlaps = true;
                            // Shift upwards, but if hitting top boundary, reverse and shift down
                            if (targetY + 60f < yTopLimit - 60f)
                                targetY += 60f;
                            else
                                targetY -= 60f;
                            break;
                        }
                    }
                    attempts++;
                }
            }

            nodeRt.anchoredPosition = new Vector2(targetX, targetY);

            var button = chestView.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnChestTapped(chapterNum));
            }

            _chestNodes.Add(chestView);
        }

        private bool IsChapterAllCleared(int chapterNum)
        {
            var progress = PlayerProgressService.Instance;
            if (progress == null || _stages == null) return false;
            bool hasStages = false;
            foreach (var s in _stages)
            {
                if (s.chapter_id != chapterNum) continue;
                hasStages = true;
                if (progress.GetBestMoves(s.stage_id) == 0) return false; // any uncleared → not complete
            }
            return hasStages;
        }

        private (int current, int max) GetChapterClearInfo(int chapterNum)
        {
            var progress = PlayerProgressService.Instance;
            if (progress == null || _stages == null) return (0, 0);
            int current = 0;
            int max = 0;
            foreach (var s in _stages)
            {
                if (s.chapter_id != chapterNum) continue;
                if (progress.GetBestMoves(s.stage_id) > 0) current++;
                max += 1;
            }
            return (current, max);
        }

        private void RefreshChestNodes()
        {
            for (int i = 0; i < _chestNodes.Count; i++)
            {
                int chapterNum = i + 1;
                var chestView = _chestNodes[i];
                string sourceId = $"chapter{chapterNum}_chest";

                _chestClaimed.TryGetValue(sourceId, out bool claimed);

                ChapterChestView.ChestState state = ChapterChestView.ChestState.Inactive;
                if (claimed)
                    state = ChapterChestView.ChestState.Claimed;
                else if (IsChapterAllCleared(chapterNum))
                    state = ChapterChestView.ChestState.Active;

                chestView.SetState(state);

                var (current, max) = GetChapterClearInfo(chapterNum);
                chestView.SetClearedInfo(current, max);

                chestView.gameObject.SetActive(true);
            }
        }

        private void OnChestTapped(int chapterNum)
        {
            string sourceId = $"chapter{chapterNum}_chest";
            
            bool claimed = false;
            _chestClaimed.TryGetValue(sourceId, out claimed);
            if (claimed) return;

            if (PlayerProgressService.Instance == null) return;

            if (!IsChapterAllCleared(chapterNum))
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.chapter_unlock_requirement"), Game.Core.UI.ToastType.Warning);
                return;
            }

            Game.Core.UIManager.Instance?.ShowLoading();
            RewardsApiService.Instance?.ClaimReward(sourceId,
                onSuccess: response =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    _chestClaimed[sourceId] = true;
                    RefreshChestNodes();

                    var loc = LocalizationService.Instance;
                    var dynRes = DynamicResourceService.Instance;
                    var rewardItems = new List<Game.Core.UI.RewardItem>();
                    foreach (var r in response.GrantedRewards)
                    {
                        if (r.RewardType == "SOFT_CURRENCY")
                        {
                            PlayerProgressService.Instance?.AddGold(r.Amount);
                            var currency = CurrencyDataService.Instance?.GetByRewardType("SOFT_CURRENCY");
                            rewardItems.Add(new Game.Core.UI.RewardItem
                            {
                                Icon     = currency != null ? dynRes?.GetSprite(currency.icon_name) : null,
                                Quantity = r.Amount,
                                Label    = loc.Get("common.gold"),
                                NameKey  = currency?.name_key ?? string.Empty,
                                DescKey  = currency?.desc_key ?? string.Empty,
                            });
                        }
                        else if (r.RewardType == "ITEM")
                        {
                            var item = ItemDataService.Instance?.GetItem(r.TargetId);
                            rewardItems.Add(new Game.Core.UI.RewardItem
                            {
                                Icon     = item != null ? dynRes?.GetSprite(item.icon_name) : null,
                                Quantity = r.Amount,
                                Label    = item != null ? loc.Get(item.name_key) : $"Item {r.TargetId}",
                                NameKey  = item?.name_key ?? string.Empty,
                                DescKey  = item?.desc_key ?? string.Empty,
                            });
                        }
                    }
                    Game.Core.UIManager.Instance?.ShowPopup<RewardPopupView>(popup => popup.Init(rewardItems));
                },
                onError: error =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.chest_claim_failed"), Game.Core.UI.ToastType.Warning);
                }
            );
        }

        private void BuildChapterBackgrounds(Vector2[] positions, int count, float totalHeight)
        {
            var chapters = new Dictionary<int, (int first, int last)>();
            for (int i = 0; i < count; i++)
            {
                int cid = _stages[i].chapter_id;
                if (!chapters.TryGetValue(cid, out var range))
                    chapters[cid] = (i, i);
                else
                    chapters[cid] = (range.first, i);
            }

            const float nodeHalf = 100f;
            const float chPad    = 160f;

            var sortedIds = new List<int>(chapters.Keys);
            sortedIds.Sort();

            var bounds = new Dictionary<int, (float yTop, float yBot)>();
            foreach (int cid in sortedIds)
            {
                var r = chapters[cid];
                bounds[cid] = (
                    positions[r.last].y  + nodeHalf + chPad,
                    positions[r.first].y - nodeHalf - chPad
                );
            }

            // ── Single multi-stop gradient spanning ALL chapters ──────────────
            // No seam strips needed: stops computed from chapter bounds ensure
            // the gradient blends exactly at each chapter boundary.
            // t = 0 (bottom of content) → t = 1 (top of content)
            // t(y) = (totalHeight + y) / totalHeight  (y is negative anchor-top space)
            {
                var stopList = new List<(float t, Color color)>();
                for (int c = 0; c < sortedIds.Count; c++)
                {
                    int   cid   = sortedIds[c];
                    var   theme = ChapterBgTheme.Get(cid);
                    var   (yTop, yBot) = bounds[cid];
                    float tBot  = (totalHeight + yBot) / totalHeight;
                    float tTop  = (totalHeight + yTop) / totalHeight;
                    tBot = Mathf.Clamp01(tBot);
                    tTop = Mathf.Clamp01(tTop);
                    stopList.Add((tBot, theme.BottomColor));
                    stopList.Add((tTop, theme.TopColor));
                }

                var gradGo = new GameObject("ChapterGradient", typeof(RectTransform));
                gradGo.transform.SetParent(_contentRoot, false);
                var grad = gradGo.AddComponent<UIVerticalGradient>();
                grad.raycastTarget = false;
                grad.SetStops(stopList.ToArray());
                var grt = gradGo.GetComponent<RectTransform>();
                grt.anchorMin = Vector2.zero;
                grt.anchorMax = Vector2.one;
                grt.offsetMin = grt.offsetMax = Vector2.zero;
                gradGo.transform.SetSiblingIndex(0); // bottom of render stack
                _bgGradientGo = gradGo;
            }

            // ── Per-chapter decoration views (on top of gradient, behind PathStrip) ──
            // Create in reverse so ch1 dec index < ch2 dec index (ch1 renders behind ch2 at seam)
            for (int c = sortedIds.Count - 1; c >= 0; c--)
            {
                int   cid          = sortedIds[c];
                var   (yTop, yBot) = bounds[cid];
                float height       = yTop - yBot;

                var go = new GameObject($"ChapterBg_{cid}", typeof(RectTransform));
                go.transform.SetParent(_contentRoot, false);
                go.AddComponent<ScrollRectForwarder>();
                var bgView = go.AddComponent<ChapterBackgroundView>();
                bgView.Bind(chapterId: cid, bgThemeId: cid, yAnchoredTop: yTop, height: height);
                go.transform.SetSiblingIndex(1); // after gradient (index 0), before PathStrip
                _bgViews.Add(bgView);
            }
        }

        private void BuildPath(Vector2[] nodePositions, int count, float totalHeight)
        {
            foreach (var ps in _pathStrips)
            {
                if (ps != null) Destroy(ps.gameObject);
            }
            _pathStrips.Clear();

            var chapters = new Dictionary<int, (int first, int last)>();
            for (int i = 0; i < count; i++)
            {
                int cid = _stages[i].chapter_id;
                if (!chapters.TryGetValue(cid, out var range))
                    chapters[cid] = (i, i);
                else
                    chapters[cid] = (range.first, i);
            }

            var sortedIds = new List<int>(chapters.Keys);
            sortedIds.Sort();

            float yOffset = totalHeight * 0.5f;
            var progress = PlayerProgressService.Instance;

            foreach (int cid in sortedIds)
            {
                var range = chapters[cid];
                int startIdx = range.first;
                
                // Connect to next chapter's first node for continuous lines
                int endIdx = range.last;
                if (endIdx < count - 1)
                {
                    endIdx++;
                }

                int segCount = endIdx - startIdx + 1;
                if (segCount < 2) continue;

                var chapterPts = new Vector2[segCount];
                for (int s = 0; s < segCount; s++)
                {
                    chapterPts[s] = nodePositions[startIdx + s];
                }

                var curve = SampleCatmullRom(chapterPts, segCount, 12);
                for (int s = 0; s < curve.Count; s++)
                {
                    curve[s] = new Vector2(curve[s].x, curve[s].y + yOffset);
                }

                var go = new GameObject($"PathStrip_Chapter_{cid}");
                go.transform.SetParent(_contentRoot, false);
                go.transform.SetAsFirstSibling();

                var pathStrip = go.AddComponent<UILineStrip>();
                var theme = ChapterBgTheme.Get(cid);

                pathStrip.lineWidth = theme.PathWidth;
                pathStrip.scrollSpeed = theme.PathScrollSpeed;
                pathStrip.useOutline = true;
                pathStrip.outlineWidth = 8f;
                pathStrip.outlineColor = new Color(0f, 0.05f, 0.15f, 0.65f);
                pathStrip.raycastTarget = false;

                Texture2D customTex = null;
                if (!string.IsNullOrEmpty(theme.PathResourceKey))
                {
                    string resourcePath = $"Sprites/Path/{theme.PathResourceKey}";
                    customTex = Resources.Load<Texture2D>(resourcePath);
                }

                if (customTex == null)
                {
                    customTex = Resources.Load<Texture2D>("Sprites/Path/path_chapter");
                }

                if (customTex != null)
                {
                    pathStrip.SetTexture(customTex);
                    pathStrip.color = Color.white;
                    pathStrip.textureTiling = totalHeight / (theme.PathWidth * 8f);
                }
                else
                {
                    // Fallback procedural dashed path style
                    pathStrip.color = theme.PathColor;
                    pathStrip.useProceduralDashes = true;
                    pathStrip.dashLength = 40f;
                    pathStrip.gapLength = 20f;
                    pathStrip.textureTiling = 10f;
                    pathStrip.scrollSpeed = theme.PathScrollSpeed * 10f; // Scale speed for procedural dash animation
                }

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -totalHeight * 0.5f);
                rt.sizeDelta = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

                pathStrip.SetPoints(curve);
                _pathStrips.Add(pathStrip);

                ApplyChapterPathStyle(pathStrip, cid, startIdx, endIdx, progress);
            }
        }

        private void ApplyChapterPathStyle(UILineStrip pathStrip, int chapterId, int startIdx, int endIdx, PlayerProgressService progress)
        {
            if (progress == null) return;

            bool isChapterLocked = true;
            bool isChapterFullyCleared = true;

            for (int idx = startIdx; idx <= endIdx; idx++)
            {
                if (idx >= _stages.Length) break;
                var stageId = _stages[idx].stage_id;
                
                if (progress.IsStageUnlocked(stageId))
                {
                    isChapterLocked = false;
                }
                
                if (progress.GetBestMoves(stageId) == 0)
                {
                    isChapterFullyCleared = false;
                }
            }

            if (isChapterLocked)
            {
                pathStrip.color = new Color(0.4f, 0.4f, 0.4f, 0.15f);
                pathStrip.scrollSpeed = 0f;
            }
            else if (isChapterFullyCleared)
            {
                var theme = ChapterBgTheme.Get(chapterId);
                pathStrip.color = new Color(theme.PathColor.r, theme.PathColor.g, theme.PathColor.b, 0.55f);
                pathStrip.scrollSpeed = theme.PathScrollSpeed * 0.5f;
            }
            else
            {
                var theme = ChapterBgTheme.Get(chapterId);
                pathStrip.color = theme.PathColor;
                pathStrip.scrollSpeed = theme.PathScrollSpeed;
            }
        }

        private void StartGuideOrb(Vector2[] positions, int count)
        {
            if (_guideOrbCoroutine != null) { StopCoroutine(_guideOrbCoroutine); _guideOrbCoroutine = null; }
            if (_guideOrb != null) { Destroy(_guideOrb.gameObject); _guideOrb = null; }

            int currentIdx = _currentStageId - 1;
            if (currentIdx < 0 || currentIdx >= count) return;

            var go = new GameObject("GuideOrb", typeof(Image));
            go.transform.SetParent(_contentRoot, false);
            _guideOrb = go.GetComponent<Image>();
            _guideOrb.raycastTarget = false;

            if (_guideOrbSprite != null)
            {
                _guideOrb.sprite = _guideOrbSprite;
            }

            var cid = _stages[currentIdx].chapter_id;
            var theme = ChapterBgTheme.Get(cid);
            _guideOrb.color = theme.PathColor;

            var rt = _guideOrb.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(24f, 24f);

            if (currentIdx == 0)
            {
                rt.anchoredPosition = positions[0];
                _guideOrbCoroutine = StartCoroutine(OrbPulseRoutine(rt));
            }
            else
            {
                Vector2 startPos = positions[currentIdx - 1];
                Vector2 endPos = positions[currentIdx];
                _guideOrbCoroutine = StartCoroutine(OrbTravelRoutine(rt, startPos, endPos));
            }
        }

        private IEnumerator OrbPulseRoutine(RectTransform rt)
        {
            while (true)
            {
                float s = 1.0f + Mathf.PingPong(Time.time * 2f, 0.4f);
                rt.localScale = new Vector3(s, s, 1f);
                
                float alpha = 0.5f + 0.5f * Mathf.PingPong(Time.time * 2f, 0.5f);
                _guideOrb.color = new Color(_guideOrb.color.r, _guideOrb.color.g, _guideOrb.color.b, alpha);
                yield return null;
            }
        }

        private IEnumerator OrbTravelRoutine(RectTransform rt, Vector2 start, Vector2 end)
        {
            Vector2 mid = Vector2.Lerp(start, end, 0.5f);
            Vector2 dir = (end - start).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * 35f;
            Vector2 curveControl = mid + perp;

            while (true)
            {
                float t = 0f;
                while (t < 1.0f)
                {
                    t += Time.deltaTime * 0.7f;
                    float tc = Mathf.Clamp01(t);

                    Vector2 m1 = Vector2.Lerp(start, curveControl, tc);
                    Vector2 m2 = Vector2.Lerp(curveControl, end, tc);
                    rt.anchoredPosition = Vector2.Lerp(m1, m2, tc);

                    float s = 1.0f + 0.35f * Mathf.Sin(tc * Mathf.PI);
                    rt.localScale = new Vector3(s, s, 1f);

                    yield return null;
                }
                yield return new WaitForSeconds(0.4f);
            }
        }

        private List<Vector2> SampleCatmullRom(Vector2[] pts, int count, int steps)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < count - 1; i++)
            {
                var p0 = pts[Mathf.Max(0,         i - 1)];
                var p1 = pts[i];
                var p2 = pts[i + 1];
                var p3 = pts[Mathf.Min(count - 1, i + 2)];
                for (int s = 0; s < steps; s++)
                    result.Add(CatmullRom(p0, p1, p2, p3, (float)s / steps));
            }
            result.Add(pts[count - 1]);
            return result;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private void OnScrolled(Vector2 scrollVal)
        {
            if (_stages == null || _pool.Count == 0) return;

            float viewportH = _scrollRect.viewport.rect.height;
            if (viewportH <= 0f)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.viewport);
                viewportH = _scrollRect.viewport.rect.height;
                if (viewportH <= 0f) return;
            }

            float totalH     = _contentRoot.sizeDelta.y;
            float scrollable = Mathf.Max(0f, totalH - viewportH);
            float offset     = Mathf.Clamp01(1f - scrollVal.y) * scrollable;

            // visible Y range in anchor-top space (y is negative going down from top)
            float visTop = -(offset - OverdrawBuffer);
            float visBot = -(offset + viewportH + OverdrawBuffer);

            var progress = PlayerProgressService.Instance;
            int  poolIdx = 0;

            for (int i = 0; i < _stages.Length && poolIdx < _pool.Count; i++)
            {
                if (!_stagePositions.TryGetValue(_stages[i].stage_id, out Vector2 nodePos)) continue;
                if (nodePos.y > visTop || nodePos.y < visBot) continue;

                var node = _pool[poolIdx++];
                var s    = _stages[i];
                bool cleared = (progress?.GetBestMoves(s.stage_id) ?? 0) > 0;
                bool unlock  = progress?.IsStageUnlocked(s.stage_id) ?? (s.stage_id == 1);
                bool cur     = s.stage_id == _currentStageId;
                node.Bind(s.stage_id, cleared, unlock, cur, s.chapter_id, (int)s.difficulty);
                var rt      = node.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = nodePos;
                node.gameObject.SetActive(true);
            }

            for (; poolIdx < _pool.Count; poolIdx++)
                _pool[poolIdx].gameObject.SetActive(false);

            // Pause animations for off-screen chapter backgrounds (coroutine CPU saving)
            const float bgBuffer = 350f;
            foreach (var bg in _bgViews)
            {
                if (bg == null) continue;
                bool inView = bg.YTop >= visBot - bgBuffer && bg.YBot <= visTop + bgBuffer;
                if (bg.enabled != inView) bg.enabled = inView;
            }
        }

        private void RestoreScrollPosition()
        {
            if (_scrollRect != null)
                StartCoroutine(ApplyScrollNextFrame());
        }

        private IEnumerator ApplyScrollNextFrame()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.viewport);

            int targetId = ScrollStateCache.LastPlayedStageId;
            if (targetId > 0)
            {
                ScrollStateCache.LastPlayedStageId = 0;
                if (_stagePositions.TryGetValue(targetId, out Vector2 nodePos))
                {
                    float totalH     = _contentRoot.sizeDelta.y;
                    float viewportH  = _scrollRect.viewport.rect.height;
                    float scrollable = totalH - viewportH;
                    _scrollRect.verticalNormalizedPosition = scrollable > 0f
                        ? Mathf.Clamp01(1f + (nodePos.y + viewportH * 0.5f) / scrollable)
                        : 1f;
                    OnScrolled(new Vector2(0f, _scrollRect.verticalNormalizedPosition));
                    yield break;
                }
            }
            _scrollRect.verticalNormalizedPosition = ScrollStateCache.HomeScrollPosition;
            OnScrolled(new Vector2(0f, _scrollRect.verticalNormalizedPosition));
        }

        private void OnStageTapped(int stageId)
        {
            var stage = StageDataService.Instance?.GetStage(stageId);
            if (stage == null) return;

            var progress  = PlayerProgressService.Instance;
            int bestMoves = progress?.GetBestMoves(stageId) ?? 0;
            bool isLocked = !(progress?.IsStageUnlocked(stageId) ?? (stageId == 1));

            Game.Core.UIManager.Instance?.ShowPopup<StageInfoPopupView>(v => v.Init(
                stageId:    stageId,
                bestMoves:  bestMoves,
                onPlay:     () => EnterStage(stageId),
                difficulty: (int)stage.difficulty,
                isLocked:   isLocked));
        }

        private void EnterStage(int stageId)
        {
            ScrollStateCache.HomeScrollPosition = _scrollRect != null
                ? _scrollRect.verticalNormalizedPosition : 0f;
            ScrollStateCache.LastPlayedStageId = stageId;

            var transition = Game.Core.SceneTransition.Instance;
            if (transition != null)
                transition.SlideUpToScene(InGameScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(InGameScene);
        }

        private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
            if (Mathf.Abs(d) < 0.0001f) return false;

            float u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
            float v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

            return (u >= 0f && u <= 1f && v >= 0f && v <= 1f);
        }
    }
}
