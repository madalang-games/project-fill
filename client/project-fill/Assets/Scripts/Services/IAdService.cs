using System;

namespace Game.Services
{
    public struct AdWatchResult
    {
        public bool   Earned;
        public string AdToken; // SSV nonce — pass to server for reward claim
    }

    public interface IAdService
    {
        // Returns AdWatchResult{Earned=true, AdToken=nonce} when reward earned; null on cancel/fail/no-ad.
        void WatchRewardedAd(string placementId, Action<AdWatchResult?> onComplete);

        // Shows interstitial if cache says eligible and not suppressed.
        // wasShown=true if ad was actually displayed; caller should call POST /api/ad/interstitial/shown.
        void ShowInterstitialIfEligible(int stageId, bool suppressByDoubleReward, Action<bool> onComplete);
    }
}
