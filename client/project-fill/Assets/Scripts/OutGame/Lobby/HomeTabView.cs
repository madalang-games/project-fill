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
        private readonly List<Image>         _signalPulses    = new List<Image>();
        private readonly List<Coroutine>     _pulseCoroutines = new List<Coroutine>();
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

            if (_stageNodePrefab == null)
                _stageNodePrefab = Resources.Load<GameObject>("Prefabs/UI/StageNodeView");

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

            if (_layoutBuilt && _builtPositions != null && _stages != null)
            {
                StartGuideOrb(_builtPositions, _stages.Length);
                StartSignalPulses(_builtPositions, _stages.Length);
            }

            _scrollRect.onValueChanged.AddListener(OnScrolled);

            // Immediate render only on tab re-enter (layout already built).
            // First build is deferred to ApplyScrollNextFrame so the viewport is
            // measured AFTER its first layout pass — gives the real device width,
            // letting the serpentine X-spacing scale responsively instead of
            // baking a fallback width while the canvas is still unsized.
            if (_layoutBuilt && _scrollRect.viewport.rect.height > 0f)
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

        // Re-pull current stage + re-bind visible nodes so a server-side progress change (e.g. the dev
        // /stage cheat) reflects live without leaving the lobby. Mirrors the OnEnable render path.
        public void Refresh()
        {
            _stages = StageDataService.Instance?.GetAll();
            _currentStageId = FindCurrentStage();
            if (_layoutBuilt && _scrollRect != null && _scrollRect.viewport != null
                && _scrollRect.viewport.rect.height > 0f)
                OnScrolled(new Vector2(0f, _scrollRect.verticalNormalizedPosition));
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



            // 2. Compute deterministic serpentine (ㄹ-shape) grid coordinates.
            //    Pattern repeats every 4 stages: a 3-node horizontal row followed
            //    by a single connector node above its end. Horizontal direction
            //    flips each cycle, so the trace winds like the ㄹ character.
            var positions = new Vector2[stageCount];
            float maxAllowedX = (viewportWidth * 0.5f) - 180f;
            float colSpacing  = Mathf.Min(maxAllowedX, 320f); // X gap from the center column to a side column
            float rowSpacing  = 330f;                          // Y gap between rows (keeps ≥300f node distance)

            for (int i = 0; i < stageCount; i++)
            {
                int  cycle       = i / 4;          // one cycle = a 3-node row + a connector node
                int  within      = i % 4;
                bool leftToRight = (cycle % 2) == 0;

                int   row;
                float x;
                if (within < 3)
                {
                    // 3-node horizontal row (col index 0=left, 1=center, 2=right)
                    row = cycle * 2;
                    int colFromLeft = leftToRight ? within : (2 - within);
                    x = (colFromLeft - 1) * colSpacing;
                }
                else
                {
                    // Single connector node directly above the end of the 3-node row
                    row = cycle * 2 + 1;
                    int endColFromLeft = leftToRight ? 2 : 0;
                    x = (endColFromLeft - 1) * colSpacing;
                }

                positions[i] = new Vector2(x, bottomPadding + row * rowSpacing);
            }

            // Find total height based on the laid-out positions
            float maxRelaxedY = 0f;
            for (int i = 0; i < stageCount; i++)
            {
                if (positions[i].y > maxRelaxedY) maxRelaxedY = positions[i].y;
            }
            float totalHeight = maxRelaxedY + bottomPadding + 50f;
            _contentRoot.sizeDelta = new Vector2(_contentRoot.sizeDelta.x, totalHeight);

            // Convert positions to anchor-top space (negative Y) and store
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
            StartSignalPulses(positions, stageCount);
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

                    foreach (var r in response.GrantedRewards)
                    {
                        if (r.RewardType == "SOFT_CURRENCY")
                            PlayerProgressService.Instance?.AddGold(r.Amount);
                    }
                    var rewardItems = RewardDisplay.Build(response.GrantedRewards);
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
                int endIdx   = range.last;

                // Draw the incoming bridge (prev chapter's last node → this chapter's first node)
                // as part of THIS chapter, so the boundary segment takes the new chapter's color
                // instead of the previous chapter's.
                int drawStart = startIdx > 0 ? startIdx - 1 : startIdx;

                int segCount = endIdx - drawStart + 1;
                if (segCount < 2) continue;

                var chapterPts = new Vector2[segCount];
                for (int s = 0; s < segCount; s++)
                {
                    chapterPts[s] = nodePositions[drawStart + s];
                }

                // Straight polyline through the nodes — no curve smoothing.
                var curve = new List<Vector2>(chapterPts);
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

                // Light the trace only up to the furthest reachable (unlocked) node; the locked
                // tail beyond the current challenge stage stays unlit.
                float activeLen = 0f, cum = 0f;
                for (int s = 0; s < curve.Count; s++)
                {
                    if (s > 0) cum += Vector2.Distance(curve[s - 1], curve[s]);
                    int gi  = drawStart + s;
                    int sid = gi < _stages.Length ? _stages[gi].stage_id : -1;
                    if (sid > 0 && (progress?.IsStageUnlocked(sid) ?? false)) activeLen = cum;
                }
                pathStrip.inactiveColor = new Color(theme.PathColor.r, theme.PathColor.g, theme.PathColor.b, 0.10f);
                pathStrip.activeLength  = activeLen;
                pathStrip.color         = activeLen > 0f ? theme.PathColor : pathStrip.inactiveColor;
                pathStrip.scrollSpeed   = activeLen > 0f ? theme.PathScrollSpeed : 0f;
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

        // Ambient signal pulses travelling along the cleared portion of the trace.
        private void StartSignalPulses(Vector2[] positions, int count)
        {
            foreach (var c in _pulseCoroutines) if (c != null) StopCoroutine(c);
            _pulseCoroutines.Clear();
            foreach (var p in _signalPulses) if (p != null) Destroy(p.gameObject);
            _signalPulses.Clear();

            int maxIdx = _currentStageId - 1; // travel nodes 0..maxIdx (reached path)
            if (maxIdx < 1 || maxIdx >= count) return;

            var sprite = DynamicResourceService.Instance?.GetSprite("deco_pulse")
                      ?? DynamicResourceService.Instance?.GetSprite("led_star");

            const int pulseCount = 3;
            for (int k = 0; k < pulseCount; k++)
            {
                var go = new GameObject($"SignalPulse{k}", typeof(Image));
                go.transform.SetParent(_contentRoot, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.sprite        = sprite;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(18f, 18f);
                _signalPulses.Add(img);
                _pulseCoroutines.Add(StartCoroutine(
                    SignalPulseRoutine(img, positions, maxIdx, k / (float)pulseCount)));
            }
        }

        private IEnumerator SignalPulseRoutine(Image img, Vector2[] positions, int maxIdx, float startOffset)
        {
            var   rt    = img.rectTransform;
            const float speed = 320f; // px/sec along the trace
            float prog  = startOffset * maxIdx; // progress in node-index space

            while (true)
            {
                int     i    = Mathf.Clamp((int)prog, 0, maxIdx - 1);
                Vector2 a    = positions[i];
                Vector2 b    = positions[i + 1];
                float   frac = prog - i;
                rt.anchoredPosition = Vector2.Lerp(a, b, frac);

                int   cid   = _stages[Mathf.Clamp(i, 0, _stages.Length - 1)].chapter_id;
                Color col   = ChapterBgTheme.Get(cid).PathColor;
                float edge  = Mathf.Clamp01(Mathf.Min(prog, maxIdx - prog)); // fade at trace ends
                img.color   = new Color(col.r, col.g, col.b, edge);

                float segLen = Vector2.Distance(a, b);
                prog += segLen > 0.01f ? (speed * Time.deltaTime / segLen) : 1f;
                if (prog >= maxIdx) prog -= maxIdx; // loop
                yield return null;
            }
        }

        private IEnumerator OrbTravelRoutine(RectTransform rt, Vector2 start, Vector2 end)
        {
            while (true)
            {
                float t = 0f;
                while (t < 1.0f)
                {
                    t += Time.deltaTime * 0.7f;
                    float tc = Mathf.Clamp01(t);

                    // Straight-line travel between nodes (matches the straight path).
                    rt.anchoredPosition = Vector2.Lerp(start, end, tc);

                    float s = 1.0f + 0.35f * Mathf.Sin(tc * Mathf.PI);
                    rt.localScale = new Vector3(s, s, 1f);

                    yield return null;
                }
                yield return new WaitForSeconds(0.4f);
            }
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
                bool unlock  = progress?.IsStageUnlocked(s.stage_id) ?? (s.stage_id == 1);
                node.Bind(s.stage_id, unlock, s.chapter_id, (int)s.difficulty);
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

            // First build happens here: the viewport now carries its real,
            // device-correct width from the first layout pass, so BuildPool's
            // X-spacing math scales to the actual screen instead of a fallback.
            if (!_layoutBuilt)
            {
                BuildPool();
                _layoutBuilt = true;
            }

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
    }
}
