using System;
using System.Collections.Generic;
using System.Linq;
using Game.Services;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Data.Generated;
using UnityEngine;

namespace Game.Services.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        private static TutorialManager _instance;
        public static TutorialManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[TutorialManager] Instance is missing! Ensure it is placed in the Boot scene as a GameObject.");
                }
                return _instance;
            }
        }

        private const int RecapGroupId = 8; // Manual "How to play" recap group in tutorial_step.csv

        private TutorialStep[] _allSteps;
        private readonly HashSet<int> _completedGroupIds = new HashSet<int>();
        private readonly TutorialStepSequencer _sequencer = new TutorialStepSequencer();
        private int _activeGroupId = -1;
        private bool _recapMode; // recap (Pause "How to play") — never persists completion

        // A tutorial is active. IsBlocking gates boosters/pause; board taps use BoardInputBlocked
        // (action steps let the real tap through).
        public bool IsBlocking => _sequencer.IsActive;
        public bool IsActive => _sequencer.IsActive;
        public TutorialStep CurrentStep => _sequencer.CurrentStep;

        public event Action<TutorialStep> OnStepChanged;
        public event Action OnTutorialComplete;

        // Null-safe static access for scene callers that may run without a Boot-scene manager
        // (offline/dev). These avoid the error-logging Instance getter.
        public static bool ActiveBlocking => _instance != null && _instance.IsBlocking;
        // Board input is blocked only on Tap (informational) steps; Select/Move steps let the real
        // board action through so the player plays for real and the step advances on the action.
        public static bool BoardInputBlocked =>
            _instance != null && _instance._sequencer.IsActive &&
            _instance.CurrentStep != null && _instance.CurrentStep.advance_mode == TutorialAdvanceMode.Tap;
        // Called by the scene when the player performs a real action; advances if the current step expects it.
        public static void NotifyAction(TutorialAdvanceMode action)
        {
            if (_instance == null || !_instance._sequencer.IsActive) return;
            var step = _instance.CurrentStep;
            if (step != null && step.advance_mode == action) _instance._sequencer.Next();
        }
        public static void NotifyBoardReady(int stageId, IReadOnlyCollection<TutorialGimmick> gimmicks) => _instance?.OnBoardReady(stageId, gimmicks);
        public static void NotifyFailRepeat(int failCount) => _instance?.CheckFailTriggers(failCount);
        // Manual "How to play" recap (Pause button).
        public static void Recap() => _instance?.ShowRecap();
        // Null-safe re-pull of server tutorial progress (replace, not merge) — used by the dev /tutorial
        // cheat so a server-side change reflects on the client without a relaunch.
        public static void ReloadFromServer() => _instance?.ReloadProgress();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _allSteps = Utils.CsvLoader.Load<TutorialStep>(TutorialStep.ResourcePath);
            _sequencer.OnStepChanged += step => OnStepChanged?.Invoke(step);
            _sequencer.OnComplete += CompleteActiveTutorial;
        }

        public void LoadProgress()
        {
            if (TutorialApiService.Instance != null && AuthService.Instance != null && AuthService.Instance.IsAuthenticated)
            {
                TutorialApiService.Instance.FetchProgress(
                    onSuccess: response =>
                    {
                        foreach (var id in response.CompletedTutorialIds)
                        {
                            _completedGroupIds.Add(id);
                        }
                    },
                    onError: err =>
                    {
                        Debug.LogWarning($"[TutorialManager] Failed to fetch server progress: {err}, falling back to PlayerPrefs");
                        LoadLocalProgress();
                    }
                );
            }
            else
            {
                LoadLocalProgress();
            }
        }

        // Replace (not merge) completed groups from the server. LoadProgress only ADDS, so a server-side
        // removal (e.g. the /tutorial cheat un-seeing a group) needs a clear-then-fetch to reflect.
        public void ReloadProgress()
        {
            _completedGroupIds.Clear();
            LoadProgress();
        }

        private void LoadLocalProgress()
        {
            if (_allSteps == null) return;
            foreach (var step in _allSteps)
            {
                if (PlayerPrefs.GetInt(GroupPrefKey(step.group_id), 0) == 1)
                {
                    _completedGroupIds.Add(step.group_id);
                }
            }
        }

        private static string GroupPrefKey(int groupId) => "tut_done_" + groupId;

        public bool IsGroupCompleted(int groupId) => _completedGroupIds.Contains(groupId);

        // Kept for LobbyView; no lobby-triggered tutorials in the current design.
        public void CheckLobbyTriggers() { }

        /// <summary>
        /// Evaluate board-entry triggers. FirstLaunch fires by stage id; GimmickAppear fires when a
        /// matching board gimmick is present. Only one group runs at a time; remaining eligible groups
        /// fire on a later board-ready evaluation.
        /// </summary>
        public void OnBoardReady(int stageId, IReadOnlyCollection<TutorialGimmick> gimmicks)
        {
            if (_sequencer.IsActive) return;

            int group = FindEligibleGroup(s =>
                s.trigger_type == TutorialTriggerType.FirstLaunch &&
                int.TryParse(s.trigger_value, out int sid) && sid == stageId);

            if (group == -1 && gimmicks != null)
            {
                group = FindEligibleGroup(s =>
                    s.trigger_type == TutorialTriggerType.GimmickAppear &&
                    Enum.TryParse(s.trigger_value, out TutorialGimmick g) && gimmicks.Contains(g));
            }

            if (group != -1) TriggerGroup(group);
        }

        /// <summary>Evaluate consecutive-failure triggers (FailRepeat). trigger_value = fail threshold.</summary>
        public void CheckFailTriggers(int failCount)
        {
            if (_sequencer.IsActive) return;

            int group = FindEligibleGroup(s =>
                s.trigger_type == TutorialTriggerType.FailRepeat &&
                int.TryParse(s.trigger_value, out int threshold) && threshold == failCount);

            if (group != -1) TriggerGroup(group);
        }

        // Lowest not-yet-completed group_id whose steps satisfy the trigger match (any step of a group
        // shares the group's trigger, so matching one step is enough).
        private int FindEligibleGroup(Func<TutorialStep, bool> match)
        {
            if (_allSteps == null) return -1;
            int best = -1;
            foreach (var step in _allSteps)
            {
                if (IsGroupCompleted(step.group_id)) continue;
                if (!match(step)) continue;
                if (best == -1 || step.group_id < best) best = step.group_id;
            }
            return best;
        }

        private void TriggerGroup(int groupId)
        {
            var steps = new List<TutorialStep>();
            foreach (var step in _allSteps)
            {
                if (step.group_id == groupId) steps.Add(step);
            }
            steps.Sort((a, b) => a.step_index.CompareTo(b.step_index));
            if (steps.Count == 0) return;

            Debug.Log($"[TutorialManager] Triggered tutorial group: {groupId}");
            _activeGroupId = groupId;
            _sequencer.Start(steps);

            Core.UIManager.Instance?.ShowOverlay<Core.UI.TutorialOverlay>(overlay =>
            {
                overlay.Init(_sequencer);
            });
        }

        public void NextStep()
        {
            if (_sequencer.IsActive) _sequencer.Next();
        }

        // Manual "How to play" recap (Pause). Always shows; never persists completion.
        public void ShowRecap()
        {
            if (_sequencer.IsActive) return;
            _recapMode = true;
            TriggerGroup(RecapGroupId);
        }

        private void CompleteActiveTutorial()
        {
            if (_recapMode)
            {
                _recapMode = false;
                _activeGroupId = -1;
                Core.UIManager.Instance?.CloseOverlay();
                OnTutorialComplete?.Invoke();
                return;
            }

            if (_activeGroupId != -1)
            {
                CompleteTutorialGroup(_activeGroupId);
                _activeGroupId = -1;
            }
            OnTutorialComplete?.Invoke();
        }

        public void CompleteTutorialGroup(int groupId)
        {
            _completedGroupIds.Add(groupId);
            PlayerPrefs.SetInt(GroupPrefKey(groupId), 1);
            PlayerPrefs.Save();

            if (TutorialApiService.Instance != null && AuthService.Instance != null && AuthService.Instance.IsAuthenticated)
            {
                TutorialApiService.Instance.CompleteTutorial(groupId,
                    onSuccess: response => Debug.Log($"[TutorialManager] Saved progress for group {groupId} on server"),
                    onError: err => Debug.LogWarning($"[TutorialManager] Server save progress failed for group {groupId}: {err}")
                );
            }

            Core.UIManager.Instance?.CloseOverlay();
        }
    }
}
