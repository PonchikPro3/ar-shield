using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Shield.Editor
{
    public static class PackageInstaller
    {
        static AddAndRemoveRequest request;
        static double deadline;
        public static void Install()
        {
            request = Client.AddAndRemove(new[] {
                "com.unity.xr.management@4.5.3",
                "com.de-panther.webxr@file:../../Vendor/webxr",
                "com.de-panther.webxr-interactions@file:../../Vendor/webxr-interactions"
            }, new[] { "com.unity.collab-proxy", "com.unity.feature.development",
                "com.unity.multiplayer.center", "com.unity.timeline", "com.unity.visualscripting" });
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (!request.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= Poll;
            bool ok = request.IsCompleted && request.Status == StatusCode.Success;
            if (ok) Debug.Log("[Shield] WebXR packages installed.");
            else Debug.LogError(request.Error?.message ?? "UPM timeout");
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
