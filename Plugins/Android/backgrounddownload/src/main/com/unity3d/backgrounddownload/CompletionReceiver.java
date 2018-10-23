package com.unity3d.backgrounddownload;

import android.app.DownloadManager;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

public class CompletionReceiver extends BroadcastReceiver {
    public interface Callback {
        void downloadCompleted();
    }

    private static Callback callback;

    public static void setCallback(Callback cback) {
        callback = cback;
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        if (callback == null)
            return;
        if (DownloadManager.ACTION_DOWNLOAD_COMPLETE.equals(intent.getAction())) {
            callback.downloadCompleted();
        }
    }
}
