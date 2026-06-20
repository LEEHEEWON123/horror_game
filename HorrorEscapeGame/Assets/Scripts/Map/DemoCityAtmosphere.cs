#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

public static class DemoCityAtmosphere
{
    public const string SkyboxMaterialPath =
        "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/SkyBox/CustomSky.mat";

    public enum Mood
    {
        DayPrologue,
        RealityReturn
    }

    public static void Apply(Bounds mapBounds, Mood mood)
    {
        if (mood == Mood.DayPrologue)
            ApplyDayPrologue(mapBounds);
        else
            ApplyRealityReturn(mapBounds);
    }

    private static void ApplyDayPrologue(Bounds bounds)
    {
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.78f, 0.74f, 0.66f);
        RenderSettings.fogStartDistance = 10f;
        RenderSettings.fogEndDistance = 58f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.74f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.58f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.46f, 0.42f, 0.38f);
        RenderSettings.ambientIntensity = 1.05f;
        RenderSettings.reflectionIntensity = 0.62f;

        ConfigureSun(new Color(1f, 0.9f, 0.74f), 0.88f, new Vector3(48f, 158f, 0f));
        DimCityLights(intensityScale: 0.06f, shadowDistance: 55f);
        EnsureReflectionProbe(bounds, intensity: 0.72f);
    }

    private static void ApplyRealityReturn(Bounds bounds)
    {
#if UNITY_EDITOR
        var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        if (sky != null)
            RenderSettings.skybox = sky;
        else
            RenderSettings.skybox = null;
#else
        RenderSettings.skybox = null;
#endif

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.74f);
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 72f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.8f, 0.86f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.66f, 0.7f);
        RenderSettings.ambientGroundColor = new Color(0.46f, 0.48f, 0.5f);
        RenderSettings.ambientIntensity = 1.35f;
        RenderSettings.reflectionIntensity = 1f;

        ConfigureSun(new Color(1f, 0.94f, 0.82f), 0.75f, new Vector3(38f, 165f, 0f));
        DimCityLights(intensityScale: 0.35f, shadowDistance: 70f);
        EnsureReflectionProbe(bounds, intensity: 0.95f);
    }

    private static void ConfigureSun(Color color, float intensity, Vector3 euler)
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
            var sunGo = new GameObject("DemoCitySun");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.color = color;
        sun.intensity = intensity;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(euler);
    }

    private static void DimCityLights(float intensityScale, float shadowDistance)
    {
        QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, shadowDistance);

        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type == LightType.Directional) continue;
            light.intensity *= intensityScale;
            light.shadows = LightShadows.None;
        }
    }

    private static void EnsureReflectionProbe(Bounds bounds, float intensity)
    {
        var probeGo = new GameObject("DemoCityReflectionProbe");
        probeGo.transform.position = new Vector3(bounds.center.x, bounds.center.y + 2f, bounds.center.z);

        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.size = bounds.size + Vector3.up * 8f;
        probe.intensity = intensity;
        probe.boxProjection = true;
        probe.RenderProbe();
    }
}
