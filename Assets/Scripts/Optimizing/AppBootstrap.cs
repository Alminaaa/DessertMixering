using UnityEngine;

public static class AppBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Mobile defaults to 30 FPS, which feels laggy.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Do not let the screen turn off while playing.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}