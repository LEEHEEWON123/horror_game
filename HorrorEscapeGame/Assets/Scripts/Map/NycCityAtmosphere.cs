using UnityEngine;
using UnityEngine.Rendering;

public static class NycCityAtmosphere
{
    public static void Apply(Bounds mapBounds)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.12f, 0.14f, 0.2f);
        RenderSettings.ambientEquatorColor = new Color(0.08f, 0.09f, 0.11f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.05f);
        RenderSettings.ambientIntensity = 1.1f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.08f, 0.1f, 0.14f);
        RenderSettings.fogStartDistance = Mathf.Max(28f, mapBounds.size.x * 0.25f);
        RenderSettings.fogEndDistance = Mathf.Max(75f, mapBounds.size.x * 0.75f);

        Light sun = null;
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Directional) continue;
            sun = light;
            break;
        }

        if (sun == null)
        {
            var sunGo = new GameObject("CitySun");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(38f, -55f, 0f);
        sun.intensity = 0.55f;
        sun.color = new Color(0.72f, 0.78f, 0.95f);
        sun.shadows = LightShadows.None;
    }
}
