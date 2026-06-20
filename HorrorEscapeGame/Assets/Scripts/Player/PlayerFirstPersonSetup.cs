using UnityEngine;

public static class PlayerFirstPersonSetup
{
    public static void HideBodyRenderers(GameObject visualRoot)
    {
        if (visualRoot == null) return;

        foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    public static Light AttachFlashlight(Transform cameraTransform)
    {
        var lightGo = new GameObject("Flashlight");
        lightGo.transform.SetParent(cameraTransform, false);
        lightGo.transform.localPosition = Vector3.zero;
        lightGo.transform.localRotation = Quaternion.identity;

        var spot = lightGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 20f;
        spot.spotAngle = 60f;
        spot.innerSpotAngle = 40f;
        spot.intensity = 0.35f;
        spot.shadows = LightShadows.Soft;
        spot.color = new Color(1f, 0.94f, 0.82f);
        return spot;
    }
}
