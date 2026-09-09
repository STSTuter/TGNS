using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Ensures every Windows standalone build ships with a steam_appid.txt (AppID 480 - Spacewar)
/// sitting right next to the built .exe, so Steamworks.NET can initialize outside the Editor
/// without the game being launched through Steam.
/// </summary>
public class SteamAppIdPostBuild : IPostprocessBuildWithReport
{
    private const string AppId = "480";

    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows &&
            report.summary.platform != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        string buildDir = Path.GetDirectoryName(report.summary.outputPath);
        if (string.IsNullOrEmpty(buildDir))
        {
            Debug.LogError("[SteamAppIdPostBuild] Could not determine build output directory; steam_appid.txt was not written.");
            return;
        }

        string destination = Path.Combine(buildDir, "steam_appid.txt");
        File.WriteAllText(destination, AppId);
        Debug.Log($"[SteamAppIdPostBuild] Wrote {destination} with AppID {AppId}.");
    }
}
