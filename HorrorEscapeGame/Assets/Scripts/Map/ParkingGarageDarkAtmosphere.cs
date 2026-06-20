using UnityEngine;
using UnityEngine.Rendering;

public static class ParkingGarageDarkAtmosphere
{
    public static void Apply(Bounds mapBounds, float floorHeight, int floorCount)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.012f, 0.014f, 0.018f);
        RenderSettings.ambientIntensity = 1f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.008f, 0.01f, 0.014f);
        RenderSettings.fogStartDistance = 5f;
        RenderSettings.fogEndDistance = 24f;

        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Directional) continue;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
        }

        AddSparseFluorescents(mapBounds, floorHeight, floorCount);
        EnsurePostProcessVolume();
        EnsureAmbientAudio();
    }

    private static void EnsureAmbientAudio()
    {
        if (Object.FindAnyObjectByType<BackroomsAmbientAudio>() != null) return;
        var root = new GameObject("ParkingGarageAudio");
        BackroomsAmbientAudio.AddTo(root);
    }

    private static void AddSparseFluorescents(Bounds bounds, float floorHeight, int floorCount)
    {
        var root = new GameObject("DarkGarageLights");
        const float spacing = 9f;

        for (int floor = 0; floor < floorCount; floor++)
        {
            float floorY = floor * floorHeight;
            float ceilingY = floorY + 2.85f;

            for (float x = bounds.min.x + 4f; x <= bounds.max.x - 4f; x += spacing)
            {
                for (float z = bounds.min.z + 4f; z <= bounds.max.z - 4f; z += spacing)
                {
                    if (((int)(x + z)) % 18 != 0) continue;

                    var panel = new GameObject($"Fluorescent_F{floor + 1}");
                    panel.transform.SetParent(root.transform);
                    panel.transform.position = new Vector3(x, ceilingY, z);

                    var light = panel.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 9f;
                    light.intensity = 0.55f;
                    light.color = new Color(0.72f, 0.8f, 0.95f);
                    light.shadows = LightShadows.Soft;
                }
            }
        }
    }

    private static void EnsurePostProcessVolume()
    {
        if (Object.FindAnyObjectByType<Volume>() != null) return;

#if UNITY_EDITOR
        var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            BackroomsAtmosphere.VolumeProfilePath);
        if (profile == null) return;

        var volumeGo = new GameObject("ParkingGarageGlobalVolume");
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 5;
        volume.profile = profile;
#endif
    }
}
