using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Services
{
    public class AdEligibilityCache : MonoBehaviour
    {
        public static AdEligibilityCache Instance { get; private set; }

        [Serializable]
        private class PlacementStatusJson
        {
            public string placementId;
            public bool   isEligible;
            public int    cooldownRemainingSeconds;
        }

        [Serializable]
        private class EligibilityWrapper
        {
            public List<PlacementStatusJson> placements;
        }

        private readonly Dictionary<string, PlacementStatusJson> _cache
            = new Dictionary<string, PlacementStatusJson>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // 인증 완료 후 세션 시작 시 호출합니다. NetworkService 에서 baseUrl/authToken 을 자동으로 가져옵니다.
        public void Refresh()
        {
            NetworkService.Instance.Get("/api/ad/eligibility", (ok, result) =>
            {
                if (!ok) return;
                var wrapper = JsonUtility.FromJson<EligibilityWrapper>(result);
                if (wrapper?.placements == null) return;
                _cache.Clear();
                foreach (var s in wrapper.placements)
                    if (s?.placementId != null)
                        _cache[s.placementId] = s;
            });
        }

        // 하위 호환용 오버로드 (baseUrl, authToken 을 직접 지정할 때 사용).
        public void Refresh(string baseUrl, string authToken = null)
        {
            StartCoroutine(FetchEligibility(baseUrl, authToken));
        }

        private IEnumerator FetchEligibility(string baseUrl, string authToken)
        {
            using var req = UnityWebRequest.Get($"{baseUrl}/api/ad/eligibility");
            if (!string.IsNullOrEmpty(authToken))
                req.SetRequestHeader("Authorization", $"Bearer {authToken}");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            var wrapper = JsonUtility.FromJson<EligibilityWrapper>(req.downloadHandler.text);
            if (wrapper?.placements == null) yield break;
            _cache.Clear();
            foreach (var s in wrapper.placements)
                if (s?.placementId != null)
                    _cache[s.placementId] = s;
        }

        public bool IsEligible(string placementId)
            => _cache.TryGetValue(placementId, out var s) && s.isEligible;

        public int GetCooldownSeconds(string placementId)
            => _cache.TryGetValue(placementId, out var s) ? s.cooldownRemainingSeconds : 0;

        // Optimistically mark interstitial ineligible until next Refresh.
        public void OnInterstitialShown()
        {
            const string id = "INTERSTITIAL_POST_STAGE";
            if (_cache.TryGetValue(id, out var s))
            {
                s.isEligible = false;
                s.cooldownRemainingSeconds = 180;
            }
        }
    }
}
