using UnityEngine;
using UnityEngine.Rendering;

public static class PrototypeMapAtmosphere
{
    public static void Apply(Bounds mapBounds)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.38f, 0.4f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.23f, 0.24f);
        RenderSettings.ambientIntensity = 1f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.72f, 0.76f, 0.82f);
        RenderSettings.fogStartDistance = Mathf.Max(35f, mapBounds.size.x * 0.35f);
        RenderSettings.fogEndDistance = Mathf.Max(90f, mapBounds.size.x * 0.9f);

        Light sun = null;
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Directional) continue;
            sun = light;
            break;
        }

        if (sun == null)
        {
            var sunGo = new GameObject("OutdoorSun");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        sun.intensity = 1.05f;
        sun.color = new Color(1f, 0.96f, 0.9f);
        sun.shadows = LightShadows.Soft;
    }
}
