using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Film Grain post-processing. Toggle <see cref="Enabled"/> to disable.
/// Remove <see cref="HorrorSceneBootstrap"/> call to revert fully.
/// </summary>
public static class HorrorFilmGrain
{
    public const bool Enabled = false;

    private const float DefaultIntensity = 0.32f;
    private const float OverlayMapIntensity = 0.18f;

    public static void TryApplyForScene(string sceneName)
    {
        if (!Enabled || DimensionMapPool.IsRandomMap(sceneName) || !ShouldApply(sceneName))
            return;

        if (Object.FindAnyObjectByType<HorrorFilmGrainVolume>() != null)
            return;

        float intensity = UsesAnalogOverlay(sceneName) ? OverlayMapIntensity : DefaultIntensity;
        CreateVolume(intensity);
    }

    private static bool ShouldApply(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (sceneName == "Map_00" || sceneName == "Map_Reality")
            return true;

        return DimensionMapPool.IsRandomMap(sceneName);
    }

    private static bool UsesAnalogOverlay(string sceneName) =>
        sceneName == "Map_00" || sceneName == "Map_Reality";

    private static void CreateVolume(float intensity)
    {
        var volumeGo = new GameObject("HorrorFilmGrainVolume");
        volumeGo.AddComponent<HorrorFilmGrainVolume>();

        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 12;
        volume.weight = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "RuntimeHorrorFilmGrain";

        var grain = profile.Add<FilmGrain>(true);
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(intensity);
        grain.response.Override(0.75f);

        volume.profile = profile;
    }
}

public sealed class HorrorFilmGrainVolume : MonoBehaviour
{
}
