package com.madalang.pixelpop;

import android.content.Intent;
import com.unity3d.player.UnityPlayerGameActivity;

public class UnityGoogleSignInActivity extends UnityPlayerGameActivity {
    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        GoogleSignInPlugin.handleActivityResult(requestCode, resultCode, data);
    }
}
