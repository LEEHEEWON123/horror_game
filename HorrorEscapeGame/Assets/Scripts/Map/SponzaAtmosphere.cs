using UnityEngine;
using UnityEngine.Rendering;

public static class SponzaAtmosphere
{
    public static void Apply(Bounds mapBounds, float ceilingY)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;

        RenderSettings.ambientSkyColor = new Color(0.18f, 0.17f, 0.16f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.13f, 0.12f);
        RenderSettings.ambientGroundColor = new Color(0.1f, 0.095f, 0.09f);
        RenderSettings.ambientIntensity = 1.35f;
        RenderSettings.reflectionIntensity = 0.65f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.08f, 0.075f, 0.07f);
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 42f;

        ConfigureFillLight();
        AddSparseTorches(mapBounds, ceilingY);
        EnsureReflectionProbe(mapBounds, ceilingY);
        EnsureAmbientAudio();
    }

    private static void EnsureAmbientAudio()
    {
        if (Object.FindAnyObjectByType<BackroomsAmbientAudio>() != null) return;
        var root = new GameObject("SponzaAudio");
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
            var sunGo = new GameObject("SponzaFillLight");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.color = new Color(0.92f, 0.88f, 0.82f);
        sun.intensity = 0.22f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(52f, 145f, 0f);
    }

    private static void AddSparseTorches(Bounds bounds, float ceilingY)
    {
        var root = new GameObject("SponzaMazeLights");
        const float spacing = 9f;

        for (float x = bounds.min.x + 2f; x <= bounds.max.x - 2f; x += spacing)
        {
            for (float z = bounds.min.z + 2f; z <= bounds.max.z - 2f; z += spacing)
            {
                var torch = new GameObject("Torch");
                torch.transform.SetParent(root.transform);
                torch.transform.position = new Vector3(x, ceilingY - 0.55f, z);

                var light = torch.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 14f;
                light.intensity = 0.95f;
                light.color = new Color(1f, 0.82f, 0.58f);
                light.shadows = LightShadows.None;
            }
        }
    }

    private static void EnsureReflectionProbe(Bounds bounds, float ceilingY)
    {
        var probeGo = new GameObject("SponzaReflectionProbe");
        probeGo.transform.position = new Vector3(bounds.center.x, ceilingY * 0.45f, bounds.center.z);

        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.size = bounds.size + Vector3.up * 2f;
        probe.intensity = 0.7f;
        probe.boxProjection = true;
        probe.RenderProbe();
    }
}
