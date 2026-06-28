using UnityEngine;

namespace Game.Services
{
    // Client-side mirror of the server error codes the client must branch on (not just display).
    // Display-only codes stay in error_messages.csv; these are the ones that change client control flow.
    public static class ServerErrorCodes
    {
        // Mirror ProjectFill.Application.Common.ErrorCodes (stage attempt-token failures).
        public const string InvalidStageAttempt = "INVALID_STAGE_ATTEMPT";
        public const string StageAttemptExpired = "STAGE_ATTEMPT_EXPIRED";

        // Rewarded-ad SSV not yet verified. Model B: the SSV callback grants the reward server-side,
        // so the client treats this as pending-success (not a failure). See conventions/ad-reward-ssv-system.md.
        public const string AdSsvPending = "AD_SSV_PENDING";

        // IAP verification transiently unreachable (store/Google API outage). The purchase may be
        // valid; the client retries verification a few times behind the loading overlay before failing.
        public const string IapVerifyPending = "IAP_VERIFY_PENDING";

        // Receipt already redeemed server-side. The grant happened on a prior attempt; the client
        // still confirms the pending store transaction so it stops being redelivered.
        public const string DuplicateOrder = "DUPLICATE_ORDER";

        // Extracts the error code from a server failure body ({"code":"..."}); falls back to the raw text.
        public static string Parse(string errOrJson)
        {
            if (string.IsNullOrEmpty(errOrJson)) return errOrJson;
            try
            {
                var parsed = JsonUtility.FromJson<ErrorResponseJson>(errOrJson);
                if (parsed != null && !string.IsNullOrEmpty(parsed.code)) return parsed.code;
            }
            catch { }
            return errOrJson;
        }

        // True when a stage clear was rejected because its attempt token was missing/mismatched/expired.
        public static bool IsStageSessionError(string errOrJson)
        {
            string code = Parse(errOrJson);
            return code == InvalidStageAttempt || code == StageAttemptExpired;
        }
    }
}
