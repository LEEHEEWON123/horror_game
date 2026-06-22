using UnityEngine;
using UnityEngine.SceneManagement;

public static class HorrorSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BootstrapActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
            return;

        BootstrapActiveScene();
        HorrorScreenEffectsRunner.Schedule(scene.name);
        ApplyDimensionEntrySafety();
        TryApplyPortalEntry();
        TryScheduleDimensionWake(scene.name);
        TryScheduleRealityFall(scene.name);
        ReturnMapNotice.TryShowForScene(scene.name);
    }

    private static void TryApplyPortalEntry()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            PortalEntrySpawn.ApplyIfPending(player);
    }

    private static void ApplyDimensionEntrySafety()
    {
        if (!DimensionMapPool.IsRandomMap(SceneManager.GetActiveScene().name)) return;
        if (!NoclipEntryState.HasPending()) return;

        DimensionWakeState.SetActive(true);
        MonsterAI.PauseAllForWake();
    }

    private static void TryScheduleRealityFall(string sceneName)
    {
        if (sceneName != DimensionMapPool.RealityScene || !RealityEscapeState.HasPending())
            return;

        if (Object.FindAnyObjectByType<RealityFallCoordinator>() != null)
            return;

        new GameObject("RealityFallCoordinator").AddComponent<RealityFallCoordinator>();
    }

    private static void TryScheduleDimensionWake(string sceneName)
    {
        if (!DimensionMapPool.IsRandomMap(sceneName) || !NoclipEntryState.HasPending())
            return;

        if (Object.FindAnyObjectByType<DimensionWakeCoordinator>() != null)
            return;

        new GameObject("DimensionWakeCoordinator").AddComponent<DimensionWakeCoordinator>();
    }

    private static void BootstrapActiveScene()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Map_01":
                if (Object.FindAnyObjectByType<Map01Bootstrap>() == null)
                    new GameObject("Map01Bootstrap").AddComponent<Map01Bootstrap>();
                break;
            case "Map_02":
                if (Object.FindAnyObjectByType<Map02Bootstrap>() == null)
                    new GameObject("Map02Bootstrap").AddComponent<Map02Bootstrap>();
                break;
            case "Map_03":
                if (Object.FindAnyObjectByType<Map03Bootstrap>() == null)
                    new GameObject("Map03Bootstrap").AddComponent<Map03Bootstrap>();
                break;
            case "Map_04":
                if (Object.FindAnyObjectByType<Map04Bootstrap>() == null)
                    new GameObject("Map04Bootstrap").AddComponent<Map04Bootstrap>();
                break;
            case "Map_05":
                if (Object.FindAnyObjectByType<Map05Bootstrap>() == null)
                    new GameObject("Map05Bootstrap").AddComponent<Map05Bootstrap>();
                break;
            case "Map_06":
                if (Object.FindAnyObjectByType<Map06Bootstrap>() == null)
                    new GameObject("Map06Bootstrap").AddComponent<Map06Bootstrap>();
                break;
            case "Map_07":
                if (Object.FindAnyObjectByType<Map07Bootstrap>() == null)
                    new GameObject("Map07Bootstrap").AddComponent<Map07Bootstrap>();
                break;
            case "Map_00":
                if (Object.FindAnyObjectByType<Map00Bootstrap>() == null)
                    new GameObject("Map00Bootstrap").AddComponent<Map00Bootstrap>();
                break;
            case "Map_Reality":
                if (Object.FindAnyObjectByType<MapRealityBootstrap>() == null)
                    new GameObject("MapRealityBootstrap").AddComponent<MapRealityBootstrap>();
                break;
            case "MainMenu":
                if (Object.FindAnyObjectByType<MainMenuBootstrap>() == null)
                    new GameObject("MainMenuBootstrap").AddComponent<MainMenuBootstrap>();
                break;
            case "ComingSoon":
                if (Object.FindAnyObjectByType<ComingSoonBootstrap>() == null)
                    new GameObject("ComingSoonBootstrap").AddComponent<ComingSoonBootstrap>();
                break;
        }

        PlayerSettingsAttach.TryAttach();
    }
}
