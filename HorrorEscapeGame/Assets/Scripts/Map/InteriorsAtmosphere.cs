using UnityEngine;
using UnityEngine.Rendering;

public static class InteriorsAtmosphere
{
    public static void Apply(Bounds mapBounds)
    {
        ConfigureRenderSettings();
        ConfigureSunLight();
        AddCorridorLightGrid(mapBounds);
        EnsureReflectionProbe(mapBounds);
        EnsureAmbientAudio();
    }

    private static void EnsureAmbientAudio()
    {
        if (Object.FindAnyObjectByType<BackroomsAmbientAudio>() != null) return;
        var root = new GameObject("InteriorsAudio");
        BackroomsAmbientAudio.AddTo(root);
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.58f, 0.6f, 0.62f);
        RenderSettings.ambientEquatorColor = new Color(0.48f, 0.5f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.36f, 0.38f, 0.4f);
        RenderSettings.ambientIntensity = 1.15f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.68f, 0.7f, 0.73f);
        RenderSettings.fogStartDistance = 5f;
        RenderSettings.fogEndDistance = 22f;
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
            var sunGo = new GameObject("InteriorsSun");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.color = new Color(0.88f, 0.9f, 0.94f);
        sun.intensity = 0.1f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(88f, 18f, 0f);
    }

    private static void AddCorridorLightGrid(Bounds bounds)
    {
        var root = new GameObject("InteriorsCorridorLights");
        float lowCeilingY = bounds.min.y + 2.65f;
        const float spacing = 9f;

        for (float x = bounds.min.x + 4.5f; x <= bounds.max.x - 4.5f; x += spacing)
        {
            for (float z = bounds.min.z + 4.5f; z <= bounds.max.z - 4.5f; z += spacing)
            {
                var panel = new GameObject("CorridorLight");
                panel.transform.SetParent(root.transform);
                panel.transform.position = new Vector3(x, lowCeilingY, z);

                var light = panel.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 11f;
                light.intensity = 1.85f;
                light.color = new Color(0.9f, 0.94f, 1f);
                light.shadows = LightShadows.None;
            }
        }
    }

    private static void EnsureReflectionProbe(Bounds bounds)
    {
        var probeGo = new GameObject("InteriorsReflectionProbe");
        probeGo.transform.position = new Vector3(bounds.center.x, bounds.center.y + 0.5f, bounds.center.z);

        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.size = bounds.size + Vector3.up * 2f;
        probe.boxProjection = true;
        probe.intensity = 0.52f;
        probe.RenderProbe();
    }
}
