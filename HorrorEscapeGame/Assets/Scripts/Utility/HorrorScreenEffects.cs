using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Map_01~07: Film Grain / UI static / VHS-style overlays off. Map_00·Reality는 그대로.
/// </summary>
public static class HorrorScreenEffects
{
    public static void DisableAnalogNoiseForDimensionMaps()
    {
        DestroyRuntimeGrainVolumes();
        DestroyAnalogUiOverlays();
        DisableFilmGrainOnAllVolumes();
        SceneTransitioner.Instance?.HideStaticOverlay();
    }

    private static void DestroyRuntimeGrainVolumes()
    {
        foreach (var grainVolume in Object.FindObjectsByType<HorrorFilmGrainVolume>(FindObjectsInactive.Include))
            Object.Destroy(grainVolume.gameObject);
    }

    private static void DestroyAnalogUiOverlays()
    {
        foreach (var overlay in Object.FindObjectsByType<VillageAnalogOverlay>(FindObjectsInactive.Include))
            Object.Destroy(overlay.gameObject);

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas == null)
                continue;

            string name = canvas.gameObject.name;
            if (name == "VillageAnalogCanvas" || name == "BackroomsAnalogCanvas")
                Object.Destroy(canvas.gameObject);
        }
    }

    private static void DisableFilmGrainOnAllVolumes()
    {
        foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
        {
            if (volume.profile == null)
                continue;

            if (!volume.profile.TryGet(out FilmGrain grain))
                continue;

            grain.active = false;
            grain.intensity.Override(0f);
        }
    }
}

public sealed class HorrorScreenEffectsRunner : MonoBehaviour
{
    public static void Schedule(string sceneName)
    {
        if (!DimensionMapPool.IsRandomMap(sceneName))
            return;

        HorrorScreenEffects.DisableAnalogNoiseForDimensionMaps();

        if (Object.FindAnyObjectByType<HorrorScreenEffectsRunner>() != null)
            return;

        new GameObject("HorrorScreenEffectsRunner").AddComponent<HorrorScreenEffectsRunner>();
    }

    private void Start() => StartCoroutine(DisableAfterMapReady());

    private System.Collections.IEnumerator DisableAfterMapReady()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        HorrorScreenEffects.DisableAnalogNoiseForDimensionMaps();
        Destroy(gameObject);
    }
}
