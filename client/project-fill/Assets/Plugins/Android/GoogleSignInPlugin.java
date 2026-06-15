package com.madalang.pixelpop;

import android.app.Activity;
import android.content.Intent;
import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;

public class GoogleSignInPlugin {
    public static final int RC_SIGN_IN = 9001;

    public static void signIn(Activity activity, String webClientId) {
        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
            .requestIdToken(webClientId)
            .requestEmail()
            .build();
        GoogleSignInClient client = GoogleSignIn.getClient(activity, gso);
        activity.startActivityForResult(client.getSignInIntent(), RC_SIGN_IN);
    }

    public static void signOut(Activity activity, String webClientId) {
        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
            .requestIdToken(webClientId)
            .build();
        GoogleSignIn.getClient(activity, gso).signOut();
    }

    public static void handleActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode != RC_SIGN_IN) return;
        Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(data);
        try {
            GoogleSignInAccount account = task.getResult(ApiException.class);
            String idToken = account.getIdToken();
            if (idToken != null && !idToken.isEmpty()) {
                UnityPlayer.UnitySendMessage("GoogleSignInBridge", "OnSignInSuccess", idToken);
            } else {
                UnityPlayer.UnitySendMessage("GoogleSignInBridge", "OnSignInFailed", "GOOGLE_ID_TOKEN_MISSING");
            }
        } catch (ApiException e) {
            UnityPlayer.UnitySendMessage("GoogleSignInBridge", "OnSignInFailed", mapStatusCode(e.getStatusCode()));
        }
    }

    private static String mapStatusCode(int code) {
        switch (code) {
            case 12501: return "GOOGLE_SIGN_IN_CANCELLED";
            case 7:     return "NETWORK_ERROR";
            default:    return "GOOGLE_SIGN_IN_FAILED_" + code;
        }
    }
}
