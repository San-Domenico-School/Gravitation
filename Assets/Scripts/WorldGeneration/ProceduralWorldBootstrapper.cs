using UnityEngine;

public static class ProceduralWorldBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSessionExists()
    {
        if (Object.FindFirstObjectByType<ProceduralWorldSession>() != null)
            return;

        GameObject sessionObject = new GameObject("ProceduralWorldSession");
        Object.DontDestroyOnLoad(sessionObject);
        sessionObject.AddComponent<ProceduralWorldSession>();
    }
}
