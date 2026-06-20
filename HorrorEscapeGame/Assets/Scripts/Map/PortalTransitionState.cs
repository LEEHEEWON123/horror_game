using UnityEngine;

public static class PortalTransitionState
{
    public static string NextScene { get; private set; }
    public static int MapSeed { get; private set; }
    public static bool PendingEntry { get; private set; }
    public static Vector3 EntryForward { get; private set; } = Vector3.forward;

    public static bool HasNext => !string.IsNullOrEmpty(NextScene);

    public static void Prepare(string currentScene)
    {
        NextScene = DimensionMapPool.PickRandom(currentScene);
        MapSeed = Random.Range(1000, 999999);
    }

    public static void UseGenerationSeed()
    {
        MapGenerationContext.OverrideSeed = MapSeed;
    }

    public static void MarkPendingEntry(Vector3 portalForward)
    {
        PendingEntry = true;
        EntryForward = portalForward.sqrMagnitude > 0.01f ? portalForward.normalized : Vector3.forward;
    }

    public static void ClearAfterEntry()
    {
        PendingEntry = false;
        NextScene = null;
        MapSeed = 0;
        MapGenerationContext.Clear();
    }

    public static void Reset()
    {
        NextScene = null;
        MapSeed = 0;
        PendingEntry = false;
        EntryForward = Vector3.forward;
        MapGenerationContext.Clear();
    }
}
