using UnityEngine;
using UnityEngine.SceneManagement;

public static class DimensionPortalSpawner
{
    public static void Spawn(
        Vector3 pos,
        bool snapToFloor = true,
        float footOffset = 0f,
        PortalTransitionKind? kind = null)
    {
        if (snapToFloor)
            pos = MapFloor.PlaceOnFloor(pos, footOffset);

        var kindValue = kind ?? ResolveKind();

        var portal = new GameObject("DimensionPortal");
        portal.transform.position = pos;
        portal.transform.rotation = Quaternion.LookRotation(FindForwardHint(pos));

        bool showDoorPreview = kindValue == PortalTransitionKind.Dimension
            && DimensionMapPool.IsRandomMap(SceneManager.GetActiveScene().name);

        var visuals = DimensionPortalVisuals.Build(portal.transform, kindValue, showDoorPreview);

        var triggerGo = new GameObject("Trigger");
        triggerGo.transform.SetParent(portal.transform, false);
        triggerGo.transform.localPosition = new Vector3(0f, 1.2f, 0.1f);
        var col = triggerGo.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.5f, 2.8f, 2f);

        var proximity = portal.AddComponent<PortalProximityFx>();
        proximity.Configure(visuals.PortalLight, visuals.HumSource, visuals.Particles);

        var portalLogic = triggerGo.AddComponent<DimensionPortal>();
        portalLogic.Configure(kindValue, portal.transform);

        if (showDoorPreview && visuals.PortalView != null)
        {
            var preview = portal.AddComponent<PortalNextMapPreview>();
            preview.Initialize(kindValue, visuals.PortalView);
        }
    }

    private static PortalTransitionKind ResolveKind() => PortalTransitionKind.Dimension;

    private static Vector3 FindForwardHint(Vector3 portalPos)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return Vector3.forward;

        Vector3 toPlayer = player.transform.position - portalPos;
        toPlayer.y = 0f;
        return toPlayer.sqrMagnitude > 0.01f ? -toPlayer.normalized : Vector3.forward;
    }
}
