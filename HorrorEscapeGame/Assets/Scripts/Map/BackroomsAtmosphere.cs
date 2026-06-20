#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

public static class BackroomsAtmosphere
{
    public const string VolumeProfilePath =
        "Assets/LoafbrrAssets/BackroomsLikeAssetRe/scenes/LevelTst/Global Volume Profile.asset";

    public static void Apply(Bounds mapBounds)
    {
        ConfigureRenderSettings();
        ConfigureSunLight();
        AddCeilingLightGrid(mapBounds);
        EnsurePostProcessVolume();
        EnsureReflectionProbe(mapBounds);
        EnsureAmbientAudio();
    }

    private static void EnsureAmbientAudio()
    {
        if (Object.FindAnyObjectByType<BackroomsAmbientAudio>() != null) return;

        var root = new GameObject("BackroomsAudio");
        BackroomsAmbientAudio.AddTo(root);
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.38f, 0.44f, 0.34f);
        RenderSettings.ambientEquatorColor = new Color(0.32f, 0.38f, 0.3f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.26f, 0.2f);
        RenderSettings.ambientIntensity = 1.2f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.42f, 0.52f, 0.36f);
        RenderSettings.fogStartDistance = 10f;
        RenderSettings.fogEndDistance = 32f;
    }

    private static void ConfigureSunLight()
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
            var sunGo = new GameObject("BackroomsSun");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.color = new Color(0.88f, 0.95f, 0.78f);
        sun.intensity = 0.08f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(88f, 12f, 0f);
    }

    private static void AddCeilingLightGrid(Bounds bounds)
    {
        var root = new GameObject("BackroomsCeilingLights");
        float ceilingY = bounds.max.y - 0.25f;
        const float spacing = 4f;

        for (float x = bounds.min.x + 1.5f; x <= bounds.max.x - 1.5f; x += spacing)
        {
            for (float z = bounds.min.z + 1.5f; z <= bounds.max.z - 1.5f; z += spacing)
            {
                var panel = new GameObject("FluorescentPanel");
                panel.transform.SetParent(root.transform);
                panel.transform.position = new Vector3(x, ceilingY, z);

                var light = panel.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 13f;
                light.intensity = 1.75f;
                light.color = new Color(0.92f, 1f, 0.82f);
                light.shadows = LightShadows.None;
            }
        }
    }

    private static void EnsurePostProcessVolume()
    {
        if (Object.FindAnyObjectByType<Volume>() != null) return;

        var profile = LoadVolumeProfile();
        if (profile == null) return;

        var volumeGo = new GameObject("BackroomsGlobalVolume");
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 5;
        volume.profile = profile;
    }

    private static VolumeProfile LoadVolumeProfile()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
#else
        return null;
#endif
    }

    private static void EnsureReflectionProbe(Bounds bounds)
    {
        var probeGo = new GameObject("BackroomsReflectionProbe");
        probeGo.transform.position = new Vector3(bounds.center.x, bounds.center.y + 1.2f, bounds.center.z);

        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.size = bounds.size + Vector3.up * 2f;
        probe.boxProjection = true;
        probe.intensity = 0.55f;
        probe.RenderProbe();
    }
}
