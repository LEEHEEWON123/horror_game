using UnityEngine;
using UnityEngine.Rendering;

public static class SponzaAtmosphere
{
    public static void Apply(Bounds mapBounds, float ceilingY)
    {
        HorrorScreenEffects.DisableAnalogNoiseForDimensionMaps();

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;

        RenderSettings.ambientSkyColor = new Color(0.16f, 0.15f, 0.14f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.11f, 0.1f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.075f, 0.07f);
        RenderSettings.ambientIntensity = 1.45f;
        RenderSettings.reflectionIntensity = 0.12f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.07f, 0.065f, 0.06f);
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 62f;

        ConfigureFillLight();
        EnsureAmbientAudio();
    }

    private static void EnsureAmbientAudio()
    {
        if (Object.FindAnyObjectByType<BackroomsAmbientAudio>() != null) return;
        var root = new GameObject("Map03AmbientAudio");
        BackroomsAmbientAudio.AddTo(root);
    }

    private static void ConfigureFillLight()
    {
        Light sun = null;
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Directional) continue;
            sun = light;
            break;
        }

        if (sun == null)
        {
            var sunGo = new GameObject("Map03FillLight");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.color = new Color(0.9f, 0.86f, 0.8f);
        sun.intensity = 0.28f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(52f, 145f, 0f);
    }
}
